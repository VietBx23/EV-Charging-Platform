using FocusEV.OCPP.Database;
using FocusEV.OCPP.Management.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FocusEV.OCPP.Management.ViewComponents
{
    public class MenuViewComponent : ViewComponent
    {
        protected IConfiguration Config { get; private set; }
        public MenuViewComponent(IConfiguration config)
        {
            this.Config = config;
        }

        public async Task<IViewComponentResult> InvokeAsync(int parentId)
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var Listmenu = new List<Menu>();
                var userName = claimsIdentity.FindFirst(ClaimTypes.Name)?.Value;
                if (userName != null)
                {
                    var role = claimsIdentity.FindFirst(ClaimTypes.Role)?.Value;
                    var account = dbContext.Accounts.Where(m => m.UserName == userName).FirstOrDefault();
                    ViewBag.Account = account;
                    var permisson = dbContext.Permissions.ToList().Where(m => m.PermissionId == account.PermissionId).FirstOrDefault();
                    var getlst = permisson.MenuID.Split(',').ToList();
                    var model = dbContext.Menus.ToList();

                    foreach (var item in getlst)
                    {
                        if (item != "")
                        {
                            var menu = model.ToList().Where(m => m.MenuId == int.Parse(item)).FirstOrDefault();
                            if (menu != null)
                            {
                                Listmenu.Add(menu);
                            }
                        }
                       
                    }
                }
                
                return await Task.FromResult((IViewComponentResult)View("Menu", Listmenu));
            }
        }
    }
}
