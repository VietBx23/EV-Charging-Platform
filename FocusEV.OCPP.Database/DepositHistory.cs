using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusEV.OCPP.Database
{
    public class DepositHistory
    {
        public int DepositHistoryId { get; set; }
        public string DepositCode { get; set; }  
        public string UserAppId { get; set; }
        public decimal Amount { get; set; }
        public int Method { get; set; }
        public int Gateway { get; set; }
        public string Message { get; set; }
        public DateTime DateCreate { get; set; }
        public decimal ? NewBalance { get; set; }    
        public DateTime ? BankTransTime { get; set; }    
        public int ? BankTransStatus { get; set; }   
        public string ? BankTransID { get; set; }   
        public string ? BankTransDesc  { get; set; }   
        public string? BankTransCode { get; set; }   
        public decimal? currentBalance { get; set; }   
        public string IsStatus { get; set; }   
        public string? checksum { get; set; }    
        public string? paymentURL { get; set; }   
        public string? remoteIPaddress { get; set; }   

        public string? Remark { get; set; }   
    }
}
