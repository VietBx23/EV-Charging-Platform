using System;
using MailKit.Net.Smtp;
using MimeKit;
using System.Threading.Tasks;

namespace FocusEV.OCPP.Management.Services
{
    public class EmailService
    {
        private readonly string _smtpServer = "smtp.gmail.com";
        private readonly int _smtpPort = 587;
        private readonly string _smtpUsername = "vietbx23";
        private readonly string _smtpPassword = "vgpi nxqy pbal whja";

        // Send OTP xác thực người dùng
        public async Task SendOtpEmail(string toEmail, string otpCode)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("SolarEV", _smtpUsername));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Your OTP Code";

            message.Body = new TextPart("plain")
            {
                Text = $"Your OTP code is: {otpCode}"
            };

            using (var client = new SmtpClient())
            {
                try
                {
                    await client.ConnectAsync(_smtpServer, _smtpPort, false);
                    await client.AuthenticateAsync(_smtpUsername, _smtpPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
                catch (Exception ex)
                {
                    // Handle exception
                    Console.WriteLine(ex.Message);
                }
            }
        }

        // Gửi email thông báo trạng thái trụ sạc
        public async Task SendChargePointStatusEmail(string chargePointName, string status)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("SolarEV", _smtpUsername));
            message.To.Add(new MailboxAddress("Kỹ thuật Focus", "xuanviet06062003@gmail.com"));
            message.Subject = $"Trạng thái trụ sạc {chargePointName} đã thay đổi";

            message.Body = new TextPart("plain")
            {
                Text = $"Trạng thái của trụ sạc {chargePointName} hiện tại là: {status}. Vui lòng kiểm tra ngay."
            };

            using (var client = new SmtpClient())
            {
                try
                {
                    await client.ConnectAsync(_smtpServer, _smtpPort, false);
                    await client.AuthenticateAsync(_smtpUsername, _smtpPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                    Console.WriteLine($"Email sent successfully for ChargePoint: {chargePointName}");
                }
                catch (Exception ex)
                {
                    // Handle exception
                    Console.WriteLine($"Error sending email: {ex.Message}");
                }
            }
        }
        // Gửi email thông báo trạng thái trụ sạc
        // Gửi email thông báo lỗi hoặc trạng thái bất thường của trụ sạc
        public async Task SendChargePointErrorNotificationEmail(string chargePointName, string errorDetails)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("SolarEV", _smtpUsername));
            message.To.Add(new MailboxAddress("Kỹ thuật Focus", "xuanviet06062003@gmail.com"));
            message.Subject = $"Lỗi hoặc trạng thái bất thường trên trụ sạc {chargePointName}";

            // Nội dung email với thông tin trụ sạc và chi tiết lỗi
            message.Body = new TextPart("plain")
            {
                Text = $"Trụ sạc {chargePointName} hiện đang gặp sự cố: {errorDetails}. Vui lòng kiểm tra ngay. "
            };

            using (var client = new SmtpClient())
            {
                try
                {
                    // Kết nối đến SMTP server và gửi email
                    await client.ConnectAsync(_smtpServer, _smtpPort, false);
                    await client.AuthenticateAsync(_smtpUsername, _smtpPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                    Console.WriteLine($"Email thông báo lỗi đã được gửi thành công cho trụ sạc: {chargePointName}");
                }
                catch (Exception ex)
                {
                    // Xử lý lỗi nếu không gửi được email
                    Console.WriteLine($"Lỗi khi gửi email: {ex.Message}");
                }
            }
        }

    }


}
