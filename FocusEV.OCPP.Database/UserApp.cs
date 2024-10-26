using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusEV.OCPP.Database
{
    public partial class UserApp
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Fullname { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public int Gender { get; set; }
        public DateTime CreateDate { get; set; }
        public int isSource { get; set; }
        public decimal Balance { get; set; }
        public string TokenNotify { get; set; }
        public int isActive { get; set; }
        public string Platform { get; set; }   
        public string Version { get; set; }    
        public int OwnerId { get; set; }    
    }
}
