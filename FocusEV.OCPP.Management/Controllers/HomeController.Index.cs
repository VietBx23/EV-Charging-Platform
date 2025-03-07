﻿using System;
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
using System.Net.WebSockets;
using FocusEV.OCPP.Management.Services;

namespace FocusEV.OCPP.Management.Controllers
{
    public partial class HomeController : BaseController
    {
        private readonly IStringLocalizer<HomeController> _localizer;
        private readonly EmailService _emailService;
        public HomeController(
     UserManager userManager,
     IStringLocalizer<HomeController> localizer,
     ILoggerFactory loggerFactory,
     EmailService emailService,  // EmailService is injected here
     IConfiguration config)
     : base(userManager, loggerFactory, config)
        {
            _localizer = localizer;
            _emailService = emailService;  // Save the injected service
            Logger = loggerFactory.CreateLogger<HomeController>();
        }

        //[Route("ReturnUrl")]
        //public ActionResult ReturnUrl(string vnp_Amount,string vnp_BankCode,string vnp_BankTranNo,string vnp_CardType,string vnp_OrderInfo,string vnp_PayDate,string vnp_ResponseCode,string vnp_TmnCode,string vnp_TransactionNo,string vnp_TransactionStatus,string vnp_TxnRef,string vnp_SecureHash)
        //{
        //    return View();
        //}

        [Route("ReturnUrl")]
        public ActionResult ReturnUrl(string vnp_Amount, string vnp_BankCode, string vnp_BankTranNo, string vnp_CardType, string vnp_OrderInfo, string vnp_PayDate, string vnp_ResponseCode, string vnp_TmnCode, string vnp_TransactionNo, string vnp_TransactionStatus, string vnp_TxnRef, string vnp_SecureHash)
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                string urlRedirect = "";
                if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
                {
                    //return Redirect("http://success.sdk.merchantbackapp/");
                    urlRedirect = "http://success.sdk.merchantbackapp/";
                }
                else if (vnp_ResponseCode == "24")
                {
                    //return Redirect("http://cancel.sdk.merchantbackapp/");
                    urlRedirect = "http://cancel.sdk.merchantbackapp/";
                }
                else
                {
                    //return Redirect("http://fail.sdk.merchantbackapp/");
                    urlRedirect = "http://fail.sdk.merchantbackapp/";
                }

                //Ghi log IPN vào database bảng [VnpIPNLog]
                VnpIPNLog myIPN = new VnpIPNLog();
                myIPN.vnp_Amount = vnp_Amount.ToString();
                myIPN.vnp_BankCode = vnp_BankCode;
                myIPN.vnp_BankTranNo = vnp_BankTranNo;
                myIPN.vnp_OrderInfo = vnp_OrderInfo;
                myIPN.vnp_PayDate = vnp_PayDate;
                myIPN.vnp_ResponseCode = vnp_ResponseCode.ToString();
                myIPN.vnp_TmnCode = vnp_TmnCode;
                myIPN.vnp_BankTranNo = vnp_BankTranNo;
                myIPN.vnp_TransactionNo = vnp_TransactionNo;
                myIPN.vnp_TransactionStatus = vnp_TransactionStatus.ToString();
                myIPN.vnp_TxnRef = vnp_TxnRef;
                myIPN.vnp_SecureHash = vnp_SecureHash;
                myIPN.vnp_SecureHashType = vnp_CardType;
                myIPN.message = "ReturnURL";
                myIPN.DateCreate = DateTime.Now;
                dbContext.VnpIPNLogs.Add(myIPN);
                dbContext.SaveChanges();

