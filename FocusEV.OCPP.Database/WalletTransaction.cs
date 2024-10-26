using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusEV.OCPP.Database
{
    public class WalletTransaction
    {
        public int WalletTransactionId { get; set; }
        public string UserAppId { get; set; }
        public int TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Message { get; set; }
        public DateTime DateCreate { get; set; }
        public decimal meterValue { get;set; }
        public decimal ?  ExchangeRate { get; set; }   
        public string stopMethod { get; set; }   
        public decimal ?  currentBalance { get; set; }   
        public decimal ? newBalance { get; set; }   
        public string chargeType { get; set; } 
        public decimal? upperLimit { get; set; } 
        public string DataTransferQuery { get; set; } 
        public string DataTransferStatus { get; set; } 
    }
}
