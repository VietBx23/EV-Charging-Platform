using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FocusEV.OCPP.Database; // Thay thế bằng namespace thực tế của bạn
using FocusEV.OCPP.Management.ViewModels; // Thay thế bằng namespace thực tế của bạn
using Microsoft.Extensions.Configuration; // Để truy cập cấu hình
using System.Linq;
using System.Threading.Tasks;

namespace FocusEV.OCPP.Management.Controllers
{
    public class DepositHistoryController : Controller
    {
        private readonly IConfiguration _config;
        private const int PageSize = 10; // Số lượng bản ghi mỗi trang

        public DepositHistoryController(IConfiguration config)
        {
            _config = config;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            using (var dbContext = new OCPPCoreContext(_config))
            {
                var query = from dh in dbContext.DepositHistorys
                            join ua in dbContext.UserApps
                            on dh.UserAppId equals ua.Id
                            where dh.Message == "Nạp tiền trực tiếp từ Solar Platform"
     
                            select new DepositHistoryViewModel
                            {
                                DepositHistoryId = dh.DepositHistoryId,
                                UserAppId = dh.UserAppId,
                                UserName = ua.Fullname,
                                Amount = dh.Amount,
                                Method = dh.Method,
                                Gateway = dh.Gateway,
                                Message = dh.Message,
                                DateCreate = dh.DateCreate,
                                BalanceBeforeDeposit = ua.Balance - dh.Amount,
                                BalanceAfterDeposit = ua.Balance
                            };

                var totalCount = await query.CountAsync();
                var totalPages = (int)System.Math.Ceiling(totalCount / (double)PageSize);

                var depositHistories = await query
                    .OrderByDescending(dh => dh.DateCreate)
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(depositHistories);
            }
        }
    }
}
