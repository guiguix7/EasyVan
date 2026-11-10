using System.Threading.RateLimiting;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using EasyVan.Data;
using EasyVan.Services;
using Supabase.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);

var isProduction = builder.Environment.IsProduction();
var isRailway = !string.IsNullOrWhiteSpace(builder.Configuration["RAILWAY_PROJECT_ID"]);

if (isProduction)
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(ResolveDataProtectionDirectory(builder.Configuration, isRailway)));
}

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

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
builder.Services.AddScoped<IFotoPerfilService, FotoPerfilService>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddScoped<ValidacaoOperacional>();
builder.Services.AddScoped<PerfilUsuarioService>();
builder.Services.AddScoped<AgendaRotaService>();
builder.Services.AddScoped<VinculoAlunoService>();
builder.Services.AddScoped<PresencaViagemService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<PrazoPresencaOptions>(builder.Configuration.GetSection("PrazoPresenca"));
builder.Services.AddScoped(provider => new PrazoPresencaService(
    provider.GetRequiredService<IOptions<PrazoPresencaOptions>>().Value,
    provider.GetRequiredService<TimeProvider>()));
builder.Services.AddScoped<GeracaoViagemService>();
builder.Services.AddScoped<GeracaoViagensLinhaService>();
builder.Services.AddScoped<LinhaService>();
builder.Services.Configure<GeracaoViagensOptions>(builder.Configuration.GetSection("GeracaoViagens"));
builder.Services.AddHostedService<GeracaoViagensHostedService>();
builder.Services.AddScoped<CentroOperacoesService>();
builder.Services.AddSingleton(new GoogleMapsOptions
{
    ApiKey = builder.Configuration["GoogleMaps:ApiKey"]
        ?? Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY")
        ?? string.Empty,
    BrowserApiKey = builder.Configuration["GoogleMaps:BrowserApiKey"]
        ?? Environment.GetEnvironmentVariable("GOOGLE_MAPS_BROWSER_API_KEY")
        ?? string.Empty
});
builder.Services.AddScoped<IGeocodificador>(provider => new GoogleGeocodificador(
    provider.GetRequiredService<IHttpClientFactory>().CreateClient(),
    provider.GetRequiredService<GoogleMapsOptions>()));
builder.Services.AddScoped<IRoteirizador>(provider =>
{
    var options = provider.GetRequiredService<GoogleMapsOptions>();
    return options.Configurado
        ? new GoogleRoteirizador(provider.GetRequiredService<IHttpClientFactory>().CreateClient(), options)
        : new FakeRoteirizador();
});
builder.Services.AddScoped(provider => new RoteirizacaoService(
    provider.GetRequiredService<ApplicationDbContext>(),
    provider.GetRequiredService<IRoteirizador>(),
    provider.GetRequiredService<TimeProvider>(),
    provider.GetRequiredService<IOptions<PrazoPresencaOptions>>().Value,
    provider.GetRequiredService<ILogger<RoteirizacaoService>>()));
builder.Services.AddHostedService<RoteirizacaoHostedService>();

var supabaseUrl = builder.Configuration["Supabase:Url"];
var supabaseKey = builder.Configuration["Supabase:Key"];
if (!string.IsNullOrWhiteSpace(supabaseUrl) && !string.IsNullOrWhiteSpace(supabaseKey))
{
    builder.Services.AddSupabase(supabaseUrl, supabaseKey);
}

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

                if (usuario == null || !usuario.Ativo)
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
                AtualizarClaim(identity, "FotoPerfil", usuario.FotoPerfil ?? string.Empty, context);
                AtualizarClaim(identity, "EasyVan:OrganizacaoId", usuario.OrganizacaoId.ToString(), context);

                var tema = EasyVan.Services.TemaUi.Normalizar(usuario.Tema);
                if (context.HttpContext.Request.Cookies[EasyVan.Services.TemaUi.Cookie] != tema)
                {
                    EasyVan.Services.TemaUi.Gravar(
                        context.HttpContext.Response,
                        tema,
                        context.HttpContext.Request.IsHttps);
                }
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

static string ResolveDataProtectionDirectory(IConfiguration configuration, bool isRailway)
{
    var candidates = new List<string>();
    var volume = configuration["RAILWAY_VOLUME_MOUNT_PATH"];
    if (!string.IsNullOrWhiteSpace(volume))
        candidates.Add(Path.Combine(volume, "keys"));
    candidates.Add("/data/keys");
    candidates.Add(Path.Combine(AppContext.BaseDirectory, "keys"));
    candidates.Add(Path.Combine(Path.GetTempPath(), "easyvan-keys"));

    foreach (var candidate in candidates)
    {
        try
        {
            Directory.CreateDirectory(candidate);
            var probe = Path.Combine(candidate, ".write-test");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);

            if (isRailway && !candidate.StartsWith("/data", StringComparison.Ordinal))
            {
                Console.Error.WriteLine(
                    $"[EasyVan] Volume /data indisponível; as chaves de Data Protection serão gravadas em {candidate}. " +
                    "Anexe um Volume com mount path /data para manter as sessões entre deploys.");
            }

            return candidate;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            Console.Error.WriteLine($"[EasyVan] Não foi possível usar {candidate} para Data Protection: {ex.Message}");
        }
    }

    throw new InvalidOperationException("Nenhum diretório gravável foi encontrado para as chaves de Data Protection.");
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
