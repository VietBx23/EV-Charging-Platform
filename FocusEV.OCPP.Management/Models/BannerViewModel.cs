    using FocusEV.OCPP.Database;
    using System;
    using System.Collections.Generic;
    namespace FocusEV.OCPP.Management.Models
    {
        public class BannerViewModel
        {

            public int BannerId { get; set; }
            public string Title { get; set; }
            public string Image { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedDate { get; set; }


            public List<MainBanner> MainBanners { get; set; }
        }
    }
