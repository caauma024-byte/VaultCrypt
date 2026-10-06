using System;

namespace VaultCrypt.Models
{
    public class Document
    {
        public int Id { get; set; }

        public string FileName { get; set; } = "";

        public string EncryptedFileName { get; set; } = "";

        public string EncryptionMethod { get; set; } = "";

        public string Hmac { get; set; } = ""; // Firma HMAC-SHA256 para verificar alteración

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public int UserId { get; set; }

        public User? User { get; set; }
    }
}