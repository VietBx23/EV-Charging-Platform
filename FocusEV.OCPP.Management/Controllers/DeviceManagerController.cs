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

                    // Lấy danh sách ChargePoints
                    // Lấy danh sách ChargePoints
                    List<ChargePoint> dbChargePoints;

                    // Lấy tên người dùng đã đăng nhập
                    string username = User.Identity.Name;

                    // Tìm kiếm người dùng trong cơ sở dữ liệu để lấy OwnerId
                    var user = dbContext.Accounts.FirstOrDefault(u => u.UserName == username);
                    if (User.Identity.Name == "admin")
                    {
                        dbChargePoints = dbContext.ChargePoints.ToList();
                    }
                    else if (user != null)
                    {
                        // Nếu tìm thấy người dùng, lấy OwnerId
                        int ownerId = (int)user.OwnerId;

                        // Tìm ChargePoints theo OwnerId
                        dbChargePoints = dbContext.ChargePoints.Where(cp => cp.OwnerId == ownerId).ToList();
                    }
                    else
                    {
                        // Nếu không tìm thấy người dùng, lấy tất cả ChargePoints
                        dbChargePoints = dbContext.ChargePoints.ToList();
                    }

                    Logger.LogInformation("ChargePoint: Found {0} charge points", dbChargePoints.Count);

                    ChargePoint currentChargePoint = null;
                    if (!string.IsNullOrEmpty(Id))
                    {
                        currentChargePoint = dbChargePoints.FirstOrDefault(cp => cp.ChargePointId.Equals(Id, StringComparison.InvariantCultureIgnoreCase));
                        if (currentChargePoint != null)
                        {
                            Logger.LogTrace("ChargePoint: Current charge point: {0} / {1}", currentChargePoint.ChargePointId, currentChargePoint.Name);
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
                                // Check for duplicates
                                if (dbChargePoints.Any(cp => cp.ChargePointId.Equals(cpvm.ChargePointId, StringComparison.InvariantCultureIgnoreCase)))
                                {
                                    errorMsg = _localizer["ChargePointIdExists"].Value;
                                    Logger.LogInformation("ChargePoint: New => charge point ID already exists: {0}", cpvm.ChargePointId);
                                }
                            }

                            if (string.IsNullOrEmpty(errorMsg))
                            {
                                // Save new charge point to DB
                                var newChargePoint = new ChargePoint
                                {
                                    ChargePointId = cpvm.ChargePointId,
                                    Name = cpvm.Name,
                                    Comment = cpvm.Comment,
                                    Username = cpvm.Username,
                                    Password = cpvm.Password,
                                    ClientCertThumb = cpvm.ClientCertThumb,
                                    OwnerId = cpvm.OwnerId,
                                    ChargeStationId = cpvm.ChargeStationId,
                                    ChargePointModel = cpvm.ChargePointModel,
                                    OCPPVersion = cpvm.OCPPVersion,
                                    ChargePointState = cpvm.ChargePointState,
                                    chargerPower = "60 kW",
                                    outputType = "DC",
                                    connectorType = "CCS2"
                                };

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
                        else if (currentChargePoint?.ChargePointId == Id)
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
                    // List all charge points
                    // Kiểm tra nếu user đã đăng nhập và tài khoản là 'goev'
                    else if (User.Identity.Name.Equals("admin", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Tài khoản admin: Hiển thị toàn bộ Owner và ChargeStation
                        ViewBag.OwnerList = dbContext.Owners.ToList();
                        ViewBag.ChargeStationList = dbContext.ChargeStations.ToList();
                    }
                    else
                    {
                        // Tài khoản người dùng khác: Hiển thị theo OwnerId của họ
                        var ownerId = dbContext.Accounts
                                                .Where(u => u.UserName == User.Identity.Name)
                                                .Select(u => u.OwnerId)
                                                .FirstOrDefault();

                        ViewBag.OwnerList = dbContext.Owners.Where(o => o.OwnerId == ownerId).ToList();
                        ViewBag.ChargeStationList = dbContext.ChargeStations.Where(cs => cs.OwnerId == ownerId).ToList();
                    }

                    // Khởi tạo ChargePointViewModel
                    cpvm = new ChargePointViewModel
                    {
                        ChargePoints = dbChargePoints,
                        CurrentId = Id
                    };

                    // Nếu currentChargePoint không null, gán các giá trị
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
            catch (Exception exp)
            {
                Logger.LogError(exp, "ChargePoint: Error loading charge points from database");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }
        }

        /*[Authorize]
        public IActionResult Terminal()
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                var model = dbContext.ConnectorStatuses.ToList();
                return View(model);
            }

        }*/


        [Authorize]
        public IActionResult Terminal()
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                IEnumerable<ConnectorStatus> model;

                if (User.Identity.Name == "goev")
                {
                    model = dbContext.ConnectorStatuses
                        .Where(cs => cs.ChargePointId.StartsWith("GOEV"))
                        .ToList(); // Lấy các ConnectorStatus có ChargePointId bắt đầu bằng "GOEV"
                }
                else if(User.Identity.Name == "adminbinhphuoc")
                {
                    model = dbContext.ConnectorStatuses
                        .Where(cs => cs.ChargePointId.StartsWith("FC-BPH"))
                        .ToList(); // Lấy các ConnectorStatus có ChargePointId bắt đầu bằng "GOEV"
                }
                else if (User.Identity.Name == "adminbinhthuan")
                {
                    model = dbContext.ConnectorStatuses
                        .Where(cs => cs.ChargePointId.StartsWith("FC-BTH"))
                        .ToList(); // Lấy các ConnectorStatus có ChargePointId bắt đầu bằng "GOEV"
                }
                else if (User.Identity.Name == "inewsolar")
                {
                    model = dbContext.ConnectorStatuses
                        .Where(cs => cs.ChargePointId.StartsWith("FC-KHO"))
                        .ToList(); // Lấy các ConnectorStatus có ChargePointId bắt đầu bằng "GOEV"
                }
                else if(User.Identity.Name == "adminletsgo")
                {
                    model = dbContext.ConnectorStatuses
     .Where(cs => cs.ChargePointId.StartsWith("LGO-PYE") || cs.ChargePointId.StartsWith("LGO-BDI"))
     .ToList();

                }
                else
                {
                    model = dbContext.ConnectorStatuses.ToList(); // Lấy tất cả ConnectorStatus
                }

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

        public IActionResult Operator(int OwnerId, OwnerViewModel owvm)
        {
            try
            {
                // Kiểm tra quyền truy cập
                if (User != null && !User.IsInRole(Constants.AdminRoleName) && User.Identity.Name != "admin")
                {
                    Logger.LogWarning("ChargeTag: Request by non-administrator: {0}", User?.Identity?.Name);
                    TempData["ErrMsgKey"] = "AccessDenied";
                    return RedirectToAction("Error", new { Id = "" });
                }

                ViewBag.DatePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                ViewBag.Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {
                    var currentUser = User.Identity.Name;

                    // Nếu là admin (tên đăng nhập là "admin"), hiển thị toàn bộ danh sách
                    if (currentUser == "admin")
                    {
                        owvm.Owners = dbContext.Owners.ToList(); // Hiển thị toàn bộ
                    }
                    else
                    {
                        // Lấy OwnerId từ bảng Account
                        OwnerId = (int)dbContext.Accounts
                            .Where(a => a.UserName == currentUser)
                            .Select(a => a.OwnerId)
                            .FirstOrDefault();

                        if (OwnerId == 0)
                        {
                            TempData["ErrMsgKey"] = "Không tìm thấy OwnerId gắn với tài khoản.";
                            return RedirectToAction("Error", new { Id = "" });
                        }

                        owvm.OwnerId = OwnerId;

                        // Hiển thị dữ liệu chỉ theo OwnerId
                        owvm.Owners = dbContext.Owners
                            .Where(o => o.OwnerId == OwnerId)
                            .ToList();
                    }

                    if (Request.Method == "POST")
                    {
                        // Thêm mới
                        if (OwnerId == 0)
                        {
                            Owner owner = new Owner
                            {
                                CreateDate = DateTime.Now,
                                Name = owvm.Name,
                                Address = owvm.Address,
                                Email = owvm.Email,
                                Phone = owvm.Phone,
                                Status = 1
                            };
                            dbContext.Owners.Add(owner);
                        }
                        // Cập nhật
                        else
                        {
                            Owner owner = dbContext.Owners.FirstOrDefault(m => m.OwnerId == OwnerId);
                            if (owner != null)
                            {
                                owner.Address = owvm.Address;
                                owner.Email = owvm.Email;
                                owner.Phone = owvm.Phone;
                                owner.Status = 1;
                                owner.Name = owvm.Name;
                            }
                        }
                        dbContext.SaveChanges();
                        return RedirectToAction("Operator");
                    }
                    else
                    {
                        // Hiển thị thông tin chỉnh sửa
                        if (OwnerId != 0)
                        {
                            var current = owvm.Owners.FirstOrDefault(m => m.OwnerId == OwnerId);
                            if (current != null)
                            {
                                owvm.Address = current.Address;
                                owvm.Phone = current.Phone;
                                owvm.Email = current.Email;
                                owvm.Name = current.Name;
                            }
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
                        // Insert
                        if (ChargeStationId == 0)
                        {
                            ChargeStation chargeStation = new ChargeStation
                            {
                                CreateDate = DateTime.Now,
                                Name = cst.Name,
                                Address = cst.Address,
                                OwnerId = cst.OwnerId,
                                Position = cst.Position,
                                Status = cst.Status,
                                Type = cst.Type,
                                Lat = cst.Lat,
                                Long = cst.Long,
                                Images = cst.Image,
                                QtyChargePoint = cst.QtyChargePoint
                            };
                            dbContext.ChargeStations.Add(chargeStation);
                        }
                        // Update
                        else
                        {
                            ChargeStation chargeStation = dbContext.ChargeStations.FirstOrDefault(m => m.ChargeStationId == ChargeStationId);
                            if (chargeStation != null)
                            {
                                chargeStation.Name = cst.Name;
                                chargeStation.Address = cst.Address;
                                chargeStation.OwnerId = cst.OwnerId;
                                chargeStation.Position = cst.Position;
                                chargeStation.Status = cst.Status;
                                chargeStation.Type = cst.Type;
                                chargeStation.Images = cst.Image;
                                chargeStation.Lat = cst.Lat;
                                chargeStation.Long = cst.Long;
                                chargeStation.QtyChargePoint = cst.QtyChargePoint;
                            }
                        }
                        dbContext.SaveChanges();
                        return RedirectToAction("ChargeStation", new { Id = "" });
                    }
                    else
                    {
                        var model = dbContext.ChargeStations.ToList(); // Lấy tất cả trạm sạc

                        var currentUser = User.Identity.Name;
                        int? ownerId = dbContext.Accounts
                            .Where(a => a.UserName == currentUser)
                            .Select(a => a.OwnerId)
                            .FirstOrDefault();

                        // Nếu tài khoản là admin, hiển thị tất cả
                        if (currentUser == "admin")
                        {
                            cst.ChargeStations = model; // Hiển thị tất cả trạm sạc
                            ViewBag.OwnerList = dbContext.Owners.ToList(); // Hiển thị tất cả chủ đầu tư
                        }
                        // Nếu là người dùng khác, hiển thị trạm sạc theo OwnerId
                        else if (ownerId.HasValue)
                        {
                            cst.ChargeStations = model.Where(cs => cs.OwnerId == ownerId.Value).ToList(); // Hiển thị trạm sạc theo OwnerId
                            ViewBag.OwnerList = dbContext.Owners.Where(o => o.OwnerId == ownerId.Value).ToList(); // Hiển thị chủ đầu tư theo OwnerId
                        }
                        else
                        {
                            // Nếu không tìm thấy OwnerId gắn với tài khoản, hiển thị tất cả
                            cst.ChargeStations = model; // Hiển thị tất cả trạm sạc
                            ViewBag.OwnerList = dbContext.Owners.ToList(); // Hiển thị tất cả chủ đầu tư
                        }

                        if (ChargeStationId != 0)
                        {
                            var current = model.FirstOrDefault(m => m.ChargeStationId == ChargeStationId);
                            if (current != null)
                            {
                                cst.Address = current.Address;
                                cst.Type = current.Type;
                                cst.Position = current.Position;
                                cst.Name = current.Name;
                                cst.OwnerId = current.OwnerId;
                                cst.Status = current.Status;
                                cst.Image = current.Images;
                                cst.Lat = current.Lat;
                                cst.Long = current.Long;
                                cst.QtyChargePoint = current.QtyChargePoint;


                            }
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
