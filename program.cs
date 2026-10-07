using Microsoft.EntityFrameworkCore;
using VaultCrypt.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Agregar MVC
builder.Services.AddControllersWithViews();

// 2. Configurar la base de datos SQL Server (Azure SQL) con reintento automático
builder.Services.AddDbContext<VaultDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()
    ));

// 3. Configurar Sesiones con Cookies esenciales
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Aplicar migraciones automáticamente al iniciar en la nube (reemplaza EnsureCreated)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VaultDbContext>();
    db.Database.Migrate();
}

// Configurar Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Activar sesión antes de la autorización
app.UseSession();
app.UseAuthorization();

// 4. RUTA POR DEFECTO: Redirige directamente al Login de Account
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();