using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using EasyVan.Data;
using EasyVan.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure EF Core with SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=easyvan.db"));

// Serviço de hash de senhas (PBKDF2) - evita senhas em texto simples.
builder.Services.AddScoped<IPasswordService, PasswordService>();

// Autenticação por cookie (sessão) com níveis de acesso.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Usuarios/Login";
        options.LogoutPath = "/Usuarios/Logout";
        options.AccessDeniedPath = "/Home/AcessoRestrito";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Railway serve a aplicação por HTTPS
        options.Cookie.SameSite = SameSiteMode.Lax; // permite o cookie em requisições same-site no navegador
    });

var app = builder.Build();

// Garante o banco e aplica migrações, e popula dados iniciais (vans de referência) quando a tabela estiver vazia.
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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
