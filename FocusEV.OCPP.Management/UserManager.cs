
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using FocusEV.OCPP.Management.Models;
using FocusEV.OCPP.Database;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FocusEV.OCPP.Management
{
    public class UserManager
    {
        private IConfiguration Configuration;

        public UserManager(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public async Task SignIn(HttpContext httpContext, UserModel user, bool isPersistent = false)
        {
            try
            {
                using OCPPCoreContext dbContext = new OCPPCoreContext(this.Configuration);

                IEnumerable<IConfigurationSection> cfgUsers = Configuration.GetSection("Users").GetChildren();

                foreach (IConfigurationSection cfgUser in cfgUsers)
                {
                    var getUser = dbContext.Accounts.FirstOrDefault(m => m.UserName.ToLower() == user.Username.ToLower());

                    if (getUser != null)
                    {
                        // Decrypt the stored password
                        string decryptedPassword = Decrypt(getUser.Password);

                        // Check if the provided password matches the decrypted password
                        if (decryptedPassword == user.Password)
                        {
                            user.IsAdmin = cfgUser.GetValue<bool>(Constants.AdminRoleName);
                            ClaimsIdentity identity = new ClaimsIdentity(this.GetUserClaims(user), CookieAuthenticationDefaults.AuthenticationScheme);
                            ClaimsPrincipal principal = new ClaimsPrincipal(identity);

                            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                            break;
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                // Handle the exception (consider logging it)
            }
        }
        private string Decrypt(string stringToDecrypt)
        {
            string sEncryptionKey = "!#$a54?3";
            byte[] key = { };
            byte[] IV = { 10, 20, 30, 40, 50, 60, 70, 80 };

            byte[] inputByteArray = new byte[stringToDecrypt.Length];
            try
            {
                key = Encoding.UTF8.GetBytes(sEncryptionKey.Substring(0, 8));
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                inputByteArray = Convert.FromBase64String(stringToDecrypt);

                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(key, IV), CryptoStreamMode.Write);
                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();

                Encoding encoding = Encoding.UTF8;
                return encoding.GetString(ms.ToArray());
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public async Task SignOut(HttpContext httpContext)
        {
            await httpContext.SignOutAsync();
        }

        private IEnumerable<Claim> GetUserClaims(UserModel user)
        {
            List<Claim> claims = new List<Claim>();

            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Username));
            claims.Add(new Claim(ClaimTypes.Name , user.Username));
            claims.AddRange(this.GetUserRoleClaims(user));
            return claims;
        }

        private IEnumerable<Claim> GetUserRoleClaims(UserModel user)
        {
            List<Claim> claims = new List<Claim>();

            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Username));
            if (user.IsAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, Constants.AdminRoleName));
            }
            return claims;
        }
    }
}
