using System;

namespace FocusEV.OCPP.Management.ViewModels
{
    public class DepositHistoryViewModel
    {
        public int DepositHistoryId { get; set; }
        public string UserAppId { get; set; }
        public string UserName { get; set; }
        public decimal Amount { get; set; }
        public int Method { get; set; }
        public int Gateway { get; set; }
        public string Message { get; set; }
        public DateTime DateCreate { get; set; }
        public decimal BalanceBeforeDeposit { get; set; }
        public decimal BalanceAfterDeposit { get; set; }
    }
}
