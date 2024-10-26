using System;
using System.Collections.Generic;

namespace FocusEV.OCPP.Management.Models
{
    public class DailyRevenueViewModel
    {
        public DateTime TransactionDate { get; set; }
        public string TotalRevenue { get; set; }

        public IEnumerable<ChargeStationReportModel> ReportData { get; set; }
        public IEnumerable<dynamic> ChargeStations { get; set; } // Used to populate dropdown
        public int SelectedMonth { get; set; }
        public int SelectedYear { get; set; }
    }



}
