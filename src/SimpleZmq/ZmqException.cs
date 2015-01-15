using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;

namespace SimpleZmq
{
    [Serializable]
    public class ZmqException : Exception
    {
        private readonly int _zmqErrNo;

        protected ZmqException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            _zmqErrNo = info.GetInt32("ZmqException._zmqErrNo");
        }

        public ZmqException(int zmqErrNo)
        {
            _zmqErrNo = zmqErrNo;
        }

        public ZmqException(int zmqErrNo, string message) : base(message)
        {
            _zmqErrNo = zmqErrNo;
        }

        public ZmqException(int zmqErrNo, string message, Exception innerException) : base(message, innerException)
        {
            _zmqErrNo = zmqErrNo;
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("ZmqException._zmqErrNo", _zmqErrNo);
        }

        public int ZmqErrNo
        {
            get { return _zmqErrNo; }
        }
    }
}
