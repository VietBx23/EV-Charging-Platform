using System.Collections.Generic;
using FocusEV.OCPP.Database;

namespace FocusEV.OCPP.Management.Models
{
    public class UserAppViewModel
    {

        public IEnumerable<UserApp> UserApps { get; set; }
        public UserApp UserApp { get; set; }

    }
}
