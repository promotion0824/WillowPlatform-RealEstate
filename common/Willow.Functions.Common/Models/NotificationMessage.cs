using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Willow.DataValidation.Annotations;

namespace Willow.Functions.Common
{
    public class NotificationMessage : CustomerMessage
    {
        public Guid UserId { get; set; }
        
        [IsOneOf(AllowableValues = new string[] {
            "customeruser",
            "Customer",
            "supervisor"
        })]
        public string? UserType { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "CommunicationType is required")]
        [IsOneOf(AllowableValues = new string[] { "email", "pushnotification" })]
        public string CommunicationType { get; set; } = "";

        [MaxLength(5)]
        [AlphaNumeric(AllowDash = true)]
        public string Locale { get; set; } = "";

        [Required(AllowEmptyStrings = false, ErrorMessage = "TemplateName is required")]
        [AlphaNumeric]
        public string TemplateName { get; set; } = "";

        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
    }   
}
