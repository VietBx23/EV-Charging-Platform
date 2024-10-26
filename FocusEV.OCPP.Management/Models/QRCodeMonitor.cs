using FocusEV.OCPP.Database;

namespace FocusEV.OCPP.Management.Models
{
    public class QRCodeMonitor
    {   
        public ConnectorStatus ConnectorStatus { get; set; }
        public StaticQRIPNLog StaticQRIPNLog { get; set; }
        public string hasTransaction { get; set; }
    }
}
