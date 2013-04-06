using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace XbyakSharp.Test
{
    [TestClass]
    public class MMXTest
    {
        class WriteMMX : CodeGenerator
        {
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            private delegate int Func(int x);

            public WriteMMX()
            {
                if (!Environment.Is64BitProcess)
                {
                    var a = ptr[esp + 4];
                    mov(ecx, a);
                }
                movd(mm0, ecx);
                movd(eax, mm0);
                ret();
            }

            private Func setFunc;

            public int Set(int x)
            {
                if (setFunc == null)
                {
                    setFunc = GetDelegate<Func>();
                }
                return setFunc(x);
            }
        }

        class ReadMMX : CodeGenerator
        {
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            private delegate int Func();

            public ReadMMX()
            {
                movd(eax, mm0);
                ret();
            }

            private Func getFunc;

            public int Get()
            {
                if (getFunc == null)
                {
                    getFunc = GetDelegate<Func>();
                }
                return getFunc();
            }
        }

        private bool exitTest = false;

        bool TestWorker(int val)
        {
            using (WriteMMX w = new WriteMMX())
            using (ReadMMX r = new ReadMMX())
            {
                if (val != w.Set(val))
                {
                    return false;
                }
                while (!exitTest)
                {
                    int b = r.Get();
                    if (b != val)
                    {
                        return false;
                    }
                    Thread.Sleep(1000);
                }
            }
            return true;
        }

        [TestMethod]
        public void Test()
        {
            int n = 15347;
            bool result = true;
            exitTest = false;
            object sync = new object();
            Thread t1 = new Thread(() =>
                {
                    bool tr = TestWorker(n);
                    lock (sync)
                    {
                        result &= tr;
                    }
                });
            Thread t2 = new Thread(() =>
            {
                bool tr = TestWorker(n + 1);
                lock (sync)
                {
                    result &= tr;
                }
            });

            t1.Start();
            t2.Start();

            Thread.Sleep(5000);

            exitTest = true;
            t1.Join();
            t2.Join();

            Assert.IsTrue(result);
        }
    }
}
