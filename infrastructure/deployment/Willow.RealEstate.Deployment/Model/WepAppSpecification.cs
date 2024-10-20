using System.Collections.Generic;

using Pulumi.AzureNative.Web;

namespace Willow.RealEstate.Deployment
{
    internal class WebAppSpecification
    {
        /// <summary>
        /// Actual Azure resource name
        /// </summary>
        internal string Name { get; init; }

        /// <summary>
        /// Determines if this app will call (other) core services
        /// </summary>
        internal bool CallsCoreServices { get; init; } = false;

        // Initial configuration settings for this app (some settings will be set automatically)
        internal IDictionary<string, string> AppSettings { get; init; }
        
    }
}
