// Models/ChargePointStatisticsViewModel.cs
namespace FocusEV.OCPP.Database
{
    public class ChargePointStatisticsViewModel
    {
        public string ChargePointId { get; set; }
        public double kWh_Connector1 { get; set; }
        public double kWh_Connector2 { get; set; }
        public double Total_kWhUsed { get; set; }
    }
}
