using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FocusEV.OCPP.Database;
using FocusEV.OCPP.Management.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using System.Globalization;
using FocusEV.OCPP.Management.Models.Api;
using System.Security.Claims;

namespace FocusEV.OCPP.Management.Controllers
{
    public class ChargingStatisticsController : BaseController
    {
        private readonly IStringLocalizer<ChargingStatisticsController> _localizer;

        public ChargingStatisticsController(
            UserManager userManager,
            IStringLocalizer<ChargingStatisticsController> localizer,
            ILoggerFactory loggerFactory,
            IConfiguration config) : base(userManager, loggerFactory, config)
        {
            _localizer = localizer;
            Logger = loggerFactory.CreateLogger<ChargingStatisticsController>();
        }
        [Authorize]
        public IActionResult ChargeDataDaily()
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                string myUsername = string.Empty;
                int myOwnerid = 0;
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userName = claimsIdentity.FindFirst(ClaimTypes.Name)?.Value;
                if (userName != null)
                {
                    var role = claimsIdentity.FindFirst(ClaimTypes.Role)?.Value;
                    var account = dbContext.Accounts.Where(m => m.UserName == userName).FirstOrDefault();
                    myUsername = account.UserName.ToString();
                    myOwnerid = (int)account.OwnerId;
                    if (myUsername == "ladoadmin")
                    {
                        var lstTransaction = (from tr in dbContext.Transactions
                                              join cp in dbContext.ChargePoints
                                              on tr.ChargePointId equals cp.ChargePointId
                                              where cp.OwnerId == 1
                                              select tr).ToList();
                        var groupbyTransaction = lstTransaction.ToList().GroupBy(m => new { m.ChargePointId, m.ConnectorId }).SelectMany(m => m.Take(1)).OrderByDescending(m => m.ChargePointId).ToList();
                        foreach (var item in groupbyTransaction)
                        {
                            var getlast = dbContext.Transactions.ToList().Where(m => m.ChargePointId == item.ChargePointId && m.ConnectorId == item.ConnectorId && m.MeterStop.HasValue).LastOrDefault();
                            item.MeterStop = getlast != null ? getlast.MeterStop.Value : 0;

                        }
                        var getRate = dbContext.Unitprices.Where(p => p.IsActive == 1).FirstOrDefault();

                        var totalUse = groupbyTransaction.Sum(m => m.MeterStop);
                        var totalCo2 = totalUse * 0.0008041;
                        ViewBag.TotalMeter = $"{groupbyTransaction.Sum(m => m.MeterStop):n}";
                        ViewBag.ListTrancsaction = groupbyTransaction;
                        ViewBag.CurrentTime = DateTime.Now;
                        ViewBag.Co2data = $"{totalCo2:n}";
                        ViewBag.Totalcost = $"{(totalUse * (float)getRate.Price):n0}";
                        return View();
                    }
                    else
                    {
                        var lstTransaction = (from tr in dbContext.Transactions
                                              join cp in dbContext.ChargePoints
                                              on tr.ChargePointId equals cp.ChargePointId
                                              //where (cp.ChargePointId != "Insitu001" || cp.ChargePointId != "InsituHCM")
                                              where cp.OwnerId != 4
                                              select tr).ToList();
                        var groupbyTransaction = lstTransaction.ToList().GroupBy(m => new { m.ChargePointId, m.ConnectorId }).SelectMany(m => m.Take(1)).OrderByDescending(m => m.ChargePointId).ToList();
                        foreach (var item in groupbyTransaction)
                        {
                            var getlast = dbContext.Transactions.ToList().Where(m => m.ChargePointId == item.ChargePointId && m.ConnectorId == item.ConnectorId && m.MeterStop.HasValue).LastOrDefault();
                            item.MeterStop = getlast != null ? getlast.MeterStop.Value : 0;

                        }
                        var getRate = dbContext.Unitprices.Where(p => p.IsActive == 1).FirstOrDefault();

                        var totalUse = groupbyTransaction.Sum(m => m.MeterStop);
                        var totalCo2 = totalUse * 0.0008041;
                        ViewBag.TotalMeter = $"{groupbyTransaction.Sum(m => m.MeterStop):n}";
                        ViewBag.ListTrancsaction = groupbyTransaction;
                        ViewBag.CurrentTime = DateTime.Now;
                        ViewBag.Co2data = $"{totalCo2:n}";
                        ViewBag.Totalcost = $"{(totalUse * (float)getRate.Price):n0}";
                        return View();
                    }
                }
                else
                {
                    var lstTransaction = (from tr in dbContext.Transactions
                                          join cp in dbContext.ChargePoints
                                          on tr.ChargePointId equals cp.ChargePointId
                                          where cp.OwnerId != 4
                                          select tr).ToList();
                    var groupbyTransaction = lstTransaction.ToList().GroupBy(m => new { m.ChargePointId, m.ConnectorId }).SelectMany(m => m.Take(1)).OrderByDescending(m => m.ChargePointId).ToList();
                    foreach (var item in groupbyTransaction)
                    {
                        var getlast = dbContext.Transactions.ToList().Where(m => m.ChargePointId == item.ChargePointId && m.ConnectorId == item.ConnectorId && m.MeterStop.HasValue).LastOrDefault();
                        //item.MeterStop = getlast.MeterStop.Value;
                        //var lastMeter = getlast!=null? getlast.MeterStop.Value:0;
                        //item.MeterStop = $"{lastMeter:n}";
                        item.MeterStop = getlast != null ? getlast.MeterStop.Value : 0;

                    }
                    var getRate = dbContext.Unitprices.Where(p => p.IsActive == 1).FirstOrDefault();

                    var totalUse = groupbyTransaction.Sum(m => m.MeterStop);
                    var totalCo2 = totalUse * 0.0008041;
                    //ViewBag.TotalMeter = groupbyTransaction.Sum(m => m.MeterStop);
                    ViewBag.TotalMeter = $"{groupbyTransaction.Sum(m => m.MeterStop):n}";
                    ViewBag.ListTrancsaction = groupbyTransaction;
                    ViewBag.CurrentTime = DateTime.Now;
                    ViewBag.Co2data = $"{totalCo2:n}";
                    ViewBag.Totalcost = $"{(totalUse * (float)getRate.Price):n0}";
                    return View();
                }                           
            }
        }

        [Authorize]
        public IActionResult ChargeDataDaily_2()
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                string myUsername = string.Empty;
                int myOwnerid = 0;
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userName = claimsIdentity.FindFirst(ClaimTypes.Name)?.Value;
                if (userName != null)
                {
                    var role = claimsIdentity.FindFirst(ClaimTypes.Role)?.Value;
                    var account = dbContext.Accounts.Where(m => m.UserName == userName).FirstOrDefault();
                    myUsername = account.UserName.ToString();
                    myOwnerid = (int)account.OwnerId;
                    var lstTransaction = (from tr in dbContext.Transactions
                                          join cp in dbContext.ChargePoints
                                          on tr.ChargePointId equals cp.ChargePointId
                                          where (cp.OwnerId == 1 && myUsername == "ladoadmin") || cp.OwnerId != 1
                                          select tr).ToList();
                    var groupbyTransaction = lstTransaction.ToList().GroupBy(m => new { m.ChargePointId, m.ConnectorId }).SelectMany(m => m.Take(1)).OrderByDescending(m => m.ChargePointId).ToList();
                    foreach (var item in groupbyTransaction)
                    {
                        var getlast = dbContext.Transactions.ToList().Where(m => m.ChargePointId == item.ChargePointId && m.ConnectorId == item.ConnectorId && m.MeterStop.HasValue).LastOrDefault();
                        item.MeterStop = getlast != null ? getlast.MeterStop.Value : 0;

                    }
                    var getRate = dbContext.Unitprices.Where(p => p.IsActive == 1).FirstOrDefault();

                    var totalUse = groupbyTransaction.Sum(m => m.MeterStop);
                    var totalCo2 = totalUse * 0.0008041;
                    ViewBag.TotalMeter = $"{groupbyTransaction.Sum(m => m.MeterStop):n}";
                    ViewBag.ListTrancsaction = groupbyTransaction;
                    ViewBag.CurrentTime = DateTime.Now;
                    ViewBag.Co2data = $"{totalCo2:n}";
                    ViewBag.Totalcost = $"{(totalUse * (float)getRate.Price):n0}";
                    return View();
                }
                else
                {
                    var lstTransaction = (from tr in dbContext.Transactions
                                          join cp in dbContext.ChargePoints
                                          on tr.ChargePointId equals cp.ChargePointId
                                          select tr).ToList();
                    var groupbyTransaction = lstTransaction.ToList().GroupBy(m => new { m.ChargePointId, m.ConnectorId }).SelectMany(m => m.Take(1)).OrderByDescending(m => m.ChargePointId).ToList();
                    foreach (var item in groupbyTransaction)
                    {
                        var getlast = dbContext.Transactions.ToList().Where(m => m.ChargePointId == item.ChargePointId && m.ConnectorId == item.ConnectorId && m.MeterStop.HasValue).LastOrDefault();
                        //item.MeterStop = getlast.MeterStop.Value;
                        //var lastMeter = getlast!=null? getlast.MeterStop.Value:0;
                        //item.MeterStop = $"{lastMeter:n}";
                        item.MeterStop = getlast != null ? getlast.MeterStop.Value : 0;

                    }
                    var getRate = dbContext.Unitprices.Where(p => p.IsActive == 1).FirstOrDefault();

                    var totalUse = groupbyTransaction.Sum(m => m.MeterStop);
                    var totalCo2 = totalUse * 0.0008041;
                    //ViewBag.TotalMeter = groupbyTransaction.Sum(m => m.MeterStop);
                    ViewBag.TotalMeter = $"{groupbyTransaction.Sum(m => m.MeterStop):n}";
                    ViewBag.ListTrancsaction = groupbyTransaction;
                    ViewBag.CurrentTime = DateTime.Now;
                    ViewBag.Co2data = $"{totalCo2:n}";
                    ViewBag.Totalcost = $"{(totalUse * (float)getRate.Price):n0}";
                    return View();
                }
            }
        }

        [Authorize]
        public IActionResult SearchChargeOrder()
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                string myUsername = string.Empty;
                int myOwnerid = 0;
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userName = claimsIdentity.FindFirst(ClaimTypes.Name)?.Value;
                if (userName != null)
                {
                    var role = claimsIdentity.FindFirst(ClaimTypes.Role)?.Value;
                    var account = dbContext.Accounts.Where(m => m.UserName == userName).FirstOrDefault();
                    myUsername = account.UserName.ToString();
                    myOwnerid = (int)account.OwnerId;
                    if (myUsername == "ladoadmin")
                    {
                        ViewBag.ListOwner = dbContext.Owners.Where(m => m.OwnerId == 1).ToList();
                    }
                    else
                    {
                        ViewBag.ListOwner = dbContext.Owners.Where(v => v.OwnerId != 4).ToList();
                    }
                }
                else
                {
                    ViewBag.ListOwner = dbContext.Owners.Where(v => v.OwnerId != 4).ToList();
                }
                return View();
            }
        }
        public IActionResult SearchData_old(string ChargePointId,string FromDate, string ToDate)
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                DateTime myTimeFrom = DateTime.ParseExact(FromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                DateTime myTimeTo = DateTime.ParseExact(ToDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var getModel = dbContext.Transactions.Where(m => m.ChargePointId == ChargePointId || ChargePointId=="0").ToList();
                getModel = getModel.Where(m => m.StartTime >= myTimeFrom && m.StartTime <= myTimeTo).ToList();
                List<api_Transaction> lstapi_Transactions = new List<api_Transaction>();

                foreach(var item in getModel)
                {
                    api_Transaction api_Transaction = new api_Transaction();
                    api_Transaction.Transaction = item;
                    api_Transaction.ChargePoint = dbContext.ChargePoints.Find(item.ChargePointId);
                    api_Transaction.ChargeStation = dbContext.ChargeStations.Find(api_Transaction.ChargePoint.ChargeStationId);
                    api_Transaction.Owner = dbContext.Owners.Find(api_Transaction.ChargeStation.OwnerId);
                    api_Transaction.Customer = dbContext.Customers.Where(m => m.TagId == api_Transaction.Transaction.StartTagId).FirstOrDefault();
                    var getUnitPrice = dbContext.Unitprices.Where(m => m.IsActive == 1).FirstOrDefault().Price;
                    decimal totalHour = 0;
                    if (item.StopTime.Value.Second != item.StartTime.Second)
                    {
                        totalHour= Math.Abs(decimal.Parse((item.StopTime - item.StartTime).Value.TotalHours.ToString()));
                    }
                   
                    api_Transaction.TotalPrice = Math.Round(totalHour * getUnitPrice);
                    lstapi_Transactions.Add(api_Transaction);
                }
                return PartialView(lstapi_Transactions);
            }
        }


        public IActionResult SearchData(int OwnerId,int ChargeStationId,string ChargePointId, string FromDate, string ToDate)
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                var getsplittoday = ToDate.Split('/');
                if (getsplittoday[0].Length == 1)
                {
                    getsplittoday[0] = "0" + getsplittoday[0];
                }
                if (getsplittoday[1].Length == 1)
                {
                    getsplittoday[1] = "0" + getsplittoday[1];
                }
                ToDate = getsplittoday[0] + "/" + getsplittoday[1] + "/" + getsplittoday[2];
                DateTime myTimeFrom = DateTime.ParseExact(FromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                DateTime myTimeTo = DateTime.ParseExact(ToDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                var model = (from t in dbContext.Transactions
                             join cp in dbContext.ChargePoints
                             on t.ChargePointId equals cp.ChargePointId
                             join st in dbContext.ChargeStations.Where(m=> ChargeStationId==0 || m.ChargeStationId== ChargeStationId)
                             on cp.ChargeStationId equals st.ChargeStationId
                             join ow in dbContext.Owners.Where(m=>m.OwnerId== OwnerId)
                             on st.OwnerId equals ow.OwnerId
                             select t
                           ).ToList();
                //var getModel = dbContext.Transactions.Where(m => m.ChargePointId == ChargePointId || ChargePointId == "0").ToList();
                var getModel = model.Where(m => m.ChargePointId == ChargePointId || ChargePointId == "0").ToList();
                getModel = getModel.Where(m => m.StartTime.Date >= myTimeFrom.Date && m.StartTime.Date <= myTimeTo.Date).OrderByDescending(m=>m.StartTime).ToList();
                List<api_Transaction> lstapi_Transactions = new List<api_Transaction>();

                foreach (var item in getModel)
                {
                    api_Transaction api_Transaction = new api_Transaction();
                    api_Transaction.Transaction = item;
                    api_Transaction.ChargeTag = dbContext.ChargeTags.Where(m => m.TagId == item.StartTagId).FirstOrDefault();
                    api_Transaction.ChargePoint = dbContext.ChargePoints.Find(item.ChargePointId);
                    if (api_Transaction.ChargePoint != null)
                    {
                        api_Transaction.ChargeStation = dbContext.ChargeStations.Find(api_Transaction.ChargePoint.ChargeStationId);
                        api_Transaction.Owner = dbContext.Owners.Find(api_Transaction.ChargeStation.OwnerId);
                        api_Transaction.Customer = dbContext.Customers.Where(m => m.TagId == api_Transaction.Transaction.StartTagId).FirstOrDefault();

                        var getwallet = dbContext.WalletTransactions.Where(m => m.TransactionId == item.TransactionId).FirstOrDefault();
                        if (getwallet != null)
                        {
                            api_Transaction.UserApp = dbContext.UserApps.Find(getwallet.UserAppId);
                        }
                        var getUnitPrice = dbContext.Unitprices.Where(m => m.IsActive == 1).FirstOrDefault().Price;
                        decimal totalMeter = 0;
                        totalMeter = item.MeterStop != null ? Math.Abs(decimal.Parse((item.MeterStop - item.MeterStart).Value.ToString())) : 0;
                        //totalMeter = Math.Abs(decimal.Parse((item.MeterStop - item.MeterStart).Value));

                        api_Transaction.TotalPrice = Math.Round(totalMeter * getUnitPrice);
                        api_Transaction.TotalValue = totalMeter;
                        lstapi_Transactions.Add(api_Transaction);
                    }
                 
                }
                return PartialView(lstapi_Transactions);
            }
        }
        
        public IActionResult SearchDataIndex( string FromDate, string ToDate)
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                var getsplittoday = ToDate.Split('/');
                if (getsplittoday[0].Length == 1)
                {
                    getsplittoday[0] = "0" + getsplittoday[0].Length;
                }
                if (getsplittoday[1].Length == 1)
                {
                    getsplittoday[1] = "0" + getsplittoday[1].Length;
                }
                ToDate = getsplittoday[0] + "/" + getsplittoday[1] + "/" + getsplittoday[2];
                DateTime myTimeFrom = DateTime.ParseExact(FromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                DateTime myTimeTo = DateTime.ParseExact(ToDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var groupbyTransaction = dbContext.Transactions.ToList().GroupBy(m => new { m.ChargePointId, m.ConnectorId }).SelectMany(m => m.Take(1)).ToList().OrderByDescending(m => m.ChargePointId).ToList(); ;
                foreach (var item in groupbyTransaction)
                {
                    var getlist = dbContext.Transactions.Where(m => m.StartTime >= myTimeFrom && m.StartTime <= myTimeTo).Where(m => m.ChargePointId == item.ChargePointId && m.ConnectorId == item.ConnectorId).ToList();
                    item.MeterStop = Math.Round(double.Parse(getlist.Sum(m => (m.MeterStop - m.MeterStart)).ToString()), 2);
                } 
                return PartialView(groupbyTransaction);
            }
        }
        public IActionResult LoadChargeStation(int OwnerId)
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                var test = dbContext.ChargeStations.ToList();
                if (OwnerId == 1)
                {
                    var model = dbContext.ChargeStations.Where(m => m.OwnerId == OwnerId).ToList();
                    return PartialView(model);
                }
                else
                {
                    var model = dbContext.ChargeStations.ToList();
                    return PartialView(model);
                }
               // var model = dbContext.ChargeStations.Where(m=>m.OwnerId==OwnerId).ToList();
                //return PartialView(model);
            }
        }
        public IActionResult LoadChargePoint(int ChargeStationId)
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                var model = dbContext.ChargePoints.Where(m => m.ChargeStationId == ChargeStationId || ChargeStationId==0).ToList();
                return PartialView(model);
            }
        }

        //[HttpPost]
        //public IActionResult SearchChargeOrder()
        //{
        //    return PartialView();
        //}

        [Authorize]
        public IActionResult CreateNewOrder()
        {
            return View();
        }
          

    }
}
