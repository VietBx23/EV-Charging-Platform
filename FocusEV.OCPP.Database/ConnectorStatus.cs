

using System;
using System.Collections.Generic;

#nullable disable

namespace FocusEV.OCPP.Database
{
    public partial class ConnectorStatus
    {
        public string ChargePointId { get; set; }
        public int ConnectorId { get; set; }

        public string ConnectorName { get; set; }

        public string LastStatus { get; set; }
        public DateTime? LastStatusTime { get; set; }

        public double? LastMeter { get; set; }
        public string  LastMeterRemote { get; set; }
        public DateTime? LastMeterTime { get; set; }
        public DateTime ? lastSeen { get; set; }
        public DateTime? RemoteTime { get; set; }
        public string terminalId { get; set; }
    }
}
