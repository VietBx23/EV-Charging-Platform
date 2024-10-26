using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusEV.OCPP.Database
{
    public class TransactionVirtualQR
    {
        public int TransactionQRId { get; set; }
        public int TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string ChargePointId { get; set; }
        public int ConnectorId { get; set; }
        public string qrTrace { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
