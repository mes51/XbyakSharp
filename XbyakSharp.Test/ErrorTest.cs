using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace XbyakSharp.Test
{
    [TestClass]
    public class ErrorTest
    {
        private class ErrorCode : CodeGenerator
        {
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void MovErrorTest()
        {
            using (ErrorCode ec = new ErrorCode())
            {
                ec.mov(ec.ptr[ec.eax], 1);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestErrorTest()
        {
            using (ErrorCode ec = new ErrorCode())
            {
                ec.test(ec.ptr[ec.eax], 1);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AdcErrorTest()
        {
            using (ErrorCode ec = new ErrorCode())
            {
                ec.adc(ec.ptr[ec.eax], 1);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SetZErrorTest()
        {
            using (ErrorCode ec = new ErrorCode())
            {
                ec.setz(ec.eax);
            }
        }
    }
}
