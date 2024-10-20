using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Willow.Platform.Common
{
    public static class ConfigurationExtensions
    {
        public static string Get(this IConfiguration config, string name, ILogger logger, bool required = true)
        {
            string val = "";
            
            try
            {
                val = config[name];
            }
            catch
            {
                // Not in config
            }

            if(required && (string.IsNullOrWhiteSpace(val) || val.StartsWith("[value", StringComparison.InvariantCultureIgnoreCase)))
            {
                if(logger != null)
                    logger.LogError($"Missing configuration entry: {name}");

                throw new Exception($"Missing configuration entry: {name}");
            }

            if(val == null || val.StartsWith("[value", StringComparison.InvariantCultureIgnoreCase))
                return null;

            return val;
        }
    }    
}
