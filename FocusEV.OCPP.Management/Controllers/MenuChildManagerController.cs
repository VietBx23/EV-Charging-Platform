
using System;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

using System.Threading.Tasks;
using FocusEV.OCPP.Database;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using FocusEV.OCPP.Management.Models;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;

namespace FocusEV.OCPP.Management.Controllers
{
    public class MenuChildManagerController : BaseController
    {
        private readonly IStringLocalizer<MenuChildManagerController> _localization;
        private readonly IConfiguration _config;
        private readonly ILogger<MenuChildManagerController> _logger;

        public MenuChildManagerController(
            UserManager userManager,
            IStringLocalizer<MenuChildManagerController> localizer,
            ILoggerFactory loggerFactory,
            IConfiguration config) : base(userManager, loggerFactory, config)
        {
            _localization = localizer;
            _config = config;
            Logger = loggerFactory.CreateLogger<MenuChildManagerController>();

        }


        public async Task<IActionResult> Index()
        {
            try
            {
                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {

                    var menuChilds = await dbContext.MenuChilds.ToListAsync();
                    var model = new MenuChildViewModel
                    {
                        MenuChilds = menuChilds
                    };
                    return View(model);

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading menu childs.");
                return RedirectToAction("Error", new { Id = "" });
            }
        }
        //save and update Save and Update Menu Childs 
        [HttpPost]
        public async Task<IActionResult> SaveAndUpdateMenuChilds(MenuChildViewModel model)
        {
            try
            {
                using (OCPPCoreContext dbcontext = new OCPPCoreContext(this.Config))
                {
                    var menuChild = await dbcontext.MenuChilds.FirstOrDefaultAsync(m => m.MenuchildId == model.MenuchildId);

                    if (ModelState.IsValid)
                    {
                        if (menuChild != null)
                        {

                            menuChild.MenuChildName = model.MenuChildName;
                            menuChild.ParentId = model.ParentId;
                            menuChild.Url = model.Url;

                            dbcontext.MenuChilds.Update(menuChild);
                        }
                        else
                        {
                            var newMenuChild = new MenuChild
                            {
                                MenuchildId = model.MenuchildId,
                                MenuChildName = model.MenuChildName,
                                ParentId = model.ParentId,
                                Url = model.Url
                            };

                            dbcontext.MenuChilds.Add(newMenuChild);

                        }
                        await dbcontext.SaveChangesAsync();
                        return RedirectToAction("Index");
                    }
                    var menuChilds = await dbcontext.MenuChilds.ToListAsync();
                    model.MenuChilds = menuChilds;
                    return View("Index", menuChilds);

                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving or updating menu childs.");
                return View("Index");
            }
        }


        // delete menu childs 
        [HttpPost]

        public async Task<IActionResult> DeleteMenuChild(int id)
        {
            try
            {
                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {
                    var menuChild = await dbContext.MenuChilds.FirstOrDefaultAsync(m => m.MenuchildId == id);
                    if (menuChild != null)
                    {
                        dbContext.MenuChilds.Remove(menuChild);
                        await dbContext.SaveChangesAsync();
                        return Json(new { success = true });
                    }
                    else
                    {
                        return Json(new { success = false, message = "MenuChild not found. " });
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting menu childs.");
                return Json(new { success = false, message = ex.Message });
            }
        }


        // eidt Menu Childs 


        public async Task<IActionResult> EditMenuChild(int id)
        {
            try
            {
                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {
                    var menuChild = await dbContext.MenuChilds.FirstOrDefaultAsync(m => m.MenuchildId == id);

                    if (menuChild == null)
                    {
                        return NotFound();
                    }

                    var model = new MenuChildViewModel
                    {
                        MenuchildId = menuChild.MenuchildId,
                        MenuChildName = menuChild.MenuChildName,
                        ParentId = menuChild.ParentId,
                        Url = menuChild.Url

                    };
                    return Json(model);

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Error retrieving menu child data");
                return StatusCode(500, "Internal Server Error");

            }
        }
    }
}
