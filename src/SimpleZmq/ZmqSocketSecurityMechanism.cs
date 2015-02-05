using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleZmq
{
    /// <summary>
    /// The available zmq socket security mechanisms.
    /// </summary>
    public enum ZmqSocketSecurityMechanism
    {
        /// <summary>
        /// Null.
        /// </summary>
        Null = 0,

        /// <summary>
        /// Plain.
        /// </summary>
        Plain = 1,

        /// <summary>
        /// Curve.
        /// </summary>
        Curve = 2
    }
}
