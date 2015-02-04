using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleZmq.Test
{
    public static class ExceptionAssert
    {
        public static void Throws<T>(Action task, string expectedMessage = null) where T : Exception
        {
            try
            {
                task();
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(T), "Expected exception type failed.");
                if (!string.IsNullOrEmpty(expectedMessage))
                {
                    Assert.AreEqual(expectedMessage.ToUpper(), ex.Message.ToUpper(), "Expected exception message failed.");
                }
                return;
            }
            if (typeof(T).Equals(new Exception().GetType()))
            {
                Assert.Fail("Expected exception but no exception was thrown.");
            }
            else
            {
                Assert.Fail(string.Format("Expected exception of type {0} but no exception was thrown.", typeof(T)));
            }
        }
    }
}
