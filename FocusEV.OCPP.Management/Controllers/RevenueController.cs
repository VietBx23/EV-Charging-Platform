using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using FocusEV.OCPP.Database;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using FocusEV.OCPP.Management.Models;
using System;




namespace FocusEV.OCPP.Management.Controllers
{
    public class RevenueController : BaseController
    {
        private readonly IStringLocalizer<RevenueController> _localizer;
        private readonly IConfiguration _config;

        public RevenueController(
            UserManager userManager,
            IStringLocalizer<RevenueController> localizer,
            ILoggerFactory loggerFactory,
            IConfiguration config) : base(userManager, loggerFactory, config)
        {
            _localizer = localizer;
            _config = config;
            Logger = loggerFactory.CreateLogger<RevenueController>();
        }

        public async Task<IActionResult> Index(int month = 0, int year = 0)
        {
            // Đặt giá trị mặc định nếu month hoặc year không được cung cấp
            if (month == 0)
            {
                month = DateTime.Now.Month;
            }

            if (year == 0)
            {
                year = DateTime.Now.Year;
            }
            ViewBag.Month = month;
            ViewBag.Year = year;
            using (var dbContext = new OCPPCoreContext(_config))
            {
                var dailyRevenues = await dbContext.DepositHistorys
                    .Join(dbContext.UserApps,
                          dh => dh.UserAppId,
                          ua => ua.Id,
                          (dh, ua) => new { dh, ua })
                    .Where(x => x.dh.Message == "Nạp tiền từ VNPay"
                                && x.dh.DateCreate.Year == year
                                && x.dh.DateCreate.Month == month
                                && x.dh.BankTransStatus == 0)
                    .GroupBy(x => x.dh.DateCreate.Date)
                    .Select(g => new DailyRevenueViewModel
                    {
                        TransactionDate = g.Key,
                        TotalRevenue = g.Sum(x => x.dh.Amount).ToString("N0", new CultureInfo("vi-VN")) + " VNĐ"
                    })
                    .OrderByDescending(x => x.TransactionDate)
                    .ToListAsync();

                return View(dailyRevenues);
            }
        }

        public async Task<IActionResult> MonthlyRevenue(int year = 0)
        {
            if (year == 0)
            {
                year = DateTime.Now.Year;
            }

            ViewBag.Year = year;

            using (var dbContext = new OCPPCoreContext(_config))
            {
                var data = await dbContext.DepositHistorys
                    .Join(dbContext.UserApps,
                          dh => dh.UserAppId,
                          ua => ua.Id,
                          (dh, ua) => new { dh, ua })
                    .Where(x => x.dh.Message == "Nạp tiền từ VNPay"
                                && x.dh.DateCreate.Year == year
                                && x.dh.BankTransStatus == 0)
                    .GroupBy(x => new { x.dh.DateCreate.Month, x.dh.DateCreate.Year })
                    .Select(g => new
                    {
                        g.Key.Month,
                        g.Key.Year,
                        TotalAmount = g.Sum(x => x.dh.Amount)
                    })
                    .ToListAsync();

                var monthlyRevenues = data
                    .Select(g => new MonthlyRevenueData
                    {
                        MonthNumber = (int)g.Month, // Đảm bảo rằng giá trị được gán đúng
                        MonthNameFormatted = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Month),
                        TotalRevenue = g.TotalAmount.ToString("N0", new CultureInfo("vi-VN")) + " VNĐ"
                    })
                    .OrderBy(x => x.MonthNumber)  // Sắp xếp theo số tháng
                    .ToList();




                ViewData["SelectedYear"] = year;
                return View(monthlyRevenues);
            }
        }




    }
}
