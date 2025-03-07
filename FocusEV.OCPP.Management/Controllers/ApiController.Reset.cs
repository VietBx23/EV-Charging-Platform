﻿
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FocusEV.OCPP.Database;
using System.Linq;
using FocusEV.OCPP.Management.Models.Api;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FocusEV.OCPP.Management.Controllers
{
    public partial class ApiController : BaseController
    {
        private readonly IStringLocalizer<ApiController> _localizer;

        public ApiController(
            UserManager userManager,
            IStringLocalizer<ApiController> localizer,
            ILoggerFactory loggerFactory,
            IConfiguration config) : base(userManager, loggerFactory, config)
        {
            _localizer = localizer;
            Logger = loggerFactory.CreateLogger<HomeController>();
        }

        [Authorize]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Reset(string Id)
        {
            if (User != null && !User.IsInRole(Constants.AdminRoleName))
            {
                Logger.LogWarning("Reset: Request by non-administrator: {0}", User?.Identity?.Name);
                return StatusCode((int)HttpStatusCode.Unauthorized);
            }

            int httpStatuscode = (int)HttpStatusCode.OK;
            string resultContent = string.Empty;

            Logger.LogTrace("Reset: Request to restart chargepoint '{0}'", Id);
            if (!string.IsNullOrEmpty(Id))
            {
                try
                {
                    using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                    {
                        ChargePoint chargePoint = dbContext.ChargePoints.Find(Id);
                        if (chargePoint != null)
                        {
                            string serverApiUrl = base.Config.GetValue<string>("ServerApiUrl");
                            string apiKeyConfig = base.Config.GetValue<string>("ApiKey");
                            if (!string.IsNullOrEmpty(serverApiUrl))
                            {
                                try
                                {
                                    using (var httpClient = new HttpClient())
                                    {
                                        if (!serverApiUrl.EndsWith('/'))
                                        {
                                            serverApiUrl += "/";
                                        }
                                        Uri uri = new Uri(serverApiUrl);
                                        uri = new Uri(uri, $"Reset/{Uri.EscapeDataString(Id)}");
                                        httpClient.Timeout = new TimeSpan(0, 0, 4); // use short timeout

                                        // API-Key authentication?
                                        if (!string.IsNullOrWhiteSpace(apiKeyConfig))
                                        {
                                            httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKeyConfig);
                                        }
                                        else
                                        {
                                            Logger.LogWarning("Reset: No API-Key configured!");
                                        }

                                        HttpResponseMessage response = await httpClient.GetAsync(uri);
                                        if (response.StatusCode == HttpStatusCode.OK)
                                        {
                                            string jsonResult = await response.Content.ReadAsStringAsync();
                                            if (!string.IsNullOrEmpty(jsonResult))
                                            {
                                                try
                                                {
                                                    dynamic jsonObject = JsonConvert.DeserializeObject(jsonResult);
                                                    Logger.LogInformation("Reset: Result of API request is '{0}'", jsonResult);
                                                    string status = jsonObject.status;
                                                    switch (status)
                                                    {
                                                        case "Accepted":
                                                            resultContent = _localizer["ResetAccepted"];
                                                            break;
                                                        case "Rejected":
                                                            resultContent = _localizer["ResetRejected"];
                                                            break;
                                                        case "Scheduled":
                                                            resultContent = _localizer["ResetScheduled"];
                                                            break;
                                                        default:
                                                            resultContent = string.Format(_localizer["ResetUnknownStatus"], status);
                                                            break;
                                                    }
                                                }
                                                catch (Exception exp)
                                                {
                                                    Logger.LogError(exp, "Reset: Error in JSON result => {0}", exp.Message);
                                                    httpStatuscode = (int)HttpStatusCode.OK;
                                                    resultContent = _localizer["ResetError"];
                                                }
                                            }
                                            else
                                            {
                                                Logger.LogError("Reset: Result of API request is empty");
                                                httpStatuscode = (int)HttpStatusCode.OK;
                                                resultContent = _localizer["ResetError"];
                                            }
                                        }
                                        else if (response.StatusCode == HttpStatusCode.NotFound)
                                        {
                                            // Chargepoint offline
                                            httpStatuscode = (int)HttpStatusCode.OK;
                                            resultContent = _localizer["ResetOffline"];
                                        }
                                        else
                                        {
                                            Logger.LogError("Reset: Result of API  request => httpStatus={0}", response.StatusCode);
                                            httpStatuscode = (int)HttpStatusCode.OK;
                                            resultContent = _localizer["ResetError"];
                                        }
                                    }
                                }
                                catch (Exception exp)
                                {
                                    Logger.LogError(exp, "Reset: Error in API request => {0}", exp.Message);
                                    httpStatuscode = (int)HttpStatusCode.OK;
                                    resultContent = _localizer["ResetError"];
                                }
                            }
                        }
                        else
                        {
                            Logger.LogWarning("Reset: Error loading charge point '{0}' from database", Id);
                            httpStatuscode = (int)HttpStatusCode.OK;
                            resultContent = _localizer["UnknownChargepoint"];
                        }
                    }
                }
                catch (Exception exp)
                {
                    Logger.LogError(exp, "Reset: Error loading charge point from database");
                    httpStatuscode = (int)HttpStatusCode.OK;
                    resultContent = _localizer["ResetError"];
                }
            }

            return StatusCode(httpStatuscode, resultContent);
        }

        public IActionResult GetMessageLog()
        {

            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                var check = dbContext.Database.GetDbConnection().State;
                var dd = dbContext.Owners.ToList();
                var model = dbContext.MessageLogs.Where(m => m.Message != "Heartbeat" && m.Message != "BootNotification" && m.Message != "MeterValues").ToList();
                return new JsonResult(model);
            }
          
        }
        public DateTime GetLastseen(List<MessageLog> lstMessage,string ChargePointId)
        { 
            var getlast = lstMessage.ToList().Where(m => m.ChargePointId == ChargePointId && m.Message == "Heartbeat").LastOrDefault();
            if (getlast != null)
            {
                return getlast.LogTime;
            }
            return DateTime.MinValue;
        }   
        public IActionResult GetStatusChargePoint()
        {

            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            { 
                var getListmess = dbContext.MessageLogs.Where(m => m.Message == "Heartbeat").ToList();
                var model = (from cp in dbContext.ChargePoints.Include(p => p.OwnerVtl).Include(p => p.ChargeStationVtl).ToList()
                             select new Api_ChargePoint()
                             {
                                 ChargePointID = cp.ChargePointId,
                                 ChargePointName = cp.Name,
                                 ChargePointComment = cp.Comment,
                                 OwnerID = cp.OwnerVtl != null ? cp.OwnerVtl.OwnerId : 0,
                                 OwnerName = cp.OwnerVtl != null ? cp.OwnerVtl.Name : "",
                                 ChargeStationID = cp.ChargeStationVtl != null ? cp.ChargeStationVtl.ChargeStationId : 0,
                                 ChargeStationName = cp.ChargeStationVtl != null ? cp.ChargeStationVtl.Name : "",
                                 LastSeen = GetLastseen(getListmess, cp.ChargePointId)
                             }
                           ).ToList();
                return new JsonResult(model);
            }
        }

    }
}
