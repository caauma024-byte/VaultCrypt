using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace VaultCrypt.Controllers
{
    public class DocumentsController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            // Validar sesión
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Si la vista está en Views/Documents/Index.cshtml, forzamos la ruta explícita:
            return View("~/Views/Documents/Index.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, string publicKeyPem)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
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
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    byte[] fileBytes = memoryStream.ToArray();

                    using Aes aes = Aes.Create();
                    aes.KeySize = 256;
                    aes.GenerateKey();
                    aes.GenerateIV();

                    using ICryptoTransform encryptor = aes.CreateEncryptor();
                    byte[] encryptedFile = encryptor.TransformFinalBlock(fileBytes, 0, fileBytes.Length);

                    if (!string.IsNullOrWhiteSpace(publicKeyPem))
                    {
                        using RSA rsa = RSA.Create();
                        rsa.ImportFromPem(publicKeyPem);
                        byte[] encryptedAesKey = rsa.Encrypt(aes.Key, RSAEncryptionPadding.OaepSHA256);
                    }
                }

                TempData["Success"] = $"El documento '{file.FileName}' fue cargado y cifrado exitosamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al procesar el documento: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}