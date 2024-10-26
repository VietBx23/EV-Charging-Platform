using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusEV.OCPP.Database
{
    public class Unitprice
    {

        public int UnitpriceId { get; set; }
        public decimal Price { get; set; }
        public DateTime CreateDate { get; set; }
        public int IsActive { get; set; }
    }
}
