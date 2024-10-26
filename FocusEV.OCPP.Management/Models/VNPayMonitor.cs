using FocusEV.OCPP.Database;

namespace FocusEV.OCPP.Management.Models
{
    public class VNPayMonitor
    {
        public UserApp UserApp { get; set; }
        public DepositHistory DepositHistory { get; set; }
        public VnpIPNLog VnpIPNLog { get; set; }
    }
}
