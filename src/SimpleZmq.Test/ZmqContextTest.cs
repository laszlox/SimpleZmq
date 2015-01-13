using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SimpleZmq.Test
{
    [TestClass]
    public class ZmqContextTest
    {
        [TestMethod]
        public void Create_And_Destroy_Context()
        {
            using (var zmqContext = new ZmqContext())
            {
            }
        }
    }
}
