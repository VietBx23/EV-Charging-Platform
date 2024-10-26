

using System;
using System.Collections.Generic;

#nullable disable

namespace FocusEV.OCPP.Database
{
    public partial class ChargePoint
    {
        public ChargePoint()
        {
            Transactions = new HashSet<Transaction>();
        }

        public string ChargePointId { get; set; }
        public string Name { get; set; }
        public string Comment { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ClientCertThumb { get; set; }

        public int OwnerId { get; set; }  //Terry add 02/10/2023

        public int ChargeStationId { get; set; }  //Terry add 02/10/2023

        public string ChargePointModel { get; set; }  //Terry add 02/10/2023

        public string OCPPVersion { get; set; }  //Terry add 02/10/2023

        public string ChargePointState { get; set; }  //Terry add 02/10/2023

        public string ChargePointSerial { get; set; }  //Terry add 02/10/2023
        public string chargerPower { get; set; }  //Terry add 03/01/2024 for API
        public string outputType { get; set; }  //Terry add 03/01/2024 for API
        public string connectorType { get; set; }  //Terry add 03/01/2024 for API

        public virtual ChargeStation ChargeStationVtl { get; set; }

        public virtual Owner OwnerVtl { get; set; }

        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
