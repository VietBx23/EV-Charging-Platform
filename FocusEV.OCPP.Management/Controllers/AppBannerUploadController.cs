using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FocusEV.OCPP.Management.Controllers
{
    public class AppBannerUploadController : Controller
    {
        private readonly ILogger<AppBannerUploadController> _logger;

        public AppBannerUploadController(ILogger<AppBannerUploadController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/banner");
            var imageFiles = Directory.GetFiles(uploads).Select(Path.GetFileName).ToList();
            ViewBag.ImageFiles = imageFiles;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadBanner()
        {
            try
            {
                var files = Request.Form.Files;
                if (files.Count > 0)
                {
                    var file = files[0];
                    var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/banner");
                    var fileName = Path.GetFileName(file.FileName);
                    var filePath = Path.Combine(uploads, fileName);

                    if (!Directory.Exists(uploads))
                    {
                        Directory.CreateDirectory(uploads);
                    }

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    _logger.LogInformation($"Uploaded file: {fileName}");

                    return Json(new { success = true, message = "File uploaded successfully", fileName = fileName });
                }

                return Json(new { success = false, message = "No file selected" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading file: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while uploading the file" });
            }
        }

        [HttpPost]
        public IActionResult DeleteImage([FromBody] DeleteImageRequest request)
        {
            try
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/banner");
                var filePath = Path.Combine(uploads, request.FileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogInformation($"Deleted file: {request.FileName}");
                    return Json(new { success = true, message = "File deleted successfully" });
                }

                return Json(new { success = false, message = "File not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting file: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while deleting the file" });
            }
        }








        public class DeleteImageRequest
        {
            public string FileName { get; set; }
        }
    }
}
