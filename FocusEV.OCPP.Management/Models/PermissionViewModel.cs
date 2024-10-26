using FocusEV.OCPP.Database;
using System;
using System.Collections.Generic;

namespace FocusEV.OCPP.Management.Models
{
    public class PermissionViewModel
    {
        public List<Permission> Permissions { get; set; }
        public int PermissionId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreateDate { get; set; }
        public string MenuID { get; set; }
        public string MenuName { get; set; }
    }
}
