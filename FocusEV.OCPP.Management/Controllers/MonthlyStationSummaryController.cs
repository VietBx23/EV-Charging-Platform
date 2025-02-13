using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FocusEV.OCPP.Database;
using FocusEV.OCPP.Management.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Collections.Generic;




namespace FocusEV.OCPP.Management.Controllers
{
    public class MonthlyStationSummaryController : BaseController
    {
        private readonly IStringLocalizer<MonthlyStationSummaryController> _localizer;
        private readonly IConfiguration _config;

        public MonthlyStationSummaryController(
            UserManager userManager,
            IStringLocalizer<MonthlyStationSummaryController> localizer,
            ILoggerFactory loggerFactory,
            IConfiguration config) : base(userManager, loggerFactory, config)
        {
            _localizer = localizer;
            _config = config;
            Logger = loggerFactory.CreateLogger<MonthlyStationSummaryController>();
        }
        private async Task<List<MonthlyStationSummaryModel>> GetMonthlySummaryData(int year)
        {
            using (var dbContext = new OCPPCoreContext(_config))
            {
                return await (
                    from t in dbContext.Transactions
                    join up in dbContext.Unitprices on 1 equals up.IsActive // Join với điều kiện IsActive = 1
                    where t.StartTime.Year == year
                    group new { t, up } by new { Month = t.StartTime.Month } into g
                    orderby g.Key.Month
                    select new MonthlyStationSummaryModel
                    {
                        Month = g.Key.Month,
                        TotalTransactions = g.Count(),
                        TotalRevenue = (double)g.Sum(x => (decimal)((x.t.MeterStop - x.t.MeterStart) ?? 0) * x.up.Price)
                    }).ToListAsync();
            }
        }
        private async Task<List<MonthlyStationSummaryModel>> GetTotalVnpayRevenueSummary(int year)
        {
            using (var dbContext = new OCPPCoreContext(_config))
            {
                return await dbContext.DepositHistorys
                    .Where(t => t.DateCreate.Year == year && t.Message == "Nạp tiền từ VNPay" && t.BankTransStatus == 0)
                    .GroupBy(t => t.DateCreate.Month)
                    .Select(g => new MonthlyStationSummaryModel
                    {
                        Month = g.Key,
                        TotalVnpayRevenue = (double)g.Sum(t => t.Amount) // Tổng doanh thu từ VNPay
                    })
                    .ToListAsync();
            }
        }

       private async Task<List<TopChargingStationModel>> GetTop5ChargingStations(int year, int month)
        {
            using (var dbContext = new OCPPCoreContext(_config)) 
            {
                return await (
                    from t in dbContext.Transactions
                    join cp in dbContext.ChargePoints on t.ChargePointId equals cp.ChargePointId
                    join cs in dbContext.ChargeStations on cp.ChargeStationId equals cs.ChargeStationId
                    join up in dbContext.Unitprices on 1 equals up.IsActive
                    where t.StartTime.Year == year && t.StartTime.Month == month
                    group new { t, up } by new { cs.ChargeStationId, cs.Name } into g
                    orderby g.Sum(x => (decimal)((x.t.MeterStop - x.t.MeterStart) ?? 0) * x.up.Price) descending
                    select new TopChargingStationModel
                    {
                        ChargeStationName = g.Key.Name,
                        TotalRevenue =  (double)g.Sum(x => (decimal)((x.t.MeterStop - x.t.MeterStart) ?? 0) * x.up.Price)
                    })
                    .Take(5)
                    .ToListAsync();
                    
            } 
            
        }

        private async Task<List<TopUserConsumptionModel>> GetTop5UserConsumption(int year, int month)
        {
            using (var dbContext = new OCPPCoreContext(_config))
            {
                return await (
                    from wt in dbContext.WalletTransactions
                    join ua in dbContext.UserApps on wt.UserAppId equals ua.Id
                    join t in dbContext.Transactions on wt.TransactionId equals t.TransactionId
                    join cp in dbContext.ChargePoints on t.ChargePointId equals cp.ChargePointId
                    join up in dbContext.Unitprices on 1 equals up.IsActive
                    where t.StartTime.Year == year && t.StartTime.Month == month
                    group new { t, up, ua.UserName, ua.Fullname } by new { ua.UserName, ua.Fullname } into g
                    orderby g.Sum(x => (decimal)((x.t.MeterStop - x.t.MeterStart) ?? 0)) descending
                    select new TopUserConsumptionModel
                    {
                        UserName = g.Key.UserName,
                        Fullname = g.Key.Fullname,
                        TotalkWhUsed = g.Sum(x => (decimal)((x.t.MeterStop - x.t.MeterStart) ?? 0)),
                        TotalAmount = string.Format("{0:#,##0}", g.Sum(x => (decimal)((x.t.MeterStop - x.t.MeterStart) ?? 0) * x.up.Price))
                    })
                    .Take(5) // Chỉ lấy 5 người dùng có số kWh sử dụng nhiều nhất
                    .ToListAsync();
            }
        }

