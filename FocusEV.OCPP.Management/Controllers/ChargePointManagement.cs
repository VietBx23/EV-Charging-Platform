    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using System.Linq;
    using System.Threading.Tasks;
    using FocusEV.OCPP.Database;
    using Microsoft.EntityFrameworkCore;
using System;


    namespace FocusEV.OCPP.Management.Controllers
    {
    public class ChargePointManagementController : BaseController
    {
        private readonly IStringLocalizer<ChargePointManagementController> _localizer;
        private readonly IConfiguration _config;

        public ChargePointManagementController(
            UserManager userManager,
            IStringLocalizer<ChargePointManagementController> localizer,
            ILoggerFactory loggerFactory,
            IConfiguration config) : base(userManager, loggerFactory, config)
        {
            _localizer = localizer;
            _config = config;
            Logger = loggerFactory.CreateLogger<ChargePointManagementController>();
        }

        public async Task<IActionResult> Index(int? month, int? year)
        {
            // Sử dụng tháng và năm hiện tại nếu không có giá trị nào được truyền vào
            int selectedMonth = month ?? DateTime.Now.Month;
            int selectedYear = year ?? DateTime.Now.Year;

            using (var dbContext = new OCPPCoreContext(_config))
            {
                // Xác định ngày bắt đầu và kết thúc của tháng
                DateTime startDate = new DateTime(selectedYear, selectedMonth, 1);
                DateTime endDate = startDate.AddMonths(1).AddDays(-1);

                var transactions = await dbContext.Transactions
                    .Where(t => t.StartTime >= startDate && t.StopTime <= endDate && (t.ConnectorId == 1 || t.ConnectorId == 2))
                    .GroupBy(t => t.ChargePointId)
                    .Select(g => new ChargePointStatisticsViewModel
                    {
                        ChargePointId = g.Key,
                        kWh_Connector1 = g.Where(t => t.ConnectorId == 1)
                  .Sum(t => (t.MeterStop ?? 0) - (t.MeterStart ?? 0)),
                        kWh_Connector2 = g.Where(t => t.ConnectorId == 2)
                  .Sum(t => (t.MeterStop ?? 0) - (t.MeterStart ?? 0)),
                        Total_kWhUsed = g.Sum(t => (t.MeterStop ?? 0) - (t.MeterStart ?? 0))

                    })
                    .OrderBy(cp => cp.ChargePointId) // Sắp xếp theo ChargePointId
                    .ToListAsync();

                // Truyền giá trị tháng và năm đã chọn vào ViewBag để giữ giá trị đã chọn
                ViewBag.SelectedMonth = selectedMonth;
                ViewBag.SelectedYear = selectedYear;

                return View(transactions);
            }
        }
    }
    }
