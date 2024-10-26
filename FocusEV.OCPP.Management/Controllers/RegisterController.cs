using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using FocusEV.OCPP.Database;
using FocusEV.OCPP.Management.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace FocusEV.OCPP.Management.Controllers
{
    public class RegisterController : Controller
    {
        private readonly EmailVerificationService _emailVerificationService;
        private readonly EmailService _emailService;
        private static Dictionary<string, string> _otpStore = new Dictionary<string, string>();
        private readonly IStringLocalizer<RegisterController> _localizer;
        private readonly IConfiguration _config;

        public RegisterController(EmailVerificationService emailVerificationService, EmailService emailService, IStringLocalizer<RegisterController> localizer, IConfiguration config)
        {
            _emailVerificationService = emailVerificationService;
            _emailService = emailService;
            _localizer = localizer;
            _config = config;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra mật khẩu xác nhận có khớp hay không
                if (model.Password != model.ConfirmPassword)
                {
                    return Json(new { success = false, message = "Mật khẩu xác nhận không trùng khớp" });
                }

                // Validate Email bằng biểu thức chính quy
                if (!IsValidEmail(model.Email))
                {
                    return Json(new { success = false, message = "Email không hợp lệ." });
                }

                // Kiểm tra email và username đã tồn tại hay chưa
                if (await IsEmailOrUserNameExists(model.Email, model.UserName))
                {
                    return Json(new { success = false, message = "Email hoặc Username đã tồn tại" });
                }

                var otpCode = GenerateOtpCode();
                _otpStore[model.Email] = otpCode;

                await _emailService.SendOtpEmail(model.Email, otpCode);

                TempData["Email"] = model.Email;
                TempData["Password"] = Encrypt(model.Password);
                TempData["UserName"] = model.UserName;
                TempData["Fullname"] = model.Fullname;

                return Json(new { success = true, message = "OTP đã được gửi" });
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = "Thông tin không hợp lệ", errors });
            }
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOtp(string otpCode)
        {
            try
            {
                var email = TempData["Email"]?.ToString();
                var password = TempData["Password"]?.ToString();
                var userName = TempData["UserName"]?.ToString();
                var fullName = TempData["FullName"]?.ToString();

                if (email != null && _otpStore.ContainsKey(email) && _otpStore[email] == otpCode)
                {
                    using (OCPPCoreContext dbContext = new OCPPCoreContext(_config))
                    {
                        var user = new UserApp
                        {
                            Id = GenerateRandomString(8),
                            UserName = userName,
                            Email = email,
                            Fullname = fullName,
                            Password = password,
                            CreateDate = DateTime.Now
                        };

                        dbContext.UserApps.Add(user);
                        await dbContext.SaveChangesAsync();
                    }

                    _otpStore.Remove(email);
                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "Mã OTP không hợp lệ" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Json(new { success = false, message = "Đã xảy ra lỗi. Vui lòng thử lại sau." });
            }
        }

        private async Task<bool> IsEmailOrUserNameExists(string email, string userName)
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(_config))
            {
                var existingEmailUser = await dbContext.UserApps.AnyAsync(u => u.Email == email);
                var existingUserNameUser = await dbContext.UserApps.AnyAsync(u => u.UserName == userName);
                return existingEmailUser || existingUserNameUser;
            }
        }

        private bool IsValidEmail(string email)
        {
            // Regular expression to validate email format
            var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return emailRegex.IsMatch(email);
        }

        private string GenerateOtpCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        public string Encrypt(string stringToEncrypt)
        {
            string sEncryptionKey = "!#$a54?3";
            byte[] key = { };
            byte[] IV = { 10, 20, 30, 40, 50, 60, 70, 80 };
            byte[] inputByteArray;

            try
            {
                key = Encoding.UTF8.GetBytes(sEncryptionKey.Substring(0, 8));
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                inputByteArray = Encoding.UTF8.GetBytes(stringToEncrypt);
                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(key, IV), CryptoStreamMode.Write);
                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();

                return Convert.ToBase64String(ms.ToArray());
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEF123456789";
            Random random = new Random();
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public IActionResult Success()
        {
            return View();
        }
    }

    public class RegisterViewModel
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Fullname { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
