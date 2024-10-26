using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusEV.OCPP.Database
{
    public class StaticQRIPNLog
    {
        public int IpnId { get; set; }
        public string txnId { get; set; }   // Required Max(20)
        public string code { get; set; }     // Required Max(10)
        public string? message { get; set; }  // Required Max(100)
        public string amount { get; set; }  // Required Max(13)
        public string payDate { get; set; }  // Required Max(14)
        public string merchantCode { get; set; }  // Required Max(20)
        public string terminalId { get; set; }  // Required Max(8)
        public string? checksum { get; set; }  // Required Max(32)
        public string? qrTrace { get; set; }  // OPTIONAL Max(100)
        public string? mobile { get; set; }  // OPTIONAL Max(20)
        public string? accountNo { get; set; }  // OPTIONAL Max(30)
        public string? name { get; set; }  // OPTIONAL Max(100)
        public string? phone { get; set; }  // OPTIONAL Max(20)
        public string? province_id { get; set; }  // OPTIONAL Max(14)
        public string? district_id { get; set; }  // OPTIONAL Max(14)
        public string? address { get; set; }  // OPTIONAL Max(100)
        public string? email { get; set; }  // OPTIONAL Max(100)
        public string? bankCode { get; set; }  // OPTIONAL Max(100)
        public string? masterMerCode { get; set; }  // OPTIONAL Max(100)
    }
}
