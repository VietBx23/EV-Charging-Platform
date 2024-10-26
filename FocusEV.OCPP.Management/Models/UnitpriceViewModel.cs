using FocusEV.OCPP.Database;
using System;
using System.Collections.Generic;

namespace FocusEV.OCPP.Management.Models
{
    public class UnitpriceViewModel
    {
        public List<Unitprice> Unitprices { get; set; }
        public int UnitpriceId { get; set; }
        public decimal Price { get; set; }
        public DateTime CreateDate { get; set; }
        public int IsActive { get; set; }
    }
}
