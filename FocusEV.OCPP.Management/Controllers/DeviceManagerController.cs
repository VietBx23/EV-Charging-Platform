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
using Humanizer;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace FocusEV.OCPP.Management.Controllers
{
    //public class DeviceManagerController : Controller
    public partial class DeviceManagerController : BaseController
    {
        private readonly IStringLocalizer<DeviceManagerController> _localizer;
        private object _logger;

        public DeviceManagerController(
            UserManager userManager,
            IStringLocalizer<DeviceManagerController> localizer,
            ILoggerFactory loggerFactory,
            IConfiguration config) : base(userManager, loggerFactory, config)
        {
            _localizer = localizer;
            Logger = loggerFactory.CreateLogger<DeviceManagerController>();
        }

        [Authorize]
        public IActionResult ChargePoint(string Id, ChargePointViewModel cpvm)
        {
            try
            {
                if (User != null && !User.IsInRole(Constants.AdminRoleName))
                {
                    Logger.LogWarning("ChargePoint: Request by non-administrator: {0}", User?.Identity?.Name);
                    TempData["ErrMsgKey"] = "AccessDenied";
                    return RedirectToAction("Error", new { Id = "" });
                }

                cpvm.CurrentId = Id;

                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {
                    Logger.LogTrace("ChargePoint: Loading charge points...");
                    List<ChargePoint> dbChargePoints = dbContext.ChargePoints.ToList<ChargePoint>();
                    Logger.LogInformation("ChargePoint: Found {0} charge points", dbChargePoints.Count);

                    ChargePoint currentChargePoint = null;
                    if (!string.IsNullOrEmpty(Id))
                    {
                        foreach (ChargePoint cp in dbChargePoints)
                        {
                            if (cp.ChargePointId.Equals(Id, StringComparison.InvariantCultureIgnoreCase))
                            {
                                currentChargePoint = cp;
                                Logger.LogTrace("ChargePoint: Current charge point: {0} / {1}", cp.ChargePointId, cp.Name);
                                break;
                            }
                        }
                    }

                    if (Request.Method == "POST")
                    {
                        string errorMsg = null;

                        if (Id == null || Id == "")
                        {
                            Logger.LogTrace("ChargePoint: Creating new charge point...");

                            // Create new tag
                            if (string.IsNullOrWhiteSpace(cpvm.ChargePointId))
                            {
                                errorMsg = _localizer["ChargePointIdRequired"].Value;
                                Logger.LogInformation("ChargePoint: New => no charge point ID entered");
                            }

                            if (string.IsNullOrEmpty(errorMsg))
                            {
                                // check if duplicate
                                foreach (ChargePoint cp in dbChargePoints)
                                {
                                    if (cp.ChargePointId.Equals(cpvm.ChargePointId, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        // id already exists
                                        errorMsg = _localizer["ChargePointIdExists"].Value;
                                        Logger.LogInformation("ChargePoint: New => charge point ID already exists: {0}", cpvm.ChargePointId);
                                        break;
                                    }
                                }
                            }

                            if (string.IsNullOrEmpty(errorMsg))
                            {
                                // Save tag in DB
                                ChargePoint newChargePoint = new ChargePoint();
                                newChargePoint.ChargePointId = cpvm.ChargePointId;
                                newChargePoint.Name = cpvm.Name;
                                newChargePoint.Comment = cpvm.Comment;
                                newChargePoint.Username = cpvm.Username;
                                newChargePoint.Password = cpvm.Password;
                                newChargePoint.ClientCertThumb = cpvm.ClientCertThumb;
                                newChargePoint.OwnerId = cpvm.OwnerId;
                                newChargePoint.ChargeStationId = cpvm.ChargeStationId;
                                newChargePoint.ChargePointModel = cpvm.ChargePointModel;
                                newChargePoint.OCPPVersion = cpvm.OCPPVersion;
                                newChargePoint.ChargePointState = cpvm.ChargePointState;
                                newChargePoint.chargerPower = "60 kW";
                                newChargePoint.outputType = "DC";
                                newChargePoint.connectorType = "CCS2";
                                dbContext.ChargePoints.Add(newChargePoint);
                                dbContext.SaveChanges();
                                Logger.LogInformation("ChargePoint: New => charge point saved: {0} / {1}", cpvm.ChargePointId, cpvm.Name);

                                return RedirectToAction("ChargePoint", new { Id = "" });
                            }
                            else
                            {
                                ViewBag.ErrorMsg = errorMsg;
                                return View("ChargePoint", cpvm);
                            }
                        }
                        else if (currentChargePoint.ChargePointId == Id)
                        {
                            // Save existing charge point
                            Logger.LogTrace("ChargePoint: Saving charge point '{0}'", Id);
                            currentChargePoint.Name = cpvm.Name;
                            currentChargePoint.Comment = cpvm.Comment;
                            currentChargePoint.Username = cpvm.Username;
                            currentChargePoint.Password = cpvm.Password;
                            currentChargePoint.ClientCertThumb = cpvm.ClientCertThumb;
                            currentChargePoint.OwnerId = cpvm.OwnerId;
                            currentChargePoint.ChargeStationId = cpvm.ChargeStationId;
                            currentChargePoint.ChargePointModel = cpvm.ChargePointModel;
                            currentChargePoint.OCPPVersion = cpvm.OCPPVersion;
                            currentChargePoint.ChargePointState = cpvm.ChargePointState;

                            dbContext.SaveChanges();
                            Logger.LogInformation("ChargePoint: Edit => charge point saved: {0} / {1}", cpvm.ChargePointId, cpvm.Name);
                        }

                        return RedirectToAction("ChargePoint", new { Id = "" });
                    }
                    else  //list danh sách các trục sạc toàn bộ
                    {
                        // Display charge point
                        cpvm = new ChargePointViewModel();
                        cpvm.ChargePoints = dbChargePoints;
                        cpvm.CurrentId = Id;

                        ViewBag.OwnerList = dbContext.Owners.ToList();
                        ViewBag.ChargeStationList = dbContext.ChargeStations.ToList();

                        if (currentChargePoint != null)
                        {
                            cpvm.ChargePointId = currentChargePoint.ChargePointId;
                            cpvm.Name = currentChargePoint.Name;
                            cpvm.Comment = currentChargePoint.Comment;
                            cpvm.Username = currentChargePoint.Username;
                            cpvm.Password = currentChargePoint.Password;
                            cpvm.ClientCertThumb = currentChargePoint.ClientCertThumb;
                            cpvm.OwnerId = currentChargePoint.OwnerId;
                            cpvm.ChargeStationId = currentChargePoint.ChargeStationId;
                            cpvm.ChargePointModel = currentChargePoint.ChargePointModel;
                            cpvm.OCPPVersion = currentChargePoint.OCPPVersion;
                            cpvm.ChargePointState = currentChargePoint.ChargePointState;
                            cpvm.ChargePointSerial = currentChargePoint.ChargePointSerial;
                        }
                        return View(cpvm);
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "ChargePoint: Error loading charge points from database");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }
        }
        [Authorize]
        public IActionResult Terminal()
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                var model = dbContext.ConnectorStatuses.ToList();
                return View(model);
            }

        }

        [HttpPost]
        public bool ChangeData(string id, string data, int ConnectorId)
        {
            try
            {
                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {
                    var model = dbContext.ConnectorStatuses.Where(m => m.ChargePointId == id && m.ConnectorId == ConnectorId).FirstOrDefault();
                    if (model != null)
                    {
                        model.terminalId = data;
                        dbContext.SaveChanges();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }


        }
        /// <summary>
        /// Thông tin chủ đầu tư
        /// </summary>
        /// <param name="Nghiệp"></param>
        /// <param name="owvm"></param>
        /// <returns></returns>
        public IActionResult Operator(int OwnerId, OwnerViewModel owvm)
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
                owvm.OwnerId = OwnerId;

                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {
                    if (Request.Method == "POST")
                    {
                        //Insert
                        if (OwnerId == 0)
                        {
                            Owner owner = new Owner();
                            owner.CreateDate = DateTime.Now;
                            owner.Name = owvm.Name;
                            owner.Address = owvm.Address;
                            owner.Email = owvm.Email;
                            owner.Phone = owvm.Phone;
                            owner.Status = 1;
                            dbContext.Owners.Add(owner);
                        }
                        //Update
                        else
                        {
                            Owner owner = dbContext.Owners.Where(m => m.OwnerId == OwnerId).FirstOrDefault();
                            owner.Address = owvm.Address;
                            owner.Email = owvm.Email;
                            owner.Phone = owvm.Phone;
                            owner.Status = 1;
                            owner.Name = owvm.Name;
                        }
                        dbContext.SaveChanges();
                        return RedirectToAction("Operator", new { Id = "" });
                    }
                    else
                    {
                        var model = dbContext.Owners.ToList();
                        owvm.Owners = model;
                        if (OwnerId != 0)
                        {
                            var current = model.Where(m => m.OwnerId == OwnerId).FirstOrDefault();
                            owvm.Address = current.Address;
                            owvm.Phone = current.Phone;
                            owvm.Email = current.Email;
                            owvm.Name = current.Name;
                        }
                        return View(owvm);
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
        public IActionResult DeleteOperator(int OwnerId)
        {
            try
            {
                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {
                    Owner owner = dbContext.Owners.Where(m => m.OwnerId == OwnerId).FirstOrDefault();
                    dbContext.Owners.Remove(owner);
                    dbContext.SaveChanges();
                    return RedirectToAction("Operator", new { Id = "" });
                }

            }
            catch (Exception ex)
            {
                return RedirectToAction("Error", new { Id = "" });
            }

        }

        [HttpPost]
        // Upload images charge station
        public IActionResult UploadChargeStationImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file selected.");
            }

            var fileName = Path.GetFileName(file.FileName);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/banner", fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            // Tạo URL đầy đủ cho hình ảnh
            var fileUrl = $"http://103.77.167.17:8482/images/banner/{fileName}";

            return Ok(new { fileName = fileName, fileUrl = fileUrl });
        }




        public IActionResult ChargeStation(int ChargeStationId, ChargeStationViewModel cst)
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
                cst.ChargeStationId = ChargeStationId;

                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {
                    if (Request.Method == "POST")
                    {
                        //Insert
                        if (ChargeStationId == 0)
                        {
                            ChargeStation ChargeStation = new ChargeStation();
                            ChargeStation.CreateDate = DateTime.Now;
                            ChargeStation.Name = cst.Name;
                            ChargeStation.Address = cst.Address;
                            ChargeStation.OwnerId = cst.OwnerId;
                            ChargeStation.Position = cst.Position;
                            ChargeStation.Status = cst.Status;
                            ChargeStation.Type = cst.Type;
                            ChargeStation.Lat = cst.Lat;
                            ChargeStation.Long = cst.Long;
                            ChargeStation.Images = cst.Image;
                            ChargeStation.QtyChargePoint = cst.QtyChargePoint;
                            dbContext.ChargeStations.Add(ChargeStation);
                        }
                        //Update
                        else
                        {
                            ChargeStation ChargeStation = dbContext.ChargeStations.Where(m => m.ChargeStationId == ChargeStationId).FirstOrDefault();
                            ChargeStation.Name = cst.Name;
                            ChargeStation.Address = cst.Address;
                            ChargeStation.OwnerId = cst.OwnerId;
                            ChargeStation.Position = cst.Position;
                            ChargeStation.Status = cst.Status;
                            ChargeStation.Type = cst.Type;
                            ChargeStation.Images = cst.Image;
                            ChargeStation.Lat = cst.Lat;
                            ChargeStation.Long = cst.Long;
                            ChargeStation.QtyChargePoint = cst.QtyChargePoint;
                        }
                        dbContext.SaveChanges();
                        return RedirectToAction("ChargeStation", new { Id = "" });
                    }
                    else
                    {
                        var model = dbContext.ChargeStations.ToList();
                        ViewBag.OwnerList = dbContext.Owners.ToList();
                        cst.ChargeStations = model;
                        if (ChargeStationId != 0)
                        {
                            var current = model.Where(m => m.ChargeStationId == ChargeStationId).FirstOrDefault();
                            cst.Address = current.Address;
                            cst.Type = current.Type;
                            cst.Position = current.Position;
                            cst.Name = current.Name;
                            cst.OwnerId = current.OwnerId;
                            cst.Status = current.Status;
                            cst.Type = current.Type;
                            cst.Image = current.Images;
                            cst.Lat = current.Lat;
                            cst.Long = current.Long;
                            cst.QtyChargePoint = current.QtyChargePoint;

                        }
                        return View(cst);
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
    }

}
