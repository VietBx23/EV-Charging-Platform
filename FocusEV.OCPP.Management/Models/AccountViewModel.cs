using FocusEV.OCPP.Database;
using System;
using System.Collections.Generic;

namespace FocusEV.OCPP.Management.Models
{
    public class AccountViewModel
    {
        public List<Account> Accounts { get; set; } 
        public int AccountId { get; set; }
        public string Name { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Code { get; set; }
        public int DepartmentId { get; set; }
        public int PermissionId { get; set; }
        public DateTime CreateDate { get; set; }
        public string Images { get; set; }
       
    }
}