        public async Task<List<DailyRegistration>> DailyRegistrations(int year, int month)
        {
            using (var dbContext = new OCPPCoreContext(_config))
            {
                // Tạo ngày bắt đầu và kết thúc của tháng
                DateTime startDate = new DateTime(year, month, 1);
                DateTime endDate = startDate.AddMonths(1).AddDays(-1);

                var dailyRegistrations = await dbContext.UserApps
                    .Where(u => u.CreateDate >= startDate && u.CreateDate <= endDate)
                    .GroupBy(u => u.CreateDate.Day)
                    .Select(g => new DailyRegistration
                    {
                        Day = g.Key,
                        TotalRegistrations = g.Count()
                    })
                    //.OrderByDescending(r => r.Day)
                    .ToListAsync();

                return dailyRegistrations;
            }
        }




        private async Task<int> GetTotalChargingStations()
        {
            using (var dbContext = new OCPPCoreContext(_config))
            {
                return await dbContext.ChargeStations.CountAsync();
            }
        }


        private async Task<int> getTotalChargePoints()
        {
            using(var dbContext = new OCPPCoreContext(_config))
            {
                return await dbContext.ChargePoints.CountAsync();
            }    
        }

        private async Task<int> getTotalUserApps()
        {
            using (var dbContext = new OCPPCoreContext(_config))
            {
                return await dbContext.UserApps.CountAsync();
            }
        }
        private async Task<int> getTotalOwners()
        {
            using (var dbContext = new OCPPCoreContext(_config))
            {
                return await dbContext.Owners.CountAsync();
            }
        }




        public async Task<IActionResult> Index(int? year, int? month, int? yearDaily, int? monthDaily)
        {
            int selectedYear = year ?? DateTime.Now.Year;
            int selectedMonth = month ?? DateTime.Now.Month;
            int selectedYearDaily = yearDaily ?? DateTime.Now.Year;
            int selectedMonthDaily = monthDaily ?? DateTime.Now.Month;

            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYearDaily = selectedYearDaily;
            ViewBag.SelectedMonthDaily = selectedMonthDaily;

            // Lấy dữ liệu tổng hợp hàng tháng
            var monthlySummaryData = await GetMonthlySummaryData(selectedYear);
            var vnpayRevenueSummary = await GetTotalVnpayRevenueSummary(selectedYear);

            // Hợp nhất dữ liệu doanh thu VNPay vào dữ liệu tổng hợp hàng tháng
            foreach (var summary in monthlySummaryData)
            {
                var vnpayData = vnpayRevenueSummary.FirstOrDefault(v => v.Month == summary.Month);
                if (vnpayData != null)
                {
                    summary.TotalVnpayRevenue = vnpayData.TotalVnpayRevenue;
                }
            }

            // Lấy dữ liệu đăng ký hàng ngày theo tháng và năm đã chọn
            var dailyRegistrations = await DailyRegistrations(selectedYearDaily, selectedMonthDaily);
            ViewBag.DailyRegistrations = dailyRegistrations;

            // Lấy thông tin Top 5 trạm sạc và người dùng có tiêu thụ cao nhất
            ViewBag.Top5ChargingStations = await GetTop5ChargingStations(DateTime.Now.Year, DateTime.Now.Month);
            ViewBag.Top5UserConsumption = await GetTop5UserConsumption(DateTime.Now.Year, DateTime.Now.Month);

            // Lấy tổng số trạm sạc, trụ sạc, người dùng, chủ đầu tư
            ViewBag.TotalChargingStations = await GetTotalChargingStations();
            ViewBag.TotalChargePoints = await getTotalChargePoints();
            ViewBag.TotalUserApps = await getTotalUserApps();
            ViewBag.TotalOwners = await getTotalOwners();

            return View("Index", monthlySummaryData);
        }


    }
}
