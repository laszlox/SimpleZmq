using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;

namespace SimpleZmq
{
    /// <summary>
    /// The zmq exception class, thrown when a zmq error occurs.
    /// </summary>
    [Serializable]
    public class ZmqException : Exception
    {
        private readonly int _zmqErrNo;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZmqException"/> class from the serialization info.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        protected ZmqException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            _zmqErrNo = info.GetInt32("ZmqException._zmqErrNo");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZmqException"/> class.
        /// </summary>
        /// <param name="zmqError">The zmq error that needs to be thrown as an exception.</param>
        public ZmqException(ZmqError zmqError) : base(zmqError.ToString())
        {
            _zmqErrNo = zmqError.Number;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZmqException"/> class.
        /// </summary>
        /// <param name="zmqError">The zmq error that needs to be thrown as an exception.</param>
        /// <param name="innerException">The inner exception.</param>
        public ZmqException(ZmqError zmqError, Exception innerException) : base(zmqError.ToString(), innerException)
        {
            _zmqErrNo = zmqError.Number;
        }

        /// <summary>
        /// Serializes the exception into the specified serialization info.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("ZmqException._zmqErrNo", _zmqErrNo);
        }

        /// <summary>
        /// The zmq error number.
        /// </summary>
        public int Number
        {
            get { return _zmqErrNo; }
        }
    }
}
