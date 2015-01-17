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

        public ZmqException(ZmqError zmqError) : base(zmqError.ToString())
        {
            _zmqErrNo = zmqError.Number;
        }

        public ZmqException(ZmqError zmqError, Exception innerException) : base(zmqError.ToString(), innerException)
        {
            _zmqErrNo = zmqError.Number;
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("ZmqException._zmqErrNo", _zmqErrNo);
        }

        public int Number
        {
            get { return _zmqErrNo; }
        }
    }
}
