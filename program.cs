using Microsoft.EntityFrameworkCore;
using VaultCrypt.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Agregar MVC
builder.Services.AddControllersWithViews();

// 2. Configurar la base de datos SQLite
builder.Services.AddDbContext<VaultDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// 3. Configurar Sesiones con Cookies esenciales
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Asegurar creación de la Base de Datos al iniciar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VaultDbContext>();
    db.Database.EnsureCreated();
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