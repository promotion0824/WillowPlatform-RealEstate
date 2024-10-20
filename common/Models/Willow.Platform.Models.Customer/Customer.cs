using System;

namespace Willow.Platform.Models
{
    public class Customer
    {
        public Guid           Id     { get; init; }
        public string?        Name   { get; init; }
        public CustomerStatus Status { get; init; }

        public enum CustomerStatus
        {
            None     = 0,
            Active   = 1,
            Inactive = 2
        }
   }
}