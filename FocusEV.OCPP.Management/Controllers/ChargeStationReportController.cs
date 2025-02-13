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
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Globalization;
using OfficeOpenXml;
using System.IO;
namespace FocusEV.OCPP.Management.Controllers
{
    public class ChargeStationReportController : BaseController
    {
        private readonly IStringLocalizer<ChargeStationReportController> _localizer;
        private readonly IConfiguration _config;

        public ChargeStationReportController(
            UserManager userManager,
            IStringLocalizer<ChargeStationReportController> localizer,
            ILoggerFactory loggerFactory,
            IConfiguration config) : base(userManager, loggerFactory, config)
        {
            _localizer = localizer;
            _config = config;
            Logger = loggerFactory.CreateLogger<ChargeStationReportController>();
        }


       
        public async Task<IActionResult> ChargeStationReport( int? month, int? year, DateTime? startDate, DateTime? endDate)
        {
            // Nếu người dùng không chọn tháng hoặc năm, sử dụng tháng và năm hiện tại
            int selectedMonth = month ?? DateTime.Now.Month;
            int selectedYear = year ?? DateTime.Now.Year;
            // Lưu lại tháng và năm đã chọn vào ViewBag để sử dụng lại trong View
            ViewBag.SelectedMonth = month ?? DateTime.Now.Month; // Giá trị mặc định là tháng hiện tại
            ViewBag.SelectedYear = year ?? DateTime.Now.Year;   // Giá trị mặc định là năm hiện tại
           
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            // Lấy thông tin username của người dùng hiện tại
            string currentUsername = User.Identity.Name?.ToLower();

            using (var dbContext = new OCPPCoreContext(_config))
            {
                // Nếu không có ngày bắt đầu và kết thúc, dùng mặc định theo tháng và năm đã chọn
                DateTime actualStartDate = startDate ?? new DateTime(selectedYear, selectedMonth, 1);
                DateTime actualEndDate = endDate?.Date.AddDays(1).AddTicks(-1) ?? actualStartDate.AddMonths(1).AddDays(-1);

                // Fetch transaction data, thêm điều kiện lọc theo OwnerId nếu là goev
                var transactionData = await (from t in dbContext.Transactions
                                             join cp in dbContext.ChargePoints on t.ChargePointId equals cp.ChargePointId
                                             join cs in dbContext.ChargeStations on cp.ChargeStationId equals cs.ChargeStationId
                                             join wt in dbContext.WalletTransactions on t.TransactionId equals wt.TransactionId into wtGroup
                                             from wt in wtGroup.DefaultIfEmpty()
                                             join ua in dbContext.UserApps on wt.UserAppId equals ua.Id into uaGroup
                                             from ua in uaGroup.DefaultIfEmpty()
                                             where t.StartTime >= actualStartDate && t.StopTime <= actualEndDate
                                             && ((currentUsername == "goev" && cs.OwnerId == 5)
                                                 || (currentUsername == "adminbinhphuoc" && cs.OwnerId == 6)
                                                 || (currentUsername == "adminbinhthuan" && cs.OwnerId == 7)
                                                   || (currentUsername == "adminbinhthuan" && cs.OwnerId == 8)
                                                    || (currentUsername == "inewsolar" && cs.OwnerId == 9)

                                                 || currentUsername != "goev" && currentUsername != "adminbinhphuoc" && currentUsername!="adminbinhthuan" && currentUsername != "inewsolar" && currentUsername != "adminletsgo") // Lọc theo OwnerId nếu là goev hoặc adminbinhphuoc
                                             select new
                                             {
                                                 ChargeStationName = cs.Name,
                                                 MeterStop = t.MeterStop ?? 0,
                                                 MeterStart = t.MeterStart ?? 0,
                                                 wtAmount = wt != null ? (decimal)wt.Amount : 0m,
                                                 Fullname = ua.Fullname
                                             }).ToListAsync();


                // Tính toán summary cho giao dịch
                var transactionSummary = transactionData
                    .GroupBy(g => g.ChargeStationName)
                    .Select(g => new ChargeStationTransactionSummary
                    {
                        ChargeStationName = g.Key,
                        TotalTransactions = g.Count(),
                        LadoUseMobileTransactions = g.Count(x => x.Fullname != null && x.Fullname.ToLower().StartsWith("lado")),
                        FocusUserTransactions = g.Count(x => x.Fullname != null && x.Fullname.ToLower().StartsWith("focus")),
                        GuestUserTransactions = g.Count(x => x.Fullname != null && !x.Fullname.ToLower().StartsWith("lado") && !x.Fullname.ToLower().StartsWith("focus")),
                        LadoCardUserTransactions = g.Count(x => x.Fullname == null)
                    })
                    .OrderByDescending(x => x.TotalTransactions)
                    .ToList();

                // Tính toán kWh tiêu thụ
                var kWhSummary = transactionData
                    .GroupBy(g => g.ChargeStationName)
                    .Select(g => new ChargeStationKWhSummary
                    {
                        ChargeStationName = g.Key,
                        TotalKWhConsumedByStation = Math.Round(g.Sum(x => Math.Max(x.MeterStop - x.MeterStart, 0)), 2),
                        TotalKWhLadoMobile = Math.Round(g.Where(x => x.Fullname != null && x.Fullname.ToLower().StartsWith("lado"))
                                                          .Sum(x => Math.Max(x.MeterStop - x.MeterStart, 0)), 2),
                        TotalKWhFocus = Math.Round(g.Where(x => x.Fullname != null && x.Fullname.ToLower().StartsWith("focus"))
                                                     .Sum(x => Math.Max(x.MeterStop - x.MeterStart, 0)), 2),
                        TotalKWhGuest = Math.Round(g.Where(x => x.Fullname != null && !x.Fullname.ToLower().StartsWith("lado") && !x.Fullname.ToLower().StartsWith("focus"))
                                                     .Sum(x => Math.Max(x.MeterStop - x.MeterStart, 0)), 2),
                        TotalKWhLadoCard = Math.Round(g.Where(x => x.Fullname == null)
                                                        .Sum(x => Math.Max(x.MeterStop - x.MeterStart, 0)), 2)
                    })
                    .OrderByDescending(x => x.TotalKWhConsumedByStation)
                    .ToList();

                // Tính toán tổng tiền
                var unitPrice = await dbContext.Unitprices.Where(up => up.IsActive == 1).Select(up => (double)up.Price).FirstOrDefaultAsync();

                var amountSummary = transactionData
                    .GroupBy(g => g.ChargeStationName)
                    .Select(grouped => new ChargeStationAmountSummary
                    {
                        ChargeStationName = grouped.Key,
                        TotalAmount = grouped.Sum(g => (double)g.wtAmount +
                            (g.Fullname == null
                                ? Math.Max(0, (double)((g.MeterStop - g.MeterStart) * unitPrice))
                                : 0))
                            .ToString("N0", CultureInfo.CreateSpecificCulture("vi-VN")) + " VNĐ",
                        TotalAmountLadoMobile = grouped.Sum(g => g.Fullname != null && g.Fullname.ToLower().StartsWith("lado")
                            ? (double)g.wtAmount
                            : 0)
                            .ToString("N0", CultureInfo.CreateSpecificCulture("vi-VN")) + " VNĐ",
                        TotalAmountFocus = grouped.Sum(g => g.Fullname != null && g.Fullname.ToLower().StartsWith("focus")
                            ? (double)g.wtAmount
                            : 0)
                            .ToString("N0", CultureInfo.CreateSpecificCulture("vi-VN")) + " VNĐ",
                        TotalAmountGuest = grouped.Sum(g => g.Fullname != null && !g.Fullname.ToLower().StartsWith("lado") && !g.Fullname.ToLower().StartsWith("focus")
                            ? (double)g.wtAmount
                            : 0)
                            .ToString("N0", CultureInfo.CreateSpecificCulture("vi-VN")) + " VNĐ",
                        TotalAmountLadoCard = grouped.Sum(g => g.Fullname == null
                            ? (double)(g.MeterStop - g.MeterStart) * unitPrice
                            : 0)
                            .ToString("N0", CultureInfo.CreateSpecificCulture("vi-VN")) + " VNĐ"
                    })
                    .OrderByDescending(x => double.Parse(x.TotalAmount.Replace(" VNĐ", ""), NumberStyles.AllowThousands, CultureInfo.CreateSpecificCulture("vi-VN")))
                    .ToList();

                // Lưu lại giá trị vào ViewBag để sử dụng trong View
                ViewBag.StartDate = actualStartDate.ToString("yyyy-MM-dd");
                ViewBag.EndDate = actualEndDate.ToString("yyyy-MM-dd");

                var reportModel = new ChargeStationReportModel
                {
                    TransactionSummaries = transactionSummary,
                    KWhSummaries = kWhSummary,
                    AmountSummaries = amountSummary
                    
                };

                return View(reportModel);
            }
        }




