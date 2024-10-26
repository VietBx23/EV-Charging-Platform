using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FocusEV.OCPP.Database;
using FocusEV.OCPP.Management.Models;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FocusEV.OCPP.Management.Controllers
{
    public class AccoutManagerController : BaseController
    {
        private readonly IStringLocalizer<AccoutManagerController> _localizer;
        public AccoutManagerController(
         UserManager userManager,
         IStringLocalizer<AccoutManagerController> localizer,
         ILoggerFactory loggerFactory,
         IConfiguration config) : base(userManager, loggerFactory, config)
        {
            _localizer = localizer;
            Logger = loggerFactory.CreateLogger<AccoutManagerController>();
        }




        private string Encrypt(string stringToEncrypt)
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
            catch (Exception ex)
            {
                throw ex;
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


        [Authorize]
        public IActionResult AccountInfo(int AccountId, AccountViewModel avm)
        {
            try
            {
                if (User != null && !User.IsInRole(Constants.AdminRoleName))
                {
                    Logger.LogWarning("ChargeTag: Request by non-administrator: {0}", User?.Identity?.Name);
                    TempData["ErrMsgKey"] = "AccessDenied";
                    return RedirectToAction("Error", new { Id = "" });
                }
                ViewBag.DatePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                ViewBag.Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                avm.AccountId = AccountId;

                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {
                    if (Request.Method == "POST")
                    {
                        // Insert new account
                        if (AccountId == 0)
                        {
                            Account owner = new Account();
                            owner.CreateDate = DateTime.Now;
                            owner.Name = avm.Name;
                            owner.UserName = avm.UserName;
                            owner.Password = Encrypt(avm.Password); // Mã hóa mật khẩu
                            owner.Code = avm.Code;
                            owner.DepartmentId = avm.DepartmentId;
                            owner.PermissionId = avm.PermissionId;
                            owner.Images = avm.Images;
                            dbContext.Accounts.Add(owner);
                        }
                        // Update existing account
                        else
                        {
                            Account owner = dbContext.Accounts.Where(m => m.AccountId == AccountId).FirstOrDefault();
                            owner.CreateDate = DateTime.Now;
                            owner.Name = avm.Name;
                            owner.UserName = avm.UserName;
                            if (!string.IsNullOrEmpty(avm.Password))
                            {
                                owner.Password = Encrypt(avm.Password); // Mã hóa mật khẩu nếu có sự thay đổi
                            }
                            owner.Code = avm.Code;
                            owner.DepartmentId = avm.DepartmentId;
                            owner.PermissionId = avm.PermissionId;
                            owner.Images = avm.Images;
                        }
                        dbContext.SaveChanges();
                        return RedirectToAction("AccountInfo", new { Id = "" });
                    }
                    else
                    {
                        var model = dbContext.Accounts.ToList();
                        ViewBag.ListDapartment = dbContext.Departments.ToList();
                        ViewBag.ListPermission = dbContext.Permissions.ToList();
                        avm.Accounts = model;

                        if (AccountId != 0)
                        {
                            var current = model.Where(m => m.AccountId == AccountId).FirstOrDefault();
                            avm.Name = current.Name;
                            avm.UserName = current.UserName;
                            avm.Password = Decrypt(current.Password); // Giải mã mật khẩu
                            avm.Code = current.Code;
                            avm.DepartmentId = current.DepartmentId;
                            avm.PermissionId = current.PermissionId;
                            avm.Images = current.Images;
                        }
                        return View(avm);
                    }
                }

            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "ChargeTag: Error loading charge tags from database");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }
        }




        public IActionResult DeleteAccount(int AccountId)
        {
            try
            {
                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {
                    Account owner = dbContext.Accounts.Where(m => m.AccountId == AccountId).FirstOrDefault();
                    dbContext.Accounts.Remove(owner);
                    dbContext.SaveChanges();
                    return RedirectToAction("AccountInfo", new { Id = "" });
                }

            }
            catch (Exception ex)
            {
                return RedirectToAction("Error", new { Id = "" });
            }

        }
        public string AjaxUpload(IList<Microsoft.AspNetCore.Http.IFormFile> files)
        {
            string fileName = "";
            foreach (var formFile in files)
            {
                fileName = Helper.Upload(formFile, "Account");
            }
            return fileName;
        }


        [Authorize]
        public IActionResult AccountGroup(int PermissionId, PermissionViewModel pmv)
        {
            try
            {
                if (User != null && !User.IsInRole(Constants.AdminRoleName))
                {
                    Logger.LogWarning("ChargeTag: Request by non-administrator: {0}", User?.Identity?.Name);
                    TempData["ErrMsgKey"] = "AccessDenied";
                    return RedirectToAction("Error", new { Id = "" });
                }
                ViewBag.DatePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                ViewBag.Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                pmv.PermissionId = PermissionId;

                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {
                    if (Request.Method == "POST")
                    {
                        //Insert
                        if (PermissionId == 0)
                        {
                            Permission Permission = new Permission();
                            Permission.CreateDate = DateTime.Now;
                            Permission.Name = pmv.Name;
                            Permission.Description = pmv.Description;
                            Permission.MenuID = pmv.MenuID;
                            dbContext.Permissions.Add(Permission);
                        }
                        //Update
                        else
                        {
                            Permission Permission = dbContext.Permissions.Where(m => m.PermissionId == PermissionId).FirstOrDefault();
                            Permission.Name = pmv.Name;
                            Permission.Description = pmv.Description;
                            Permission.MenuID = pmv.MenuID;
                        }
                        dbContext.SaveChanges();
                        return RedirectToAction("AccountGroup", new { Id = "" });
                    }
                    else
                    {
                        var model = dbContext.Permissions.ToList();
                        foreach (var item in model)
                        {
                            var getsplits = item.MenuID.Split(",");
                            foreach (var it in getsplits)
                            {
                                if (it != "")
                                {
                                    Menu mn = dbContext.Menus.ToList().Where(m => m.MenuId == int.Parse(it)).FirstOrDefault();

                                    if (mn != null)
                                    {
                                        if (getsplits.ToList().IndexOf(it) == 0)
                                        {
                                            item.MenuName = "";
                                        }
                                        item.MenuName += mn.Name + " | ";
                                    }
                                }
                            }

                        }
                        ViewBag.ListMenu = dbContext.Menus.ToList();
                        pmv.Permissions = model;

                        if (PermissionId != 0)
                        {
                            var current = dbContext.Permissions.Where(m => m.PermissionId == PermissionId).FirstOrDefault();
                            pmv.Name = current.Name;
                            pmv.Description = current.Description;
                            pmv.MenuID = current.MenuID;
                        }
                        return View(pmv);
                    }
                }

            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "ChargeTag: Error loading charge tags from database");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }
        }

        [Authorize]
        public IActionResult LoadMenu()
        {
            return PartialView();
        }
        [Authorize]
        public IActionResult Unitprice(int UnitpriceId, UnitpriceViewModel upvm)
        {
            try
            {
                if (User != null && !User.IsInRole(Constants.AdminRoleName))
                {
                    Logger.LogWarning("ChargeTag: Request by non-administrator: {0}", User?.Identity?.Name);
                    TempData["ErrMsgKey"] = "AccessDenied";
                    return RedirectToAction("Error", new { Id = "" });
                }
                ViewBag.DatePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                ViewBag.Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                upvm.UnitpriceId = UnitpriceId;

                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {
                    if (Request.Method == "POST")
                    {
                        dbContext.Unitprices.ToList().ForEach(m => m.IsActive = 0);
                        dbContext.SaveChanges();
                        //Insert
                        if (UnitpriceId == 0)
                        {
                            Unitprice Unitprice = new Unitprice();
                            Unitprice.CreateDate = DateTime.Now;
                            Unitprice.Price = upvm.Price;
                            Unitprice.IsActive = 1;
                            dbContext.Unitprices.Add(Unitprice);
                        }
                        //Update
                        else
                        {
                            Unitprice Unitprice = dbContext.Unitprices.Where(m => m.UnitpriceId == UnitpriceId).FirstOrDefault();
                            Unitprice.Price = upvm.Price;
                        }
                        dbContext.SaveChanges();
                        return RedirectToAction("Unitprice", new { Id = "" });
                    }
                    else
                    {
                        var lst1 = dbContext.Unitprices.Where(m => m.IsActive == 1).ToList();
                        var lst2 = dbContext.Unitprices.OrderByDescending(m => m.UnitpriceId).Where(m => m.IsActive == 0).ToList();
                        List<Unitprice> lst = new List<Unitprice>();
                        lst.AddRange(lst1);
                        lst.AddRange(lst2);
                        upvm.Unitprices = lst;
                        if (UnitpriceId != 0)
                        {
                            var current = lst.Where(m => m.UnitpriceId == UnitpriceId).FirstOrDefault();
                            upvm.Price = current.Price;
                        }
                        return View(upvm);
                    }
                }

            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "ChargeTag: Error loading charge tags from database");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }
        }

        public IActionResult DeleteUnitprice(int UnitpriceId)
        {
            try
            {
                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {
                    Unitprice Unitprice = dbContext.Unitprices.Where(m => m.UnitpriceId == UnitpriceId).FirstOrDefault();
                    dbContext.Unitprices.Remove(Unitprice);
                    dbContext.SaveChanges();
                    return RedirectToAction("Unitprice", new { Id = "" });
                }

            }
            catch (Exception ex)
            {
                return RedirectToAction("Error", new { Id = "" });
            }

        }
        public bool UpdateActive(int UnitpriceId, bool status)
        {
            try
            {
                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {

                    var getdata = dbContext.Unitprices.Find(UnitpriceId);
                    if (status == true)
                    {
                        dbContext.Unitprices.ToList().ForEach(m => m.IsActive = 0);
                        dbContext.SaveChanges();
                        getdata.IsActive = 1;
                    }
                    else
                        getdata.IsActive = 1;
                    dbContext.SaveChanges();
                    return true;
                }

            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
