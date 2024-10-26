using FocusEV.OCPP.Database;
using FocusEV.OCPP.Management.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FocusEV.OCPP.Management.Controllers
{
    public class DailyConsumptionController : BaseController
    {
        private readonly ILogger<DailyConsumptionController> _logger;
        private readonly IConfiguration _config;

        public DailyConsumptionController(
            UserManager userManager,
            ILoggerFactory loggerFactory,
            IConfiguration config) : base(userManager, loggerFactory, config)
        {
            _logger = loggerFactory.CreateLogger<DailyConsumptionController>();
            _config = config;
        }

        public ActionResult Index(string chargePointId, int? month, int? year)
        {
            int selectedMonth = month ?? DateTime.Now.Month;
            int selectedYear = year ?? DateTime.Now.Year;

            using (var dbContext = new OCPPCoreContext(_config))
            {
                // Lấy danh sách các trụ sạc
                var chargePoints = dbContext.Transactions
                    .Select(t => t.ChargePointId)
                    .Distinct()
                    .OrderBy(id => id)
                    .ToList();

                ViewBag.ChargePoints = chargePoints;
                ViewBag.ChargePointId = chargePointId;
                ViewBag.SelectedMonth = selectedMonth;
                ViewBag.SelectedYear = selectedYear;

                if (string.IsNullOrEmpty(chargePointId) || !month.HasValue || !year.HasValue)
                {
                    return View(new List<DailyConsumption>());
                }

                var startDate = new DateTime(selectedYear, selectedMonth, 1);
                var endDate = startDate.AddMonths(1);

                var transactions = dbContext.Transactions
                    .Where(t => t.StartTime >= startDate && t.StopTime < endDate && t.ChargePointId == chargePointId)
                    .ToList();

                var dailyConsumptions = transactions
                    .GroupBy(t => t.StartTime.Date)
                    .Select(g => new DailyConsumption
                    {
                        ChargingDate = g.Key,
                        StartkWh_Sung1 = g.Where(x => x.ConnectorId == 1).Sum(x => (decimal?)x.MeterStart) ?? 0,
                        EndkWh_Sung1 = g.Where(x => x.ConnectorId == 1).Sum(x => (decimal?)x.MeterStop) ?? 0,
                        StartkWh_Sung2 = g.Where(x => x.ConnectorId == 2).Sum(x => (decimal?)x.MeterStart) ?? 0,
                        EndkWh_Sung2 = g.Where(x => x.ConnectorId == 2).Sum(x => (decimal?)x.MeterStop) ?? 0,
                        kWhUsed_Sung1 = g.Where(x => x.ConnectorId == 1).Sum(x => (decimal?)(x.MeterStop - x.MeterStart)) ?? 0,
                        kWhUsed_Sung2 = g.Where(x => x.ConnectorId == 2).Sum(x => (decimal?)(x.MeterStop - x.MeterStart)) ?? 0,
                        TotalkWhUsed = g.Sum(x => (decimal?)(x.MeterStop - x.MeterStart)) ?? 0
                    })
                    .OrderBy(d => d.ChargingDate)
                    .ToList();

                return View(dailyConsumptions);
            }
        }

        public ActionResult ExportToExcel(string chargePointId, int? month, int? year)
        {
            using (var dbContext = new OCPPCoreContext(_config))
            {
                var startDate = new DateTime(year.Value, month.Value, 1);
                var endDate = startDate.AddMonths(1);

                var transactions = dbContext.Transactions
                    .Where(t => t.StartTime >= startDate && t.StopTime < endDate && t.ChargePointId == chargePointId)
                    .ToList();

                var dailyConsumptions = transactions
                    .GroupBy(t => t.StartTime.Date)
                    .Select(g => new DailyConsumption
                    {
                        ChargingDate = g.Key,
                        StartkWh_Sung1 = g.Where(x => x.ConnectorId == 1).Sum(x => (decimal?)x.MeterStart) ?? 0,
                        EndkWh_Sung1 = g.Where(x => x.ConnectorId == 1).Sum(x => (decimal?)x.MeterStop) ?? 0,
                        StartkWh_Sung2 = g.Where(x => x.ConnectorId == 2).Sum(x => (decimal?)x.MeterStart) ?? 0,
                        EndkWh_Sung2 = g.Where(x => x.ConnectorId == 2).Sum(x => (decimal?)x.MeterStop) ?? 0,
                        kWhUsed_Sung1 = g.Where(x => x.ConnectorId == 1).Sum(x => (decimal?)(x.MeterStop - x.MeterStart)) ?? 0,
                        kWhUsed_Sung2 = g.Where(x => x.ConnectorId == 2).Sum(x => (decimal?)(x.MeterStop - x.MeterStart)) ?? 0,
                        TotalkWhUsed = g.Sum(x => (decimal?)(x.MeterStop - x.MeterStart)) ?? 0
                    })
                    .OrderBy(d => d.ChargingDate)
                    .ToList();

                // Tạo file Excel
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Daily Consumption");

                    // Thêm tiêu đề cột
                    worksheet.Cells[1, 1].Value = "Charging Date";
                    worksheet.Cells[1, 2].Value = "Start kWh (Sung 1)";
                    worksheet.Cells[1, 3].Value = "End kWh (Sung 1)";
                    worksheet.Cells[1, 4].Value = "Start kWh (Sung 2)";
                    worksheet.Cells[1, 5].Value = "End kWh (Sung 2)";
                    worksheet.Cells[1, 6].Value = "kWh Used (Sung 1)";
                    worksheet.Cells[1, 7].Value = "kWh Used (Sung 2)";
                    worksheet.Cells[1, 8].Value = "Total kWh Used";

                    // Thêm dữ liệu
                    int row = 2;
                    foreach (var item in dailyConsumptions)
                    {
                        worksheet.Cells[row, 1].Value = item.ChargingDate.ToShortDateString();
                        worksheet.Cells[row, 2].Value = item.StartkWh_Sung1;
                        worksheet.Cells[row, 3].Value = item.EndkWh_Sung1;
                        worksheet.Cells[row, 4].Value = item.StartkWh_Sung2;
                        worksheet.Cells[row, 5].Value = item.EndkWh_Sung2;
                        worksheet.Cells[row, 6].Value = item.kWhUsed_Sung1;
                        worksheet.Cells[row, 7].Value = item.kWhUsed_Sung2;
                        worksheet.Cells[row, 8].Value = item.TotalkWhUsed;
                        row++;
                    }

                    // Định dạng file Excel
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    // Trả file Excel về client
                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;
                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DailyConsumptionReport.xlsx");
                }
            }
        }




    }
}
