using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Willow.Data;
using Willow.Platform.Common;
using Willow.Platform.Models;

namespace Willow.Communications.Resolvers
{
    public class UserRequest
    {
        public Guid      UserId   { get; set; }
        public UserType? UserType { get; set; }

    }

    public sealed class InvalidUserStatusException : WarningException
    { 
        public UserStatus Status { get; }

        public InvalidUserStatusException(UserStatus status) : base($"Cannot send notification to this user. Status = {status}")
        {
            this.Status = status;
        }
    }

    public class RecipientResolver : IRecipientResolver
    {
        private readonly IReadRepository<UserRequest, User> _userRepo;

        public RecipientResolver(IReadRepository<UserRequest, User> userRepo)
        {
            _userRepo = userRepo;
        }

        public async Task<(string Address, string Language)> GetRecipient(Guid customerId, Guid userId, UserType? userType, string notificationType, IDictionary<string, object> data)
        {
            IUser user = await _userRepo.Get(new UserRequest { UserId = userId, UserType = userType}); 

            if(user.Status != UserStatus.Active && user.Status != UserStatus.Pending)
            { 
                throw new InvalidUserStatusException(user.Status);
            }

            data["UserName"]     = user.Name;
            data["FirstName"]    = user.FirstName;
            data["LastName"]     = user.LastName;
            data["Email"]        = user.Email;

            return (notificationType switch 
                    { 
                        "email"             => user.Email,
                        "pushnotification"  => user.Id.ToString(),
                        _                   => throw new ArgumentException($"Unknown notification type: {notificationType}")
                    },
                    user.Language);
        }
    }
}