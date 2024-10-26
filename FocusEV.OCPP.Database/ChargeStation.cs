using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusEV.OCPP.Database
{
    public class ChargeStation
    {
        public int ChargeStationId { get; set; }
        public int OwnerId { get; set; }
        public string Address { get; set; }
        public int Type { get; set; }
        public string Position { get; set; }
        public int Status { get; set; }
        public string Name { get; set; }
        public DateTime CreateDate { get; set; }
        public string Long { get; set; }
        public string Lat { get; set; }
        public string Images { get; set; }  //Terry add 03/01/2024 for API
        public int QtyChargePoint { get; set; }  //Terry add 03/01/2024 for API

        public virtual Owner OwnerVtl { get; set; }    
    }
}
