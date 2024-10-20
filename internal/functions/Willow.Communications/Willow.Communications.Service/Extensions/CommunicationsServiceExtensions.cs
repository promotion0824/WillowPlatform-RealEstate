using System;
using System.Threading.Tasks;
using Willow.Platform.Models;

namespace Willow.Communications.Service
{
    public static class CommunicationsServiceExtensions
    {
        public static Task SendEmail(this ICommunicationsService svc, Guid customerId, Guid userId, UserType userType, string templateName, string defaultLanguage, object data, object tags = null)
        { 
            return svc.SendNotification(customerId, userId, userType, templateName, defaultLanguage, data, tags, "email");
        }

        public static Task SendPushNotification(this ICommunicationsService svc, Guid customerId, Guid userId, UserType userType, string templateName, string defaultLanguage, object data, object tags = null)
        { 
            return svc.SendNotification(customerId, userId, userType, templateName, defaultLanguage, data, tags, "pushnotification");
        }
    }
}
