using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusEV.OCPP.Database
{
    public  class TransactionQR
    {
        public int TransactionQRId { get; set; }
        public decimal Amount { get; set; }
        public string ChargePointId { get; set; }
        public int ConnectorId { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
