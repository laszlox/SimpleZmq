using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleZmq.Native
{
    /// <summary>
    /// Static class containing zmq error codes.
    /// </summary>
    public static class ZmqErrNo
    {
        /// <summary>
        /// The base error code.
        /// </summary>
        public const int ZMQ_HAUSNUMERO = 156384712;

        /// <summary>
        /// EINTR.
        /// </summary>
        public const int EINTR = 4;

        /// <summary>
        /// EBADF.
        /// </summary>
        public const int EBADF = 9;

        /// <summary>
        /// EAGAIN.
        /// </summary>
        public const int EAGAIN = 11;

        /// <summary>
        /// EACCES.
        /// </summary>
        public const int EACCES = 13;

        /// <summary>
        /// EFAULT.
        /// </summary>
        public const int EFAULT = 14;

        /// <summary>
        /// EINVAL.
        /// </summary>
        public const int EINVAL = 22;

        /// <summary>
        /// EMFILE.
        /// </summary>
        public const int EMFILE = 24;

        /// <summary>
        /// ETERM.
        /// </summary>
        public const int ETERM = ZMQ_HAUSNUMERO + 53;
    }
}
