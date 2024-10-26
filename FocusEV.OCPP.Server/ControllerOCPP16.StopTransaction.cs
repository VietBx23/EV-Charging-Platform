
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
using Newtonsoft.Json.Linq;
using Microsoft.EntityFrameworkCore;

namespace FocusEV.OCPP.Server
{
    public partial class ControllerOCPP16
    {
        public bool checkTime(DateTime tm1,DateTime tm2)
        {
            return tm1.Date == tm2.Date && tm1.Hour == tm2.Hour && tm1.Minute == tm2.Minute;
        }
        public string HandleStopTransaction(OCPPMessage msgIn, OCPPMessage msgOut)
        {
         

            string errorCode = null;
            LogStopTransaction logStopTransaction = null;
            StopTransactionResponse stopTransactionResponse = new StopTransactionResponse();
            logStopTransaction = new LogStopTransaction();
            try
            {
                Logger.LogTrace("Processing stopTransaction request...");
                StopTransactionRequest stopTransactionRequest = JsonConvert.DeserializeObject<StopTransactionRequest>(msgIn.JsonPayload);
                Logger.LogTrace("StopTransaction => Message deserialized");
                
                OCPPCoreContext dbContextlog = new OCPPCoreContext(Configuration);
                try
                {
                  
                    logStopTransaction.Timestart = DateTime.Now;
                    logStopTransaction.Descriptions += "State 1 started | ";
                    logStopTransaction.Timestop = DateTime.Now;
                    dbContextlog.LogStopTransactions.Add(logStopTransaction);
                    logStopTransaction.StopTransactionResponse = msgIn.JsonPayload;
                    logStopTransaction.TransactionId = stopTransactionRequest.TransactionId;
                    var findLogstop = dbContextlog.LogStopTransactions.Any(m => m.TransactionId == stopTransactionRequest.TransactionId);
                    if (!findLogstop)
                    {
                        dbContextlog.SaveChanges();
                    }

                }
                catch (Exception ex)
                {

                }
               
                string idTag = CleanChargeTagId(stopTransactionRequest.IdTag, Logger);

                if (string.IsNullOrEmpty(idTag) )
                {
                    using (OCPPCoreContext dbContext = new OCPPCoreContext(Configuration))
                    {
                        var transac = dbContext.Transactions.Find(stopTransactionRequest.TransactionId);
                        idTag = transac.StartTagId;
                    }
                       
                }

                if (string.IsNullOrWhiteSpace(idTag))
                {
                    // no RFID-Tag => accept request
                    stopTransactionResponse.IdTagInfo = new IdTagInfo();
                    stopTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Accepted;
                    Logger.LogInformation("StopTransaction => no charge tag => Status: {0}", stopTransactionResponse.IdTagInfo.Status);
                }
                else
                {
                    stopTransactionResponse.IdTagInfo = new IdTagInfo();
                    stopTransactionResponse.IdTagInfo.ExpiryDate = MaxExpiryDate;

                    try
                    {
                        using (OCPPCoreContext dbContext = new OCPPCoreContext(Configuration))
                        {
                            ChargeTag ct = dbContext.Find<ChargeTag>(idTag);
                            if (ct != null)
                            {
                                if (ct.ExpiryDate.HasValue) stopTransactionResponse.IdTagInfo.ExpiryDate = ct.ExpiryDate.Value;
                                stopTransactionResponse.IdTagInfo.ParentIdTag = ct.ParentTagId;
                                if (ct.Blocked.HasValue && ct.Blocked.Value)
                                {
                                    stopTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Blocked;
                                }
                                else if (ct.ExpiryDate.HasValue && ct.ExpiryDate.Value < DateTime.Now)
                                {
                                    stopTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Expired;
                                }
                                else
                                {
                                    stopTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Accepted;
                                }
                            }
                            else
                            {
                                stopTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Invalid;
                            }

                            Logger.LogInformation("StopTransaction => RFID-tag='{0}' => Status: {1}", idTag, stopTransactionResponse.IdTagInfo.Status);

                            logStopTransaction.Descriptions += "| State 2" + " | ";
                            dbContextlog.SaveChanges();
                        }


                    }
                    catch (Exception exp)
                    {
                        Logger.LogError(exp, "StopTransaction => Exception reading charge tag ({0}): {1}", idTag, exp.Message);
                        stopTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Invalid;
                        logStopTransaction.Descriptions += "| State 2" + exp.Message+" | ";
                        dbContextlog.SaveChanges();
                    }
                }

                if (stopTransactionResponse.IdTagInfo.Status == IdTagInfoStatus.Accepted)
                {
                    try
                    {
                        using (OCPPCoreContext dbContext = new OCPPCoreContext(Configuration))
                        {
                            Transaction transaction = dbContext.Find<Transaction>(stopTransactionRequest.TransactionId);

                            if (transaction == null ||
                                transaction.ChargePointId != ChargePointStatus.Id ||
                                transaction.StopTime.HasValue)
                            {
                                Logger.LogWarning("StopTransaction => Unknown or closed transaction id={0}", transaction?.TransactionId);
                                transaction = dbContext.Transactions
                                    .Where(t => t.ChargePointId == ChargePointStatus.Id)
                                    .OrderByDescending(t => t.TransactionId)
                                    .FirstOrDefault();

                                if (transaction != null)
                                {                                 
                                    if (transaction.StopTime.HasValue)
                                    {
                                        Logger.LogTrace("StopTransaction => Last transaction (id={0}) is already closed ", transaction.TransactionId);
                                        ResponseLog ResponseLog = new ResponseLog();
                                        ResponseLog.CreateDate = DateTime.Now;
                                        ResponseLog.isType = "Thiết lập lại transaction = null";
                                        ResponseLog.isResponse = "transaction.ChargePointId:" + transaction.ChargePointId + " ChargePointStatus.Id:" + ChargePointStatus.Id + " StartTagId " + transaction.StartTagId + " StartTime " + transaction.StartTime + " transaction.StopTime:" + stopTransactionRequest.Timestamp + " transaction.TransactionId:" + transaction.TransactionId + " stopTransactionRequest.TransactionId:" + stopTransactionRequest.TransactionId + " meterstop " + stopTransactionRequest.MeterStop + " stoptagid " + stopTransactionRequest.IdTag + " Meter start " + transaction.MeterStart;
                                        dbContext.ResponseLogs.Add(ResponseLog);
                                        dbContext.SaveChanges();

                                        transaction = null;

                                        logStopTransaction.Descriptions += "| State 3" + " | ";
                                        dbContextlog.SaveChanges();                                       
                                    }
                                    else
                                    {
                                        Logger.LogInformation("INS_log [154] Tìm đơn last transaction => KHÔNG có StopTime.HasValue => đơn đúng =>  lastTransID={0} / requestStopTransID={1}", transaction.TransactionId, stopTransactionRequest.TransactionId);  
                                    }
                                }
                                else
                                {
                                    logStopTransaction.Descriptions += "| State 3 " + "StopTransaction => Found no transaction for charge point " + ChargePointStatus.Id+ " | ";
                                    dbContextlog.SaveChanges();
                                    Logger.LogTrace("StopTransaction => Found no transaction for charge point '{0}'", ChargePointStatus.Id);
                                }
                            }

                            if (transaction != null)
                            {
                                logStopTransaction.Descriptions += "| State 4" + " | ";
                                dbContextlog.SaveChanges();
                                if (transaction.ConnectorId > 0)
                                {
                                    // Update meter value in db connector status 
                                    UpdateConnectorStatus(transaction.ConnectorId, null, null, (double)stopTransactionRequest.MeterStop / 1000, stopTransactionRequest.Timestamp);

                                    try
                                    {
                                        var getTagId = transaction.StartTagId;
                                        var userApp = dbContext.UserApps.Find(getTagId);
                                        string body = "";
                                        WalletTransaction findwallet = dbContext.WalletTransactions.Where(m => m.TransactionId == transaction.TransactionId).FirstOrDefault();
                                        //App used
                                        if (findwallet != null)
                                        {
                                            if (findwallet.chargeType == "normal")
                                            {
                                                SendMessage(userApp.TokenNotify, "Quý khách đã sạc thành công, vui lòng rút sạc");
                                            }
                                            if (findwallet.chargeType == "valueControl")
                                            {
                                                if (transaction.StopReason != "Remote")
                                                    SendMessage(userApp.TokenNotify, "Đơn  sạc thành công, vui lòng rút sạc");

                                                else
                                                    SendMessage(userApp.TokenNotify, "Đơn sạc đã dừng do tiền trong ví không đủ, vui lòng nạp thêm và thực hiện lại đơn sạc");
                                            }
                                        }
                                        //QRCode used
                                        else
                                        {
                                            if (userApp != null)
                                                SendMessage(userApp.TokenNotify, "Quý khách đã sạc thành công, vui lòng rút sạc");
                                        }

                                    }
                                    catch (Exception ex)
                                    {
                                        logStopTransaction.Descriptions += "| State 4" + ex.Message+ "" + " | ";
                                        dbContextlog.SaveChanges();

                                        Logger.LogError("StopTransaction => lỗi mobile app : '{0}'", ex.Message);
                                        ResponseLog ResponseLog = new ResponseLog();
                                        ResponseLog.CreateDate = DateTime.Now;
                                        ResponseLog.isType = "Check lỗi remote stop {Thông báo app}";
                                        ResponseLog.isResponse = ex.Message;
                                        dbContext.ResponseLogs.Add(ResponseLog);
                                        dbContext.SaveChanges();
                                    }
                                }

                                // check current tag against start tag
                                bool valid = true;
                                if (!string.Equals(transaction.StartTagId, idTag, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    // tags are different => same group?
                                    ChargeTag startTag = dbContext.Find<ChargeTag>(transaction.StartTagId);
                                    if (startTag != null)
                                    {
                                        if (!string.Equals(startTag.ParentTagId, stopTransactionResponse.IdTagInfo.ParentIdTag, StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            Logger.LogInformation("StopTransaction => Start-Tag ('{0}') and End-Tag ('{1}') do not match: Invalid!", transaction.StartTagId, idTag);
                                            stopTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Invalid;
                                            valid = false;
                                        }
                                        else
                                        {
                                            Logger.LogInformation("StopTransaction => Different RFID-Tags but matching group ('{0}')", stopTransactionResponse.IdTagInfo.ParentIdTag);
                                        }
                                    }
                                    else
                                    {
                                        Logger.LogError("StopTransaction => Start-Tag not found: '{0}'", transaction.StartTagId);
                                        // assume "valid" and allow to end the transaction
                                    }
                                }

                                logStopTransaction.Descriptions += "| Valid=" + valid + "" + " | ";
                                dbContextlog.SaveChanges();

                                if (valid)
                                {
                                    logStopTransaction.Descriptions += "| State 5" + " | ";
                                    dbContextlog.SaveChanges();
                                    if (stopTransactionRequest != null)
                                    {
                                        Transaction getlastTransaction = new Transaction();
                                        int totalTimes = 1;
                                        getlastTransaction = dbContext.Transactions.ToList().Where(m => m.ChargePointId == ChargePointStatus.Id).LastOrDefault();
                                        //loop searching
                                        OCPPCoreContext dbnewContext = new OCPPCoreContext(Configuration);
                                        while (getlastTransaction.StopTime.HasValue && totalTimes < 20)
                                        {
                                            using (dbnewContext = new OCPPCoreContext(Configuration))
                                            {
                                                getlastTransaction = dbnewContext.Transactions.ToList().Where(m => m.ChargePointId == ChargePointStatus.Id && m.ConnectorId == transaction.ConnectorId).LastOrDefault();
                                                totalTimes += 1;
                                            }
                                        }

                                        if (getlastTransaction.TransactionId == stopTransactionRequest.TransactionId)
                                        {
                                            getlastTransaction.StopTagId = idTag;
                                            getlastTransaction.MeterStop = (double)stopTransactionRequest.MeterStop / 1000; // Meter value here is always Wh
                                            getlastTransaction.StopReason = stopTransactionRequest.Reason.ToString();
                                            getlastTransaction.StopTime = stopTransactionRequest.Timestamp.UtcDateTime;
                                            dbContext.SaveChanges();

                                            logStopTransaction.Descriptions += "| State 5_1 (" + getlastTransaction.MeterStop + ") | ";
                                            dbContextlog.SaveChanges();
                                        }

                                        if (getlastTransaction.TransactionId != stopTransactionRequest.TransactionId)
                                        {

                                            WalletTransaction fwl = dbContext.WalletTransactions.Where(m => m.UserAppId == getlastTransaction.StartTagId && m.newBalance == null).FirstOrDefault();

                                            if (fwl != null)
                                            {
                                                logStopTransaction.Descriptions += "| fwl (not null)" + " | ";
                                                if (fwl.TransactionId == getlastTransaction.TransactionId)
                                                {
                                                    //Lấy metervalue trong bảng ConnectorStatus
                                                    var getConnectorStatus = dbContext.ConnectorStatuses.Where(m => m.ChargePointId == getlastTransaction.ChargePointId && m.ConnectorId == getlastTransaction.ConnectorId).FirstOrDefault();
                                                    if (getConnectorStatus != null)
                                                    {
                                                        try
                                                        {
                                                            getlastTransaction.StopReason = "Success [354]";
                                                            getlastTransaction.MeterStop = getConnectorStatus.LastMeterRemote != null ? double.Parse(getConnectorStatus.LastMeterRemote.Split('|')[0]) : 0;
                                                                getlastTransaction.StopTime = DateTime.UtcNow;
                                                                getlastTransaction.StopTagId = idTag;
                                                                dbContext.SaveChanges();
                                                            if (totalTimes == 1)
                                                                dbContext.SaveChanges();
                                                            else
                                                                dbnewContext.SaveChanges();

                                                            logStopTransaction.Descriptions += "| State 5_2_1 (" + getlastTransaction.MeterStop + ") | ";
                                                            dbContextlog.SaveChanges();
                                                        }

                                                        catch (Exception ex)
                                                        {
                                                            logStopTransaction.Descriptions += "| State 5 " + ex.Message + "" + " | ";
                                                        }
                                                        
                                                    }
                                                }
                                                else  
                                                {
                                                    transaction.StopTagId = idTag;
                                                    transaction.MeterStop = (double)stopTransactionRequest.MeterStop / 1000; // Meter value here is always Wh
                                                    transaction.StopReason = "Success  [339]";// stopTransactionRequest.Reason.ToString();
                                                    transaction.StopTime = stopTransactionRequest.Timestamp.UtcDateTime;
                                                    dbContext.SaveChanges();

                                                    logStopTransaction.Descriptions += "| State 5_2_2 (" + getlastTransaction.MeterStop + ") | ";
                                                    dbContextlog.SaveChanges();
                                                }
                                            }
                                            else
                                            {
                                                logStopTransaction.Descriptions += "| fwl (null)" + " | ";
                                                transaction.StopTagId = idTag;
                                                transaction.MeterStop = (double)stopTransactionRequest.MeterStop / 1000; 
                                                transaction.StopReason = "Success [348]";
                                                transaction.StopTime = stopTransactionRequest.Timestamp.UtcDateTime;
                                                dbContext.SaveChanges();

                                                logStopTransaction.Descriptions += "| State 5_2_3 (" + transaction.MeterStop + ") | ";
                                                dbContextlog.SaveChanges();
                                            }
                                        }

                                        try
                                        {
                                            WalletTransaction WalletTransaction = dbContext.WalletTransactions.Where(m => m.TransactionId == transaction.TransactionId).FirstOrDefault();
                                            if (WalletTransaction != null)
                                            {
                                                if (transaction.MeterStop.HasValue)
                                                {
                                                    logStopTransaction.Descriptions += "| pre6 (" + transaction.MeterStop.Value + ") | ";

                                                    WalletTransaction.meterValue = decimal.Parse((transaction.MeterStop.Value - transaction.MeterStart).ToString());
                                                    WalletTransaction.stopMethod = "Normal";
                                                    WalletTransaction.Amount = WalletTransaction.meterValue * WalletTransaction.ExchangeRate.Value;
                                                    WalletTransaction.newBalance = WalletTransaction.currentBalance - WalletTransaction.Amount;
                                                }
                                                else
                                                {
                                                    logStopTransaction.Descriptions += "|  pre6 (null) getlastTransaction.MeterStop(" + getlastTransaction.MeterStop + ") stopTransactionRequest.MeterStop (" + stopTransactionRequest.MeterStop + ") | ";

                                                    WalletTransaction.meterValue = decimal.Parse((getlastTransaction.MeterStop - transaction.MeterStart).ToString());
                                                    WalletTransaction.stopMethod = "Normal [456]";
                                                    WalletTransaction.Amount = WalletTransaction.meterValue * WalletTransaction.ExchangeRate.Value;
                                                    WalletTransaction.newBalance = WalletTransaction.currentBalance - WalletTransaction.Amount;
                                                }
                                            }
                                            UserApp us = dbContext.UserApps.Where(m => m.Id == transaction.StartTagId).FirstOrDefault();
                                            if (us != null)
                                            {
                                                us.Balance = WalletTransaction.newBalance.Value;
                                                dbContext.SaveChanges();
                                            }

                                            //Delete virtual
                                            TransactionVirtual tv = dbContext.TransactionVirtuals.Where(m => m.TransactionId == transaction.TransactionId).FirstOrDefault();
                                            if (tv != null)
                                            {
                                                dbContext.Remove(tv);
                                                dbContext.SaveChanges();
                                            }
                                            //delete tag virtual
                                            ChargeTag QrChargeTag = dbContext.ChargeTags.Where(m => m.TagId == transaction.StartTagId && m.TagType == "QR_Payment").FirstOrDefault();

                                            if (QrChargeTag != null)
                                            {
                                                dbContext.ChargeTags.Remove(QrChargeTag);
                                                dbContext.SaveChanges();
                                                //Update QRTransaction
                                                QRTransaction QRTransaction = dbContext.QRTransactions.Where(m => m.TransactionId == transaction.TransactionId).FirstOrDefault();
                                                if (QRTransaction != null)
                                                {
                                                    QRTransaction.EndTime = transaction.StopTime.Value;
                                                    QRTransaction.EnergyUsed = decimal.Parse((transaction.MeterStop.Value - transaction.MeterStart).ToString());
                                                    QRTransaction.StopMethod = transaction.StopReason;
                                                    dbContext.SaveChanges();
                                                }
                                            }
                                            //Delete virtal
                                            TransactionVirtualQR tvqr = dbContext.TransactionVirtualQRs.Where(m => m.TransactionId == transaction.TransactionId).FirstOrDefault();
                                            if (tvqr != null)
                                            {
                                                dbContext.Remove(tvqr);
                                                dbContext.SaveChanges();
                                            }

                                            logStopTransaction.Descriptions += "| State 6" + " | ";
                                            dbContextlog.SaveChanges();
                                        }
                                        catch (Exception ex)
                                        {
                                            ResponseLog ResponseLog = new ResponseLog();
                                            ResponseLog.CreateDate = DateTime.Now;
                                            ResponseLog.isType = "Check lỗi remote stop {Cập nhật wallet}" + transaction.TransactionId;
                                            ResponseLog.isResponse = ex.Message;
                                            ResponseLog.isResponse += " | " + ex.StackTrace;
                                            dbContext.ResponseLogs.Add(ResponseLog);
                                            dbContext.SaveChanges();

                                            logStopTransaction.Descriptions += "| State 6 " + "wallet error (Nullable object must have a value.) | ";
                                            dbContextlog.SaveChanges();
                                        }
                                    }
                                    else
                                    {
                                        logStopTransaction.Descriptions += "| State 6 " + "stopTransactionRequest is NULL" + "" + " | ";
                                        dbContextlog.SaveChanges();
                                        errorCode = ErrorCodes.PropertyConstraintViolation;
                                    }
                                }
                                else 
                                {
                                    Logger.LogError("StopTransaction => Unknown transaction: id={0} / chargepoint={1} / tag={2}", stopTransactionRequest.TransactionId, ChargePointStatus?.Id, idTag);
                                    WriteMessageLog(ChargePointStatus?.Id, transaction?.ConnectorId, msgIn.Action, string.Format("UnknownTransaction:ID={0}/Meter={1}", stopTransactionRequest.TransactionId, stopTransactionRequest.MeterStop), errorCode);
                                    errorCode = ErrorCodes.PropertyConstraintViolation;
                                }
                            }
                        }
                    }
                    catch (Exception exp)
                    {
                        Logger.LogError(exp, "StopTransaction => Exception writing transaction: chargepoint={0} / tag={1} / reason={2}", ChargePointStatus?.Id, idTag, exp.Message);
                        errorCode = ErrorCodes.InternalError;
                        LogStopTransaction ls = new LogStopTransaction();
                        ls.Descriptions = exp.Message;
                        ls.Timestart = DateTime.Now;
                        ls.Timestop = DateTime.Now;
                        ls.TransactionId = stopTransactionRequest.TransactionId;
                        ls.StopTransactionResponse = "Internal error";
                        dbContextlog.LogStopTransactions.Add(ls);
                        dbContextlog.SaveChanges();
                        
                    }
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(stopTransactionResponse);
               
                Logger.LogTrace("StopTransaction => Response serialized");
                using (OCPPCoreContext dbContext = new OCPPCoreContext(Configuration))
                {
                    if (logStopTransaction != null)
                        logStopTransaction.Descriptions += "| State 7 " + "StopTransaction errorCode: " + errorCode + " " + "" + " | ";
                    dbContextlog.SaveChanges();
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "StopTransaction => Exception: {0}", exp.Message);
                errorCode = exp.Message;
                if (logStopTransaction != null)
                    logStopTransaction.Descriptions += "| State 8 " + "StopTransaction error: " + "" +exp.Message+ " | ";

            }

            WriteMessageLog(ChargePointStatus?.Id, null, msgIn.Action, stopTransactionResponse.IdTagInfo?.Status.ToString(), errorCode);
            LogOCPP("Finished");
            using (OCPPCoreContext dbContextlog = new OCPPCoreContext(Configuration))
            {
                if (logStopTransaction != null)
                    logStopTransaction.Descriptions += "| State 9 " + "Finished errorCode is " + errorCode + " " + "" + " | ";
                dbContextlog.SaveChanges();
            }


            return errorCode;
        }


        public async  Task  SendMeter(string apiUrl)
        {
            HttpClient _httpClient = new HttpClient();
            HttpResponseMessage response =await _httpClient.GetAsync(apiUrl);
        }
        public async Task SendMessage(string userTo,string body)
        {
            string serverKey = "AAAAvXGNK6E:APA91bG7sMWvF2POHTv4RbGIbkH9fA0v_lDvS2GTTvMD5lJamx7mLR_Df6rPusr9JHD4J3ZSxiKQyfCIG9uqcXX2lDPbO0c7CK_zUnrfu_UTg69jHe_ruaVeQIL448HDTF1dLIo0JRFw";
            string fcmUrl = "https://fcm.googleapis.com/fcm/send";

            var jsonMessage = @"{
           ""to"": """ + userTo + @""",
            ""notification"": {
                ""title"": ""Sạc thành công"",
                ""body"": """+body+@""",
            },
            ""data"": {
                ""promo_type"": ""CHARGE_FINISH"",
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