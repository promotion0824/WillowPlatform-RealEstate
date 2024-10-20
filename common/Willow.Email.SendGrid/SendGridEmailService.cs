using System;
using System.Linq;
using System.Threading.Tasks;

using SendGrid;
using SendGrid.Helpers.Mail;

using Willow.Common;
using Willow.Platform.Common;

namespace Willow.Email.SendGrid
{
    public class SendGridEmailService : INotificationHub
    {
        private readonly EmailAddress _fromAddress;
        private readonly SendGridClient _sendGridClient;
        private readonly string _apiKey;

        public SendGridEmailService(string apiKey, string fromAddress, string fromName = null)
        {
            if(string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException(nameof(apiKey));

            if(string.IsNullOrWhiteSpace(fromAddress))
                throw new ArgumentException(nameof(fromAddress));

            _sendGridClient = new SendGridClient(apiKey);
            _fromAddress = new EmailAddress(fromAddress, fromName);
            _apiKey = apiKey?.Substring(0, Math.Min(8, apiKey.Length)); // This is for logging only
        }

        #region INotificationHub

        public async Task Send(string recipient, string subject, string body, object tags = null)
        {
            if(string.IsNullOrWhiteSpace(recipient))
                throw new ArgumentException(nameof(recipient));

            if(string.IsNullOrWhiteSpace(subject))
                throw new ArgumentException(nameof(subject));

            if(string.IsNullOrWhiteSpace(body))
                throw new ArgumentException(nameof(body));

            var message = new SendGridMessage
            {
                From        = _fromAddress,
                Subject     = subject,
                HtmlContent = body,
            };

            var dtags = tags?.ToDictionary();

            if(dtags?.Count != 0)
            {
                // SendGrid has categories which are a single value instead of a key-value tag. Convert key-value to a single string. e.g. "Environment:dev"
                message.Categories = dtags.Select( kv=> $"{kv.Key}:{kv.Value}").ToList();
            }

            message.AddTo(new EmailAddress(recipient));

            Response response;

            try
            { 
                response = await _sendGridClient.SendEmailAsync(message);
            }
            catch(Exception ex)
            {
                throw new Exception("Exception while sending email", ex).WithData( new { Subject = subject, ApiKey = _apiKey } );
            }

            if(response.StatusCode != System.Net.HttpStatusCode.Accepted && response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                var responseBody = await response.Body.ReadAsStringAsync();

                switch(response.StatusCode) 
                {
                    case System.Net.HttpStatusCode.OK:
                        return;

                    case System.Net.HttpStatusCode.Unauthorized:
                        throw new UnauthorizedAccessException($"Email send failed").WithData( new { Subject = subject, Response = responseBody, ApiKey = _apiKey } );

                    case System.Net.HttpStatusCode.TooManyRequests:
                        throw new TooManyRequestsException($"Email send failed").WithData( new { Subject = subject, Response = responseBody, ApiKey = _apiKey } );

                    default:
                        if((int)response.StatusCode >= 500)
                            throw new TransientException($"Email send failed, Status = {response.StatusCode}").WithData( new { Subject = subject, StatusCode = response.StatusCode, Response = responseBody, ApiKey = _apiKey } );

                    throw new Exception($"Email send failed, Status = {response.StatusCode}").WithData( new { Subject = subject, StatusCode = response.StatusCode, Response = responseBody, ApiKey = _apiKey } );
                }
            }
        }

        #endregion
    }
}
