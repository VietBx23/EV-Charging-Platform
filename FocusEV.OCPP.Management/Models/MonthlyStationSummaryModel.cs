namespace FocusEV.OCPP.Management.Models
{
    public class MonthlyStationSummaryModel
    {
        public int Month { get; set; }                 // Tháng
        public int TotalTransactions { get; set; }    // Tổng số giao dịch
        public double TotalRevenue { get; set; }      // Tổng doanh thu từ các giao dịch
        public double TotalVnpayRevenue { get; set; } // Tổng doanh thu từ VNPay
    }
    public class TopChargingStationModel
    {
        public string ChargeStationName { get; set; }
        public double TotalRevenue { get; set; }
    }
    public class TopUserConsumptionModel
    {
        public string UserName { get; set; }
        public string Fullname { get; set; }
        public decimal TotalkWhUsed { get; set; }
        public string TotalAmount { get; set; }
    }
    public class DailyRegistration
    {
        public int Day { get; set; }
        public int TotalRegistrations { get; set; }
    }
}
