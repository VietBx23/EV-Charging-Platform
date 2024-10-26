    using FocusEV.OCPP.Database;
    using FocusEV.OCPP.Management.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
using System.IO;
using System.Linq;
    using System.Threading.Tasks;

    namespace FocusEV.OCPP.Management.Controllers
    {
        public class MainBannerController : Controller
        {
            private readonly IConfiguration _config;
            private readonly ILogger<MainBannerController> _logger;

            public MainBannerController(IConfiguration config, ILogger<MainBannerController> logger)
            {
                _config = config;
                _logger = logger;
            }

            // GET: MainBanner/Index
            public async Task<IActionResult> Index()
            
        {
                try
                {
                    using (var dbContext = new OCPPCoreContext(_config))
                    {
                        var banners = await dbContext.MainBanners.ToListAsync();
                        var model = new BannerViewModel
                        {
                            MainBanners = banners
                        };
                        return View(model);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading banners.");
                    return RedirectToAction("Error", new { Id = "" });
                }
            }



        /*// POST: MainBanner/SaveAndUpdateMainBanner
        [HttpPost]
        public async Task<IActionResult> SaveAndUpdateMainBanner(BannerViewModel model)
        {
            try
            {
                using (var dbContext = new OCPPCoreContext(_config))
                {
                    if (ModelState.IsValid)
                    {
                        var existingBanner = await dbContext.MainBanners
                            .FirstOrDefaultAsync(b => b.BannerId == model.BannerId);

                        if (existingBanner != null)
                        {
                            existingBanner.Title = model.Title;
                            existingBanner.Image = model.Image;
                            existingBanner.IsActive = model.IsActive;
                            existingBanner.CreatedDate = model.CreatedDate;

                            dbContext.MainBanners.Update(existingBanner);
                        }
                        else
                        {
                            dbContext.MainBanners.Add(new MainBanner
                            {
                                Title = model.Title,
                                Image = model.Image,
                                IsActive = model.IsActive,
                                CreatedDate = DateTime.Now
                            });
                        }

                        await dbContext.SaveChangesAsync();
                        return RedirectToAction("Index");
                    }

                    var banners = await dbContext.MainBanners.ToListAsync();
                    model.MainBanners = banners;
                    return View("Index", model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving or updating banner.");
                return View("Index");
            }
        }*/

        [HttpPost]
        // POST: MainBanner/SaveAndUpdateMainBanner
        [HttpPost]
        public async Task<IActionResult> SaveAndUpdateMainBanner(BannerViewModel model, IList<IFormFile> files)
        {
            try
            {
                using (var dbContext = new OCPPCoreContext(_config))
                {
                    if (ModelState.IsValid)
                    {
                        var existingBanner = await dbContext.MainBanners
                            .FirstOrDefaultAsync(b => b.BannerId == model.BannerId);

                        if (existingBanner != null)
                        {
                            // Update existing banner
                            existingBanner.Title = model.Title;
                            existingBanner.IsActive = model.IsActive;
                            existingBanner.CreatedDate = DateTime.Now;

                            if (files != null && files.Count > 0)
                            {
                                var file = files.First();
                                var fileName = Path.GetFileName(file.FileName);
                                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/banner", fileName);

                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }

                                existingBanner.Image = fileName; // Update image path in the database
                            }

                            dbContext.MainBanners.Update(existingBanner);
                        }
                        else
                        {
                            // Create new banner
                            var newBanner = new MainBanner
                            {
                                Title = model.Title,
                                IsActive = model.IsActive,
                                CreatedDate = DateTime.Now
                            };

                            if (files != null && files.Count > 0)
                            {
                                var file = files.First();
                                var fileName = Path.GetFileName(file.FileName);
                                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/banner", fileName);

                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }

                                newBanner.Image = fileName; // Set image path in the database
                            }

                            dbContext.MainBanners.Add(newBanner);
                        }

                        await dbContext.SaveChangesAsync();
                        return RedirectToAction("Index");
                    }

                    // If model state is not valid, reload the banners and return to the view
                    var banners = await dbContext.MainBanners.ToListAsync();
                    model.MainBanners = banners;
                    return View("Index", model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving or updating banner.");
                return View("Index");
            }
        }

        // POST: MainBanner/DeleteMainBanner
        [HttpPost]
            public async Task<IActionResult> DeleteMainBanner(int id)
            {
                try
                {
                    using (var dbContext = new OCPPCoreContext(_config))
                    {
                        var banner = await dbContext.MainBanners.FirstOrDefaultAsync(m => m.BannerId == id);
                        if (banner != null)
                        {
                            dbContext.MainBanners.Remove(banner);
                            await dbContext.SaveChangesAsync();
                            return Json(new { success = true });
                        }
                        else
                        {
                            return Json(new { success = false, message = "Banner not found." });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting banner.");
                    return Json(new { success = false, message = ex.Message });
                }
            }

            // POST: MainBanner/AjaxUpload
            [HttpPost]
            public IActionResult AjaxUpload(IList<Microsoft.AspNetCore.Http.IFormFile> files)
            {
                try
                {
                    string fileName = "";
                    foreach (var formFile in files)
                    {
                        fileName = Helper.Upload(formFile, "Banner");
                    }
                    return Json(new { fileName });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading file.");
                    return Json(new { fileName = "", error = ex.Message });
                }
            }
        // GET: MainBanner/EditBanner

        [HttpGet]
        public async Task<IActionResult> EditBanner(int id)
        {
            try
            {
                using (var dbContext = new OCPPCoreContext(_config))
                {
                    var banner = await dbContext.MainBanners.FirstOrDefaultAsync(b => b.BannerId == id);
                    if (banner == null)
                    {
                        return NotFound(); // Trả về 404 nếu không tìm thấy banner
                    }

                    var model = new BannerViewModel
                    {
                        BannerId = banner.BannerId,
                        Title = banner.Title,
                        Image = banner.Image,
                        IsActive = banner.IsActive,
                        CreatedDate = banner.CreatedDate,
                       
                    };

                    return Json(model); // Trả về dữ liệu dưới dạng JSON
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving banner data.");
                return StatusCode(500, "Internal server error"); // Trả về mã lỗi 500 nếu có lỗi xảy ra
            }
        }



    }
}
