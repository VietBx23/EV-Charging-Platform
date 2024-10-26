using System;

namespace FocusEV.OCPP.Management.Models.Api
{
    public class Api_ChargePoint
    {
        public string ChargePointID { get; set; }
        public string ChargePointName { get; set; }
        public string ChargePointComment { get; set; }
        public int OwnerID { get; set; }
        public string OwnerName { get; set; }
        public int ChargeStationID { get; set; }
        public string ChargeStationName { get; set; }
        public DateTime LastSeen { get; set; }
    }
}
