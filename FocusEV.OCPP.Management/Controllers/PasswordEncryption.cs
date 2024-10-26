using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FocusEV.OCPP.Management.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PasswordEncryptionController : ControllerBase
    {
        private const string EncryptionKey = "!#$a54?3";
        private static readonly byte[] IV = { 10, 20, 30, 40, 50, 60, 70, 80 };

        [HttpGet("encrypt")]
        public IActionResult EncryptPassword()
        {
            string passwordToEncrypt = "SolarEV08@2024"; // Mật khẩu bạn muốn mã hóa
            string encryptedPassword = Encrypt(passwordToEncrypt);
            return Ok(new { EncryptedPassword = encryptedPassword });
        }

        [HttpGet("decrypt")]
        public IActionResult DecryptPassword([FromQuery] string encryptedPassword)
        {
            if (string.IsNullOrEmpty(encryptedPassword))
            {
                return BadRequest("Encrypted password is required.");
            }

            string decryptedPassword = Decrypt(encryptedPassword);
            return Ok(new { DecryptedPassword = decryptedPassword });
        }

        private string Encrypt(string stringToEncrypt)
        {
            byte[] key = Encoding.UTF8.GetBytes(EncryptionKey.Substring(0, 8));
            byte[] inputByteArray = Encoding.UTF8.GetBytes(stringToEncrypt);

            try
            {
                using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
                {
                    des.Mode = CipherMode.CBC;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(key, IV), CryptoStreamMode.Write))
                        {
                            cs.Write(inputByteArray, 0, inputByteArray.Length);
                            cs.FlushFinalBlock();
                            return Convert.ToBase64String(ms.ToArray());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Encryption failed: {ex.Message}";
            }
        }

        private string Decrypt(string stringToDecrypt)
        {
            byte[] key = Encoding.UTF8.GetBytes(EncryptionKey.Substring(0, 8));
            byte[] inputByteArray = Convert.FromBase64String(stringToDecrypt);

            try
            {
                using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
                {
                    des.Mode = CipherMode.CBC;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(key, IV), CryptoStreamMode.Write))
                        {
                            cs.Write(inputByteArray, 0, inputByteArray.Length);
                            cs.FlushFinalBlock();
                            return Encoding.UTF8.GetString(ms.ToArray());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Decryption failed: {ex.Message}";
            }
        }
    }
}