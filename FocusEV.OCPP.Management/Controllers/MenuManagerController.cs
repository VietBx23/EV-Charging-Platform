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



namespace FocusEV.OCPP.Management.Controllers
{
    public class MenuManagerController: BaseController
    {
        private readonly IStringLocalizer<MenuManagerController> _localization;
        private readonly IConfiguration _config;
        private readonly ILogger<MenuManagerController> _logger;

        public MenuManagerController(
            UserManager userManager,
            IStringLocalizer<MenuManagerController> localizer,
            ILoggerFactory loggerFactory,
            IConfiguration config) : base (userManager, loggerFactory, config)
        {
            _localization = localizer;
            _config = config;
            Logger = loggerFactory.CreateLogger<MenuManagerController>();
        }

        // view list menu plf 
        public async Task<IActionResult> Index()
        {
            try
            {

                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {

                    var menus = await dbContext.Menus.ToListAsync();
                    var model = new MenuviewModel
                    {
                       Menus = menus
                    };
                    return View(model);


                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading menu.");
                return RedirectToAction("Error", new { Id = "" });
            }
         
        }
        // MenuManager/SaveAndUpdateMenuManager 
        [HttpPost]
        public async Task<IActionResult> SaveAndUpdateMenuManager(MenuviewModel model)
        {
            try
            {
                using (var dbContext = new OCPPCoreContext(Config))
                {

                     var menuManager = await dbContext.Menus
                .FirstOrDefaultAsync(m => m.MenuId == model.MenuId);

                    if (ModelState.IsValid)
                    {

                        if (menuManager != null)
                        {
                            menuManager.Name = model.Name;
                            menuManager.Href = model.Href;
                            menuManager.Icon = model.Icon;
                            dbContext.Menus.Update(menuManager);
                        }
                        else
                        {
                            var newMenu = new Menu
                            {
                                MenuId = model.MenuId,
                                Name = model.Name,
                                Href = model.Href,
                                Icon = model.Icon,
                            };
                            dbContext.Menus.Add(newMenu);


                        }
                        await dbContext.SaveChangesAsync();
                        return RedirectToAction("Index");
                    }
                  var menus = await dbContext.Menus.ToListAsync();
                    model.Menus = menus;
                    return View("Index", model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving or updating menu.");
                return View("Index");
            }
        }

        // delete menu manager 
        [HttpPost]
        public async Task<IActionResult> DeleteMenuManager(int id)
        {
            try
            {
                using (var dbContext = new OCPPCoreContext(Config))
                {

                    var menu = await dbContext.Menus.FirstOrDefaultAsync(m => m.MenuId == id);
                if(menu != null)
                    {
                        dbContext.Menus.Remove(menu);
                        await dbContext.SaveChangesAsync();
                        return Json(new {success = true});

                    }else
                    {
                        return Json ( new {success = false, message ="Banner not found. "});
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error deleting menu.");
                return Json(new { success = false, message = ex.Message });
            }
        }



        // Edit Menu Manager 

      
        // Edit Menu Manager 
        [HttpGet]
        public async Task<IActionResult> EditMenuManager(int id)
        {
            try
            {
                using (var dbContext = new OCPPCoreContext(_config))
                {
                    var menu = await dbContext.Menus.FirstOrDefaultAsync(m => m.MenuId == id);
                    if (menu == null)
                    {
                        return NotFound();
                    }

                    var model = new MenuviewModel
                    {
                        MenuId = menu.MenuId,
                        Name = menu.Name,
                        Href = menu.Href,
                        Icon = menu.Icon,
                    };

                    return Json(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving menu data.");
                return StatusCode(500, "Internal server error");
            }
        }


    }
}

