using FocusEV.OCPP.Database;
using System;

namespace FocusEV.OCPP.Management.Models.Api
{
    public class api_Transaction
    {
        public Transaction Transaction { get; set; }

        public decimal TotalPrice { get; set; }
        public decimal TotalValue { get; set; }

        public virtual ChargePoint ChargePoint { get; set; }
        public virtual Owner Owner { get; set; }
        public virtual ChargeStation ChargeStation { get; set; }
        public virtual ChargeTag ChargeTag { get; set; }
        public virtual Customer Customer { get; set; }
        public virtual UserApp UserApp { get;set;}
    }
}
