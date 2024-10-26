    using System;

    namespace FocusEV.OCPP.Management.Models
    {
        public class DailyConsumption
        {
            public DateTime ChargingDate { get; set; }
            public decimal StartkWh_Sung1 { get; set; }
            public decimal EndkWh_Sung1 { get; set; }
            public decimal StartkWh_Sung2 { get; set; }
            public decimal EndkWh_Sung2 { get; set; }
            public decimal kWhUsed_Sung1 { get; set; }
            public decimal kWhUsed_Sung2 { get; set; }
            public decimal TotalkWhUsed { get; set; }
        }
    }
