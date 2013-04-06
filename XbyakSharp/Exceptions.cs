using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XbyakSharp
{
    public class ErrorException : Exception
    {
        public ErrorException(Error error)
        {
            Error = error;
        }

        public Error Error { get; private set; }

        public override string Message
        {
            get { return Util.ConvertErrorToString(Error); }
        }
    }
}
