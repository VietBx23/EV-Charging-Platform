

using System;
using System.Collections.Generic;

#nullable disable

namespace FocusEV.OCPP.Database
{
    public partial class ChargeTag
    {
        public string TagId { get; set; }
        public string TagName { get; set; }
        public string ParentTagId { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool? Blocked { get; set; }

        public string SideId { get; set; } 

        public int ChargeStationId { get; set; } 

        public string TagDescription { get; set; } 

        public string TagType { get; set; }  

        public string TagState { get; set; }  

        public DateTime? CreateDate { get; set; }  

        public virtual ChargeStation ChargeStationVtl { get; set; } 

    }
}
