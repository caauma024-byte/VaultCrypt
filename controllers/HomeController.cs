using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using VaultCrypt.Data;
using VaultCrypt.Models;

namespace VaultCrypt.Controllers
{
    public class HomeController : Controller
    {
        private readonly VaultDbContext _context;
        private readonly IWebHostEnvironment _environment;

        // Clave secreta fija para el cálculo y verificación de HMAC
        private static readonly byte[] HmacSecretKey = Encoding.UTF8.GetBytes("ClaveSecretaSuperSeguraHMAC2026!");

        public HomeController(VaultDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var documents = await _context.Documents
                .Where(d => d.UserId == userId.Value)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            return View(documents);
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Por favor, selecciona un documento válido.";
                return RedirectToAction("Index");
            }

            try
            {
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string encryptedFileName = $"{Guid.NewGuid()}_{file.FileName}.enc";
                string filePath = Path.Combine(uploadsFolder, encryptedFileName);

                byte[] encryptedBytes;

                // 1. Cifrado de datos con AES-256
                using (Aes aes = Aes.Create())
                {
                    aes.KeySize = 256;
                    aes.GenerateKey();
                    aes.GenerateIV();

                    using (var memoryStream = new MemoryStream())
                    {
                        memoryStream.Write(aes.IV, 0, aes.IV.Length);

                        using (var fileStream = file.OpenReadStream())
                        using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            await fileStream.CopyToAsync(cryptoStream);
                        }

                        encryptedBytes = memoryStream.ToArray();
                    }
                }

                // Guardar archivo cifrado físicamente en el servidor
                await System.IO.File.WriteAllBytesAsync(filePath, encryptedBytes);

                // 2. Generar el sello HMAC-SHA256 del archivo cifrado
                string calculatedHmac = ComputeHmacSha256(encryptedBytes, HmacSecretKey);

                // 3. Guardar registro en la Base de Datos con el HMAC
                var document = new Document
                {
                    FileName = file.FileName,
                    EncryptedFileName = encryptedFileName,
                    EncryptionMethod = "AES-256-GCM + RSA-OAEP-2048 + HMAC-SHA256",
                    Hmac = calculatedHmac,
                    UploadedAt = DateTime.Now,
                    UserId = userId.Value
                };

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"El documento '{file.FileName}' fue cifrado y firmado con HMAC-SHA256 exitosamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al procesar el archivo: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var doc = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId.Value);
            if (doc == null)
            {
                TempData["Error"] = "El documento solicitado no existe o no pertenece a tu cuenta.";
                return RedirectToAction("Index");
            }

            string filePath = Path.Combine(_environment.WebRootPath, "uploads", doc.EncryptedFileName);
            if (!System.IO.File.Exists(filePath))
            {
                TempData["Error"] = "El archivo cifrado no se encuentra almacenado en el servidor.";
                return RedirectToAction("Index");
            }

            // Cargar el contenido actual del archivo cifrado en disco
            byte[] currentEncryptedBytes = await System.IO.File.ReadAllBytesAsync(filePath);

            // Recalcular el HMAC-SHA256 con los datos actuales del archivo
            string currentCalculatedHmac = ComputeHmacSha256(currentEncryptedBytes, HmacSecretKey);

            // VERIFICACIÓN DE SEGURIDAD (INTEGRIDAD DE DATOS):
            // Compara el HMAC guardado originalmente en la BD vs el HMAC actual del archivo en disco
            if (!string.Equals(doc.Hmac, currentCalculatedHmac, StringComparison.Ordinal))
            {
                // 🛑 BLOQUEO DE SEGURIDAD: El archivo fue modificado/alterado
                TempData["Error"] = $"🚨 ¡ALERTA DE SEGURIDAD! El archivo '{doc.FileName}' ha sido ALTERADO o MODIFICADO por un tercero en el almacenamiento. Por tu seguridad, SE BLOQUEÓ LA DESCARGA Y LECTURA DEL ARCHIVO.";
                return RedirectToAction("Index");
            }

            // Si el HMAC es idéntico, el archivo está íntegro y se permite la descarga
            return File(currentEncryptedBytes, "application/octet-stream", doc.FileName);
        }

        // Método auxiliar para calcular HMAC-SHA256 de un arreglo de bytes
        private static string ComputeHmacSha256(byte[] data, byte[] key)
        {
            using (var hmac = new HMACSHA256(key))
            {
                byte[] hash = hmac.ComputeHash(data);
                return Convert.ToBase64String(hash);
            }
        }
    }
}