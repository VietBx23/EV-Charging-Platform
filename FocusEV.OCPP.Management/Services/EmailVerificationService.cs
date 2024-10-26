using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RestSharp;

namespace FocusEV.OCPP.Management.Services
{
    public class EmailVerificationService
    {
        private readonly string _apiKey;
        private readonly RestClient _client;

        public EmailVerificationService(string apiKey)
        {
            _apiKey = apiKey;
            _client = new RestClient("https://api.hunter.io");
        }

        public async Task<bool> VerifyEmailAsync(string email)
        {
            var request = new RestRequest("v2/email-verifier", Method.Get);
            request.AddParameter("email", email);
            request.AddParameter("api_key", _apiKey);

            var response = await _client.ExecuteAsync<EmailVerificationResponse>(request);

            if (response.IsSuccessful)
            {
                var result = response.Data;
                if (result == null || result.Data == null)
                {
                    throw new Exception("Invalid response from email verification service");
                }

                return result.Data.Status == "valid";
            }

            throw new Exception($"Failed to verify email. Status code: {response.StatusCode}, Message: {response.Content}");
        }

        public async Task<Dictionary<string, bool>> VerifyEmailsAsync(IEnumerable<string> emails)
        {
            var validEmails = emails.Where(email => !string.IsNullOrEmpty(email));

            var tasks = validEmails.Select(async email =>
            {
                bool isValid = false;
                try
                {
                    isValid = await VerifyEmailAsync(email);
                }
                catch (Exception ex)
                {
                    // Log exception if necessary
                }
                return new { Email = email, IsValid = isValid };
            }).ToArray();

            var results = await Task.WhenAll(tasks);

            return results.ToDictionary(result => result.Email, result => result.IsValid);
        }

        private class EmailVerificationResponse
        {
            public EmailVerificationData Data { get; set; }
        }

        private class EmailVerificationData
        {
            public string Status { get; set; }
        }
    }
}



