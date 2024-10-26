using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusEV.OCPP.Database
{
    public class QRTransaction
    {
        public int QrTransactionId { get; set; }
        public string QrTagId { get; set; }
        public int TransactionId { get; set; }
        public decimal ChargingAmount { get; set; }
        public decimal ExchangeRate { get; set; }   
        public decimal UpperLimit { get; set; }
        public decimal ? EnergyUsed { get; set; }   
        public string StopMethod { get; set; }   
        public string? Message { get; set; }   
        public string? QrSource { get; set; }   
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string DataTransferQuery { get; set; }
        public string DataTransferStatus { get; set; }
        public string qrTrace { get; set; }
    }
}
