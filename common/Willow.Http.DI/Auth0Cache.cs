using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;

using Willow.Common;

namespace Willow.Platform.Common
{
    internal interface IAuth0Cache 
    {
        public ICache Cache { get; }
    }

    internal class Auth0Cache : IAuth0Cache
    {
        private readonly ICache _cache;

        public Auth0Cache(ICache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public ICache Cache => _cache;
    }
}
