﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Willow.Platform.Common
{
    public class TransientException : Exception
    {
        public TransientException()
        {
        }

        public TransientException(string message) : base(message)
        {
        }

        public TransientException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
