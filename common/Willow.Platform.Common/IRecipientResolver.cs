using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Willow.Platform.Models;

namespace Willow.Platform.Common
{
    /// <summary>
    ///  Translate to email address for email type
    /// </summary>
    public interface IRecipientResolver
    {
        Task<(string Address, string Language)> GetRecipient(Guid customerId, Guid userId, UserType? userType, string notificationType, IDictionary<string, object> data);
    }
}
