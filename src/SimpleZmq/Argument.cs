using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleZmq
{
    internal static class Argument
    {
        public static void ExpectNonNull(object value, string paramName)
        {
            if (ReferenceEquals(value, null)) throw new ArgumentNullException(paramName);
        }

        public static void ExpectNonZero(IntPtr value, string paramName)
        {
            if (value == IntPtr.Zero) throw new ArgumentNullException(paramName);
        }

        public static void ExpectNonNullAndWhiteSpace(string value, string paramName)
        {
            ExpectNonNull(value, paramName);
            if (String.IsNullOrWhiteSpace(value)) throw new ArgumentException(String.Format("{0} cannot be empty or whitespace string.", paramName), paramName);
        }
    }
}
