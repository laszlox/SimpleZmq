using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleZmq.Native
{
    public static class ErrNo
    {
        public const int EINTR = 4;
        public const int EBADF = 9;
        public const int EAGAIN = 11;
        public const int EACCES = 13;
        public const int EFAULT = 14;
        public const int EINVAL = 22;
        public const int EMFILE = 24;
    }
}
