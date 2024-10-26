using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FocusEV.OCPP.Database;
using FocusEV.OCPP.Management.Models;
using System.Globalization;

namespace FocusEV.OCPP.Management.Controllers
{
    public partial class MenuContronller : BaseController
    {
        private readonly IStringLocalizer<MenuContronller> _localizer;

        public MenuContronller(
            UserManager userManager,
            IStringLocalizer<MenuContronller> localizer,
            ILoggerFactory loggerFactory,
            IConfiguration config) : base(userManager, loggerFactory, config)
        {
            _localizer = localizer;
            Logger = loggerFactory.CreateLogger<MenuContronller>();
        }
        [Authorize]
        public IActionResult LoadMenu()
        {
            return PartialView();
        }
    }
}
