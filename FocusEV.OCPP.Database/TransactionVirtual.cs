using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusEV.OCPP.Database
{
    public class TransactionVirtual
    {
        public int TransactionsVirtualID { get; set; }
        public int TransactionId { get; set; }
        public DateTime StartTime { get; set; }
        public string StartTagId { get; set; }
        public string ChargePointId { get; set; }
        public decimal upperLimit { get; set; }
    }
}
