using SimpleZmq.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SimpleZmq
{
    /// <summary>
    /// Struct representing a zmq error or the fact that there was no error (success).
    /// </summary>
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

        /// <summary>
        /// Returns a success <see cref="ZmqError"/> if this error means context-termination or success, otherwise just this error.
        /// </summary>
        /// <returns>A success <see cref="ZmqError"/> if this error means context-termination or success, otherwise just this error.</returns>
        public ZmqError IgnoreContextTerminated()
        {
            return IsError && ContextTerminated ? ZmqError.Success() : this;
        }

        /// <summary>
        /// Creates a <see cref="ZmqError"/> instance that means success.
        /// </summary>
        /// <returns>A <see cref="ZmqError"/> instance that means success.</returns>
        public static ZmqError Success()
        {
            return new ZmqError();
        }

        /// <summary>
        /// Creates a <see cref="ZmqError"/> instance from the specified error number.
        /// </summary>
        /// <param name="number">The zmq error number.</param>
        /// <returns>A <see cref="ZmqError"/> instance from the specified error number.</returns>
        public static ZmqError FromErrNo(int number)
        {
            return new ZmqError(number);
        }

        /// <summary>
        /// Gets the zmq error number.
        /// </summary>
        public int Number
        {
            get { return _number; }
        }

        /// <summary>
        /// Gets the zmq error description.
        /// </summary>
        public string Description
        {
            get { return _description; }
        }

        /// <summary>
        /// Gets a value indicating whether the zmq error means success.
        /// </summary>
        public bool NoError
        {
            get { return _number == 0 && _description == null; }
        }

        /// <summary>
        /// Gets a value indicating whether the zmq error means error and not success.
        /// </summary>
        public bool IsError
        {
            get { return !NoError; }
        }

        /// <summary>
        /// Gets a value indicating whether the zmq operation was interrupted.
        /// </summary>
        public bool WasInterrupted
        {
            get { return _number == ZmqErrNo.EINTR; }
        }

        /// <summary>
        /// Gets a value indicating whether the zmq operation can be retried.
        /// </summary>
        public bool ShouldTryAgain
        {
            get { return _number == ZmqErrNo.EAGAIN; }
        }

        /// <summary>
        /// Gets a value indicating whether the zmq operation failed because the context was terminated.
        /// </summary>
        public bool ContextTerminated
        {
            get { return _number == ZmqErrNo.ETERM; }
        }

        /// <summary>
        /// Returns a string representation of the zmq error.
        /// </summary>
        /// <returns>The string representation of the zmq error.</returns>
        public override string ToString()
        {
            return String.Format("{0} ({1})", _description, _number);
        }
    }
}
