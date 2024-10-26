using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusEV.OCPP.Database
{
    public class Account
    {
        public int AccountId { get; set; }
        public string Name { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }    
        public string Code { get; set; }
        public int DepartmentId { get; set; }
        public int PermissionId { get; set; }
        public DateTime CreateDate { get; set; }
        public string Images { get; set; }
        public int ? OwnerId { get; set; }
        public virtual Department Department { get; set; }
        public virtual Permission Permission { get; set; }
    }
}
