// Models/ChargeStationReportModel.cs
using System;
using System.Collections.Generic;

namespace FocusEV.OCPP.Management.Models
{
    public class ChargeStationReportModel
    {
        public List<ChargeStationTransactionSummary> TransactionSummaries { get; set; }
        public List<ChargeStationKWhSummary> KWhSummaries { get; set; }
        public List<ChargeStationAmountSummary> AmountSummaries { get; set; }
    }
}
