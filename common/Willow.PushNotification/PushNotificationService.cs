using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.NotificationHubs.Messaging;
using System.Text.Json;
using Willow.Common;

namespace Willow.PushNotification
{
    public interface IPushNotificationService : INotificationHub
    {
        Task AddOrUpdateInstallation(Guid userId, string handle, string platform);
        Task DeleteInstallation(Guid userId, string handle);
    }

    public class PushNotificationService : IPushNotificationService
    {
        private readonly NotificationHubClient _hub;
        private const int InstallationExpirationDays = 60;

        public PushNotificationService(string connectionString, string hubPath)
        {
            _hub = NotificationHubClient.CreateClientFromConnectionString(connectionString, hubPath);
        }

        public async Task Send(string recipient, string subject, string body, object tags)
        {
            var pushNotification = new PushNotification() { UserId = Guid.Parse(recipient), Title = subject, Body = body };

            await _hub.SendFcmNativeNotificationAsync(pushNotification.FcmPayload, pushNotification.UserTag);
            await _hub.SendAppleNativeNotificationAsync(pushNotification.ApnsPayload, pushNotification.UserTag);
        }

        public async Task AddOrUpdateInstallation(Guid userId, string handle, string platform)
        {
            if (!Enum.TryParse(platform, out NotificationPlatform notificationPlatform))
            {
                throw new ArgumentException("Failed to parse the notificationPlatform").WithData(new { UserId = userId, Handle = handle, Platform = platform });
            }

            var installation = new Installation()
            {
                InstallationId = await GetInstallationId(userId, handle) ?? Guid.NewGuid().ToString(),
                Platform = notificationPlatform,
                PushChannel = handle,
                ExpirationTime = DateTime.UtcNow.AddDays(InstallationExpirationDays),
                Tags = new List<string> { PushNotification.GetUserTag(userId) }
            };

            try
            {
                await _hub.CreateOrUpdateInstallationAsync(installation);
            }
            catch (MessagingException ex)
            {
                throw new ArgumentException(ex.Message).WithData(new { UserId = userId, Handle = handle, Paltform = platform });
            }
        }

        public async Task DeleteInstallation(Guid userId, string handle)
        {
            var installationId = await GetInstallationId(userId, handle);

            if (string.IsNullOrEmpty(installationId))
            {
                throw new NotFoundException().WithData(new { UserId = userId, Handle = handle });
            }

            await _hub.DeleteInstallationAsync(installationId);
        }

        private async Task<string?> GetInstallationId(Guid userId, string handle)
        {
            var userRegistrations = await _hub.GetRegistrationsByTagAsync(PushNotification.GetUserTag(userId), 0);

            var existingRegistration = userRegistrations.FirstOrDefault(x => x.PnsHandle == handle);
            if (existingRegistration != null)
            {
                var installationIdTag = existingRegistration.Tags.First(x => x.StartsWith("$InstallationId:", StringComparison.InvariantCulture));
                return installationIdTag.Split(":").Last().TrimStart('{').TrimEnd('}');
            }

            return null;
        }
    }

    internal class PushNotification
    {
        internal Guid UserId { get; set; }
        internal string UserTag { get { return GetUserTag(UserId); } }
        internal string Title { get; set; }
        internal string Body { get; set; }

        internal string FcmPayload
        {
            get
            {
                var fcmPayload = new
                {
                    notification = new
                    {
                        title = Title,
                        body = Body
                    }
                };

                return JsonSerializer.Serialize(fcmPayload);
            }
        }

        internal string ApnsPayload
        {
            get
            {
                var apnsPayload = new
                {
                    aps = new
                    {
                        alert = new
                        {
                            title = Title,
                            body = Body
                        }
                    }
                };

                return JsonSerializer.Serialize(apnsPayload);
            }
        }

        internal static string GetUserTag(Guid userId)
        {
            return $"userId:{userId}";
        }
    }
}