        public async Task<IActionResult> ExportToExcel(int? month, int? year)
        {
            int selectedMonth = month ?? DateTime.Now.Month;
            int selectedYear = year ?? DateTime.Now.Year;

            using (var dbContext = new OCPPCoreContext(_config))
            {
                DateTime startDate = new DateTime(selectedYear, selectedMonth, 1);
                DateTime endDate = startDate.AddMonths(1).AddDays(-1);

                // Fetch transaction data
                var transactionData = await (from t in dbContext.Transactions
                                             join cp in dbContext.ChargePoints on t.ChargePointId equals cp.ChargePointId
                                             join cs in dbContext.ChargeStations on cp.ChargeStationId equals cs.ChargeStationId
                                             join wt in dbContext.WalletTransactions on t.TransactionId equals wt.TransactionId into wtGroup
                                             from wt in wtGroup.DefaultIfEmpty()
                                             join ua in dbContext.UserApps on wt.UserAppId equals ua.Id into uaGroup
                                             from ua in uaGroup.DefaultIfEmpty()
                                             where t.StartTime >= startDate && t.StopTime <= endDate
                                             select new
                                             {
                                                 ChargeStationName = cs.Name,
                                                 MeterStop = t.MeterStop ?? 0,
                                                 MeterStart = t.MeterStart ?? 0,
                                                 wtAmount = wt != null ? (decimal)wt.Amount : 0m,
                                                 Fullname = ua.Fullname
                                             }).ToListAsync();

                var transactionSummary = transactionData
                    .GroupBy(g => g.ChargeStationName)
                    .Select(g => new ChargeStationTransactionSummary
                    {
                        ChargeStationName = g.Key,
                        TotalTransactions = g.Count(),
                        LadoUseMobileTransactions = g.Count(x => x.Fullname != null && x.Fullname.ToLower().StartsWith("lado")),
                        FocusUserTransactions = g.Count(x => x.Fullname != null && x.Fullname.ToLower().StartsWith("focus")),
                        GuestUserTransactions = g.Count(x => x.Fullname != null && !x.Fullname.ToLower().StartsWith("lado") && !x.Fullname.ToLower().StartsWith("focus")),
                        LadoCardUserTransactions = g.Count(x => x.Fullname == null)
                    })
                    .ToList();

                var kWhSummary = transactionData
                    .GroupBy(g => g.ChargeStationName)
                    .Select(g => new ChargeStationKWhSummary
                    {
                        ChargeStationName = g.Key,
                        TotalKWhConsumedByStation = Math.Round(g.Sum(x => Math.Max(x.MeterStop - x.MeterStart, 0)), 2),
                        TotalKWhLadoMobile = Math.Round(g.Where(x => x.Fullname != null && x.Fullname.ToLower().StartsWith("lado"))
                                                                .Sum(x => Math.Max(x.MeterStop - x.MeterStart, 0)), 2),
                        TotalKWhFocus = Math.Round(g.Where(x => x.Fullname != null && x.Fullname.ToLower().StartsWith("focus"))
                                                                .Sum(x => Math.Max(x.MeterStop - x.MeterStart, 0)), 2),
                        TotalKWhGuest = Math.Round(g.Where(x => x.Fullname != null && !x.Fullname.ToLower().StartsWith("lado") && !x.Fullname.ToLower().StartsWith("focus"))
                                                                .Sum(x => Math.Max(x.MeterStop - x.MeterStart, 0)), 2),
                        TotalKWhLadoCard = Math.Round(g.Where(x => x.Fullname == null)
                                                                .Sum(x => Math.Max(x.MeterStop - x.MeterStart, 0)), 2)
                    })
                    .ToList();

                var unitPrice = await dbContext.Unitprices.Where(up => up.IsActive == 1).Select(up => (double)up.Price).FirstOrDefaultAsync();

                var amountSummary = transactionData
                    .GroupBy(g => g.ChargeStationName)
                    .Select(grouped => new ChargeStationAmountSummary
                    {
                        ChargeStationName = grouped.Key,
                        TotalAmount = grouped.Sum(g => (double)g.wtAmount +
                            (g.Fullname == null
                                ? Math.Max(0, (double)((g.MeterStop - g.MeterStart) * unitPrice))
                                : 0))
                            .ToString("N0", CultureInfo.CreateSpecificCulture("vi-VN")) + " VNĐ",
                        TotalAmountLadoMobile = grouped.Sum(g => g.Fullname != null && g.Fullname.ToLower().StartsWith("lado")
                            ? (double)g.wtAmount
                            : 0)
                            .ToString("N0", CultureInfo.CreateSpecificCulture("vi-VN")) + " VNĐ",
                        TotalAmountFocus = grouped.Sum(g => g.Fullname != null && g.Fullname.ToLower().StartsWith("focus")
                            ? (double)g.wtAmount
                            : 0)
                            .ToString("N0", CultureInfo.CreateSpecificCulture("vi-VN")) + " VNĐ",
                        TotalAmountGuest = grouped.Sum(g => g.Fullname != null && !g.Fullname.ToLower().StartsWith("lado") && !g.Fullname.ToLower().StartsWith("focus")
                            ? (double)g.wtAmount
                            : 0)
                            .ToString("N0", CultureInfo.CreateSpecificCulture("vi-VN")) + " VNĐ",
                        TotalAmountLadoCard = grouped.Sum(g => g.Fullname == null
                            ? Math.Max(0, (double)((g.MeterStop - g.MeterStart) * unitPrice))
                            : 0)
                            .ToString("N0", CultureInfo.CreateSpecificCulture("vi-VN")) + " VNĐ"
                    })
                    .ToList();

                // Exporting to Excel
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    // Sheet 1: Transaction Summary
                    var worksheet1 = package.Workbook.Worksheets.Add("Transaction Summary");
                    worksheet1.Cells[1, 1].Value = "Charge Station Name";
                    worksheet1.Cells[1, 2].Value = "Total Transactions";
                    worksheet1.Cells[1, 3].Value = "Lado Mobile Transactions";
                    worksheet1.Cells[1, 4].Value = "Focus Transactions";
                    worksheet1.Cells[1, 5].Value = "Guest Transactions";
                    worksheet1.Cells[1, 6].Value = "Lado Card Transactions";
                    for (int i = 0; i < transactionSummary.Count; i++)
                    {
                        worksheet1.Cells[i + 2, 1].Value = transactionSummary[i].ChargeStationName;
                        worksheet1.Cells[i + 2, 2].Value = transactionSummary[i].TotalTransactions;
                        worksheet1.Cells[i + 2, 3].Value = transactionSummary[i].LadoUseMobileTransactions;
                        worksheet1.Cells[i + 2, 4].Value = transactionSummary[i].FocusUserTransactions;
                        worksheet1.Cells[i + 2, 5].Value = transactionSummary[i].GuestUserTransactions;
                        worksheet1.Cells[i + 2, 6].Value = transactionSummary[i].LadoCardUserTransactions;
                    }
                    worksheet1.Cells[worksheet1.Dimension.Address].AutoFitColumns();

                    // Sheet 2: KWh Summary
                    var worksheet2 = package.Workbook.Worksheets.Add("KWh Summary");
                    worksheet2.Cells[1, 1].Value = "Charge Station Name";
                    worksheet2.Cells[1, 2].Value = "Total KWh Consumed";
                    worksheet2.Cells[1, 3].Value = "Lado Mobile KWh";
                    worksheet2.Cells[1, 4].Value = "Focus KWh";
                    worksheet2.Cells[1, 5].Value = "Guest KWh";
                    worksheet2.Cells[1, 6].Value = "Lado Card KWh";
                    for (int i = 0; i < kWhSummary.Count; i++)
                    {
                        worksheet2.Cells[i + 2, 1].Value = kWhSummary[i].ChargeStationName;
                        worksheet2.Cells[i + 2, 2].Value = kWhSummary[i].TotalKWhConsumedByStation;
                        worksheet2.Cells[i + 2, 3].Value = kWhSummary[i].TotalKWhLadoMobile;
                        worksheet2.Cells[i + 2, 4].Value = kWhSummary[i].TotalKWhFocus;
                        worksheet2.Cells[i + 2, 5].Value = kWhSummary[i].TotalKWhGuest;
                        worksheet2.Cells[i + 2, 6].Value = kWhSummary[i].TotalKWhLadoCard;
                    }
                    worksheet2.Cells[worksheet2.Dimension.Address].AutoFitColumns();

                    // Sheet 3: Amount Summary
                    var worksheet3 = package.Workbook.Worksheets.Add("Amount Summary");
                    worksheet3.Cells[1, 1].Value = "Charge Station Name";
                    worksheet3.Cells[1, 2].Value = "Total Amount";
                    worksheet3.Cells[1, 3].Value = "Lado Mobile Amount";
                    worksheet3.Cells[1, 4].Value = "Focus Amount";
                    worksheet3.Cells[1, 5].Value = "Guest Amount";
                    worksheet3.Cells[1, 6].Value = "Lado Card Amount";
                    for (int i = 0; i < amountSummary.Count; i++)
                    {
                        worksheet3.Cells[i + 2, 1].Value = amountSummary[i].ChargeStationName;
                        worksheet3.Cells[i + 2, 2].Value = amountSummary[i].TotalAmount;
                        worksheet3.Cells[i + 2, 3].Value = amountSummary[i].TotalAmountLadoMobile;
                        worksheet3.Cells[i + 2, 4].Value = amountSummary[i].TotalAmountFocus;
                        worksheet3.Cells[i + 2, 5].Value = amountSummary[i].TotalAmountGuest;
                        worksheet3.Cells[i + 2, 6].Value = amountSummary[i].TotalAmountLadoCard;
                    }
                    worksheet3.Cells[worksheet3.Dimension.Address].AutoFitColumns();

                    // Return Excel file
                    var stream = new MemoryStream(package.GetAsByteArray());
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ChargeStationReport.xlsx");
                }
            }
        }


    }
}












