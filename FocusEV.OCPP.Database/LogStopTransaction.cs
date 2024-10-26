using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusEV.OCPP.Database
{
    public class LogStopTransaction
    {
        public int LogstopId { get; set; }  
        public int TransactionId { get; set;}
        public string Descriptions { get; set; }    
        public DateTime Timestart { get; set; } 
        public DateTime Timestop { get; set; }
        public string StopTransactionResponse { get; set; }
    }
}
