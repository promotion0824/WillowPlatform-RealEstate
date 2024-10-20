using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Willow.Platform.Common
{
    public class WarningException : Exception
    {
        public WarningException()
        {
        }

        public WarningException(string message) : base(message)
        {
        }

        public WarningException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
