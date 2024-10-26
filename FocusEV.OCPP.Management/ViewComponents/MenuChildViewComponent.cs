using FocusEV.OCPP.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace FocusEV.OCPP.Management.ViewComponents
{
    public class MenuChildViewComponent : ViewComponent
    {
        protected IConfiguration Config { get; private set; }
        public MenuChildViewComponent(IConfiguration config)
        {
            this.Config = config;
        }

        public async Task<IViewComponentResult> InvokeAsync(int parentId)
        {
            OCPPCoreContext dbContext = new OCPPCoreContext(this.Config);
            var model = dbContext.MenuChilds.Where(m => m.ParentId == parentId);
            return await Task.FromResult((IViewComponentResult)View("MenuChild", model));
        }
    }
}
