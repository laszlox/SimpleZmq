using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleZmq
{
    internal static class Argument
    {
        public static void ExpectGreaterThan(int constraint, int value, string paramName)
        {
            if (value <= constraint) throw new ArgumentException(String.Format("{0} must be greater than {1} but it was {2}.", paramName, constraint, value));
        }

        public static void ExpectGreaterOrEqualThan(int constraint, int value, string paramName)
        {
            if (value < constraint) throw new ArgumentException(String.Format("{0} must be greater or equal than {1} but it was {2}.", paramName, constraint, value));
        }

        public static void ExpectGreaterThanZero(int value, string paramName)
        {
            ExpectGreaterThan(0, value, paramName);
        }

        public static void ExpectGreaterOrEqualThanZero(int value, string paramName)
        {
            ExpectGreaterOrEqualThan(0, value, paramName);
        }

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
