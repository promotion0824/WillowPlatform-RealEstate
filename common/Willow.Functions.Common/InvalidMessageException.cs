using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Willow.Functions.Common;

[Serializable]
public class InvalidMessageException : Exception
{
    public IEnumerable<ValidationResult> Errors { get; }

    public InvalidMessageException(IEnumerable<ValidationResult> errors, string message): base (message)
    {
        Errors = errors;
    }

    protected InvalidMessageException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        Errors = new List<ValidationResult>();
    }
}