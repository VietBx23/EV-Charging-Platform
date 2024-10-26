
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FocusEV.OCPP.Database;
using FocusEV.OCPP.Server.Messages_OCPP16;
using System.Net.Http;
using System.Text;

namespace FocusEV.OCPP.Server
{
    public partial class ControllerOCPP16
    {
        public string HandleStartTransaction(OCPPMessage msgIn, OCPPMessage msgOut)
        {
            string errorCode = null;
            StartTransactionResponse startTransactionResponse = new StartTransactionResponse();
            string tagid = "";
            int connectorId = -1;

            try
            {
                Logger.LogTrace("Processing startTransaction request...");
                StartTransactionRequest startTransactionRequest = JsonConvert.DeserializeObject<StartTransactionRequest>(msgIn.JsonPayload);
                Logger.LogTrace("StartTransaction => Message deserialized");

                string idTag = CleanChargeTagId(startTransactionRequest.IdTag, Logger);
                connectorId = startTransactionRequest.ConnectorId;

                startTransactionResponse.IdTagInfo.ParentIdTag = string.Empty;
                startTransactionResponse.IdTagInfo.ExpiryDate = MaxExpiryDate;

                if (string.IsNullOrWhiteSpace(idTag))
                {
                    // no RFID-Tag => accept request
                    startTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Accepted;
                    Logger.LogInformation("StartTransaction => no charge tag => Status: {0}", startTransactionResponse.IdTagInfo.Status);
                }
                else
                {
                    try
                    {
                        using (OCPPCoreContext dbContext = new OCPPCoreContext(Configuration))
                        {
                            ChargeTag ct = dbContext.Find<ChargeTag>(idTag);
                            if (ct != null)
                            {
                               
                                if (ct.ExpiryDate.HasValue) startTransactionResponse.IdTagInfo.ExpiryDate = ct.ExpiryDate.Value;
                                startTransactionResponse.IdTagInfo.ParentIdTag = ct.ParentTagId;
                                if (ct.Blocked.HasValue && ct.Blocked.Value)
                                {
                                    startTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Blocked;
                                }
                                else if (ct.ExpiryDate.HasValue && ct.ExpiryDate.Value < DateTime.Now)
                                {
                                    startTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Expired;
                                }
                                else
                                {
                                    startTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Accepted;
                                }
                            }
                            else
                            {
                                startTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Invalid;
                            }

                            Logger.LogInformation("StartTransaction => Charge tag='{0}' => Status: {1}", idTag, startTransactionResponse.IdTagInfo.Status);
                        }
                    }
                    catch (Exception exp)
                    {
                        Logger.LogError(exp, "StartTransaction => Exception reading charge tag ({0}): {1}", idTag, exp.Message);
                        startTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Invalid;
                    }
                }

                if (connectorId > 0)
                {
                    // Update meter value in db connector status 
                    UpdateConnectorStatus(connectorId, ConnectorStatusEnum.Occupied.ToString(), startTransactionRequest.Timestamp, (double)startTransactionRequest.MeterStart / 1000, startTransactionRequest.Timestamp);
                }

                if (startTransactionResponse.IdTagInfo.Status == IdTagInfoStatus.Accepted)
                {
                    try
                    {
                        using (OCPPCoreContext dbContext = new OCPPCoreContext(Configuration))
                        {
                            Transaction transaction = new Transaction();
                            transaction.ChargePointId = ChargePointStatus?.Id;
                            tagid = transaction.ChargePointId;
                            transaction.ConnectorId = startTransactionRequest.ConnectorId;
                            transaction.StartTagId = idTag;
                            transaction.StartTime = startTransactionRequest.Timestamp.UtcDateTime;
                            transaction.MeterStart = (double)startTransactionRequest.MeterStart / 1000; // Meter value here is always Wh
                            transaction.StartResult = startTransactionResponse.IdTagInfo.Status.ToString();
                            dbContext.Add<Transaction>(transaction);
                            dbContext.SaveChanges();

                            // Return DB-ID as transaction ID
                            startTransactionResponse.TransactionId = transaction.TransactionId;
                            MessageLog MessageLog = new MessageLog();
                            MessageLog.ChargePointId = "MT0009";
                            var checkuser = dbContext.UserApps.Find(transaction.StartTagId);

                            //Check if Tagid was userappid
                            var checkUser=dbContext.UserApps.Find(transaction.StartTagId);
                            if (checkUser != null)
                            {
                                WalletTransaction WalletTransaction = new WalletTransaction();
                                WalletTransaction.DateCreate= transaction.StartTime;
                                WalletTransaction.UserAppId = transaction.StartTagId;
                                WalletTransaction.TransactionId = transaction.TransactionId;
                                var getExchangeRate = dbContext.Unitprices.Where(m => m.IsActive == 1).FirstOrDefault();
                                WalletTransaction.ExchangeRate = getExchangeRate != null ? getExchangeRate.Price : 0;
                                UserApp us = dbContext.UserApps.Find(WalletTransaction.UserAppId);
                                WalletTransaction.currentBalance= us!=null? us.Balance : 0;
                                if(WalletTransaction.currentBalance>(92 * WalletTransaction.ExchangeRate))
                                {
                                    WalletTransaction.chargeType = "normal";
                                }
                                else
                                {
                                    WalletTransaction.chargeType = "valueControl";
                                    WalletTransaction.upperLimit = WalletTransaction.currentBalance.Value / WalletTransaction.ExchangeRate.Value;
                                }
                                dbContext.WalletTransactions.Add(WalletTransaction);
                                dbContext.SaveChanges();

                                //first delete record
                             
                                //Create transaction Virtual 
                                TransactionVirtual tv = new TransactionVirtual();
                                tv.TransactionId = transaction.TransactionId;
                                tv.StartTagId = transaction.StartTagId;
                                tv.StartTime = transaction.StartTime;
                                tv.ChargePointId = transaction.ChargePointId;
                                tv.upperLimit = WalletTransaction.upperLimit.HasValue?WalletTransaction.upperLimit.Value:0;
                                dbContext.TransactionVirtuals.Add(tv);
                                dbContext.SaveChanges();
                                                          
                                //If a meter control
                                //Trigger Datatranfer
                                //Uperlimit wallet
                                if (WalletTransaction.chargeType == "valueControl")
                                {
                                    //Only send datatransfer if chargepoint is ST
                                    var getChargePointInfo = dbContext.ChargePoints.Where(m => m.ChargePointId == transaction.ChargePointId).FirstOrDefault();
                                    if (getChargePointInfo != null)
                                    {
                                        if(!getChargePointInfo.ChargePointModel.ToLower().Contains("tonhe") && !getChargePointInfo.ChargePointModel.ToLower().Contains("benny"))
                                        {
                                            var barcodeId = transaction.ChargePointId + transaction.ConnectorId.ToString();
                                            ChargingSchedule(barcodeId, (WalletTransaction.currentBalance.Value / WalletTransaction.ExchangeRate.Value)- (decimal)0.05, transaction.TransactionId);
                                        }
                                    }
                                }
                            }
                            //Kiểm tra trường hợp QR_Payment
                            if (checkUser == null)
                            {
                                var ChargeTags = dbContext.ChargeTags.Where(m => m.TagId == transaction.StartTagId && m.TagType == "QR_Payment").FirstOrDefault();
                                if (ChargeTags != null)
                                {

                                    //Create QRTransaction
                                    TransactionVirtualQR TransactionVirtualQR = dbContext.TransactionVirtualQRs.ToList().Where(m => m.ChargePointId == transaction.ChargePointId && m.ConnectorId == transaction.ConnectorId).LastOrDefault();
                                    TransactionVirtualQR.TransactionId = transaction.TransactionId;
                                    dbContext.SaveChanges();
                                     var getExchangeRate = dbContext.Unitprices.Where(m => m.IsActive == 1).FirstOrDefault();
                                    QRTransaction QRTransaction = new QRTransaction();
                                    QRTransaction.ExchangeRate= getExchangeRate != null ? getExchangeRate.Price : 0;
                                    QRTransaction.TransactionId = transaction.TransactionId;
                                    QRTransaction.QrTagId = ChargeTags.TagId;
                                    QRTransaction.StartTime = transaction.StartTime;
                                    QRTransaction.ChargingAmount = TransactionVirtualQR.Amount;
                                    QRTransaction.EndTime = DateTime.Now;
                                    QRTransaction.UpperLimit = TransactionVirtualQR != null ? TransactionVirtualQR.Amount / QRTransaction.ExchangeRate : 0 ;
                                    QRTransaction.QrSource = "VNPay";
                                    QRTransaction.qrTrace = TransactionVirtualQR.qrTrace;
                                    dbContext.QRTransactions.Add(QRTransaction);
                                    //Update
                                   
                                    dbContext.SaveChanges();
                                    var terminalId = transaction.ChargePointId + transaction.ConnectorId.ToString();
                                    ChargingSchedule(terminalId, decimal.Parse(QRTransaction.UpperLimit.ToString()), transaction.TransactionId);

                                }

                            }
                        }

                    }
                    catch (Exception exp)
                    {
                        using (OCPPCoreContext dbContext = new OCPPCoreContext(Configuration))
                        {
                            MessageLog msg = new MessageLog();
                            msg.LogTime = DateTime.Now;
                            msg.ChargePointId = ChargePointStatus?.Id;
                            msg.ConnectorId = startTransactionRequest.ConnectorId;
                            msg.Message = "Bat đầu sạc";
                            msg.Result = "Thành công";

                            msg.ErrorCode = exp.Message;
                            dbContext.SaveChanges();
                        }
                          
                        Logger.LogError(exp, "StartTransaction => Exception writing transaction: chargepoint={0} / tag={1}", ChargePointStatus?.Id, idTag);
                        errorCode = ErrorCodes.InternalError;
                    }
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(startTransactionResponse);

                Logger.LogTrace("StartTransaction => Response serialized");
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "StartTransaction => Exception: {0}", exp.Message);
                errorCode = ErrorCodes.FormationViolation;
            }

            WriteMessageLog(ChargePointStatus?.Id, connectorId, msgIn.Action, startTransactionResponse.IdTagInfo?.Status.ToString(), errorCode);
            return errorCode;
        }
        public async Task ChargingSchedule(string ChargePointId,decimal upperLimit, int TransactionId)
        {
            var serverUrl = "http://103.77.167.17:8481";
           // var serverUrl = "http://localhost:8081";
            string apiUrlTrigger = serverUrl + "/ChargingSchedule?id=" + ChargePointId + "&upperLimit=" + upperLimit + "&transactionid=" + TransactionId + "";
            HttpResponseMessage responseTrigger = await _httpClient.GetAsync(apiUrlTrigger);
        }
        public async Task SendMessageDevice(string userTo)
        {
            string serverKey = "AAAAvXGNK6E:APA91bG7sMWvF2POHTv4RbGIbkH9fA0v_lDvS2GTTvMD5lJamx7mLR_Df6rPusr9JHD4J3ZSxiKQyfCIG9uqcXX2lDPbO0c7CK_zUnrfu_UTg69jHe_ruaVeQIL448HDTF1dLIo0JRFw";
            string fcmUrl = "https://fcm.googleapis.com/fcm/send";

            var jsonMessage = @"{
           ""to"": """ + userTo + @""",
            ""notification"": {
                ""title"": ""Thông báo"",
                ""body"": ""Xe đang bắt đầu sạc, vui lòng chờ...""
            },
            ""data"": {
                ""promo_type"": ""CHARGE_START"",
                ""title"": ""fdsf"",
                ""body"": ""fdsfs"",
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
    }
}
