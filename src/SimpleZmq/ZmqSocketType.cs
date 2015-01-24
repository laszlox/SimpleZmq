using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleZmq
{
    public enum ZmqSocketType
    {
        Pair = 0,
        Pub = 1,
        Sub = 2,
        Req = 3,
        Rep = 4,
        Dealer = 5,
        Router = 6,
        Pull = 7,
        Push = 8,
        XPub = 9,
        XSub = 10,
        Stream = 11,
        XReq = Dealer,
        XRep = Router
    }
}
