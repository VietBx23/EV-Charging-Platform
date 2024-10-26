using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using FocusEV.OCPP.Database;
using FocusEV.OCPP.Management.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;
using System;
using FocusEV.OCPP.Management;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;

namespace FocusEV.OCPP.Controllers
{
    public class UserOwnerController : BaseController
    {
        private readonly IStringLocalizer<UserOwnerController> _localizer;

        public UserOwnerController(
            UserManager userManager,
            IStringLocalizer<UserOwnerController> localizer,
            ILoggerFactory loggerFactory,
            IConfiguration config) : base(userManager, loggerFactory, config)
        {
            _localizer = localizer;
            Logger = loggerFactory.CreateLogger<UserOwnerController>();
        }

        // Action to display the update view with search and pagination
        public ActionResult FocusAddDeposit()
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                try
                {
                    var userList = dbContext.UserApps.Where(m => m.OwnerId == 1).ToList();
                    ViewBag.Listuser = userList;
                    return View();
                }
                catch (Exception ex)
                {
                    // Log lỗi nếu cần
                    Console.WriteLine($"Error: {ex.Message}");
                    return View(); // Hoặc xử lý lỗi khác
                }
            }
        }



        [HttpPost]
        public async Task<bool> AddDepositFree(string Id, decimal balance)
        {
            try
            {
                string apiUrl = "https://solarev-lado-api.insitu.com.vn/api/AddDeposit_free";
                string requestBody = "{\"UserAppId\":\"" + Id + "\",\"Amount\":" + balance + ",\"PaymentGateway\":3}";

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

        public async Task<IActionResult> UpdateUserOwner(int page = 1, string search = "")
        {
            const int pageSize = 10; // Number of users per page

            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                // Query for users where OwnerId is not 1
                var usersQuery = dbContext.UserApps.Where(u => u.OwnerId != 1);

                // Apply search filter if provided
                if (!string.IsNullOrEmpty(search))
                {
                    usersQuery = usersQuery.Where(u => u.Fullname.Contains(search) || u.Email.Contains(search));
                }

                // Get total count of users after applying search filter
                var totalUsers = await usersQuery.CountAsync();

                // Get the list of users for the current page
                var users = await usersQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Get the list of users with OwnerId = 1
                var userList = dbContext.UserApps.Where(m => m.OwnerId == 1).ToList();
                ViewBag.Listuser = userList;

                // Set the pagination details and search criteria in ViewBag
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalUsers / pageSize);
                ViewBag.CurrentPage = page;
                ViewBag.Search = search;

                // Prepare the list of users for the dropdown
                ViewBag.Users = users.Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = $"{u.Email} - {u.Fullname}"
                }).ToList();

                return View(users);
            }
        }


        // Action to handle OwnerId update
        [HttpPost]
        public async Task<IActionResult> UpdateUserOwner(string userId)
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                var user = await dbContext.UserApps.FindAsync(userId);
                if (user != null)
                {
                    user.OwnerId = 1;
                    await dbContext.SaveChangesAsync();
                    TempData["Message"] = "Update successful!";
                }
                else
                {
                    TempData["Error"] = "User does not exist!";
                }
                return RedirectToAction(nameof(UpdateUserOwner));
            }
        }
    }
}
