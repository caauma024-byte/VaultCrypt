using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using VaultCrypt.Data;
using VaultCrypt.Models;

namespace VaultCrypt.Controllers
{
    public class AccountController : Controller
    {
        private readonly VaultDbContext _context;

        public AccountController(VaultDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Por favor, ingresa tu correo y contraseña.";
                return View();
            }

            string cleanEmail = email.Trim().ToLower();
            string hashedPassword = HashPassword(password);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == cleanEmail);

            // Si el usuario no existe o la contraseña no coincide
            if (user == null || !string.Equals(user.PasswordHash, hashedPassword, StringComparison.Ordinal))
            {
                ViewBag.Error = "Correo electrónico o contraseña incorrectos.";
                return View(); // Retorna la vista limpia
            }

            // Inicio de sesión exitoso
            HttpContext.Session.SetInt32("UserId", user.Id);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string email, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Todos los campos son obligatorios.";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Las contraseñas no coinciden.";
                return View();
            }

            string cleanEmail = email.Trim().ToLower();

            bool exists = await _context.Users.AnyAsync(u => u.Email.ToLower() == cleanEmail);
            if (exists)
            {
                ViewBag.Error = "El correo electrónico ya está registrado.";
                return View();
            }

            var newUser = new User
            {
                Email = cleanEmail,
                PasswordHash = HashPassword(password)
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cuenta creada exitosamente. Por favor, inicia sesión.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}