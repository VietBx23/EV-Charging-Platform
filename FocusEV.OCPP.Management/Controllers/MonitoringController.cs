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
using FocusEV.OCPP.Management.Models.Api;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;

namespace FocusEV.OCPP.Management.Controllers
{
    public partial class MonitoringController : BaseController
    {
        private readonly IStringLocalizer<MonitoringController> _localizer;

        public MonitoringController(
            UserManager userManager,
            IStringLocalizer<MonitoringController> localizer,
            ILoggerFactory loggerFactory,
            IConfiguration config) : base(userManager, loggerFactory, config)
        {
            _localizer = localizer;
            Logger = loggerFactory.CreateLogger<MonitoringController>();
        }


        [Authorize]
        public async Task<IActionResult> ChargePointOperating()
        {
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
                    List<ChargePoint> dbChargePoints = dbContext.ChargePoints.ToList<ChargePoint>();
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
                                        cpovm.Online = cpOnlineStatus != null;
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
                                            string currentCharge = string.Empty;
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
                                cpovm.Online = cpOnlineStatus != null;
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }


        public IActionResult OrderMonitoring()
        {
            return View();
        }

        public IActionResult ChargePointLogs()
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                var getmodel = dbContext.Transactions.Take(1).ToList();
                List<api_Transaction> lstapi_Transactions = new List<api_Transaction>();
                foreach (var item in getmodel)
                {
                    api_Transaction api_Transaction = new api_Transaction();
                    api_Transaction.Transaction = item;
                    api_Transaction.Customer = dbContext.Customers.Where(m => m.TagId == item.StartTagId).FirstOrDefault();
                    api_Transaction.ChargeTag = dbContext.ChargeTags.Where(m => m.TagId == item.StartTagId).FirstOrDefault();
                    api_Transaction.ChargePoint = dbContext.ChargePoints.Where(m => m.ChargePointId == item.ChargePointId).FirstOrDefault();
                    if (api_Transaction.ChargePoint != null)
                    {
                        api_Transaction.ChargeStation = dbContext.ChargeStations.Where(m => m.ChargeStationId == api_Transaction.ChargePoint.ChargeStationId).FirstOrDefault();

                    }
                    lstapi_Transactions.Add(api_Transaction);
                }
                return View(lstapi_Transactions.OrderByDescending(o => o.Transaction.StartTime));
            }
        }

        //[Authorize]
        //public IActionResult ChargePointOperating()
        //{
        //    return View();
        //}

        public IActionResult ChargePointConnectionLogs()
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                var model = dbContext.MessageLogs.Where(m=>m.Message!= "Heartbeat" && m.Message!= "MeterValues" && m.Message!= "BootNotification" && m.Message != "StatusNotification").OrderByDescending(o => o.LogId).ToList();
                return View(model);
            }
        }

        public IActionResult ChargeCardRecord()
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                var getmodel = dbContext.Transactions.OrderByDescending(m=>m.TransactionId).ToList();
                List<api_Transaction> lstapi_Transactions = new List<api_Transaction>();
                foreach(var item in getmodel)
                {
                    api_Transaction api_Transaction = new api_Transaction();
                    api_Transaction.Transaction = item;
                    api_Transaction.Customer = dbContext.Customers.Where(m => m.TagId == item.StartTagId).FirstOrDefault();
                    api_Transaction.ChargeTag = dbContext.ChargeTags.Where(m => m.TagId == item.StartTagId).FirstOrDefault();
                    api_Transaction.ChargePoint = dbContext.ChargePoints.Where(m => m.ChargePointId == item.ChargePointId).FirstOrDefault();
                    lstapi_Transactions.Add(api_Transaction);
                }
                return View(lstapi_Transactions);
            }
              
        }

      
        public IActionResult VNPayMonitoring()
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                var model = (from us in dbContext.UserApps
                             join dp in dbContext.DepositHistorys
                             on us.Id equals dp.UserAppId
                             join log in dbContext.VnpIPNLogs
                             on dp.DepositCode equals log.vnp_TxnRef
                             where dp.Message== "Nạp tiền từ VNPay"
                             select new VNPayMonitor()
                             {
                                 UserApp = us,
                                 DepositHistory = dp,
                                 VnpIPNLog = log
                             }
                             ).OrderByDescending(m=>m.DepositHistory.DateCreate).ToList();
                ViewBag.TotalSucess = model.Where(m => m.VnpIPNLog.vnp_ResponseCode == "00").Sum(m => m.DepositHistory.Amount);
                return View(model);
            }
        }
        public IActionResult QRCodeMonitoring()
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                var model = (from sq in dbContext.StaticQRIPNLogs
                             join cn in dbContext.ConnectorStatuses
                             //on sq.terminalId equals cn.ChargePointId
                             on sq.terminalId equals cn.terminalId
                             select new QRCodeMonitor()
                             {
                                 ConnectorStatus =cn,
                                 StaticQRIPNLog= sq,
                                 hasTransaction =""
                             }
                             ).OrderByDescending(p => p.StaticQRIPNLog.IpnId).ToList();
                if (model != null)
                {
                    var i = 0;
                    foreach (var item in model)
                    {
                        try
                        {
                            var check = dbContext.QRTransactions.Where(p => p.qrTrace == item.StaticQRIPNLog.qrTrace).FirstOrDefault();
                            if (check == null)
                            {
                                item.hasTransaction = "chưa tạo đơn sạc";
                            }
                            else
                            {
                                item.hasTransaction = "Đơn sạc số: " + check.QrTransactionId.ToString();
                            }
                            i++;
                        }
                        catch(Exception ex)
                        {
                            var a = i;
                        }
                       
                    }
                }
                
                return View(model);
            }
        }


        public ActionResult FocusAddDeposit()
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                ViewBag.Listuser = dbContext.UserApps.Where(m => m.OwnerId == 1).ToList();
                return View();
            }
        }

        [HttpPost]
        public async Task<bool>  AddDepositFree(string Id,decimal balance)
        {
            try
            {
                string apiUrl = "https://solarev-lado-api.insitu.com.vn/api/AddDeposit_free";
                string requestBody = "{\"UserAppId\":\""+ Id+"\",\"Amount\":"+ balance+",\"PaymentGateway\":3}";

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync(); 
                    }
                    else
                    {
                        Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}