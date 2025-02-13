
using FocusEV.OCPP.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Transactions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Net.Mail;
using MailMessage = System.Net.Mail.MailMessage;
using System.Web;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FocusEV.OCPP.API.Controllers
{

    public class ApiResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }


        public ApiResponse(int statusCode, string message, object data = null)
        {
            this.StatusCode = statusCode;
            this.Message = message;
            this.Data = data;
        }
    }

    public class ApiResponse2
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }


        public ApiResponse2(int statusCode, string message)
        {
            this.StatusCode = statusCode;
            this.Message = message;
        }
    }

    //API response cho VNPAY - QR Tĩnh
    public class ApiResponseVnpay
    {
        public string code { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }


        public ApiResponseVnpay(string code, string message, object data = null)
        {
            this.code = code;
            this.Message = message;
            this.Data = data;
        }
    }

    //API response cho VNPAY - QR Động từ App
    public class ApiResponseVnpApp
    {
        public string RspCode { get; set; }
        public string Message { get; set; }


        public ApiResponseVnpApp(string RspCode, string message)
        {
            this.RspCode = RspCode;
            this.Message = message;
        }
    }


    public class ChangePassword
    {
        public string UserAppId { get; set; }
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }
    public class VM_Transaction
    {
        public int TransactionId { get; set; }
        public string ChargePointId { get; set; }
        public int ConnectorId { get; set; }
        public string StartTagId { get; set; }
        public DateTime StartTime { get; set; }
        public double MeterStart { get; set; }
        public string StartResult { get; set; }
        public string StopTagId { get; set; }
        public DateTime? StopTime { get; set; }
        public double? MeterStop { get; set; }
        public string StopReason { get; set; }

        public string ChargeStationName { get; set; }
        public string ChargeStationAddress { get; set; }
        public string ChargePointName { get; set; }

        public decimal Amount { get; set; }  
        public decimal meterValue { get; set; }    
        public decimal ExchangeRate { get; set; }   
        public string? stopMethod { get; set; }   
    }
    [ApiController]
    [Route("[controller]")]
    public class APIController : ControllerBase
    {
        protected IConfiguration Config { get; private set; }
        public APIController(IConfiguration config)
        {
            Config = config;
        }

        #region VNPay API

        public class VNpayIPNModel
        {
            public string code { get; set; }     // Required Max(10)
            public string message { get; set; }  // Required Max(100)
            public string msgType { get; set; } // Required Max(10)
            public string txnId { get; set; }   // Required Max(20)
            public string qrTrace { get; set; } // Required Max(10)
            public string bankCode { get; set; }  // Required Max(10)
            public string? mobile { get; set; }  // OPTIONAL Max(20)
            public string? accountNo { get; set; }  // OPTIONAL Max(30)
            public string amount { get; set; }  // Required Max(13)
            public string payDate { get; set; }  // Required Max(14)
            public string merchantCode { get; set; }  // Required Max(20)
            public string terminalId { get; set; }  // Required Max(8)
            public string? name { get; set; }  // OPTIONAL Max(100)
            public string? phone { get; set; }  // OPTIONAL Max(20)
            public string? province_id { get; set; }  // OPTIONAL Max(14)
            public string? district_id { get; set; }  // OPTIONAL Max(14)
            public string? address { get; set; }  // OPTIONAL Max(100)
            public string? email { get; set; }  // OPTIONAL Max(100)
            public object? addData { get; set; }  // OPTIONAL FREE
            public string checksum { get; set; }  // Required Max(32)
            public string? ccy { get; set; }  // Required Max(32)
            public string? secretKey { get; set; }  // Required Max(32)
            public string? masterMerCode { get; set; }
        }

        public class VNpayIPNtxnid
        {
            public string? txnId { get; set; }
        }

        public class VNpayIPNForApp
        {
            public string vnp_Amount { get; set; }   
            public string vnp_BankCode { get; set; }    
            public string ? vnp_BankTranNo { get; set; }     
            public string ? vnp_CardType { get; set; }     
            public string vnp_OrderInfo { get; set; }     
            public string ? vnp_PayDate { get; set; }     
            public string vnp_ResponseCode { get; set; }     
            public string vnp_TmnCode { get; set; }     
            public string vnp_TransactionNo { get; set; }     
            public string vnp_TransactionStatus { get; set; }     
            public string vnp_TxnRef { get; set; }     
            public string vnp_SecureHash { get; set; }     
        }

        public HttpRequest GetRequest()
        {
            return Request;
        }


        [HttpGet("VnpIPNForApp")]
        public IActionResult VnpIPNForApp(
            decimal vnp_Amount,
            string vnp_BankCode,
            string? vnp_BankTranNo,
            string? vnp_CardType,
            string vnp_OrderInfo,
            string? vnp_PayDate,
            string vnp_ResponseCode,
            string vnp_TmnCode,
            int vnp_TransactionNo,
            string vnp_TransactionStatus,
            string vnp_TxnRef,
            string vnp_SecureHash)
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                try
                {
                    VnPayLibrary Vnpay = new VnPayLibrary();
                    var vnp_HashSecret = this.Config.GetValue<string>("VnpSecretKey");
                    //-----------
                    string myHash = "";
                    SortedList<String, String> myHashFull = new SortedList<String, String>();
                    myHashFull.Add("vnp_Amount", vnp_Amount.ToString());
                    myHashFull.Add("vnp_BankCode", vnp_BankCode);
                    myHashFull.Add("vnp_BankTranNo", vnp_BankTranNo);
                    myHashFull.Add("vnp_CardType", vnp_CardType);
                    myHashFull.Add("vnp_OrderInfo", vnp_OrderInfo);
                    myHashFull.Add("vnp_PayDate", vnp_PayDate);
                    myHashFull.Add("vnp_ResponseCode", vnp_ResponseCode.ToString());
                    myHashFull.Add("vnp_TmnCode", vnp_TmnCode);
                    myHashFull.Add("vnp_TransactionNo", vnp_TransactionNo.ToString());
                    myHashFull.Add("vnp_TransactionStatus", vnp_TransactionStatus.ToString());
                    myHashFull.Add("vnp_TxnRef", vnp_TxnRef);

                    StringBuilder mydata = new StringBuilder();
                    foreach (KeyValuePair<string, string> kv in myHashFull)
                    {
                        if (!String.IsNullOrEmpty(kv.Value))
                        {
                            mydata.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                        }
                    }
                    if (mydata.Length > 0)
                    {
                        mydata.Remove(mydata.Length - 1, 1);
                    }

                    myHash = mydata.ToString();
                    string myFullQuery = myHash + "&vnp_SecureHash=" + vnp_SecureHash;
                    string myChecksum = Vnpay.getchecksumnow(vnp_HashSecret, myHash);
                    var remoteIpAddress = Request.HttpContext.Connection.RemoteIpAddress;

                    VnpIPNLog myIPN = new VnpIPNLog();
                    myIPN.vnp_Amount = vnp_Amount.ToString();
                    myIPN.vnp_BankCode = vnp_BankCode;
                    myIPN.vnp_BankTranNo = vnp_BankTranNo;
                    myIPN.vnp_SecureHashType = vnp_CardType;
                    myIPN.vnp_OrderInfo = vnp_OrderInfo;
                    myIPN.vnp_PayDate = vnp_PayDate;
                    myIPN.vnp_ResponseCode = vnp_ResponseCode.ToString();
                    myIPN.vnp_TmnCode = vnp_TmnCode;
                    myIPN.vnp_BankTranNo = vnp_BankTranNo;
                    myIPN.vnp_TransactionNo = vnp_TransactionNo.ToString();
                    myIPN.vnp_TransactionStatus = vnp_TransactionStatus.ToString();
                    myIPN.vnp_TxnRef = vnp_TxnRef;
                    myIPN.vnp_SecureHash = vnp_SecureHash;
                    myIPN.message = "IPN_URL";
                    myIPN.DateCreate = DateTime.Now;
                    if (remoteIpAddress.ToString() == null)
                    {
                        myIPN.remoteIpAddress = "";
                    }
                    else
                    {
                        myIPN.remoteIpAddress = remoteIpAddress.ToString();
                    }

                    myIPN.originalQuery = myFullQuery;
                    dbContext.VnpIPNLogs.Add(myIPN);
                    dbContext.SaveChanges();

                    bool checkSignature = myChecksum.Equals(vnp_SecureHash, StringComparison.InvariantCultureIgnoreCase);

                    if (checkSignature)
                    {
                        var myDeposit = dbContext.DepositHistorys.Where(p => p.DepositCode == vnp_TxnRef).FirstOrDefault();
                        if (myDeposit != null)
                        {
                            if ((myDeposit.Amount * 100) == vnp_Amount)
                            {
                                if (myDeposit.IsStatus != "success")
                                {
                                    if (vnp_ResponseCode == "00")  
                                    {
                                        myDeposit.IsStatus = "success";
                                        myDeposit.BankTransID = vnp_TransactionNo.ToString();
                                        myDeposit.BankTransCode = vnp_BankCode;
                                        myDeposit.BankTransDesc = vnp_OrderInfo;
                                        myDeposit.BankTransStatus = int.Parse(vnp_ResponseCode);
                                        myDeposit.BankTransTime = DateTime.Now;
                                        myDeposit.Remark = "Normal deposit|No Promotion";
                                        dbContext.SaveChanges();

                                        UserApp userApp = dbContext.UserApps.Find(myDeposit.UserAppId);
                                        userApp.Balance += myDeposit.Amount;
                                        
                                        dbContext.SaveChanges();

                                        ApiResponseVnpApp ApiResponse = new ApiResponseVnpApp("00", "Confirm Success");
                                        return new JsonResult(ApiResponse);
                                    }
                                    else if (vnp_ResponseCode == "24")
                                    {
                                        myDeposit.IsStatus = "cancel";
                                        myDeposit.BankTransID = vnp_TransactionNo.ToString();
                                        myDeposit.BankTransCode = vnp_BankCode;
                                        myDeposit.BankTransDesc = vnp_OrderInfo;
                                        myDeposit.BankTransStatus = int.Parse(vnp_ResponseCode);
                                        myDeposit.BankTransTime = DateTime.Now;
                                        dbContext.SaveChanges();
                                        string responsecode = vnp_ResponseCode.ToString();
                                        ApiResponseVnpApp ApiResponse = new ApiResponseVnpApp("00", "Confirm Success");
                                        return new JsonResult(ApiResponse);
                                    }
                                    else
                                    {
                                        myDeposit.IsStatus = "fail";
                                        myDeposit.BankTransID = vnp_TransactionNo.ToString();
                                        myDeposit.BankTransCode = vnp_BankCode;
                                        myDeposit.BankTransDesc = vnp_OrderInfo;
                                        myDeposit.BankTransStatus = int.Parse(vnp_ResponseCode);
                                        myDeposit.BankTransTime = DateTime.Now;
                                        dbContext.SaveChanges();
                                        string responsecode = vnp_ResponseCode.ToString();
                                        ApiResponseVnpApp ApiResponse = new ApiResponseVnpApp("00", "Confirm Success");
                                        return new JsonResult(ApiResponse);
                                    }
                                }
                                else
                                {
                                    ApiResponseVnpApp ApiResponse = new ApiResponseVnpApp("02", "Order already confirmed");
                                    return new JsonResult(ApiResponse);
                                }
                            }
                            else
                            {
                                ApiResponseVnpApp ApiResponse = new ApiResponseVnpApp("04", "Invalid amount");
                                return new JsonResult(ApiResponse);
                            }
                        }
                        else
                        {
                            ApiResponseVnpApp ApiResponse = new ApiResponseVnpApp("01", "Order not found");
                            return new JsonResult(ApiResponse);
                        }
                    }
                    else
                    {
                        ApiResponseVnpApp ApiResponse = new ApiResponseVnpApp("97", "Invalid signature");
                        return new JsonResult(ApiResponse);
                    }
                }
                catch (Exception)
                {
                    ApiResponseVnpApp ApiResponse = new ApiResponseVnpApp("99", "Unknow error");
                    return new JsonResult(ApiResponse);
                }
            }
        }


        static string CalculateMD5Hash(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            using (MD5 md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2")); 
                }

                return sb.ToString();
            }
        }


        [HttpPost("VnpQRTinhIPN")]
        public async Task<string> VnpQRTinhIPN([FromBody] VNpayIPNModel VNpayIPNModel)
        {
            var serverUrl = this.Config.GetValue<string>("UrlServer");
            using (var dbContext = new OCPPCoreContext(this.Config))
            {
                try
                {
                    #region code xu ly
                    VNpayIPNtxnid tnxidReturn = new VNpayIPNtxnid();
                    tnxidReturn.txnId = VNpayIPNModel.txnId;

                    var remoteIpAddress = Request.HttpContext.Connection.RemoteIpAddress;

                    //Xử lý chuỗi nhận từ VNPAY vào database
                    StaticQRIPNLog StaticQRIPNLog = new StaticQRIPNLog();

                    StaticQRIPNLog.code = VNpayIPNModel.code;
                    StaticQRIPNLog.message = VNpayIPNModel.message;
                    StaticQRIPNLog.txnId = VNpayIPNModel.txnId;
                    StaticQRIPNLog.amount = VNpayIPNModel.amount;
                    StaticQRIPNLog.payDate = VNpayIPNModel.payDate;
                    StaticQRIPNLog.merchantCode = VNpayIPNModel.merchantCode;
                    StaticQRIPNLog.terminalId = VNpayIPNModel.terminalId;
                    StaticQRIPNLog.qrTrace = VNpayIPNModel.qrTrace;
                    StaticQRIPNLog.checksum = VNpayIPNModel.checksum;

                    if (remoteIpAddress.ToString() == null)
                    {
                        StaticQRIPNLog.address = "";  // detect IP WAN of IPN
                    }
                    else
                    {
                        StaticQRIPNLog.address = remoteIpAddress.ToString();  // detect IP WAN of IPN
                    }
                    dbContext.StaticQRIPNLogs.Add(StaticQRIPNLog);
                    dbContext.SaveChanges();

                    //kiểm tra CHECKSUM và response
                    var secretKey = this.Config.GetValue<string>("VnpSecretKeyQRTinh");
                    // var secretKey1 = "SolarEVHCM";
                    string hashData = VNpayIPNModel.code + "|" + VNpayIPNModel.msgType + "|" + VNpayIPNModel.txnId + "|" + VNpayIPNModel.qrTrace + "|" + VNpayIPNModel.bankCode + "|" + VNpayIPNModel.mobile + "|" + VNpayIPNModel.accountNo + "|" + VNpayIPNModel.amount + "|" + VNpayIPNModel.payDate + "|" + VNpayIPNModel.merchantCode + "|" + secretKey;
                    string isChecksum = CalculateMD5Hash(hashData);

                    if (isChecksum.ToLower() != VNpayIPNModel.checksum.ToLower())
                    {
                        ResponseLog ResponseLog = new ResponseLog();
                        ResponseLog.isType = VNpayIPNModel.txnId;
                        ResponseLog.isResponse = "checksum_invalid: " + isChecksum;
                        ResponseLog.CreateDate = DateTime.Now;
                        dbContext.ResponseLogs.Add(ResponseLog);

                        //ghi log lý do returnCode về vnpay
                        StaticQRIPNLog.bankCode = "06";
                        StaticQRIPNLog.masterMerCode = "Sai thông tin xác thực";
                        dbContext.SaveChanges();

                        ApiResponseVnpay ApiResponse = new ApiResponseVnpay("06", "sai thông tin xác thực", tnxidReturn);
                        var ApiResponseStr = JsonConvert.SerializeObject(ApiResponse);
                        return ApiResponseStr;
                    }
                    else
                    {
                        ResponseLog ResponseLog = new ResponseLog();
                        ResponseLog.isType = VNpayIPNModel.txnId;
                        ResponseLog.isResponse = "checksum_valid: " + isChecksum;
                        ResponseLog.CreateDate = DateTime.Now;
                        dbContext.ResponseLogs.Add(ResponseLog);
                        dbContext.SaveChanges();

                        // BẮT ĐẦU XỬ LÝ TẠO TRANSACTION REMOTE CHO ĐƠN HÀNG
                        string ChargePointId = VNpayIPNModel.terminalId;
                        var newchargeid = dbContext.ConnectorStatuses.Where(c => c.terminalId == VNpayIPNModel.terminalId).FirstOrDefault().ChargePointId;

                        //STEP 0:  Kiểm tra có mã terminalID trong bảng hay không
                        if (newchargeid != null)
                        {
                            //STEP 1:  Kiểm tra trụ có ONLINE không. Dựa theo heartbeat bảng.
                            ConnectorStatus ConnectorStatus = dbContext.ConnectorStatuses.Where(m => m.ChargePointId == newchargeid).FirstOrDefault();
                            if (ConnectorStatus != null)
                            {
                                //STEP 1.1:  Kiểm tra trụ có ONLINE không. Dựa theo heartbeat bảng.
                                if ((DateTime.Now - ConnectorStatus.lastSeen).Value.TotalMinutes > 15)
                                {
                                    //ghi log lý do returnCode về vnpay
                                    StaticQRIPNLog.bankCode = "01";
                                    StaticQRIPNLog.masterMerCode = "Trụ đang Offline quá 15 phút";
                                    dbContext.SaveChanges();
                                    ApiResponseVnpay ApiResponse = new ApiResponseVnpay("00", "đặt hàng thành công", tnxidReturn);
                                    var ApiResponseStr = JsonConvert.SerializeObject(ApiResponse);
                                    return ApiResponseStr;
                                }
                                else
                                {
                                    //STEP 1.2:  Kiểm tra xem ConnectorID có đang Occupied hay Available
                                    //int myConnectorId = dbContext.ConnectorStatuses.Where(c => c.terminalId == VNpayIPNModel.terminalId).FirstOrDefault().ConnectorId; //get connectorID của terminalID này
                                    int myConnectorId = ConnectorStatus.ConnectorId;
                                    var connectorCheck = dbContext.ConnectorStatuses.Where(p => p.ChargePointId == newchargeid && p.ConnectorId == myConnectorId).FirstOrDefault().LastStatus;
                                    if (connectorCheck == null || connectorCheck == "Occupied")
                                    {
                                        //ghi log lý do returnCode về vnpay
                                        StaticQRIPNLog.bankCode = "02";
                                        StaticQRIPNLog.masterMerCode = "Súng chỉ định đang được sử dụng";
                                        dbContext.SaveChanges();
                                        ApiResponseVnpay ApiResponse = new ApiResponseVnpay("00", "đặt hàng thành công", tnxidReturn);
                                        var ApiResponseStr = JsonConvert.SerializeObject(ApiResponse);
                                        return ApiResponseStr;
                                    }
                                    else
                                    {
                                        //STEP 2:  Tiến hành Remote Start transaction đơn hàng.  ==> sau đó return luôn cho Vnpay, không gọi thêm gì nữa
                                        string apiUrl = serverUrl + "/RemoteStartTransactionQR?id=" + ChargePointId + "&Amount=" + VNpayIPNModel.amount + "&QrSource=VNPay" + "&QrTrace=" + VNpayIPNModel.qrTrace;
                                        HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);

                                        //ghi log lý do returnCode về vnpay
                                        StaticQRIPNLog.bankCode = "00";
                                        StaticQRIPNLog.masterMerCode = "Gửi lệnh đặt hàng thành công";
                                        dbContext.SaveChanges();
                                        //Response về kết quả cho VNpay
                                        ApiResponseVnpay ApiResponse = new ApiResponseVnpay("00", "đặt hàng thành công", tnxidReturn);
                                        var ApiResponseStr = JsonConvert.SerializeObject(ApiResponse);
                                        return ApiResponseStr;
                                    }
                                }
                            }
                            else   //Trường hợp không tìm thấy Connector trong bảng nghĩa là Mã Trụ gửi đến không đúng.
                            {
                                StaticQRIPNLog.bankCode = "03";
                                StaticQRIPNLog.masterMerCode = "Không tìm thấy ChargepointID";
                                dbContext.SaveChanges();
                                ApiResponseVnpay ApiResponse = new ApiResponseVnpay("00", "đặt hàng thành công", tnxidReturn);
                                var ApiResponseStr = JsonConvert.SerializeObject(ApiResponse);
                                return ApiResponseStr;
                            }
                        }
                        else
                        {
                            StaticQRIPNLog.bankCode = "04";
                            StaticQRIPNLog.masterMerCode = "Không có terminalID đăng ký";
                            dbContext.SaveChanges();
                            ApiResponseVnpay ApiResponse = new ApiResponseVnpay("00", "đặt hàng thành công", tnxidReturn);
                            var ApiResponseStr = JsonConvert.SerializeObject(ApiResponse);
                            return ApiResponseStr;
                        }
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    VNpayIPNtxnid tnxidReturn = new VNpayIPNtxnid();
                    tnxidReturn.txnId = VNpayIPNModel.txnId;

                    var remoteIpAddress = Request.HttpContext.Connection.RemoteIpAddress;
                    //Xử lý chuỗi nhận từ VNPAY vào database
                    StaticQRIPNLog StaticQRIPNLog = new StaticQRIPNLog();

                    StaticQRIPNLog.code = VNpayIPNModel.code;
                    StaticQRIPNLog.message = VNpayIPNModel.message;
                    StaticQRIPNLog.txnId = VNpayIPNModel.txnId;
                    StaticQRIPNLog.amount = VNpayIPNModel.amount;
                    StaticQRIPNLog.payDate = VNpayIPNModel.payDate;
                    StaticQRIPNLog.merchantCode = VNpayIPNModel.merchantCode;
                    StaticQRIPNLog.terminalId = VNpayIPNModel.terminalId;
                    StaticQRIPNLog.qrTrace = VNpayIPNModel.qrTrace;
                    StaticQRIPNLog.checksum = VNpayIPNModel.checksum;

                    if (remoteIpAddress.ToString() == null)
                    {
                        StaticQRIPNLog.address = "";  // detect IP WAN of IPN
                    }
                    else
                    {
                        StaticQRIPNLog.address = remoteIpAddress.ToString();  // detect IP WAN of IPN
                    }
                    //ghi log lý do returnCode về vnpay
                    StaticQRIPNLog.bankCode = "05";
                    StaticQRIPNLog.masterMerCode = ex.Message.ToString();
                    dbContext.StaticQRIPNLogs.Add(StaticQRIPNLog);
                    dbContext.SaveChanges();

                    ApiResponseVnpay ApiResponse = new ApiResponseVnpay("00", "đặt hàng thành công", tnxidReturn);
                    var ApiResponseStr = JsonConvert.SerializeObject(ApiResponse);
                    return ApiResponseStr;
                }
            }
        }


        #endregion

        #region Get Information

        [HttpGet("GetExchangeRate")]
        public IActionResult GetExchangeRate()
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                try
                {
                    var model = dbContext.Unitprices.Where(p => p.IsActive == 1).FirstOrDefault().Price;
                    ApiResponse ApiResponse = new ApiResponse(200, "Đơn giá sạc", model);
                    return new JsonResult(ApiResponse);
                }
                catch (Exception ex)
                {
                    ApiResponse ApiResponse = new ApiResponse(400, ex.Message, null);
                    return new JsonResult(ApiResponse);
                }
            }
        }


        [HttpGet("GetListChargeStation")]
        public IActionResult GetListChargeStation()
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                try
                {
                    var model = dbContext.ChargeStations.Where(p =>p.Name != "App Mobile").OrderByDescending(o =>o.ChargeStationId).ToList();
                    ApiResponse ApiResponse = new ApiResponse(200, "Danh sách Trạm sạc", model);
                    return new JsonResult(ApiResponse);
                }
                catch (Exception ex)
                {
                    ApiResponse ApiResponse = new ApiResponse(400, ex.Message, null);
                    return new JsonResult(ApiResponse);
                }
            }
        }

        [HttpGet("Getlistuser")]
        public IActionResult GetListUser()
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                try
                {
                    var model = dbContext.UserApps.OrderByDescending(m => m.CreateDate).ToList();
                    ApiResponse ApiResponse = new ApiResponse(200, "Danh sách User", model);
                    return new JsonResult(ApiResponse);
                }
                catch (Exception ex)
                {
                    ApiResponse ApiResponse = new ApiResponse(400, "Lỗi khi lấy Danh sách User", null);
                    return new JsonResult(ApiResponse);
                }


            }
        }

        [HttpGet("GetlistChargePoint")]
        public IActionResult GetListChargePoint(int ChargeStationId)
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                try
                {
                    if (ChargeStationId == null)
                        ChargeStationId = 0;
                    var model = dbContext.ChargePoints.Where(m => ChargeStationId == 0 || m.ChargeStationId == ChargeStationId).ToList();
                    ApiResponse ApiResponse = new ApiResponse(200, "Danh sách Trụ sạc", model);
                    return new JsonResult(ApiResponse);
                }
                catch (Exception ex)
                {
                    ApiResponse ApiResponse = new ApiResponse(400, "Lỗi khi lấy Danh sách Trụ sạc", null);
                    return new JsonResult(ApiResponse);
                }

            }
        }

        [HttpGet("GetChargePointbyId")]
        public IActionResult GetChargePointbyId(string ChargePointId)
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                try
                {
                    string ChargePointIdSplit = "";
                    ChargePointIdSplit = ChargePointId.Substring(0, ChargePointId.Length - 1);
                    var model = dbContext.ChargePoints.Where(m => m.ChargePointId.ToLower() == ChargePointIdSplit.ToLower()).FirstOrDefault();
                    if(model == null)
                    {
                        model = dbContext.ChargePoints.Where(m => m.ChargePointId.ToLower() == ChargePointId.ToLower()).FirstOrDefault();
                    }
                    ApiResponse ApiResponse = new ApiResponse(200, "Thông tin trụ sạc", model);
                    return new JsonResult(ApiResponse);
                }
                catch
                {
                    ApiResponse ApiResponse = new ApiResponse(400, "Lỗi khi lấy Thông tin trụ sạc", null);
                    return new JsonResult(ApiResponse);
                }
            }
        }
        [HttpGet("GetlistTransactions")]
        public IActionResult GetlistTransactions(string TagId)
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                try
                {
                    // var model = dbContext.Transactions.Where(m => m.StartTagId.ToLower() == TagId.ToLower()).ToList(); 
                    var model = (from t in dbContext.Transactions.Where(m => m.StartTagId.ToLower() == TagId.ToLower() && m.StopReason != null).ToList()
                                 join p in dbContext.ChargePoints
                                 on t.ChargePointId equals p.ChargePointId
                                 join st in dbContext.ChargeStations
                                 on p.ChargeStationId equals st.ChargeStationId
                                 select new VM_Transaction()
                                 {
                                     TransactionId=t.TransactionId,
                                     ChargePointId=p.ChargePointId,
                                     ChargePointName=p.Name,
                                     ChargeStationName=st.Name,
                                     ConnectorId=t.ConnectorId,
                                     StartTagId=t.StartTagId,
                                     StartTime=t.StartTime,
                                     MeterStart = (double)t.MeterStart,
                                     StartResult=t.StartResult,
                                     StopTagId=t.StopTagId,
                                     StopTime=t.StopTime,
                                     MeterStop=t.MeterStop,
                                     StopReason=t.StopReason ,
                                     ChargeStationAddress = st.Address
                                 }
                               ).OrderByDescending(m=>m.TransactionId).ToList();
                    foreach (var item in model)
                    {
                        item.StartTime = item.StartTime.AddHours(7);
                        item.StopTime = item.StopTime.HasValue? item.StopTime.Value.AddHours(7):DateTime.MinValue;
                    }
                    ApiResponse ApiResponse = new ApiResponse(200, "Danh sách lịch sử sạc", model);
                    return new JsonResult(ApiResponse);
                }
                catch
                {
                    ApiResponse ApiResponse = new ApiResponse(400, "Lỗi khi lấy Danh sách lịch sử sạc", null);
                    return new JsonResult(ApiResponse);
                }
            }
        }

        [HttpGet("GetlistTransactionDetail")]
        public IActionResult GetlistTransactionDetail(int TransactionId)
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                try
                {
                    var model = (from t in dbContext.Transactions.Where(m => m.TransactionId== TransactionId).ToList()
                                 join p in dbContext.ChargePoints
                                 on t.ChargePointId equals p.ChargePointId
                                 join st in dbContext.ChargeStations
                                 on p.ChargeStationId equals st.ChargeStationId
                                 join w in dbContext.WalletTransactions 
                                 on t.TransactionId equals w.TransactionId into tr
                                 from wtr in tr.DefaultIfEmpty()

                                 select new VM_Transaction()
                                 {
                                     TransactionId = t.TransactionId,
                                     ChargePointId = p.ChargePointId,
                                     ChargePointName = p.Name,
                                     ChargeStationName = st.Name,
                                     ConnectorId = t.ConnectorId,
                                     StartTagId = t.StartTagId,
                                     StartTime = t.StartTime,
                                     MeterStart = (double) t.MeterStart,
                                     StartResult = t.StartResult,
                                     StopTagId = t.StopTagId,
                                     StopTime = t.StopTime,
                                     MeterStop = t.MeterStop,
                                     StopReason = t.StopReason,
                                     ChargeStationAddress=st.Address,
                                     meterValue = wtr!=null? wtr.meterValue:0,
                                     Amount = wtr != null ? wtr.Amount:0,
                                     ExchangeRate = wtr != null ? wtr.ExchangeRate.Value:0,
                                     stopMethod  = wtr != null  ? wtr.stopMethod:""                                  
                                 }
                               ).ToList();
                    foreach (var item in model)
                    {
                        item.StartTime = item.StartTime.AddHours(7);
                        item.StopTime = item.StopTime.HasValue ? item.StopTime.Value.AddHours(7) : DateTime.MinValue;
                    }
                    ApiResponse ApiResponse = new ApiResponse(200, "Danh sách lịch sử sạc", model);
                    return new JsonResult(ApiResponse);
                }
                catch
                {
                    ApiResponse ApiResponse = new ApiResponse(400, "Lỗi khi lấy Danh sách lịch sử sạc", null);
                    return new JsonResult(ApiResponse);
                }
            }
        }
        #endregion

        #region Login 'user

        [HttpPost("login")]
        public IActionResult Login([FromBody] UserApp UserApp)
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                if (Request.Headers.ContainsKey("Authorization"))
                {
                    string token = Request.Headers["Authorization"];
                }
                var checklogin = dbContext.UserApps.Where(m => m.isActive == 1).ToList().Where(m => m.UserName.ToLower() == UserApp.UserName.ToLower() && Decrypt(m.Password) == UserApp.Password).FirstOrDefault();
                if (checklogin != null)
                {
                    if (string.IsNullOrEmpty(UserApp.Platform))
                    {
                        checklogin.Platform = "n/a";
                    }
                    else
                    {
                        checklogin.Platform = UserApp.Platform;
                    }
                    if (string.IsNullOrEmpty(UserApp.Version))
                    {
                        checklogin.Version = "n/a";
                    }
                    else
                    {
                        checklogin.Version = UserApp.Version;
                    }
                    dbContext.SaveChanges();
                    ApiResponse ApiResponse = new ApiResponse(200, "Đăng nhập thành công", checklogin);
                    return new JsonResult(ApiResponse);
                }
                else
                {
                    ApiResponse ApiResponse = new ApiResponse(404, "Đăng nhâp thất bại", null);
                    return new JsonResult(ApiResponse);
                }

            }

        }

        private static string GenerateToken(string username, string email)
        {
            var secretKey = "your-secret-key";
            var expires = DateTime.UtcNow.AddDays(1);

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        new Claim(ClaimTypes.Name, username),
        new Claim("email", email)
    };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: expires,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEF123456789";
            Random random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public string Encrypt(string stringToEncrypt)
        {
            string sEncryptionKey = "!#$a54?3";
            byte[] key = { };
            byte[] IV = { 10, 20, 30, 40, 50, 60, 70, 80 };
            byte[] inputByteArray; 
            try
            {
                key = Encoding.UTF8.GetBytes(sEncryptionKey.Substring(0, 8));
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                inputByteArray = Encoding.UTF8.GetBytes(stringToEncrypt);
                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(key, IV), CryptoStreamMode.Write);
                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();

                return Convert.ToBase64String(ms.ToArray());
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        public string Decrypt(string stringToDecrypt)
        {
            string sEncryptionKey = "!#$a54?3";
            byte[] key = { };
            byte[] IV = { 10, 20, 30, 40, 50, 60, 70, 80 };

            byte[] inputByteArray = new byte[stringToDecrypt.Length];
            try
            {
                key = Encoding.UTF8.GetBytes(sEncryptionKey.Substring(0, 8));
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                inputByteArray = Convert.FromBase64String(stringToDecrypt);

                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(key, IV), CryptoStreamMode.Write);
                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();

                Encoding encoding = Encoding.UTF8;
                return encoding.GetString(ms.ToArray());
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }
        public static bool ContainsSpecialCharacters(string input)
        {
            char[] specialCharacters = { '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', ' ', ',', '.', ':', ';', '<', '>', '?', '/', '[', ']', '{', '}', '|', '\'', '"', '\\', '+', '-', '=', '_', '`', '~' };

            bool containsSpecialCharacters = input.Any(c => specialCharacters.Contains(c));

            return containsSpecialCharacters;
        }

        public static bool ContainsVietnameseCharacters(string input)
        {
            string vietnameseCharacters = "áàạảãăâđêôơưăắằẳẵặâấầẩẫậđếềểễệôốồổỗộơớờởỡợưứừửữự";

            string inputWithoutDiacritics = input;

            bool containsVietnameseCharacters = ContainsAnyCharacter(inputWithoutDiacritics, vietnameseCharacters);

            return containsVietnameseCharacters;
        }
     
        public static bool ContainsAnyCharacter(string input, string characters)
        {
            foreach (char c in input)
            {
                if (characters.Contains(c))
                {
                    return true;
                }
            }

            return false;
        }

        public bool ValidateEmailAddress(string emailAddress)
        {
            var pattern = @"^[a-zA-Z0-9.!#$%&'*+-/=?^_`{|}~]+@[a-zA-Z0-9-]+(?:\.[a-zA-Z0-9-]+)*$";

            var regex = new Regex(pattern);
            return regex.IsMatch(emailAddress);
        }

        public bool ValidatePhoneNumber(string Phone)
        {
            try
            {
                if (string.IsNullOrEmpty(Phone))
                    return false;
                var r = new Regex(@"^\(?([0-9]{3})\)?[-.●]?([0-9]{3})[-.●]?([0-9]{4})$");
                return r.IsMatch(Phone);

            }
            catch (Exception)
            {
                throw;
            }
        }

        public static bool ValidateUsingEmailAddressAttribute(string emailAddress)
        {
            var emailValidation = new EmailAddressAttribute();
            return emailValidation.IsValid(emailAddress);
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] UserApp UserApp)
        {
            ApiResponse ApiResponse;
            using (var dbContext = new OCPPCoreContext(this.Config))
            {
                try
                {
                    if (string.IsNullOrEmpty(UserApp.UserName))
                    {
                        ApiResponse = new ApiResponse(409, "Username không được để trống", null);
                        return new JsonResult(ApiResponse);
                    }
                    if (string.IsNullOrEmpty(UserApp.Password))
                    {
                        ApiResponse = new ApiResponse(409, "Password không được để trống", null);
                        return new JsonResult(ApiResponse);
                    }
                    if (ContainsVietnameseCharacters(UserApp.UserName))
                        {
                        ApiResponse = new ApiResponse(409, "Username không hợp lệ, không được có dấu", null);
                        return new JsonResult(ApiResponse);
                    }
                    if (ContainsSpecialCharacters(UserApp.UserName))
                    {
                        ApiResponse = new ApiResponse(409, "Username không hợp lệ, không được chứa ký tự đặc biệt, khoảng trắng hoặc có dấu", null);
                        return new JsonResult(ApiResponse);
                    }
                    
                    if (string.IsNullOrEmpty(UserApp.Email))
                    {
                        ApiResponse = new ApiResponse(409, "Email không được để trống", null);
                        return new JsonResult(ApiResponse);
                    }
                    if (ValidateUsingEmailAddressAttribute(UserApp.Email) == false)
                    {
                        ApiResponse = new ApiResponse(409, "Email không hợp lệ, vui lòng nhập lại", null);
                        return new JsonResult(ApiResponse);
                    }

                    if (dbContext.UserApps.Any(m => m.UserName.ToLower() == UserApp.UserName.ToLower()))
                    {
                        ApiResponse = new ApiResponse(409, "Tài khoản đã tồn tại, vui lòng chọn tài khoản khác", null);
                        return new JsonResult(ApiResponse);
                    }
                    if (dbContext.UserApps.Any(m => m.Email.ToLower() == UserApp.Email.ToLower()))
                    {
                        ApiResponse = new ApiResponse(409, "Email đã tồn tại, vui lòng chọn Email khác", null);
                        return new JsonResult(ApiResponse);
                    }
                    if (!string.IsNullOrEmpty(UserApp.Phone) && dbContext.UserApps.Any(m => m.Phone.ToLower() == UserApp.Phone.ToLower()))
                    {
                        ApiResponse = new ApiResponse(409, "Số điện thoại đã tồn tại, vui lòng chọn số điện thoại khác", null);
                        return new JsonResult(ApiResponse);
                    }
                    
                    var randomTagId = GenerateRandomString(8);
                    
                    while (dbContext.ChargeTags.Any(m => m.TagId == randomTagId))
                    {
                        randomTagId = GenerateRandomString(8);
                    }
                    if (UserApp.Phone == "" || UserApp.Phone == null)
                        UserApp.Phone = null;
                    if (UserApp.Email == "" || UserApp.Email == null)
                        UserApp.Email = null;
                    UserApp.Id = randomTagId;
                    UserApp.Password = Encrypt(UserApp.Password); ;
                    UserApp.CreateDate = DateTime.Now;
                    UserApp.Balance = 0;
                    UserApp.isActive = 1;
                    UserApp.OwnerId = 3;  
                    dbContext.UserApps.Add(UserApp);
                    dbContext.SaveChanges();

                    ChargeTag chargeTag = new ChargeTag();
                    chargeTag.TagId = randomTagId;
                    chargeTag.TagName = "ST-NO-" + (dbContext.ChargeTags.Count() + 1).ToString();
                    chargeTag.CreateDate = DateTime.Now;
                    chargeTag.SideId = "1";
                    chargeTag.ChargeStationId = 8;
                    chargeTag.TagState = "1";
                    chargeTag.Blocked = false;
                    chargeTag.TagDescription = "Thẻ App Mobile";
                    chargeTag.TagType = "APP_Mobile";
                    dbContext.ChargeTags.Add(chargeTag);
                    dbContext.SaveChanges();
                    ApiResponse = new ApiResponse(200, "Đăng ký tài khoản thành công. Vui lòng cập nhật Email và Số điện thoại ở Profile để được hỗ trợ tính năng tốt hơn", UserApp);

                    return new JsonResult(ApiResponse);
                }
                catch (Exception ex)
                {
                    ApiResponse = new ApiResponse(400, "Tạo tài khoản thất bại, vui lòng thử lại", null);
                    return new JsonResult(ApiResponse);
                }
            }
        }

        [HttpPost("UpdateUser")]
        public IActionResult UpdateUser([FromBody] UserApp UserApp)
        {
            ApiResponse ApiResponse;
            using (var dbContext = new OCPPCoreContext(this.Config))
            {
                try
                {
                    var currentUser = dbContext.UserApps.Find(UserApp.Id);
                    if (currentUser != null)
                    {
                        currentUser.Fullname = UserApp.Fullname;
                        currentUser.Phone = UserApp.Phone;
                        currentUser.Email = UserApp.Email;
                        dbContext.SaveChanges();
                        currentUser = dbContext.UserApps.Find(UserApp.Id);
                        ApiResponse = new ApiResponse(200, "Cập nhật tài khoản thành công", currentUser);
                        return new JsonResult(ApiResponse);
                    }
                    else
                    {
                        ApiResponse = new ApiResponse(400, "Tài khoản không tồn tại, vui lòng thử lại", null);
                        return new JsonResult(ApiResponse);
                    }
                   
                }
                catch(Exception ex)
                {
                    ApiResponse = new ApiResponse(400, "Cập nhật tài khoản thất bại, vui lòng thử lại", null);
                    return new JsonResult(ApiResponse);
                }
            }
             
        }
        
        [HttpPost("ChangePassword")]
        public IActionResult ChangePassword([FromBody] ChangePassword UserApp)
        {
            ApiResponse ApiResponse;
            using (var dbContext = new OCPPCoreContext(this.Config))
            {
                try
                {
                    if (UserApp.OldPassword == UserApp.NewPassword)
                    {
                        ApiResponse = new ApiResponse(400, "Mật khẩu cũ và mới không được trùng, vui lòng thử lại", null);
                        return new JsonResult(ApiResponse);
                    }

                    var currentUser = dbContext.UserApps.Where(m=>m.Id ==UserApp.UserAppId).FirstOrDefault();
                    if (currentUser != null)
                    {
                        if(currentUser.Password== Encrypt(UserApp.OldPassword))
                        {

                            currentUser.Password = Encrypt(UserApp.NewPassword);
                            dbContext.SaveChanges();
                            ApiResponse = new ApiResponse(200, "Đổi password thành công", currentUser);
                            return new JsonResult(ApiResponse);
                        }
                        else
                        {
                            ApiResponse = new ApiResponse(400, "Mật khẩu cũ không đúng, vui lòng thử lại", null);
                            return new JsonResult(ApiResponse);
                        }
                    }
                    else
                    {
                        ApiResponse = new ApiResponse(400, "Tài khoản không tồn tại, vui lòng thử lại", null);
                        return new JsonResult(ApiResponse);
                    }

                }
                catch (Exception ex)
                {
                    ApiResponse = new ApiResponse(400, "Đổi password thất bại, vui lòng thử lại", null);
                    return new JsonResult(ApiResponse);
                }
            }

        }

        [HttpGet("DeleteUser")]
        public IActionResult DeleteUser(string UserAppId)
        {
            ApiResponse ApiResponse;
            using (var dbContext = new OCPPCoreContext(this.Config))
            {
                try
                {
                    var currentUser = dbContext.UserApps.Where(m => m.Id == UserAppId).FirstOrDefault();
                    currentUser.isActive = 0;
                    dbContext.SaveChanges();
                    ApiResponse = new ApiResponse(200, "Xóa thành công", currentUser);
                    return new JsonResult(ApiResponse);
                }
                catch(Exception ex)
                {
                    ApiResponse = new ApiResponse(400, "Xóa thất bại, vui lòng thử lại", null);
                    return new JsonResult(ApiResponse);
                }
            }
        }

        [HttpGet("GetForgotPassword")]
        public IActionResult GetForgotPassword(string UserEmail)
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                try
                {
                    var checkExisting = dbContext.UserApps.Where(e => e.Email == UserEmail).ToList();
                    if (checkExisting.Count() > 0)
                    {
                        string myPassEncrypted = dbContext.UserApps.Where(p => p.Email == UserEmail).FirstOrDefault().Password.ToString();
                        string myPassDecrypted = Decrypt(myPassEncrypted);
                        string html = "";
                        html += "<div>";

                        html += "<p>Chúng tôi nhận được yêu cầu cấp lại Mật khẩu sử dụng cho Mobile App SolarEV từ quý khách.</p>";
                        html += "<p>Dưới đây là mật khẩu hiện tại của quý khách, vui lòng đổi sang một Mật Khẩu khác ngay sau khi đăng nhập thành công vào ứng dụng vào lần tới..</p>";
                        html += "<p>Your password: "+ myPassDecrypted + "</p>";
                        html += "<p>Cần thêm thông tin chi tiết hay hỗ trợ, vui lòng liên hệ bộ phận CSKH của công ty FOCUS SOLAR để được hướng dẫn thêm.</p>";
                        html += "</div>";

                        string _receiver = UserEmail;
                        string email = "notification@insitu.com.vn";
                        string password = "systemEmail*#2022";
                        string smtpserver = "bishan.apc.sg";

                        MailMessage mail = new MailMessage();
                        mail.From = new MailAddress(email);
                        mail.To.Add(_receiver);
                        mail.Bcc.Add("tuan.le@insitu.com.vn");
                        mail.Subject = "SolarEV trạm sạc xe điện - Password recovery";
                        mail.Body = html;
                        mail.IsBodyHtml = true;
                        SmtpClient smtpClient = new SmtpClient(smtpserver, 587);
                        smtpClient.Credentials = new NetworkCredential(email, password);
                        smtpClient.EnableSsl = false;

                        try
                        {
                            smtpClient.Send(mail);
                            ApiResponse ApiResponse = new ApiResponse(200, "Vui lòng kiểm tra email để nhận lại mật khẩu", null);
                            return new JsonResult(ApiResponse);
                        }
                        catch (Exception ex)
                        {
                            ApiResponse ApiResponse = new ApiResponse(400, "Không thể gửi Email phục hồi Mật khẩu lúc này, vui lòng liên hệ CSKH để được hỗ trợ.", null);
                            return new JsonResult(ApiResponse);
                        }                      
                    }
                    else
                    {
                        ApiResponse ApiResponse = new ApiResponse(400, "Email không tồn tại, vui lòng nhập Email đã đăng ký", null);
                        return new JsonResult(ApiResponse);
                    }  
                }
                catch (Exception ex)
                {
                    ApiResponse ApiResponse = new ApiResponse(400, "Có lỗi xảy ra, vui lòng liên hệ CSKH để được hỗ trợ", null);
                    return new JsonResult(ApiResponse);
                }

            }
        }

        #endregion

        #region Deposit
        public class DepositRequest
        {
            public string UserAppId { get; set; }
            public decimal Amount { get; set; }
            public int PaymentGateway { get; set; }
            public string AppIpAddr { get; set; }
        }

        public class DepositRequest1
        {
            public string UserAppId { get; set; }
            public decimal Amount { get; set; }
            public int PaymentGateway { get; set; }
        }

        public class DepositRequestURL1
        {
            public string? payment_url { get; set; }
            public string? vnp_TxnRef { get; set; }
            public string? tmnCode { get; set; }
        }

        public class editTransID
        {
            public int mID { get; set; }
        }

        public static String HmacSHA512(string key, String inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }


        [HttpPost("AddDeposit1")]
        public IActionResult AddDeposit1([FromBody] DepositRequest DepositRequest)
        {
            try
            {
                //--------
                using (var dbContext = new OCPPCoreContext(this.Config))
                {
                    UserApp userApp = dbContext.UserApps.Find(DepositRequest.UserAppId);
                    if (userApp != null)
                    {
                        DepositHistory DepositHistory = new DepositHistory();

                        DepositHistory.Method = 1;
                        DepositHistory.IsStatus = "Waiting";
                        DepositHistory.UserAppId = userApp.Id;
                        DepositHistory.Amount = DepositRequest.Amount;
                        DepositHistory.Gateway = DepositRequest.PaymentGateway;
                        if (DepositRequest.PaymentGateway == 1)
                        {
                            DepositHistory.Message = "Nạp tiền từ VNPay";
                        }
                        else if (DepositRequest.PaymentGateway == 2)
                        {
                            DepositHistory.Message = "Nạp tiền từ VietQR";
                        }
                        else
                        {
                            DepositHistory.Message = "Cổng thanh toán khác";
                        }

                        DepositHistory.DepositCode = "WD" + DateTime.Now.ToString("yyyyMMddHHmmss");

                        decimal myCurrentBalance = dbContext.UserApps.Find(DepositRequest.UserAppId).Balance;
                        DepositHistory.currentBalance = myCurrentBalance;
                        decimal myNewBalance = myCurrentBalance + DepositRequest.Amount;
                        DepositHistory.NewBalance = myNewBalance;
                        DepositHistory.DateCreate = DateTime.Now;
                        var tmnCode = this.Config.GetValue<string>("vnp_TmnCode");

                        DepositRequestURL1 DepositRequestURL1 = new DepositRequestURL1();
                        string vnp_Url = "https://pay.vnpay.vn/vpcpay.html";// "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
                        string vnp_Amount = (DepositRequest.Amount * 100).ToString();
                        string vnp_CreateDate = DepositHistory.DateCreate.ToString("yyyyMMddHHmmss");
                        string vnp_IpAddr = DepositRequest.AppIpAddr; 
                        string vnp_OrderInfo = "NapTienVaoVi-" + DepositHistory.UserAppId + "";
                        string vnp_ReturnUrl = "https://focusev-admin.insitu.com.vn/ReturnUrl"; 
                        string vnp_ExpireDate = DateTime.Now.AddSeconds(300).ToString("yyyyMMddHHmmss"); 
                        string vnp_TmnCode = tmnCode; 
                        string vnp_TxnRef = DepositHistory.DepositCode.ToString();
                        string vnp_Version = "2.1.0";
                        string vnp_Command = "pay";
                        string vnp_CurrCode = "VND";
                        string vnp_Locale = "vn";
                        string vnp_OrderType = "other";

                        var vnp_HashSecret = this.Config.GetValue<string>("VnpSecretKey");

                        //VNPAY tao secture url
                        VnPayLibrary vnpay = new VnPayLibrary();
                        vnpay.AddRequestData("vnp_Version", vnp_Version); 
                        vnpay.AddRequestData("vnp_Command", vnp_Command);
                        vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
                        vnpay.AddRequestData("vnp_Amount", vnp_Amount); 
                        vnpay.AddRequestData("vnp_CreateDate", vnp_CreateDate);
                        vnpay.AddRequestData("vnp_CurrCode", vnp_CurrCode);
                        vnpay.AddRequestData("vnp_ExpireDate", vnp_ExpireDate);
                        vnpay.AddRequestData("vnp_IpAddr", vnp_IpAddr);
                        vnpay.AddRequestData("vnp_Locale", vnp_Locale);
                        vnpay.AddRequestData("vnp_OrderInfo", vnp_OrderInfo);
                        vnpay.AddRequestData("vnp_OrderType", vnp_OrderType); 
                        vnpay.AddRequestData("vnp_ReturnUrl", vnp_ReturnUrl);
                        vnpay.AddRequestData("vnp_TxnRef", vnp_TxnRef);
                        
                        string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

                        //--------------------------------------------
                        string lastFragment = paymentUrl.Split("vnp_SecureHash=").Last();
                        var result = paymentUrl.Substring(paymentUrl.LastIndexOf("vnp_SecureHash=") + 1);
                        DepositHistory.checksum = lastFragment; 
                        DepositHistory.remoteIPaddress = DepositRequest.AppIpAddr;
                        DepositHistory.paymentURL = paymentUrl;

                        dbContext.DepositHistorys.Add(DepositHistory);
                        dbContext.SaveChanges();

                        DepositRequestURL1.payment_url = paymentUrl;
                        DepositRequestURL1.vnp_TxnRef = vnp_TxnRef;
                        DepositRequestURL1.tmnCode = tmnCode;

                        ApiResponse ApiResponse = new ApiResponse(200, "Tạo đơn deposit đang xử lý thành công", DepositRequestURL1);
                        return new JsonResult(ApiResponse);
                    }
                    else
                    {
                        ApiResponse ApiResponse = new ApiResponse(400, "Tài khoản không tồn tại", null);
                        return new JsonResult(ApiResponse);
                    }
                }
            }
            catch (Exception ex)
            {
                ApiResponse ApiResponse = new ApiResponse(400, "Nạp tiền thất bại", null);
                return new JsonResult(ApiResponse);
            }
        }

        [HttpPost("AddDeposit")]
        public IActionResult AddDeposit([FromBody] DepositRequest DepositRequest)
        {
            try
            {
                //--------
                using (var dbContext = new OCPPCoreContext(this.Config))
                {
                    UserApp userApp = dbContext.UserApps.Find(DepositRequest.UserAppId);
                    if (userApp != null)
                    {
                        if (DepositRequest.Amount > 20000000)
                        {
                            ApiResponse ApiResponse = new ApiResponse(400, "Hiện không thể nạp số tiền lớn hơn 20,000,000 đ. Vui lòng thực hiện lại.", null);
                            return new JsonResult(ApiResponse);
                        }
                        else
                        {
                            #region xừ lý nạp tiền VNPAY
                            DepositHistory DepositHistory = new DepositHistory();

                            DepositHistory.Method = 1;
                            DepositHistory.IsStatus = "Waiting";
                            DepositHistory.UserAppId = userApp.Id;
                            DepositHistory.Amount = DepositRequest.Amount;
                            DepositHistory.Gateway = DepositRequest.PaymentGateway;
                            if (DepositRequest.PaymentGateway == 1)
                            {
                                DepositHistory.Message = "Nạp tiền từ VNPay";
                            }
                            else if (DepositRequest.PaymentGateway == 2)
                            {
                                DepositHistory.Message = "Nạp tiền từ VietQR";
                            }
                            else
                            {
                                DepositHistory.Message = "Cổng thanh toán khác";
                            }

                            DepositHistory.DepositCode = "WD" + DateTime.Now.ToString("yyyyMMddHHmmss"); 
                            decimal myCurrentBalance = dbContext.UserApps.Find(DepositRequest.UserAppId).Balance;
                            DepositHistory.currentBalance = myCurrentBalance;

                            decimal myNewBalance = myCurrentBalance + DepositRequest.Amount;
                            DepositHistory.NewBalance = myNewBalance;
                            DepositHistory.DateCreate = DateTime.Now;

                            var tmnCode = this.Config.GetValue<string>("vnp_TmnCode");

                            DepositRequestURL1 DepositRequestURL1 = new DepositRequestURL1();
                            string vnp_Url = "https://pay.vnpay.vn/vpcpay.html";
                            string vnp_Amount = (DepositRequest.Amount * 100).ToString();
                            string vnp_CreateDate = DepositHistory.DateCreate.ToString("yyyyMMddHHmmss");
                            string vnp_IpAddr = DepositRequest.AppIpAddr; 
                            string vnp_OrderInfo = "NapTienVaoVi-" + DepositHistory.UserAppId + "";
                            string vnp_ReturnUrl = "https://focusev-admin.insitu.com.vn/ReturnUrl"; 
                            string vnp_ExpireDate = DateTime.Now.AddSeconds(300).ToString("yyyyMMddHHmmss");  
                            string vnp_TmnCode = tmnCode; 
                            string vnp_TxnRef = DepositHistory.DepositCode.ToString();   
                            string vnp_Version = "2.1.0";
                            string vnp_Command = "pay";
                            string vnp_CurrCode = "VND";
                            string vnp_Locale = "vn";
                            string vnp_OrderType = "other";

                            var vnp_HashSecret = this.Config.GetValue<string>("VnpSecretKey");

                            VnPayLibrary vnpay = new VnPayLibrary();
                            vnpay.AddRequestData("vnp_Version", vnp_Version); 
                            vnpay.AddRequestData("vnp_Command", vnp_Command);
                            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
                            vnpay.AddRequestData("vnp_Amount", vnp_Amount); 
                            vnpay.AddRequestData("vnp_CreateDate", vnp_CreateDate);
                            vnpay.AddRequestData("vnp_CurrCode", vnp_CurrCode);
                            vnpay.AddRequestData("vnp_ExpireDate", vnp_ExpireDate);
                            vnpay.AddRequestData("vnp_IpAddr", vnp_IpAddr);
                            vnpay.AddRequestData("vnp_Locale", vnp_Locale);
                            vnpay.AddRequestData("vnp_OrderInfo", vnp_OrderInfo);
                            vnpay.AddRequestData("vnp_OrderType", vnp_OrderType);
                            vnpay.AddRequestData("vnp_ReturnUrl", vnp_ReturnUrl);
                            vnpay.AddRequestData("vnp_TxnRef", vnp_TxnRef);

                            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

                            //--------------------------------------------
                            string lastFragment = paymentUrl.Split("vnp_SecureHash=").Last();
                            var result = paymentUrl.Substring(paymentUrl.LastIndexOf("vnp_SecureHash=") + 1);
                            DepositHistory.checksum = lastFragment; 
                            DepositHistory.remoteIPaddress = DepositRequest.AppIpAddr;
                            DepositHistory.paymentURL = paymentUrl;

                            dbContext.DepositHistorys.Add(DepositHistory);
                            dbContext.SaveChanges();

                            DepositRequestURL1.payment_url = paymentUrl;
                            DepositRequestURL1.vnp_TxnRef = vnp_TxnRef;
                            DepositRequestURL1.tmnCode = tmnCode;

                            ApiResponse ApiResponse = new ApiResponse(200, "Tạo đơn deposit đang xử lý thành công", DepositRequestURL1);
                            return new JsonResult(ApiResponse);
                            #endregion
                        }
                    }
                    else
                    {
                        ApiResponse ApiResponse = new ApiResponse(400, "Tài khoản không tồn tại", null);
                        return new JsonResult(ApiResponse);
                    }
                }
            }
            catch (Exception ex)
            {
                ApiResponse ApiResponse = new ApiResponse(400, "Nạp tiền thất bại", null);
                return new JsonResult(ApiResponse);
            }
        }


        //AddDeposit - check valid 2,000,000 - CHƯA có nạp tiền từ VNpay hay VietQR (VPS nạp free)
        [HttpPost("AddDeposit_free")]
        public IActionResult AddDeposit_free([FromBody] DepositRequest1 DepositRequest)
        {
            try
            {
                using (var dbContext = new OCPPCoreContext(this.Config))
                {
                    UserApp userApp = dbContext.UserApps.Find(DepositRequest.UserAppId);
                    if (userApp != null)
                    {
                        if (DepositRequest.Amount > 5000000)
                        {
                            ApiResponse ApiResponse = new ApiResponse(400, "Hiện không thể nạp số tiền lớn hơn 5,000,000, vui lòng thực hiện lại.", null);
                            return new JsonResult(ApiResponse);
                        }
                        else
                        {
                            DepositHistory DepositHistory = new DepositHistory();
                            DepositHistory.UserAppId = userApp.Id;
                            DepositHistory.Amount = DepositRequest.Amount;
                            DepositHistory.Message = "Nạp tiền trực tiếp từ Solar Platform";
                            DepositHistory.DepositCode = "WD" + DateTime.Now.ToString("yyyyMMddHHmmss");

                            decimal myCurrentBalance = dbContext.UserApps.Find(DepositRequest.UserAppId).Balance;
                            DepositHistory.currentBalance = myCurrentBalance;
                            decimal myNewBalance = myCurrentBalance + DepositRequest.Amount;
                            DepositHistory.NewBalance = myNewBalance;
                            DepositHistory.Method = 1;
                            DepositHistory.IsStatus = "success";
                            DepositHistory.remoteIPaddress = "n/a";

                            DepositHistory.Gateway = DepositRequest.PaymentGateway;
                            DepositHistory.DateCreate = DateTime.Now;
                            DepositHistory.Remark = "Nap tien tu API|" + DateTime.Now.ToString() + "|";
                            dbContext.DepositHistorys.Add(DepositHistory);
                            dbContext.SaveChanges();

                            userApp.Balance += DepositRequest.Amount ;
                            dbContext.SaveChanges();

                            ApiResponse ApiResponse = new ApiResponse(200, "Nạp tiền thành công", DepositHistory);
                            return new JsonResult(ApiResponse);
                        }                   
                    }
                    else
                    {
                        ApiResponse ApiResponse = new ApiResponse(400, "Tài khoản không tồn tại", null);
                        return new JsonResult(ApiResponse);
                    }
                }
            }
            catch (Exception ex)
            {
                ApiResponse ApiResponse = new ApiResponse(400, "Nạp tiền thất bại", null);
                return new JsonResult(ApiResponse);
            }
        }



        [HttpGet("GetDepositHistory")]
        public IActionResult GetDepositHistory(string UserAppId)
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                try
                {
                    var model2 = dbContext.DepositHistorys.ToList();
                    var model = dbContext.DepositHistorys.Where(m => m.UserAppId == UserAppId && m.IsStatus == "success").OrderByDescending(m => m.DepositHistoryId).ToList();
                    ApiResponse ApiResponse = new ApiResponse(200, "Danh sách lịch sử nạp tiền", model);
                    return new JsonResult(ApiResponse);
                }
                catch (Exception ex)
                {
                    ApiResponse ApiResponse = new ApiResponse(400, "Lỗi khi lấy Danh lịch sử nạp tiền", null);
                    return new JsonResult(ApiResponse);
                }

            }
        }

        [HttpGet("GetCurrentBalance")]
        public IActionResult GetCurrentBalance(string UserAppId)
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                try
                {

                    var model = dbContext.UserApps.Where(m => m.Id == UserAppId).FirstOrDefault();
                    var balance = model?.Balance ?? 0;
                    ApiResponse ApiResponse = new ApiResponse(200, "Số dư hiện tại", balance);
                    return new JsonResult(ApiResponse);
                }
                catch (Exception ex)
                {
                    ApiResponse ApiResponse = new ApiResponse(400, "Lỗi khi lấy Số dư hiện tại", null);
                    return new JsonResult(ApiResponse);
                }

            }
        }
        HttpClient _httpClient = new HttpClient();
  
        public async Task SendMessageStart(string userTo,string Message,string promo_type)
        {
            string serverKey = "AAAAvXGNK6E:APA91bG7sMWvF2POHTv4RbGIbkH9fA0v_lDvS2GTTvMD5lJamx7mLR_Df6rPusr9JHD4J3ZSxiKQyfCIG9uqcXX2lDPbO0c7CK_zUnrfu_UTg69jHe_ruaVeQIL448HDTF1dLIo0JRFw";
            string fcmUrl = "https://fcm.googleapis.com/fcm/send";

            var jsonMessage = @"{
           ""to"": """ + userTo + @""",
            ""notification"": {
                ""title"": ""Thông báo"",
                ""body"": """+ Message+@"""
            },
            ""data"": {
                ""promo_type"": """+ promo_type+@""",
                ""title"": ""xxx"",
                ""body"": ""xxx"",
                ""click_action"": ""FLUTTER_NOTIFICATION_CLICK""
            }
        }";

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"key={serverKey}");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");

                var content = new StringContent(jsonMessage, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(fcmUrl, content);

                var responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"FCM response: {responseContent}");
            }
        }


        public bool checkTruBennyTonehe(string ChargePointId)
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                var charpoint = dbContext.ChargePoints.Find(ChargePointId);
                if (charpoint != null)
                {
                    if (charpoint.ChargePointModel.ToLower().Contains("benny") || charpoint.ChargePointModel.ToLower().Contains("tonhe"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool checkTruST(string ChargePointId)
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                var charpoint = dbContext.ChargePoints.Find(ChargePointId);
                if (charpoint != null)
                {
                    if (charpoint.ChargePointModel.ToLower().Contains("st"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        [HttpGet("RequireCharging")]
        public async Task<ActionResult<ApiResponse>> RequireCharging(string UserAppId, string ChargePointId, decimal Amount, int type)
        {

            var serverUrl = this.Config.GetValue<string>("UrlServer");
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                UserApp UserApp = dbContext.UserApps.Find(UserAppId);

                var newchargeid = ChargePointId.Substring(0, ChargePointId.Length - 1);
                ConnectorStatus ConnectorStatus = dbContext.ConnectorStatuses.Where(m => m.ChargePointId == newchargeid).FirstOrDefault();
                if (ConnectorStatus != null)
                {
                    if ((DateTime.Now - ConnectorStatus.lastSeen).Value.TotalMinutes > 15)
                    {
                        ApiResponse ApiResponse2 = new ApiResponse(400, "Trụ sạc đang mất kết nối đến máy chủ, vui lòng thử lại hoặc liên hệ CSKH", null);
                        return new JsonResult(ApiResponse2);
                    }
                }

                if (UserApp == null)
                {
                    ApiResponse ApiResponse2 = new ApiResponse(400, "Tài khoản của bạn không tồn tại trong hệ thống. Vui lòng liên hệ CSKH.  (Mã lỗi: RCCV02)", null);
                    return new JsonResult(ApiResponse2);
                }

                if (UserApp.Balance == 0 || UserApp.Balance < 0)
                {
                    ApiResponse ApiResponse2 = new ApiResponse(400, "Tài khoản của bạn không có tiền. Vui lòng nạp tiền vào ví và thử lại  (Mã lỗi: RCCV03)", null);
                    return new JsonResult(ApiResponse2);
                }

                if (UserApp.Balance > 0 && UserApp.Balance < 80000)
                {
                    ApiResponse ApiResponse2 = new ApiResponse(400, "Tài khoản tối thiểu của bạn không đủ để thực hiện đơn sạc mới. Vui lòng nạp thêm tiền vào ví (Mã lỗi: RCCV04)", null);
                    return new JsonResult(ApiResponse2);
                }

                WalletTransaction getWalletCurrent = dbContext.WalletTransactions.Where(w => w.UserAppId == UserAppId).OrderByDescending(q => q.WalletTransactionId).FirstOrDefault();
                if (getWalletCurrent != null)
                {
                    if (getWalletCurrent.currentBalance == null)
                    {
                        ApiResponse ApiResponse2 = new ApiResponse(400, "Tài khoản của bạn không thể thực hiện đơn sạc mới, vui lòng liên hệ bộ phận CSKH (Mã lỗi: RCCV05)", null);
                        return new JsonResult(ApiResponse2);
                    }
                }

                if (UserApp.OwnerId == 3 && UserApp.Balance > 1500000)
                {
                    ApiResponse ApiResponse2 = new ApiResponse(400, "Tài khoản của bạn không thể thực hiện đơn sạc mới, vui lòng liên hệ bộ phận CSKH (Mã lỗi: RCCV06)", null);
                    return new JsonResult(ApiResponse2);
                }

            }

            double backMeter = 0;
          
            try
            {
                string apiUrl = serverUrl + "/RemoteStartTransaction?id=" + ChargePointId + "&TagId=" + UserAppId;
                HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
                var newchargeid = ChargePointId.Substring(0, ChargePointId.Length - 1);
                TransactionVirtual checkTransactionVirtual=new TransactionVirtual();
                if (response.IsSuccessStatusCode)
                {
                    int timeout = 1;
                    using (var dbcontext= new OCPPCoreContext(this.Config))
                    {
                        var listTransactionVirtual = dbcontext.TransactionVirtuals.Where(m => m.ChargePointId == newchargeid).ToList();
                        dbcontext.RemoveRange(listTransactionVirtual);
                        dbcontext.SaveChanges();
                    }
                    using (var ndbContext = new OCPPCoreContext(this.Config))
                    {

                        checkTransactionVirtual = ndbContext.TransactionVirtuals.ToList().Where(m => m.ChargePointId == newchargeid && m.StartTagId == UserAppId).LastOrDefault();
                    }

                    while (checkTransactionVirtual == null)
                    {
                        using (var ndbContext = new OCPPCoreContext(this.Config))
                        {
                            checkTransactionVirtual = ndbContext.TransactionVirtuals.ToList().Where(m => m.ChargePointId == newchargeid && m.StartTagId == UserAppId).LastOrDefault();
                        }
                        if (timeout == 120)
                        {
                            ApiResponse ApiResponse2 = new ApiResponse(400, "Quá thời gian kết nối, vui lòng thực hiện lại đơn hàng trên trụ.", null);
                            return new JsonResult(ApiResponse2);
                        }
                        timeout += 1;
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    } 
                   
                }
                ApiResponse ApiResponse = new ApiResponse(200, "Trụ đang sạc ...", checkTransactionVirtual);
                return new JsonResult(ApiResponse);
            }
            catch (Exception ex)
            {
                ApiResponse ApiResponse2 = new ApiResponse(400, "Không thể kích hoạt trụ bây giờ, vui lòng thử lại hoặc liên hệ CSKH.", null);

                return new JsonResult(ApiResponse2);
            }

        }


        [HttpGet("RemoteStopCharging")]
        public IActionResult   RemoteStopCharging(string ChargePointId)
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                try
                {
                    Database.Transaction gettran = new Database.Transaction();
                    ApiResponse ApiResponse;
                    ChargePoint chkchargePoint = dbContext.ChargePoints.Find(ChargePointId);
                    if (chkchargePoint != null)
                    {
                        gettran = dbContext.Transactions.ToList().Where(m => m.ChargePointId == ChargePointId && m.MeterStop == null).LastOrDefault();
                        if(gettran!=null)
                        ReCallApi(ChargePointId, gettran.TransactionId);
                    }
                    else
                    {
                        string ChargePointIdSplit = "";
                        ChargePointIdSplit = ChargePointId.Substring(0, ChargePointId.Length - 1);

                        ChargePoint chkchargePointspl = dbContext.ChargePoints.Find(ChargePointIdSplit);
                        if (chkchargePointspl != null)
                        {
                            int ConnectorId = int.Parse(ChargePointId.Substring(ChargePointId.Length - 1, 1));
                            gettran = dbContext.Transactions.ToList().Where(m => m.ChargePointId == ChargePointIdSplit && m.MeterStop == null && m.ConnectorId == ConnectorId).LastOrDefault();
                            if (gettran != null)
                                ReCallApi(ChargePointIdSplit, gettran.TransactionId);
                        }
                        else
                        {
                            ApiResponse = new ApiResponse(400, "Không dừng đơn được do giá trị trụ không đúng. Vui lòg thử lại hoặc Dừng sạc từ xe điện", null);
                            return new JsonResult(ApiResponse);
                        }
                    }

                    if (gettran!=null)
                        ApiResponse = new ApiResponse(200, "Dừng sạc thành công", gettran);
                    else
                        ApiResponse = new ApiResponse(400, "Không dừng đơn được do giá trị trụ không đúng. Vui lòg thử lại hoặc Dừng sạc từ xe điện", null);

                    return new JsonResult(ApiResponse);
                }
                catch (Exception ex)
                {
                    ResponseLog responseLog = new ResponseLog();
                    responseLog.CreateDate = DateTime.Now;
                    responseLog.isType = "Lỗi remotestop";
                    responseLog.isResponse = ex.Message;
                    dbContext.ResponseLogs.Add(responseLog);
                    dbContext.SaveChanges();
                    ApiResponse ApiResponse = new ApiResponse(400, "Không dừng đơn được do giá trị trụ không đúng. Vui lòg thử lại hoặc Dừng sạc từ xe điện", null);
                    return new JsonResult(ApiResponse);
                }
            }
            
        }


        public async Task ReCallApi(string ChargePointId, int TransactionId)
        {
            var serverUrl = this.Config.GetValue<string>("UrlServer");
            string apiUrl = serverUrl+"/RemoteStopTransaction?id=" + ChargePointId + "&TransactionId="+TransactionId;
            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
            string responseContent = await response.Content.ReadAsStringAsync();
        }
        public ActionResult ChargingRespone(string UserAppId, string ChargePointId)
        {
            return null;
        }

        [HttpPost("UpdateTokenNotify")]
        public IActionResult UpdateTokenNotify([FromBody] UserApp UserApp)
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                var user = dbContext.UserApps.Find(UserApp.Id);
                if (user != null)
                {
                    user.TokenNotify = UserApp.TokenNotify;
                    try
                    {
                        dbContext.SaveChanges();
                        ApiResponse ApiResponse = new ApiResponse(200, "Cập nhật thành công", user);
                        return new JsonResult(ApiResponse);
                    }
                    catch(Exception ex)
                    {
                        ApiResponse ApiResponse = new ApiResponse(200, "Cập nhật thất bại", null);
                        return new JsonResult(ApiResponse);
                    }
                
                }
                else
                {
                    ApiResponse ApiResponse = new ApiResponse(200, "User không hợp lệ", null);
                    return new JsonResult(ApiResponse);
                }
            }
        }
        #endregion

        #region Transaction Wallet


                
        #endregion

     
        public class getMeterValue
        {
            public string Energy { get; set; }
            public string Power { get; set; }
            public string SoC { get; set; }
            public string Voltage{ get; set; }
            public string Temperature{ get; set; }
            public string Current{ get; set; }
        }


        [HttpGet("GetMeterValue")]
        public async Task<ActionResult<ApiResponse>> GetMeterValue(string ChargePointId)
        {
            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {

                string newCharpointId = "";
                int connectorId = 0;
                newCharpointId = ChargePointId.Substring(0, ChargePointId.Length - 1);
                connectorId = int.Parse(ChargePointId.Substring(ChargePointId.Length - 1, 1));
                DateTime getBeforeConnector;
                DateTime getAfterconnector;
                getBeforeConnector = dbContext.ConnectorStatuses.Where(m => m.ChargePointId == newCharpointId && m.ConnectorId == connectorId).FirstOrDefault().RemoteTime.Value;
                var serverUrl = this.Config.GetValue<string>("UrlServer");
                string apiUrl = serverUrl + "/TriggerMessages?id=" + ChargePointId + "&message=" + "MeterValues";
                HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
                using (OCPPCoreContext dbContextnew = new OCPPCoreContext(this.Config))
                {
                    getAfterconnector = dbContextnew.ConnectorStatuses.Where(m => m.ChargePointId == newCharpointId && m.ConnectorId == connectorId).FirstOrDefault().RemoteTime.Value;
                }
                int countdown = 0;
                while (getBeforeConnector == getAfterconnector)
                {
                    using (OCPPCoreContext dbContextnew2 = new OCPPCoreContext(this.Config))
                    {
                        getAfterconnector = dbContextnew2.ConnectorStatuses.Where(m => m.ChargePointId == newCharpointId && m.ConnectorId == connectorId).FirstOrDefault().RemoteTime.Value;
                        countdown += 1;
                        await Task.Delay(TimeSpan.FromSeconds(1));

                        if (countdown == 5)
                        {
                            ApiResponse ApiResponse2 = new ApiResponse(400, "Không thể lấy Meter value", null);
                            return new JsonResult(ApiResponse2);
                        }
                    }
                }
                string returnMetervalue ="";
                using (OCPPCoreContext dbContextlast = new OCPPCoreContext(this.Config))
                {
                    returnMetervalue = dbContextlast.ConnectorStatuses.Where(m => m.ChargePointId == newCharpointId && m.ConnectorId == connectorId).FirstOrDefault().LastMeterRemote;
                }
                getMeterValue getMeterValue = new getMeterValue();
                if (returnMetervalue != "")
                {
                    var getsplit = returnMetervalue.Split('|');
                    getMeterValue.Energy = getsplit[0]=="0"?"-1":getsplit[0];
                    if (!string.IsNullOrEmpty(getMeterValue.Energy))
                    {
                        var gettransaction = dbContext.Transactions.ToList().Where(m => m.StopTagId == null).LastOrDefault();
                        if (gettransaction != null)
                        {
                            getMeterValue.Energy = Math.Round((decimal)(double.Parse(getMeterValue.Energy) - gettransaction.MeterStart), 2).ToString();
                        }
                    }
                    getMeterValue.Power = getsplit[1]=="0"?"-1":getsplit[1];
                    getMeterValue.SoC = getsplit[2] == "0" ? "-1" : getsplit[2];
                    getMeterValue.Voltage = getsplit[3] == "0" ? "-1" : getsplit[3];
                    getMeterValue.Temperature = getsplit[4] == "0" ? "-1" : getsplit[4];
                    getMeterValue.Current = getsplit[5] == "0" ? "-1" : getsplit[5] ;
                }
                ApiResponse ApiResponse = new ApiResponse(200, "Meter value", getMeterValue);
                return new JsonResult(ApiResponse);
            }
        }
    }
}
