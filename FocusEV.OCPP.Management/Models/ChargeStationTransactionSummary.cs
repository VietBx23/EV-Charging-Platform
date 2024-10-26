namespace FocusEV.OCPP.Management.Models
{
    public class ChargeStationTransactionSummary
    {
        public string ChargeStationName { get; set; }
        public int TotalTransactions { get; set; }
        public int LadoUseMobileTransactions { get; set; }
        public int FocusUserTransactions { get; set; }
        public int GuestUserTransactions { get; set; }
        public int LadoCardUserTransactions { get; set; }
    }
}
