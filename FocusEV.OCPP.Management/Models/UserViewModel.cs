using FocusEV.OCPP.Database;

namespace FocusEV.OCPP.Management.Models
{
    public class UserViewModel
    {
        public UserApp User { get; set; }
        public bool IsEmailValid { get; set; }
    }
}
