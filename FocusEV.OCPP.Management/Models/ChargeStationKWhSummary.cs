namespace FocusEV.OCPP.Management.Models
{
    public class ChargeStationKWhSummary
    {
        public string ChargeStationName { get; set; }
        public double TotalKWhConsumedByStation { get; set; } // Đổi tên để rõ ràng hơn
        public double TotalKWhLadoMobile { get; set; }
        public double TotalKWhFocus { get; set; }
        public double TotalKWhGuest { get; set; }
        public double TotalKWhLadoCard { get; set; }
    }
}
