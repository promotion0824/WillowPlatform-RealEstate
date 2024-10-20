using System;

namespace Willow.Platform.Models
{
    public class Site
    {
        public Guid       Id     { get; init; }
        public string?    Name   { get; init; }
        public SiteStatus Status { get; init; }

        public enum SiteStatus
        {
            Unknown = 0,
            Operations = 1,
            Construction = 2,
            Design = 3,
            Selling = 4,
            Deleted = 10
        }
   }
}