                return Redirect(urlRedirect);
            }
        }

        [Authorize]
        public async Task<IActionResult> Index()

        {
            long n = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
            Logger.LogTrace("Index: Loading charge points with latest transactions...");

            OverviewViewModel overviewModel = new OverviewViewModel();
            overviewModel.ChargePoints = new List<ChargePointsOverviewViewModel>();
            try
            {
                Dictionary<string, ChargePointStatus> dictOnlineStatus = new Dictionary<string, ChargePointStatus>();
                #region Load online status from OCPP server
                string serverApiUrl = base.Config.GetValue<string>("ServerApiUrl");
                string apiKeyConfig = base.Config.GetValue<string>("ApiKey");
                if (!string.IsNullOrEmpty(serverApiUrl))
                {
                    bool serverError = false;
                    try
                    {
                        ChargePointStatus[] onlineStatusList = null;

                        using (var httpClient = new HttpClient())
                        {
                            if (!serverApiUrl.EndsWith('/'))
                            {
                                serverApiUrl += "/";
                            }
                            Uri uri = new Uri(serverApiUrl);
                            uri = new Uri(uri, "Status");
                            httpClient.Timeout = new TimeSpan(0, 0, 4); // use short timeout

                            // API-Key authentication?
                            if (!string.IsNullOrWhiteSpace(apiKeyConfig))
                            {
                                httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKeyConfig);
                            }
                            else
                            {
                                Logger.LogWarning("Index: No API-Key configured!");
                            }

                            HttpResponseMessage response = await httpClient.GetAsync(uri);
                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                string jsonData = await response.Content.ReadAsStringAsync();
                                if (!string.IsNullOrEmpty(jsonData))
                                {
                                    onlineStatusList = JsonConvert.DeserializeObject<ChargePointStatus[]>(jsonData);
                                    overviewModel.ServerConnection = true;

                                    if (onlineStatusList != null)
                                    {
                                        foreach (ChargePointStatus cps in onlineStatusList)
                                        {
                                            if (!dictOnlineStatus.TryAdd(cps.Id, cps))
                                            {
                                                Logger.LogError("Index: Online charge point status (ID={0}) could not be added to dictionary", cps.Id);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Logger.LogError("Index: Result of status web request is empty");
                                    serverError = true;
                                }
                            }
                            else
                            {
                                Logger.LogError("Index: Result of status web request => httpStatus={0}", response.StatusCode);
                                serverError = true;
                            }
                        }

                        Logger.LogInformation("Index: Result of status web request => Length={0}", onlineStatusList?.Length);
                    }
                    catch (Exception exp)
                    {
                        Logger.LogError(exp, "Index: Error in status web request => {0}", exp.Message);
                        serverError = true;
                    }

                    if (serverError)
                    {
                        ViewBag.ErrorMsg = _localizer["ErrorOCPPServer"];
                    }
                }
                #endregion

                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {
                    // List of charge point status (OCPP messages) with latest transaction (if one exist)
                    List<ConnectorStatusView> connectorStatusViewList = dbContext.ConnectorStatusViews.ToList<ConnectorStatusView>();

                    // Count connectors for every charge point (=> naming scheme)
                    Dictionary<string, int> dictConnectorCount = new Dictionary<string, int>();
                    foreach (ConnectorStatusView csv in connectorStatusViewList)
                    {
                        if (dictConnectorCount.ContainsKey(csv.ChargePointId))
                        {
                            // > 1 connector
                            dictConnectorCount[csv.ChargePointId] = dictConnectorCount[csv.ChargePointId] + 1;
                        }
                        else
                        {
                            // first connector
                            dictConnectorCount.Add(csv.ChargePointId, 1);
                        }
                    }


                    // List of configured charge points
                    List<ChargePoint> dbChargePoints = dbContext.ChargePoints.ToList<ChargePoint>().ToList();
                    if (dbChargePoints != null)
                    {
                        // Iterate through all charge points in database
                        foreach (ChargePoint cp in dbChargePoints)
                        {
                            ChargePointStatus cpOnlineStatus = null;
                            dictOnlineStatus.TryGetValue(cp.ChargePointId, out cpOnlineStatus);

                            // Preference: Check for connectors status in database
                            bool foundConnectorStatus = false;
                            if (connectorStatusViewList != null)
                            {
                                foreach (ConnectorStatusView connStatus in connectorStatusViewList)
                                {
                                    if (string.Equals(cp.ChargePointId, connStatus.ChargePointId, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        foundConnectorStatus = true;

                                        ChargePointsOverviewViewModel cpovm = new ChargePointsOverviewViewModel();
                                        //Bổ sung: get ownerID của chargepoint - 20/2/2024
                                        cpovm.OwnerID = cp.OwnerId.ToString();
                                        //----------------------------------
                                        cpovm.ChargePointId = cp.ChargePointId;
                                        cpovm.ConnectorId = connStatus.ConnectorId;
                                        if (string.IsNullOrWhiteSpace(connStatus.ConnectorName))
                                        {
                                            // No connector name specified => use default
                                            if (dictConnectorCount.ContainsKey(cp.ChargePointId) &&
                                                dictConnectorCount[cp.ChargePointId] > 1)
                                            {
                                                // more than 1 connector => "<charge point name>:<connector no.>"
                                                cpovm.Name = $"{cp.Name}:{connStatus.ConnectorId}";
                                            }
                                            else
                                            {
                                                // only 1 connector => "<charge point name>"
                                                cpovm.Name = cp.Name;
                                            }
                                        }
                                        else
                                        {
                                            // Connector has name override name specified
                                            cpovm.Name = connStatus.ConnectorName;
                                        }
                                        // cpovm.Online = cpOnlineStatus != null;
                                        var getlast = dbContext.ConnectorStatuses.ToList().Where(m => m.ChargePointId == cpovm.ChargePointId).LastOrDefault();
                                        if (getlast != null && getlast.lastSeen.HasValue)
                                        {
                                            cpovm.Online = (DateTime.Now - getlast.lastSeen.Value).TotalMinutes <= 8 ? true : false;
                                        }
                                        else
                                        {
                                            cpovm.Online = cpOnlineStatus != null;
                                        }
                                        cpovm.ConnectorStatus = ConnectorStatusEnum.Undefined;
                                        OnlineConnectorStatus onlineConnectorStatus = null;
                                        if (cpOnlineStatus != null &&
                                            cpOnlineStatus.OnlineConnectors != null &&
                                            cpOnlineStatus.OnlineConnectors.ContainsKey(connStatus.ConnectorId))
                                        {
                                            onlineConnectorStatus = cpOnlineStatus.OnlineConnectors[connStatus.ConnectorId];
                                            cpovm.ConnectorStatus = onlineConnectorStatus.Status;
                                            Logger.LogTrace("Index: Found online status for CP='{0}' / Connector='{1}' / Status='{2}'", cpovm.ChargePointId, cpovm.ConnectorId, cpovm.ConnectorStatus);
                                        }

                                        if (connStatus.TransactionId.HasValue)
                                        {
                                            cpovm.MeterStart = connStatus.MeterStart.Value;
                                            cpovm.MeterStop = connStatus.MeterStop;
                                            cpovm.StartTime = connStatus.StartTime;
                                            cpovm.StopTime = connStatus.StopTime;

                                            // default status: active transaction or not?
                                            cpovm.ConnectorStatus = (cpovm.StopTime.HasValue) ? ConnectorStatusEnum.Available : ConnectorStatusEnum.Occupied;
                                        }
                                        else
                                        {
                                            cpovm.MeterStart = -1;
                                            cpovm.MeterStop = -1;
                                            cpovm.StartTime = null;
                                            cpovm.StopTime = null;

                                            // default status: Available
                                            cpovm.ConnectorStatus = ConnectorStatusEnum.Available;
                                        }

                                        // Add current charge data to view model
                                        if (cpovm.ConnectorStatus == ConnectorStatusEnum.Occupied &&
                                            onlineConnectorStatus != null)
                                        {
                                            //string currentCharge = string.Empty;
                                            string currentCharge = "mValue";
                                            if (onlineConnectorStatus.ChargeRateKW != null)
                                            {
                                                currentCharge = string.Format("{0:0.0}kW", onlineConnectorStatus.ChargeRateKW.Value);
                                            }
                                            if (onlineConnectorStatus.SoC != null)
                                            {
                                                if (!string.IsNullOrWhiteSpace(currentCharge)) currentCharge += " | ";
                                                currentCharge += string.Format("{0:0}%", onlineConnectorStatus.SoC.Value);
                                            }
                                            if (!string.IsNullOrWhiteSpace(currentCharge))
                                            {
                                                cpovm.CurrentChargeData = currentCharge;
                                            }
                                        }
                                        cpovm.MessageLog = dbContext.MessageLogs.ToList().Where(m => m.ChargePointId == cpovm.ChargePointId && m.Message == "MeterValues").LastOrDefault();
                                        overviewModel.ChargePoints.Add(cpovm);
                                    }
                                }
                            }
                            // Fallback: assume 1 connector and show main charge point
                            if (foundConnectorStatus == false)
                            {
                                // no connector status found in DB => show configured charge point in overview
                                ChargePointsOverviewViewModel cpovm = new ChargePointsOverviewViewModel();
                                cpovm.ChargePointId = cp.ChargePointId;
                                cpovm.ConnectorId = 0;
                                cpovm.Name = cp.Name;
                                cpovm.Comment = cp.Comment;
                                //cpovm.Online = cpOnlineStatus != null;
                                var getlast = dbContext.ConnectorStatuses.ToList().Where(m => m.ChargePointId == cpovm.ChargePointId).LastOrDefault();
                                if (getlast != null && getlast.lastSeen.HasValue)
                                {
                                    cpovm.Online = (DateTime.Now - getlast.lastSeen.Value).TotalMinutes <= 8 ? true : false;
                                }
                                else
                                {
                                    cpovm.Online = cpOnlineStatus != null;
                                }
                                cpovm.ConnectorStatus = ConnectorStatusEnum.Undefined;
                                overviewModel.ChargePoints.Add(cpovm);
                            }
                        }
                    }

                    Logger.LogInformation("Index: Found {0} charge points / connectors", overviewModel.ChargePoints?.Count);
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "Index: Error loading charge points from database");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }

            return View(overviewModel);
        }

        [Authorize]
        // public async Task<IActionResult> IndexNew()
        //{
        //    using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
        //   {
        //         var model = dbContext.ConnectorStatuses.ToList();

        //         var currentUsername = User.Identity.Name;

        //         // Log giá trị của username
        //         Logger.LogInformation("Current Username: {Username}", currentUsername);

        //         List<ChargePoint> dbChargePoints;

        //         if (currentUsername == "admin")
        //         {
        //             // Nếu là admin, hiển thị tất cả ChargePoints với OwnerId != 4
        //             dbChargePoints = dbContext.ChargePoints
        //                                       .Where(m => m.OwnerId != 4)
        //                                       .ToList();
        //         }
        //         else
        //         {
        //             // Tìm OwnerId trong bảng Accounts dựa trên username
        //             var owner = dbContext.Accounts.FirstOrDefault(a => a.UserName == currentUsername);

        //             if (owner != null)
        //             {
        //                 // Nếu tìm thấy tài khoản, lấy ChargePoints dựa trên OwnerId
        //                 dbChargePoints = dbContext.ChargePoints
        //                                           .Where(cp => cp.OwnerId == owner.OwnerId)
        //                                           .ToList();
        //             }
        //             else
        //             {
        //                 // Nếu không tìm thấy tài khoản, trả về danh sách trống hoặc xử lý mặc định
        //                 dbChargePoints = new List<ChargePoint>();
        //                 Logger.LogWarning("No matching account found for username: {Username}", currentUsername);
        //             }
        //         }

        //         /* List<ChargePoint> dbChargePoints = dbContext.ChargePoints.Where(m => m.OwnerId != 4).ToList<ChargePoint>().ToList();*/
        //         List<ChargePointsOverviewViewModel> lstChargePointsOverviewViewModel = new List<ChargePointsOverviewViewModel>();
        //         foreach (var item in model)
        //         {
        //             ChargePointsOverviewViewModel cpovm = new ChargePointsOverviewViewModel();
        //             var getcp = dbChargePoints.Where(m => m.ChargePointId == item.ChargePointId).FirstOrDefault();
        //             cpovm.ChargePointId = item.ChargePointId;
        //             if (getcp != null)
        //                 cpovm.Name = $"{getcp.Name}:{item.ConnectorId}";
        //             var getlast = model.Where(m => m.ChargePointId == cpovm.ChargePointId).LastOrDefault();
        //             if (getlast != null && getlast.lastSeen.HasValue)
        //             {
        //               cpovm.Online = (DateTime.Now - getlast.lastSeen.Value).TotalMinutes <= 8 ? true : false;
        //             }
        //             else
        //             {
        //                cpovm.Online = false;
        //             }
        //             if (item.LastStatus == "Available")
        //                cpovm.ConnectorStatus = ConnectorStatusEnum.Available;
        //            if (item.LastStatus == "Occupied")
        //                cpovm.ConnectorStatus = ConnectorStatusEnum.Occupied;
        //            //
        //             if (getcp != null)
        //                lstChargePointsOverviewViewModel.Add(cpovm);
        //         }
        //         return View(lstChargePointsOverviewViewModel);
        //     }
        // }
        // [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        // public IActionResult Error()
        // {
        //     return View();
        // }

        [Authorize]
        public async Task<IActionResult> IndexNew()
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                var model = dbContext.ConnectorStatuses.ToList();
                var currentUsername = User.Identity.Name;

                // Log giá trị của username
                Logger.LogInformation("Current Username: {Username}", currentUsername);

                List<ChargePoint> dbChargePoints;

                if (currentUsername == "admin")
                {
                    dbChargePoints = dbContext.ChargePoints
                                               .Where(m => m.OwnerId != 4)
                                               .ToList();
                }
                else
                {
                    var owner = dbContext.Accounts.FirstOrDefault(a => a.UserName == currentUsername);
                    dbChargePoints = owner != null
                        ? dbContext.ChargePoints.Where(cp => cp.OwnerId == owner.OwnerId).ToList()
                        : new List<ChargePoint>();
                }

                List<ChargePointsOverviewViewModel> lstChargePointsOverviewViewModel = new List<ChargePointsOverviewViewModel>();
                foreach (var item in model)
                {
                    ChargePointsOverviewViewModel cpovm = new ChargePointsOverviewViewModel();
                    var getcp = dbChargePoints.Where(m => m.ChargePointId == item.ChargePointId).FirstOrDefault();
                    cpovm.ChargePointId = item.ChargePointId;

                    if (getcp != null)
                        cpovm.Name = $"{getcp.Name}:{item.ConnectorId}";

                    var getlast = model.Where(m => m.ChargePointId == cpovm.ChargePointId).LastOrDefault();
                    if (getlast != null && getlast.lastSeen.HasValue)
                    {
                        cpovm.Online = (DateTime.Now - getlast.lastSeen.Value).TotalMinutes <= 8 ? true : false;
                    }
                    else
                    {
                        cpovm.Online = false;
                    }

                    Logger.LogInformation("ChargePoint {ChargePointName} Status: {Status}, LastSeen: {LastSeen}, Online: {Online}", cpovm.Name, item.LastStatus, getlast?.lastSeen, cpovm.Online);

                    if (item.LastStatus == "Available")
                        cpovm.ConnectorStatus = ConnectorStatusEnum.Available;
                    if (item.LastStatus == "Occupied")
                        cpovm.ConnectorStatus = ConnectorStatusEnum.Occupied;

                    // Gửi email nếu trạng thái thay đổi từ offline sang online
                    if (cpovm.Online && getlast != null && getlast.lastSeen.HasValue)
                    {
                        bool wasOffline = (DateTime.Now - getlast.lastSeen.Value).TotalMinutes > 8;

                        if (wasOffline)
                        {
                            string chargePointName = getcp.Name;
                            string status = "Online";

                            // Gửi email thông báo
                            Logger.LogInformation("Sending email for ChargePoint {ChargePointName} status change: {OldStatus} -> {NewStatus}", chargePointName, "Offline", status);
                            await _emailService.SendChargePointStatusEmail(chargePointName, status);

                            // Log việc gửi email
                            Logger.LogInformation("Email sent for ChargePoint status change: {ChargePointName} to {NewStatus}", chargePointName, status);
                        }
                    }

                    // Gửi email nếu trạng thái thay đổi từ online sang offline
                    if (!cpovm.Online && getlast != null && getlast.lastSeen.HasValue)
                    {
                        bool wasOnline = (DateTime.Now - getlast.lastSeen.Value).TotalMinutes <= 8;

                        if (wasOnline)
                        {
                            string chargePointName = getcp.Name;
                            string status = "Offline";

                            // Gửi email thông báo
                            Logger.LogInformation("Sending email for ChargePoint {ChargePointName} status change: {OldStatus} -> {NewStatus}", chargePointName, "Online", status);
                            await _emailService.SendChargePointStatusEmail(chargePointName, status);

                            // Log việc gửi email
                            Logger.LogInformation("Email sent for ChargePoint status change: {ChargePointName} to {NewStatus}", chargePointName, status);
                        }
                    }

                    if (getcp != null)
                        lstChargePointsOverviewViewModel.Add(cpovm);
                }

                return View(lstChargePointsOverviewViewModel);
            }
        }






        [Authorize]
        public async Task<IActionResult> IndexGoev()
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                var model = dbContext.ConnectorStatuses.ToList();
                List<ChargePoint> dbChargePoints = dbContext.ChargePoints.Where(m => m.OwnerId != 4).ToList<ChargePoint>().ToList();
                List<ChargePointsOverviewViewModel> lstChargePointsOverviewViewModel = new List<ChargePointsOverviewViewModel>();
                foreach (var item in model)
                {
                    ChargePointsOverviewViewModel cpovm = new ChargePointsOverviewViewModel();
                    var getcp = dbChargePoints.Where(m => m.ChargePointId == item.ChargePointId).FirstOrDefault();
                    cpovm.ChargePointId = item.ChargePointId;
                    if (getcp != null)
                        cpovm.Name = $"{getcp.Name}:{item.ConnectorId}";
                    var getlast = model.Where(m => m.ChargePointId == cpovm.ChargePointId).LastOrDefault();
                    if (getlast != null && getlast.lastSeen.HasValue)
                    {
                        cpovm.Online = (DateTime.Now - getlast.lastSeen.Value).TotalMinutes <= 8 ? true : false;
                    }
                    else
                    {
                        cpovm.Online = false;
                    }
                    if (item.LastStatus == "Available")
                        cpovm.ConnectorStatus = ConnectorStatusEnum.Available;
                    if (item.LastStatus == "Occupied")
                        cpovm.ConnectorStatus = ConnectorStatusEnum.Occupied;
                    //
                    if (getcp != null)
                        lstChargePointsOverviewViewModel.Add(cpovm);
                }
                return View(lstChargePointsOverviewViewModel);
            }
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult ErrorGoev()
        {
            return View();
        }


    }
}
