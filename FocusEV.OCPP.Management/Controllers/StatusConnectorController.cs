using FocusEV.OCPP.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace FocusEV.OCPP.Management.Controllers
{
    public class StatusConnectorController : BaseController
    {
        private readonly ILogger<StatusConnectorController> _logger;
        private readonly IConfiguration _config;

        public StatusConnectorController(
            UserManager userManager,
            ILoggerFactory loggerFactory,
            IConfiguration config) : base(userManager, loggerFactory, config)
        {
            _logger = loggerFactory.CreateLogger<StatusConnectorController>();
            _config = config;
        }

        // View Connector Status 
        public IActionResult Index()
        {
            var connectorStatuses = GetConnectorStatuses();

            // Giả sử bạn có một phương thức để lấy danh sách ChargePointIds
            var chargePointIds = GetChargePointIds();

            ViewBag.ChargePointIds = chargePointIds;

            return View(connectorStatuses);
        }


        private List<string> GetChargePointIds()
        {
            using (var dbContext = new OCPPCoreContext(_config))
            {
                // Lọc ChargePointId bắt đầu bằng "GOEV" nếu tài khoản là "goev"
                if (User.Identity.Name == "goev")
                {
                    return dbContext.ConnectorStatuses
                        .Where(cs => cs.ChargePointId.StartsWith("GOEV"))
                        .Select(cs => cs.ChargePointId)
                        .Distinct()
                        .ToList();
                }
                else if(User.Identity.Name == "adminbinhphuoc")
                {
                    return dbContext.ConnectorStatuses
                    .Where(cs => cs.ChargePointId.StartsWith("FC-BPH"))
                    .Select(cs => cs.ChargePointId)
                    .Distinct()
                    .ToList();
                }
                else if (User.Identity.Name == "adminbinhthuan")
                {
                    return dbContext.ConnectorStatuses
                    .Where(cs => cs.ChargePointId.StartsWith("FC-BTH"))
                    .Select(cs => cs.ChargePointId)
                    .Distinct()
                    .ToList();
                }
                else if (User.Identity.Name == "inewsolar")
                {
                    return dbContext.ConnectorStatuses
                    .Where(cs => cs.ChargePointId.StartsWith("FC-KHO"))
                    .Select(cs => cs.ChargePointId)
                    .Distinct()
                    .ToList();
                }
                else if (User.Identity.Name == "adminletsgo")
                {
                    return dbContext.ConnectorStatuses
                    .Where(cs => cs.ChargePointId.StartsWith("LGO-PYE") || cs.ChargePointId.StartsWith("LGO-BDI"))
                    .Select(cs => cs.ChargePointId)
                    .Distinct()
                    .ToList();
                }
                else
                {
                    return dbContext.ConnectorStatuses
                        .Select(cs => cs.ChargePointId)
                        .Distinct()
                        .ToList(); // Lấy tất cả ChargePointIds
                }
            }
        }


        // Get Connector Status by ID
        public IActionResult Details(int connectorId, string chargePointId)
        {
            var connectorStatus = GetConnectorStatus(connectorId, chargePointId);
            if (connectorStatus == null)
            {
                _logger.LogWarning("No ConnectorStatus found with ConnectorId: {ConnectorId} and ChargePointId: {ChargePointId}", connectorId, chargePointId);
                return NotFound();
            }
            return View(connectorStatus);
        }

        // Create or Update Connector Status
        [HttpPost]
        public IActionResult CreateOrUpdate(ConnectorStatus model)
        {
            if (ModelState.IsValid)
            {
                using (var dbContext = new OCPPCoreContext(_config))
                {
                    var existingConnectorStatus = dbContext.ConnectorStatuses
                        .FirstOrDefault(cs => cs.ConnectorId == model.ConnectorId && cs.ChargePointId == model.ChargePointId);

                    if (existingConnectorStatus == null)
                    {
                        dbContext.ConnectorStatuses.Add(model);
                        _logger.LogInformation("Created new ConnectorStatus with ConnectorId: {ConnectorId} and ChargePointId: {ChargePointId}", model.ConnectorId, model.ChargePointId);
                    }
                    else
                    {
                        existingConnectorStatus.ConnectorName = model.ConnectorName;
                        existingConnectorStatus.LastStatus = model.LastStatus;
                        existingConnectorStatus.LastStatusTime = model.LastStatusTime;
                        existingConnectorStatus.LastMeter = model.LastMeter;
                        existingConnectorStatus.LastMeterTime = model.LastMeterTime;
                        existingConnectorStatus.lastSeen = model.lastSeen;
                        existingConnectorStatus.RemoteTime = model.RemoteTime;
                        existingConnectorStatus.LastMeterRemote = model.LastMeterRemote;
                        existingConnectorStatus.terminalId = model.terminalId;
                        _logger.LogInformation("Updated ConnectorStatus with ConnectorId: {ConnectorId} and ChargePointId: {ChargePointId}", model.ConnectorId, model.ChargePointId);
                    }

                    dbContext.SaveChanges();
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult Delete(string chargePointId, int connectorId)
        {
            using (var dbContext = new OCPPCoreContext(_config))
            {
                var connectorStatus = dbContext.ConnectorStatuses
                    .FirstOrDefault(cs => cs.ConnectorId == connectorId && cs.ChargePointId == chargePointId);

                if (connectorStatus != null)
                {
                    dbContext.ConnectorStatuses.Remove(connectorStatus);
                    dbContext.SaveChanges();
                    _logger.LogInformation("Deleted ConnectorStatus with ConnectorId: {ConnectorId} and ChargePointId: {ChargePointId}", connectorId, chargePointId);
                }
                else
                {
                    _logger.LogWarning("No ConnectorStatus found to delete with ConnectorId: {ConnectorId} and ChargePointId: {ChargePointId}", connectorId, chargePointId);
                }
            }

            return Json(new { success = true });
        }



        // hiển thị danh sách ConnectorStatus 
        /*private List<ConnectorStatus> GetConnectorStatuses()
        {
            using (var dbContext = new OCPPCoreContext(_config))
            {
                var connectorStatuses = dbContext.ConnectorStatuses.ToList();
                // check Data ConnectorStatus 
                if (!connectorStatuses.Any())
                {
                    _logger.LogWarning("No data found in ConnectorStatuses table.");
                }
                else
                {
                    _logger.LogInformation("Fetched {Count} records from ConnectorStatuses table.", connectorStatuses.Count);
                }
                return connectorStatuses;
            }
        }
*/

        private List<ConnectorStatus> GetConnectorStatuses()
        {
            using (var dbContext = new OCPPCoreContext(_config))
            {
                IQueryable<ConnectorStatus> query = dbContext.ConnectorStatuses;

                // Nếu tài khoản là "goev", lọc theo ChargePointId
                if (User.Identity.Name == "goev")
                {
                    query = query.Where(cs => cs.ChargePointId.StartsWith("GOEV"));
                }else if (User.Identity.Name == "adminbinhphuoc")
                {
                    query = query.Where(cs => cs.ChargePointId.StartsWith("FC-BPH"));
                }
                else if (User.Identity.Name == "adminbinhthuan")
                {
                    query = query.Where(cs => cs.ChargePointId.StartsWith("FC-BTH"));
                }
                else if (User.Identity.Name == "inewsolar")
                {
                    query = query.Where(cs => cs.ChargePointId.StartsWith("FC-KHO"));
                }
                else if (User.Identity.Name == "adminletsgo")
                {
                    query = query.Where(cs => cs.ChargePointId.StartsWith("LGO-PYE") || cs.ChargePointId.StartsWith("LGO-BDI"));
                }

                var connectorStatuses = query.ToList();

                // Check Data ConnectorStatus 
                if (!connectorStatuses.Any())
                {
                    _logger.LogWarning("No data found in ConnectorStatuses table.");
                }
                else
                {
                    _logger.LogInformation("Fetched {Count} records from ConnectorStatuses table.", connectorStatuses.Count);
                }

                return connectorStatuses;
            }
        }

        // Get single Connector Status for edit
        [HttpGet]
        public IActionResult GetConnectorStatus(int connectorId, string chargePointId)
        {
            using (var dbContext = new OCPPCoreContext(_config))
            {
                var connectorStatus = dbContext.ConnectorStatuses
                    .FirstOrDefault(cs => cs.ConnectorId == connectorId && cs.ChargePointId == chargePointId);

                if (connectorStatus == null)
                {
                    _logger.LogWarning("No ConnectorStatus found with ConnectorId: {ConnectorId} and ChargePointId: {ChargePointId}", connectorId, chargePointId);
                    return NotFound();
                }

                return Json(connectorStatus);
            }
        }
    }
}
