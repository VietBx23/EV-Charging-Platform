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


        // send otp xác thực người dùng 

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
        }
    }
