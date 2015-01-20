using SimpleZmq.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SimpleZmq
{
    public struct ZmqError
    {
        private readonly int        _number;
        private readonly string     _description;

        private ZmqError(int number)
        {
            _number = number;
            var errStrPtr = LibZmq.zmq_strerror(number);
            _description = Marshal.PtrToStringAnsi(errStrPtr);
        }

        public static ZmqError Success()
        {
            return new ZmqError();
        }

        public static ZmqError FromErrNo(int number)
        {
            return new ZmqError(number);
        }

        public int Number
        {
            get { return _number; }
        }

        public string Description
        {
            get { return _description; }
        }

        public bool NoError
        {
            get { return _number == 0 && _description == null; }
        }

        public bool IsError
        {
            get { return !NoError; }
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
