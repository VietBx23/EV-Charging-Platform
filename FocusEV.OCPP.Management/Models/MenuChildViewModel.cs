using FocusEV.OCPP.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace FocusEV.OCPP.Management.Models
{
    public class MenuChildViewModel
    {
        public int MenuchildId { get; set; }
        public string MenuChildName { get; set; }
        public int ParentId { get; set; }
        public string Url { get; set; }

        public List<MenuChild> MenuChilds { get; set; }
        
    }
}
