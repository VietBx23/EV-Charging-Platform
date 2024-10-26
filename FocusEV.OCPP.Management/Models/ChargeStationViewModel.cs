using FocusEV.OCPP.Database;
using System;
using System.Collections.Generic;

namespace FocusEV.OCPP.Management.Models
{
    public class ChargeStationViewModel
    {
        public List<ChargeStation> ChargeStations { get; set; }
        public int ChargeStationId { get; set; }
        public int OwnerId { get; set; }
        public string Address { get; set; }
        public int Type { get; set; }
        public string Position { get; set; }
        public int Status { get; set; }
        public string Name { get; set; }
        public DateTime CreateDate { get; set; }
        public string Lat { get; set; }
        public string Long { get; set; }
        public string Image { get; set; }
        public int QtyChargePoint { get; set; }
    }
}
