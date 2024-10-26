using FocusEV.OCPP.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusEV.OCPP.Management.Models
{
    public class MenuviewModel
    {
        public int MenuId { get; set; }
        public string Name { get; set; }
        public string Href { get; set; }
        public string Icon { get; set; }

        public List<Menu> Menus { get; set; }

    }
}
