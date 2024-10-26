using Microsoft.AspNetCore.Http;
using System;
using System.IO;

namespace FocusEV.OCPP.Management
{
    public static class Helper
    {
        public static string Upload(IFormFile f, string folder)
        {
            if (f != null)
            {
                string root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", folder);
                string ext = Path.GetExtension(f.FileName);
                string fileName = RandomString(32 - ext.Length) + ext;
                using (Stream stream = new FileStream(Path.Combine(root, fileName), FileMode.Create))
                {
                    f.CopyTo(stream);
                }
                return fileName;
            }
            return null;
        }
        public static string RandomString(int len)
        {
            string pattern = "qwertyuiopasdfghjklzxcvbnm1234567890";
            char[] arr = new char[len];
            Random rand = new Random();
            for (int i = 0; i < len; i++)
            {
                arr[i] = pattern[rand.Next(pattern.Length)];
            }
            return string.Join(string.Empty, arr);
        }
    }
}
