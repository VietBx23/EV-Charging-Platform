using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusEV.OCPP.Database
{
    public class TransactionLogs
    {
        public int TransactionLogId { get; set; }
        public string ServerRemoteStartJson { get; set; }
        public DateTime ServerRemoteStartTime { get; set; }
        public DateTime ClientStartTime { get; set; }
        public string CientStartReqJson { get; set; }
        public DateTime ServerStartConfTime { get; set; }
        public string ServerStartConfJson { get; set; }
        public DateTime ClientStopTime { get; set; }
        public string CilentStopJson { get; set; }
        public DateTime ServerStopConfTime { get; set; }
        public string SererStopConfJson { get; set; }
        public int TransactionId { get; set; }
        public string ChargePointId { get; set; }   
        public int ConnectorId { get; set; }
    }
}