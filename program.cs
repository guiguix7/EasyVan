using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using EasyVan.Data;
using EasyVan.Services;
using Supabase.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var isProduction = builder.Environment.IsProduction();
var railwayVolumePath = builder.Configuration["RAILWAY_VOLUME_MOUNT_PATH"];
if (isProduction && !string.IsNullOrWhiteSpace(builder.Configuration["RAILWAY_PROJECT_ID"]))
{
    if (!string.Equals(railwayVolumePath, "/data", StringComparison.Ordinal))
        throw new InvalidOperationException("Anexe um Railway Volume ao serviço com Mount Path /data antes de iniciar o EasyVan.");
}

if (isProduction)
{
    Directory.CreateDirectory("/data/keys");
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo("/data/keys"));
}

builder.Services.AddControllersWithViews();

var configuredConnection = builder.Configuration.GetConnectionString("DefaultConnection");
var connectionString = isProduction
    ? (Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ?? "Data Source=/data/easyvan.db")
    : (configuredConnection ?? "Data Source=easyvan.db");

if (isProduction)
{
    var dataSource = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString).DataSource;
    if (!Path.IsPathRooted(dataSource) || !dataSource.StartsWith("/data/", StringComparison.Ordinal))
        throw new InvalidOperationException("Em produção, o SQLite deve ficar dentro do volume persistente /data.");
    Directory.CreateDirectory(Path.GetDirectoryName(dataSource)!);
}

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IPasswordService, PasswordService>();

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
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var senha = scope.ServiceProvider.GetRequiredService<IPasswordService>();
    db.Database.Migrate();
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
