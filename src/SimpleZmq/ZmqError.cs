using SimpleZmq.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SimpleZmq
{
    public class ZmqError
    {
        private readonly int        _number;
        private readonly string     _description;

        public ZmqError(int number)
        {
            _number = number;
            var errStrPtr = LibZmq.zmq_strerror(number);
            _description = Marshal.PtrToStringAnsi(errStrPtr);
        }

        public int Number
        {
            get { return _number; }
        }

        public string Description
        {
            get { return _description; }
        }

        public bool WasInterrupted
        {
            get { return _number == ErrNo.EINTR; }
        }

        public bool ShouldTryAgain
        {
            get { return _number == ErrNo.EAGAIN; }
        }

        public bool ContextTerminated
        {
            get { return _number == ErrNo.ETERM; }
        }

        public override string ToString()
        {
            return String.Format("{0} ({1})", _description, _number);
        }
    }
}
