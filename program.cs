using System.Threading.RateLimiting;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using EasyVan.Data;
using EasyVan.Services;

var builder = WebApplication.CreateBuilder(args);

var isProduction = builder.Environment.IsProduction();
var railwayVolumePath = builder.Configuration["RAILWAY_VOLUME_MOUNT_PATH"];
if (isProduction && !string.IsNullOrWhiteSpace(builder.Configuration["RAILWAY_PROJECT_ID"]))
{
    if (!string.Equals(railwayVolumePath, "/data", StringComparison.Ordinal))
        throw new InvalidOperationException("Anexe um Railway Volume ao serviço com Mount Path /data para persistir as chaves de proteção dos cookies.");
}

if (isProduction)
{
    Directory.CreateDirectory("/data/keys");
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo("/data/keys"));
}

builder.Services.AddControllersWithViews();

var configuredConnection = builder.Configuration.GetConnectionString("DefaultConnection");
var rawConnectionString = configuredConnection;
var isPostgres = !string.IsNullOrWhiteSpace(rawConnectionString)
    && (rawConnectionString.StartsWith("Host=", StringComparison.OrdinalIgnoreCase)
        || rawConnectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
        || rawConnectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase));

if (isProduction && !isPostgres)
{
    throw new InvalidOperationException("Configure ConnectionStrings__DefaultConnection na Railway com a connection string PostgreSQL do Supabase. O SQLite não é permitido em produção.");
}

if (isPostgres && rawConnectionString is not null)
{
    rawConnectionString = NormalizePostgresConnectionString(rawConnectionString);
}

var connectionString = rawConnectionString ?? "Data Source=easyvan.db";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (isPostgres)
        options.UseNpgsql(connectionString);
    else
        options.UseSqlite(connectionString);
});

builder.Services.AddScoped<IPasswordService, PasswordService>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Usuarios/Login";
        options.LogoutPath = "/Usuarios/Logout";
        options.AccessDeniedPath = "/Home/AcessoRestrito";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdValue, out var userId))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                var usuario = await db.Usuarios.AsNoTracking()
                    .SingleOrDefaultAsync(u => u.Id == userId);

                if (usuario == null)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                var identity = context.Principal?.Identity as ClaimsIdentity;
                if (identity == null)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                AtualizarClaim(identity, ClaimTypes.Name, usuario.Nome, context);
                AtualizarClaim(identity, ClaimTypes.Email, usuario.Email, context);
                AtualizarClaim(identity, ClaimTypes.Role, EasyVan.Models.Usuarios.NormalizeRole(usuario.RoleManager), context);
            }
        };
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var senha = scope.ServiceProvider.GetRequiredService<IPasswordService>();
    PostgresDatabaseBootstrapper.Initialize(db);
    DbInitializer.Initialize(
        db,
        senha,
        seedDemoUsers: app.Environment.IsDevelopment(),
        adminEmail: builder.Configuration["EASYVAN_ADMIN_EMAIL"],
        adminPassword: builder.Configuration["EASYVAN_ADMIN_PASSWORD"]);
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static void AtualizarClaim(ClaimsIdentity identity, string tipo, string valor, CookieValidatePrincipalContext context)
{
    var claimAtual = identity.FindFirst(tipo);
    if (claimAtual?.Value == valor) return;
    if (claimAtual != null) identity.RemoveClaim(claimAtual);
    identity.AddClaim(new Claim(tipo, valor));
    context.ShouldRenew = true;
}

static string NormalizePostgresConnectionString(string value)
{
    if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
        || (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
        return value;

    var credentials = uri.UserInfo.Split(':', 2);
    if (credentials.Length != 2)
        throw new InvalidOperationException("A URL PostgreSQL precisa conter usuário e senha.");

    var builder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.IsDefaultPort ? 5432 : uri.Port,
        Database = uri.AbsolutePath.Trim('/'),
        Username = Uri.UnescapeDataString(credentials[0]),
        Password = Uri.UnescapeDataString(credentials[1]),
        SslMode = SslMode.Require
    };

    return builder.ConnectionString;
}
