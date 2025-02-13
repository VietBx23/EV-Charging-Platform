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
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FocusEV.OCPP.Management.Controllers
{
    //public class UserManagerController : Controller
    public partial class UserManagerController : BaseController
    {
        private readonly IStringLocalizer<UserManagerController> _localizer;

        public UserManagerController(
            UserManager userManager,
            IStringLocalizer<UserManagerController> localizer,
            ILoggerFactory loggerFactory,
            IConfiguration config) : base(userManager, loggerFactory, config)
        {
            Logger = loggerFactory.CreateLogger<AccountController>();
        }
        public IActionResult UserInfoApp()
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                var model = dbContext.UserApps.OrderByDescending(m => m.CreateDate).ToList();
                return View(model);
            }

        }
        public IActionResult UserInfo(int CustomerId, CustomerViewModel avm)
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
                avm.CustomerId = CustomerId;

                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {
                    if (Request.Method == "POST")
                    {
                        //Insert
                        if (CustomerId == 0)
                        {
                            Customer owner = new Customer();
                            owner.CreateDate = DateTime.Now;
                            owner.FirtName = avm.FirtName;
                            owner.LastName = avm.LastName;
                            owner.UserName = avm.UserName;
                            owner.Password = avm.Password;
                            owner.Email = avm.Email;
                            owner.Gender = avm.Gender;
                            owner.Phone = avm.Phone;
                            owner.Company = avm.Company;
                            owner.TagId = avm.TagId;
                            owner.Images = avm.Images;
                            owner.RoleCustomerID = avm.RoleCustomerID;
                            dbContext.Customers.Add(owner);
                        }
                        //Update
                        else
                        {
                            Customer owner = dbContext.Customers.Where(m => m.CustomerId == CustomerId).FirstOrDefault();
                            owner.CreateDate = DateTime.Now;
                            owner.FirtName = avm.FirtName;
                            owner.LastName = avm.LastName;
                            owner.UserName = avm.UserName;
                            owner.Password = avm.Password;
                            owner.Email = avm.Email;
                            owner.Gender = avm.Gender;
                            owner.Phone = avm.Phone;
                            owner.Company = avm.Company;
                            owner.TagId = avm.TagId;
                            owner.Images = avm.Images;
                            owner.RoleCustomerID = avm.RoleCustomerID;
                        }
                        dbContext.SaveChanges();
                        return RedirectToAction("UserInfo", new { Id = "" });
                    }
                    else
                    {
                        var model = dbContext.Customers.ToList();
                        ViewBag.ListDapartment = dbContext.Departments.ToList();
                        ViewBag.ListPermission = dbContext.Permissions.ToList();
                        avm.Customers = model;
                        ViewBag.ListRole = dbContext.RoleCustomers.ToList();

                        if (CustomerId != 0)
                        {
                            var current = model.Where(m => m.CustomerId == CustomerId).FirstOrDefault();
                            avm.FirtName = current.FirtName;
                            avm.LastName = current.LastName;
                            avm.UserName = current.UserName;
                            avm.Password = current.Password;
                            avm.Email = current.Email;
                            avm.Gender = current.Gender;
                            avm.Phone = current.Phone;
                            avm.Company = current.Company;
                            avm.TagId = current.TagId;
                            avm.RoleCustomerID = current.RoleCustomerID;
                            avm.Images = current.Images;
                            ViewBag.ListTag = dbContext.ChargeTags.ToList();
                        }
                        else
                        {
                            ViewBag.ListTag = dbContext.ChargeTags.Where(m => !dbContext.Customers.Any(n => n.TagId == m.TagId)).ToList();
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
        public IActionResult DeleteUser(int CustomerId)
        {
            try
            {
                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {
                    Customer owner = dbContext.Customers.Where(m => m.CustomerId == CustomerId).FirstOrDefault();
                    dbContext.Customers.Remove(owner);
                    dbContext.SaveChanges();
                    return RedirectToAction("UserInfo", new { Id = "" });
                }

            }
            catch (Exception ex)
            {
                return RedirectToAction("Error", new { Id = "" });
            }

        }

        public IActionResult UserVehicleInfo()
        {
            return View();
        }

        public IActionResult ChargeCardDetail()
        {
            return View();
        }
        public string AjaxUpload(IList<Microsoft.AspNetCore.Http.IFormFile> files)
        {
            string fileName = "";
            foreach (var formFile in files)
            {
                fileName = Helper.Upload(formFile, "Customer");
            }
            return fileName;
        }

        //public IActionResult ChargeCard()
        //{
        //    return View();
        //}


        public IActionResult ChargeCard(string Id, ChargeTagViewModel ctvm)
        {
            try
            {
                // Kiểm tra nếu người dùng không phải admin
                if (User != null && !User.IsInRole(Constants.AdminRoleName))
                {
                    Logger.LogWarning("ChargeTag: Request by non-administrator: {0}", User?.Identity?.Name);
                    TempData["ErrMsgKey"] = "AccessDenied";
                    return RedirectToAction("Error", new { Id = "" });
                }

                ViewBag.DatePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                ViewBag.Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                ctvm.CurrentTagId = Id;

                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {
                    Logger.LogTrace("ChargeTag: Loading charge tags...");

                    // Kiểm tra tài khoản goev, nếu đúng, chỉ lấy ChargeTags có ChargeStation.OwnerId = 5
                    List<ChargeTag> dbChargeTags;

                    // Lấy username hiện tại
                    var username = User.Identity.Name;

                    // Kiểm tra nếu username là "admin", hiển thị tất cả
                    if (username == "admin")
                    {
                        Logger.LogInformation("ChargeTag: Admin user detected, displaying all charge tags.");
                        dbChargeTags = dbContext.ChargeTags.ToList();
                    }
                    else
                    {
                        // Lấy OwnerId dựa trên username
                        int? ownerId = dbContext.Accounts
                                                .Where(user => user.UserName == username)
                                                .Select(user => user.OwnerId)
                                                .FirstOrDefault();

                        // Kiểm tra và lọc ChargeTags dựa trên OwnerId
                        if (ownerId.HasValue)
                        {
                            Logger.LogInformation($"ChargeTag: Filtering charge tags for user {username} with OwnerId = {ownerId.Value}");
                            dbChargeTags = dbContext.ChargeTags
                                                    .Where(tag => tag.ChargeStationVtl.OwnerId == ownerId.Value)
                                                    .ToList();
                        }
                        else
                        {
                            Logger.LogInformation($"ChargeTag: No specific OwnerId for user {username}, displaying all charge tags.");
                            dbChargeTags = dbContext.ChargeTags.ToList();
                        }
                    }


                    Logger.LogInformation("ChargeTag: Found {0} charge tags", dbChargeTags.Count);

                    ChargeTag currentChargeTag = null;
                    if (!string.IsNullOrEmpty(Id))
                    {
                        foreach (ChargeTag tag in dbChargeTags)
                        {
                            if (tag.TagId.Equals(Id, StringComparison.InvariantCultureIgnoreCase))
                            {
                                currentChargeTag = tag;
                                Logger.LogTrace("ChargeTag: Current charge tag: {0} / {1}", tag.TagId, tag.TagName);
                                break;
                            }
                        }
                    }

                    if (Request.Method == "POST")
                    {
                        string errorMsg = null;

                        if (Id == "@" || Id == null)
                        {
                            Logger.LogTrace("ChargeTag: Creating new charge tag...");

                            // Create new tag
                            if (string.IsNullOrWhiteSpace(ctvm.TagId))
                            {
                                errorMsg = _localizer["ChargeTagIdRequired"].Value;
                                Logger.LogInformation("ChargeTag: New => no charge tag ID entered");
                            }

                            if (string.IsNullOrEmpty(errorMsg))
                            {
                                // check if duplicate
                                foreach (ChargeTag tag in dbChargeTags)
                                {
                                    if (tag.TagId.Equals(ctvm.TagId, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        // tag-id already exists
                                        errorMsg = _localizer["ChargeTagIdExists"].Value;
                                        Logger.LogInformation("ChargeTag: New => charge tag ID already exists: {0}", ctvm.TagId);
                                        break;
                                    }
                                }
                            }

                            if (string.IsNullOrEmpty(errorMsg))
                            {
                                // Save new tag in DB
                                ChargeTag newTag = new ChargeTag();
                                newTag.TagId = ctvm.TagId;
                                newTag.TagName = ctvm.TagName;
                                newTag.ParentTagId = ctvm.ParentTagId;
                                newTag.ExpiryDate = ctvm.ExpiryDate;
                                newTag.Blocked = ctvm.Blocked;
                                newTag.SideId = "1";
                                newTag.ChargeStationId = ctvm.ChargeStationId;
                                newTag.TagDescription = ctvm.TagDescription;
                                newTag.TagType = ctvm.TagType;
                                newTag.TagState = ctvm.TagState;
                                newTag.CreateDate = DateTime.Now;
                                dbContext.ChargeTags.Add(newTag);
                                dbContext.SaveChanges();
                                Logger.LogInformation("ChargeTag: New => charge tag saved: {0} / {1}", ctvm.TagId, ctvm.TagName);

                                return RedirectToAction("ChargeCard", new { Id = "" });
                            }
                            else
                            {
                                ViewBag.ErrorMsg = errorMsg;
                                return View("ChargeCardDetail", ctvm);
                            }
                        }
                        else if (currentChargeTag.TagId == Id)
                        {
                            // Save existing tag
                            currentChargeTag.TagName = ctvm.TagName;
                            currentChargeTag.ParentTagId = ctvm.ParentTagId;
                            currentChargeTag.ExpiryDate = ctvm.ExpiryDate;
                            currentChargeTag.Blocked = ctvm.Blocked;
                            currentChargeTag.ChargeStationId = ctvm.ChargeStationId;
                            currentChargeTag.TagDescription = ctvm.TagDescription;
                            currentChargeTag.TagType = ctvm.TagType;
                            currentChargeTag.TagState = ctvm.TagState;
                            dbContext.SaveChanges();
                            Logger.LogInformation("ChargeTag: Edit => charge tag saved: {0} / {1}", ctvm.TagId, ctvm.TagName);
                        }

                        return RedirectToAction("ChargeCard", new { Id = "" });
                    }
                    else
                    {
                        // List all charge tags
                        ctvm = new ChargeTagViewModel();
                        ctvm.ChargeTags = dbChargeTags;
                        ctvm.CurrentTagId = Id;

                     

                        // Kiểm tra nếu username là "admin", hiển thị tất cả trạm sạc
                        if (username.Equals("admin", StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.LogInformation("ChargeStation: Admin user detected, displaying all charge stations.");
                            ViewBag.ChargeStationList = dbContext.ChargeStations.ToList();
                        }
                        else
                        {
                            // Lấy OwnerId dựa trên username
                            int? ownerId = dbContext.Accounts
                                                    .Where(user => user.UserName == username)
                                                    .Select(user => user.OwnerId)
                                                    .FirstOrDefault();

                            // Kiểm tra và lọc ChargeStations dựa trên OwnerId
                            if (ownerId.HasValue)
                            {
                                Logger.LogInformation($"ChargeStation: Filtering charge stations for user {username} with OwnerId = {ownerId.Value}");
                                ViewBag.ChargeStationList = dbContext.ChargeStations
                                                                     .Where(cs => cs.OwnerId == ownerId.Value)
                                                                     .ToList();
                            }
                            else
                            {
                                Logger.LogInformation($"ChargeStation: No specific OwnerId for user {username}, displaying all charge stations.");
                                ViewBag.ChargeStationList = dbContext.ChargeStations.ToList();
                            }
                        }


                        if (currentChargeTag != null)
                        {
                            ctvm.TagId = currentChargeTag.TagId;
                            ctvm.TagName = currentChargeTag.TagName;
                            ctvm.ParentTagId = currentChargeTag.ParentTagId;
                            ctvm.ExpiryDate = currentChargeTag.ExpiryDate;
                            ctvm.Blocked = (currentChargeTag.Blocked != null) && currentChargeTag.Blocked.Value;
                            ctvm.CreateDate = currentChargeTag.CreateDate;
                            ctvm.SideId = currentChargeTag.SideId;
                            ctvm.ChargeStationId = currentChargeTag.ChargeStationId;
                            ctvm.TagDescription = currentChargeTag.TagDescription;
                            ctvm.TagType = currentChargeTag.TagType;
                            ctvm.TagState = currentChargeTag.TagState;
                        }

                        return View(ctvm);
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



        public IActionResult ChangePassword()

        {
            OCPPCoreContext dbContext = new OCPPCoreContext(this.Config);
            var UserApps = dbContext.UserApps.Where(m => m.OwnerId == 1 || m.OwnerId !=1  ).ToList();
            return View(UserApps);
        }
        [HttpPost]
        public bool ResetPassord(string UserappId)
        {
            try
            {
                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {
                    var us = dbContext.UserApps.Find(UserappId);
                    if (us == null)
                        return false;
                    else
                    {
                        us.Password = Encrypt("lado@2024");
                        dbContext.SaveChanges();
                        return true;
                    }

                }

            }
            catch (Exception ex)
            {
                return false;
            }
        }


        [HttpPost]
        public IActionResult DeactivateUser(string userId)
        {
            try
            {
                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {
                    var user = dbContext.UserApps.FirstOrDefault(u => u.Id == userId);
                    if (user != null)
                    {
                        user.isActive = 0; // Deactivate user
                        dbContext.SaveChanges();
                        return Json(new { success = true, message = "User deactivated successfully." });
                    }
                    else
                    {
                        return Json(new { success = false, message = "User not found." });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error deactivating user");
                return Json(new { success = false, message = "An error occurred while deactivating the user." });
            }
        }


        [HttpPost]
        public IActionResult ReactivateUser(string userId)
        {
            try
            {
                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {
                    var user = dbContext.UserApps.FirstOrDefault(u => u.Id == userId);
                    if (user != null)
                    {
                        user.isActive = 1; // Reactivate user
                        dbContext.SaveChanges();
                        return Json(new { success = true, message = "User reactivated successfully." });
                    }
                    else
                    {
                        return Json(new { success = false, message = "User not found." });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error reactivating user");
                return Json(new { success = false, message = "An error occurred while reactivating the user." });
            }
        }




        public string Encrypt(string stringToEncrypt)
        {
            string sEncryptionKey = "!#$a54?3";
            byte[] key = { };
            byte[] IV = { 10, 20, 30, 40, 50, 60, 70, 80 };
            byte[] inputByteArray; //Convert.ToByte(stringToEncrypt.Length)
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
    }
}
