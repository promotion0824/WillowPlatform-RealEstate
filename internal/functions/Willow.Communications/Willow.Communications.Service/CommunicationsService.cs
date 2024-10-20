using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Willow.Common;
using Willow.Data;
using Willow.Logging;
using Willow.Platform.Common;
using Willow.Platform.Models;

namespace Willow.Communications.Service
{
    public interface ICommunicationsService
    {
        Task SendNotification(Guid customerId, Guid userId, UserType? userType, string templateName, string language, object data, object tags, string type);
    }

    public class CommunicationsService : ICommunicationsService
    {
        private readonly IDictionary<string, INotificationHub> _hubs;
        private readonly IBlobStore                            _templateStore;
        private readonly IRecipientResolver                    _recipientResolver;
        private readonly IReadRepository<Guid, Customer>       _customerRepo;
        private readonly ILogger                               _logger;

        public CommunicationsService(IDictionary<string, INotificationHub> hubs, IBlobStore templateStore, IRecipientResolver recipientResolver, IReadRepository<Guid, Customer>  customerRepo, ILogger logger)
        {
            _hubs              = hubs ?? throw new ArgumentNullException(nameof(hubs));
            _templateStore     = templateStore ?? throw new ArgumentNullException(nameof(templateStore));
            _recipientResolver = recipientResolver ?? throw new ArgumentNullException(nameof(recipientResolver));
            _customerRepo      = customerRepo ?? throw new ArgumentNullException(nameof(customerRepo));
            _logger            = logger ?? throw new ArgumentNullException(nameof(logger));

            if(hubs.Count == 0)
            { 
                throw new ArgumentException("No notification hubs specified");
            }
        }

        #region ICommunicationsService

        public async Task SendNotification(Guid customerId, Guid userId, UserType? userType, string templateName, string language, object data, object tags, string type)
        {
            if(!_hubs.ContainsKey(type) || _hubs[type] == null)
                throw new ArgumentException("No notification hub of that type found").WithData("HubType", type);

            var templateData = data?.ToDictionary() ?? new Dictionary<string, object>();
            var messageTags  = tags?.ToDictionary() ?? new Dictionary<string, object>();
            var recipient    = await _recipientResolver.GetRecipient(customerId, userId, userType, type, templateData);

            try
            {
                var customer = await _customerRepo.Get(customerId);

                templateData["CustomerName"] = customer.Name;
            }
            catch
            {
                _logger.LogWarning("Customer not found", new Exception("Customer not found"), new { CustomerId = customerId });
            }

            language = string.IsNullOrWhiteSpace(language) ? recipient.Language : language; 

            var subjectBody = await GetSubjectAndBody(templateName, language, type, templateData); 

            templateData.Add("Recipient", recipient.Address);
            templateData.Add("Subject", subjectBody.Subject);
            templateData["Locale"] = language;

            _logger.LogInformation($"Sending {type}", templateData);

            // Send notification
            var hub = _hubs[type];

            await hub.Send(recipient.Address, subjectBody.Subject, subjectBody.Body, messageTags);
        }

        #endregion

        #region Private

        private static string GetTemplateExt(string type)
        {
            return type switch
            {
                "email" => "html",
                _ => "xml",
            };
        }

        private async Task<string> GetTemplate(string templateName, string language, string type, string ext)
        {
            var template = "";
            
            if(string.IsNullOrWhiteSpace(language))
                language = "en";

            try
            {
                template = await _templateStore.Get(type + "/" + language + "/" + templateName + "." + ext);
            }
            catch(Exception)
            {
                // Template in requested language not found, continue below
            }

            if(!string.IsNullOrWhiteSpace(template))
                return template;

            if(language == "en")
            {
                throw new NotFoundException("Template not found").WithData(new { TemplateName = templateName, Language = language, Type = type });
            }

            // Try the language without the region
            if(language.Length > 2)
                return await GetTemplate(templateName, language.Substring(0, 2), type, ext);

            // Default to English
            return await GetTemplate(templateName, "en", type, ext);
        }

        private async Task<(string Subject, string Body)> GetSubjectAndBody(string templateName, string language, string type, object data)
        {
            var logInfo = new { TemplateName = templateName, Language = language, Type = type };

            var templateExt = GetTemplateExt(type);
            var template = await GetTemplate(templateName, language, type, templateExt);

            var subjectTemplate = "";
            var bodyTemplate = "";

            switch (templateExt)
            {
                case "html":
                {
                    subjectTemplate = GetEmailSubjectFromHtmlBody(template);
                    bodyTemplate = template;
                    break;
                }

                case "xml":
                {
                    var xml = new XmlDocument();
                    xml.LoadXml(template);

                    subjectTemplate = xml.SelectSingleNode("//subject")?.InnerText;
                    bodyTemplate = xml.SelectSingleNode("//body")?.InnerText;
                    break;
                }

                default: 
                    throw new ArgumentException("Invalid template extension").WithData(new { TemplateName = templateName, Language = language, Type = type, Ext = templateExt });
            }

            if (string.IsNullOrWhiteSpace(subjectTemplate))
                throw new Exception("No subject found in template").WithData(logInfo);

            if(string.IsNullOrWhiteSpace(bodyTemplate))
                throw new Exception("No body found in template").WithData(logInfo);

            var subject = subjectTemplate.Substitute(data);
            var body    = bodyTemplate.Substitute(data);
            
            return (subject, body); 
        }
        
        private static string GetEmailSubjectFromHtmlBody(string htmlBody)
        {
            var regex = new Regex("<title>(?<subject>.*)</title>", RegexOptions.Multiline);
            var subjectMatch = regex.Match(htmlBody);

            if (subjectMatch == null)
            {
                return "";
            }

            return subjectMatch.Groups["subject"].Value;
        }

        #endregion
    }
}
