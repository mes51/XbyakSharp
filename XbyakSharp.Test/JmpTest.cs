using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices;
using System.Linq;
using System.Collections;

namespace XbyakSharp.Test
{
    [TestClass]
    public class JmpTest
    {
        class TestJmp : CodeGenerator
        {
            /*
             *   4                                  X0:
             *   5 00000004 EBFE                    jmp short X0
             *   6
             *   7                                  X1:
             *   8 00000006 <res 00000001>          dummyX1 resb 1
             *   9 00000007 EBFD                    jmp short X1
             *  10
             *  11                                  X126:
             *  12 00000009 <res 0000007E>          dummyX126 resb 126
             *  13 00000087 EB80                    jmp short X126
             *  14
             *  15                                  X127:
             *  16 00000089 <res 0000007F>          dummyX127 resb 127
             *  17 00000108 E97CFFFFFF              jmp near X127
             *  18
             *  19 0000010D EB00                    jmp short Y0
             *  20                                  Y0:
             *  21
             *  22 0000010F EB01                    jmp short Y1
             *  23 00000111 <res 00000001>          dummyY1 resb 1
             *  24                                  Y1:
             *  25
             *  26 00000112 EB7F                    jmp short Y127
             *  27 00000114 <res 0000007F>          dummyY127 resb 127
             *  28                                  Y127:
             *  29
             *  30 00000193 E980000000              jmp near Y128
             *  31 00000198 <res 00000080>          dummyY128 resb 128
             *  32                                  Y128:
             */
            public TestJmp(int offset, bool isBack, bool isShort)
            {
                if (isBack)
                {
                    L("@@");
                    PutNop(offset);
                    jmp("@b");
                }
                else
                {
                    if (isShort)
                    {
                        jmp("@f");
                    }
                    else
                    {
                        jmp("@f", LabelType.Near);
                    }
                    PutNop(offset);
                    L("@@");
                }
            }

            private void PutNop(int n)
            {
                for (int i = 0; i < n; i++)
                {
                    nop();
                }
            }
        }

        class TestJmp2 : CodeGenerator
        {
            /*
             *   1 00000000 90                      nop
             *   2 00000001 90                      nop
             *   3                                  f1:
             *   4 00000002 <res 0000007E>          dummyX1 resb 126
             *   6 00000080 EB80                     jmp f1
             *   7
             *   8                                  f2:
             *   9 00000082 <res 0000007F>          dummyX2 resb 127
             *  11 00000101 E97CFFFFFF               jmp f2
             *  12
             *  13
             *  14 00000106 EB7F                    jmp f3
             *  15 00000108 <res 0000007F>          dummyX3 resb 127
             *  17                                  f3:
             *  18
             *  19 00000187 E980000000              jmp f4
             *  20 0000018C <res 00000080>          dummyX4 resb 128
             *  22                                  f4:
             */

            public TestJmp2(bool useAutoGrow)
                : base(8192, null, useAutoGrow)
            {
                InLocalLabel();

                nop();
                nop();
            L(".f1");
                PutNop(126);
                jmp(".f1");
            L(".f2");
                PutNop(127);
                jmp(".f2", LabelType.Near);
                jmp(".f3");
                PutNop(127);
            L(".f3");
                jmp(".f4", LabelType.Near);
                PutNop(128);
            L(".f4");

                OutLocalLabel();
            }

            private void PutNop(int n)
            {
                for (int i = 0; i < n; i++)
                {
                    nop();
                }
            }
        }

        class TestJmp3 : CodeGenerator
        {
            public delegate int Func();

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            private delegate int Add(int x);

            private Add add5 = null;
            private Add add2 = null;

            public TestJmp3(int dummySize)
                : base(128, null, true)
            {
                // suppress GC
                add5 = Add5;
                add2 = Add2;

                mov(eax, 100);
                push(eax);
                call(Marshal.GetFunctionPointerForDelegate(add5));
                add(esp, 4);
                push(eax);
                call(Marshal.GetFunctionPointerForDelegate(add2));
                add(esp, 4);
                ret();
                for (int i = 0; i < dummySize; i++)
                {
                    Db(0);
                }
            }

            int Add5(int x)
            {
                return x + 5;
            }

            int Add2(int x)
            {
                return x + 2;
            }
        }

        class TestJmp4 : CodeGenerator
        {
            public TestJmp4(ulong size, bool useAutoGrow)
                : base(size, null, useAutoGrow)
            {
                InLocalLabel();
                OutLocalLabel();

                jmp(".x");
                for (int i = 0; i < 10; i++)
                {
                    nop();
                }
            L(".x");
                ret();
            }
        }

        class TestJmp5 : CodeGenerator
        {
            public delegate int Func();

            public TestJmp5(ulong size, int count, bool useAutoGrow)
                : base(size, null, useAutoGrow)
            {
                InLocalLabel();

                mov(ecx, (uint)count);
                xor(eax, eax);
            L(".lp");
                for (int i = 0; i < count; i++)
                {
                    L(Label.ToLabelIndex(i));
                    add(eax, 1);
                    int to = 0;
                    if (i < count / 2)
                    {
                        to = count - 1 - i;
                    }
                    else
                    {
                        to = count - i;
                    }

                    if (i == count / 2)
                    {
                        jmp(".exit", LabelType.Near);
                    }
                    else
                    {
                        jmp(Label.ToLabelIndex(to), LabelType.Near);
                    }
                }
            L(".exit");
                sub(ecx, 1);
                jnz(".lp", LabelType.Near);
                ret();

                OutLocalLabel();
            }
        }

        class TestJmpCx : CodeGenerator
        {
            public TestJmpCx()
                : base(16)
            {
                InLocalLabel();

            L(".lp");
                if (Environment.Is64BitProcess)
                {
                    jecxz(".lp");
                    jrcxz(".lp");
                }
                else
                {
                    jecxz(".lp");
                    jcxz(".lp");
                }

                OutLocalLabel();
            }
        }

        class TestJmpMovLabel : CodeGenerator
        {
            public TestJmpMovLabel(bool useAutoGrow)
                : base(useAutoGrow ? 128U : 4096U, null, useAutoGrow)
            {
                Reg32e a = Environment.Is64BitProcess ? (Reg32e)rax : (Reg32e)eax;

                InLocalLabel();

                nop();
            L(".lp1");
                nop();
                mov(a, ".lp1"); // 0xb8 + <4byte> / 0x48bb + <8byte>
                nop();
                mov(a, ".lp2"); // 0xb8

                // force realloc if AutoGrow
                for (int i = 0; i < 256; i++)
                {
                    nop();
                }
                nop();
            L(".lp2");

                OutLocalLabel();
            }
        }

        class TestJmpMovLabel2 : CodeGenerator
        {
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int Func();

            public TestJmpMovLabel2()
            {
                Reg32e a = Environment.Is64BitProcess ? (Reg32e)rax : (Reg32e)eax;
                Reg32e c = Environment.Is64BitProcess ? (Reg32e)rcx : (Reg32e)ecx;

                xor(a, a);
                xor(c, c);
                jmp("in");
                ud2();
            L("@@");
                add(a, 2);
                mov(c, "@f");
                jmp(c);
                ud2();
            L("in");
                mov(c, "@b");
                add(a, 1);
                jmp(c);
                ud2();
            L("@@");
                add(a, 4);
                ret();
            }
        }

        [TestMethod]
        public void Test1()
        {
            var testCase = new[]
                {
                    new { Offset = 0,   IsBack = true,  IsShort = true,  Result = new byte[] { 0xeb, 0xfe } },
                    new { Offset = 1,   IsBack = true,  IsShort = true,  Result = new byte[] { 0xeb, 0xfd } },
                    new { Offset = 126, IsBack = true,  IsShort = true,  Result = new byte[] { 0xeb, 0x80 } },
                    new { Offset = 127, IsBack = true,  IsShort = false, Result = new byte[] { 0xe9, 0x7c, 0xff, 0xff, 0xff } },
                    new { Offset = 0,   IsBack = false, IsShort = true,  Result = new byte[] { 0xeb, 0x00 } },
                    new { Offset = 1,   IsBack = false, IsShort = true,  Result = new byte[] { 0xeb, 0x01 } },
                    new { Offset = 127, IsBack = false, IsShort = true,  Result = new byte[] { 0xeb, 0x7f } },
                    new { Offset = 128, IsBack = false, IsShort = false, Result = new byte[] { 0xe9, 0x80, 0x00, 0x00, 0x00 } },
                };
            foreach (var tc in testCase)
            {
                using (TestJmp jmp = new TestJmp(tc.Offset, tc.IsBack, tc.IsShort))
                {
                    int index = 0;
                    if (tc.IsBack)
                    {
                        index = tc.Offset;
                    }
                    byte[] code = new byte[tc.Result.Length];
                    Marshal.Copy(jmp.CodeMemory.DangerousGetHandle() + index, code, 0, code.Length);
                    Assert.IsTrue(tc.Result.SequenceEqual(code));
                }
            }
        }

        [TestMethod]
        public void TestCx()
        {
            byte[] expected = null;
            if (Environment.Is64BitProcess)
            {
                expected = new byte[] { 0x67, 0xe3, 0xfd, 0xe3, 0xfb };
            }
            else
            {
                expected = new byte[] { 0xe3, 0xfe, 0x67, 0xe3, 0xfb };
            }

            using (TestJmpCx jmp = new TestJmpCx())
            {
                byte[] result = new byte[jmp.Size];
                Marshal.Copy(jmp.CodeMemory.DangerousGetHandle(), result, 0, result.Length);
                Assert.IsTrue(result.Take(expected.Length).SequenceEqual(expected));
            }
        }

        [TestMethod]
        public void Test2()
        {
            byte[] expected = Enumerable.Repeat((byte)0x90, 0x18C + 128).ToArray();
            expected[0x080] = 0xeb;
            expected[0x081] = 0x80;

            expected[0x101] = 0xe9;
            expected[0x102] = 0x7c;
            expected[0x103] = 0xff;
            expected[0x104] = 0xff;
            expected[0x105] = 0xff;

            expected[0x106] = 0xeb;
            expected[0x107] = 0x7f;

            expected[0x187] = 0xe9;
            expected[0x188] = 0x80;
            expected[0x189] = 0x00;
            expected[0x18a] = 0x00;
            expected[0x18b] = 0x00;

            for (int i = 0; i < 2; i++)
            {
                using (TestJmp2 jmp = new TestJmp2(i != 0))
                {
                    jmp.Ready();
                    Assert.AreEqual((ulong)expected.Length, jmp.Size);

                    byte[] result = new byte[jmp.Size];
                    Marshal.Copy(jmp.CodeMemory.DangerousGetHandle(), result, 0, result.Length);
                    Assert.IsTrue(expected.SequenceEqual(result));
                }
            }
        }

        [TestMethod]
        // for 32bit test
        public void Test3()
        {
            if (Environment.Is64BitProcess)
            {
                // skip test
                Assert.IsTrue(true);
            }
            else
            {
                int expected = 107;
                for (int dummySize = 0; dummySize < 40000; dummySize += 10000)
                {
                    using (TestJmp3 jmp = new TestJmp3(dummySize))
                    {
                        jmp.Ready();
                        var func = jmp.GetDelegate<TestJmp3.Func>();
                        int ret = func();
                        Assert.AreEqual(expected, ret);
                    }
                }
            }
        }

        [TestMethod]
        public void Test4()
        {
            using (TestJmp4 fc = new TestJmp4(1024, false))
            using (TestJmp4 gc = new TestJmp4(5, true))
            {
                gc.Ready();
                byte[] fm = new byte[fc.Size];
                byte[] gm = new byte[gc.Size];
                Marshal.Copy(fc.CodeMemory.DangerousGetHandle(), fm, 0, fm.Length);
                Marshal.Copy(gc.CodeMemory.DangerousGetHandle(), gm, 0, gm.Length);
                Assert.IsTrue(fm.SequenceEqual(gm));
            }
        }

        [TestMethod]
        public void Test5()
        {
            byte[] expected = new byte[]
                {
                    0xB9, 0x32, 0x00, 0x00, 0x00, 0x31, 0xC0, 0x83, 0xC0, 0x01, 0xE9, 0x80, 0x01, 0x00, 0x00, 0x83,
                    0xC0, 0x01, 0xE9, 0x70, 0x01, 0x00, 0x00, 0x83, 0xC0, 0x01, 0xE9, 0x60, 0x01, 0x00, 0x00, 0x83,
                    0xC0, 0x01, 0xE9, 0x50, 0x01, 0x00, 0x00, 0x83, 0xC0, 0x01, 0xE9, 0x40, 0x01, 0x00, 0x00, 0x83,
                    0xC0, 0x01, 0xE9, 0x30, 0x01, 0x00, 0x00, 0x83, 0xC0, 0x01, 0xE9, 0x20, 0x01, 0x00, 0x00, 0x83,
                    0xC0, 0x01, 0xE9, 0x10, 0x01, 0x00, 0x00, 0x83, 0xC0, 0x01, 0xE9, 0x00, 0x01, 0x00, 0x00, 0x83,
                    0xC0, 0x01, 0xE9, 0xF0, 0x00, 0x00, 0x00, 0x83, 0xC0, 0x01, 0xE9, 0xE0, 0x00, 0x00, 0x00, 0x83,
                    0xC0, 0x01, 0xE9, 0xD0, 0x00, 0x00, 0x00, 0x83, 0xC0, 0x01, 0xE9, 0xC0, 0x00, 0x00, 0x00, 0x83,
                    0xC0, 0x01, 0xE9, 0xB0, 0x00, 0x00, 0x00, 0x83, 0xC0, 0x01, 0xE9, 0xA0, 0x00, 0x00, 0x00, 0x83,
                    0xC0, 0x01, 0xE9, 0x90, 0x00, 0x00, 0x00, 0x83, 0xC0, 0x01, 0xE9, 0x80, 0x00, 0x00, 0x00, 0x83,
                    0xC0, 0x01, 0xE9, 0x70, 0x00, 0x00, 0x00, 0x83, 0xC0, 0x01, 0xE9, 0x60, 0x00, 0x00, 0x00, 0x83,
                    0xC0, 0x01, 0xE9, 0x50, 0x00, 0x00, 0x00, 0x83, 0xC0, 0x01, 0xE9, 0x40, 0x00, 0x00, 0x00, 0x83,
                    0xC0, 0x01, 0xE9, 0x30, 0x00, 0x00, 0x00, 0x83, 0xC0, 0x01, 0xE9, 0x20, 0x00, 0x00, 0x00, 0x83,
                    0xC0, 0x01, 0xE9, 0x10, 0x00, 0x00, 0x00, 0x83, 0xC0, 0x01, 0xE9, 0x00, 0x00, 0x00, 0x00, 0x83,
                    0xC0, 0x01, 0xE9, 0xC0, 0x00, 0x00, 0x00, 0x83, 0xC0, 0x01, 0xE9, 0xE8, 0xFF, 0xFF, 0xFF, 0x83,
                    0xC0, 0x01, 0xE9, 0xD8, 0xFF, 0xFF, 0xFF, 0x83, 0xC0, 0x01, 0xE9, 0xC8, 0xFF, 0xFF, 0xFF, 0x83,
                    0xC0, 0x01, 0xE9, 0xB8, 0xFF, 0xFF, 0xFF, 0x83, 0xC0, 0x01, 0xE9, 0xA8, 0xFF, 0xFF, 0xFF, 0x83,
                    0xC0, 0x01, 0xE9, 0x98, 0xFF, 0xFF, 0xFF, 0x83, 0xC0, 0x01, 0xE9, 0x88, 0xFF, 0xFF, 0xFF, 0x83,
                    0xC0, 0x01, 0xE9, 0x78, 0xFF, 0xFF, 0xFF, 0x83, 0xC0, 0x01, 0xE9, 0x68, 0xFF, 0xFF, 0xFF, 0x83,
                    0xC0, 0x01, 0xE9, 0x58, 0xFF, 0xFF, 0xFF, 0x83, 0xC0, 0x01, 0xE9, 0x48, 0xFF, 0xFF, 0xFF, 0x83,
                    0xC0, 0x01, 0xE9, 0x38, 0xFF, 0xFF, 0xFF, 0x83, 0xC0, 0x01, 0xE9, 0x28, 0xFF, 0xFF, 0xFF, 0x83,
                    0xC0, 0x01, 0xE9, 0x18, 0xFF, 0xFF, 0xFF, 0x83, 0xC0, 0x01, 0xE9, 0x08, 0xFF, 0xFF, 0xFF, 0x83,
                    0xC0, 0x01, 0xE9, 0xF8, 0xFE, 0xFF, 0xFF, 0x83, 0xC0, 0x01, 0xE9, 0xE8, 0xFE, 0xFF, 0xFF, 0x83,
                    0xC0, 0x01, 0xE9, 0xD8, 0xFE, 0xFF, 0xFF, 0x83, 0xC0, 0x01, 0xE9, 0xC8, 0xFE, 0xFF, 0xFF, 0x83,
                    0xC0, 0x01, 0xE9, 0xB8, 0xFE, 0xFF, 0xFF, 0x83, 0xC0, 0x01, 0xE9, 0xA8, 0xFE, 0xFF, 0xFF, 0x83,
                    0xC0, 0x01, 0xE9, 0x98, 0xFE, 0xFF, 0xFF, 0x83, 0xC0, 0x01, 0xE9, 0x88, 0xFE, 0xFF, 0xFF, 0x83,
                    0xC0, 0x01, 0xE9, 0x78, 0xFE, 0xFF, 0xFF, 0x83, 0xE9, 0x01, 0x0F, 0x85, 0x67, 0xFE, 0xFF, 0xFF,
                    0xC3
                };

            const int count = 50;
            using (TestJmp5 fc = new TestJmp5(1024 * 64, count, false))
            using (TestJmp5 gc = new TestJmp5(10, count, true))
            {
                gc.Ready();
                var func = fc.GetDelegate<TestJmp5.Func>();
                int ret = func();
                Assert.AreEqual(count * count, ret);

                byte[] fm = new byte[fc.Size];
                byte[] gm = new byte[gc.Size];
                Marshal.Copy(fc.CodeMemory.DangerousGetHandle(), fm, 0, fm.Length);
                Marshal.Copy(gc.CodeMemory.DangerousGetHandle(), gm, 0, gm.Length);
                Assert.IsTrue(expected.SequenceEqual(fm));
                Assert.IsTrue(expected.SequenceEqual(gm));
            }
        }

        [TestMethod]
        public void TestMovLabel()
        {
            var expected = new[] { new { Pos = 0, OK = 0 } };

            if (Environment.Is64BitProcess)
            {
                expected = new[]
                    {
                        new { Pos = 0x000, OK = 0x90 },
                        new { Pos = 0x001, OK = 0x90 },
                        new { Pos = 0x002, OK = 0x48 },
                        new { Pos = 0x003, OK = 0xb8 },
                        new { Pos = 0x00c, OK = 0x90 },
                        new { Pos = 0x00d, OK = 0x48 },
                        new { Pos = 0x00e, OK = 0xb8 },
                        new { Pos = 0x117, OK = 0x90 },
                    };
            }
            else
            {
                expected = new[]
                    {
                        new { Pos = 0x000, OK = 0x90 },
                        new { Pos = 0x001, OK = 0x90 },
                        new { Pos = 0x002, OK = 0xb8 },
                        new { Pos = 0x007, OK = 0x90 },
                        new { Pos = 0x008, OK = 0xb8 },
                        new { Pos = 0x10d, OK = 0x90 },
                    };
            }

            using (TestJmpMovLabel fc = new TestJmpMovLabel(false))
            using (TestJmpMovLabel gc = new TestJmpMovLabel(true))
            {
                gc.Ready();
                foreach (var e in expected)
                {
                    Assert.AreEqual(e.OK, (int)fc.CodeMemory[e.Pos]);
                    Assert.AreEqual(e.OK, (int)gc.CodeMemory[e.Pos]);
                }

                IntPtr p = fc.CodeMemory.DangerousGetHandle();
                if (Environment.Is64BitProcess)
                {
                    Assert.AreEqual(p.ToInt64() + 0x001L, Marshal.ReadInt64(p, 0x04));
                    Assert.AreEqual(p.ToInt64() + 0x118L, Marshal.ReadInt64(p, 0x0f));
                }
                else
                {
                    Assert.AreEqual(p.ToInt32() + 0x001, Marshal.ReadInt32(p, 0x03));
                    Assert.AreEqual(p.ToInt32() + 0x10e, Marshal.ReadInt32(p, 0x09));
                }

                p = gc.CodeMemory.DangerousGetHandle();
                if (Environment.Is64BitProcess)
                {
                    Assert.AreEqual(p.ToInt64() + 0x001L, Marshal.ReadInt64(p, 0x04));
                    Assert.AreEqual(p.ToInt64() + 0x118L, Marshal.ReadInt64(p, 0x0f));
                }
                else
                {
                    Assert.AreEqual(p.ToInt32() + 0x001, Marshal.ReadInt32(p, 0x03));
                    Assert.AreEqual(p.ToInt32() + 0x10e, Marshal.ReadInt32(p, 0x09));
                }
            }
        }

        [TestMethod]
        public void TestMovLabel2()
        {
            var expected = new[] { new { Pos = 0, OK = 0 } };
            if (Environment.Is64BitProcess)
            {
                expected = new[]
                    {
                        new { Pos = 0,  OK = 72 },
                        new { Pos = 1,  OK = 49 },
                        new { Pos = 2,  OK = 192 },
                        new { Pos = 3,  OK = 72 },
                        new { Pos = 4,  OK = 49 },
                        new { Pos = 5,  OK = 201 },
                        new { Pos = 6,  OK = 235 },
                        new { Pos = 7,  OK = 20 },
                        new { Pos = 8,  OK = 15 },
                        new { Pos = 9,  OK = 11 },
                        new { Pos = 10, OK = 72 },
                        new { Pos = 11, OK = 131 },
                        new { Pos = 12, OK = 192 },
                        new { Pos = 13, OK = 2 },
                        new { Pos = 14, OK = 72 },
                        new { Pos = 15, OK = 185 },
                        new { Pos = 24, OK = 255 },
                        new { Pos = 25, OK = 225 },
                        new { Pos = 26, OK = 15 },
                        new { Pos = 27, OK = 11 },
                        new { Pos = 28, OK = 72 },
                        new { Pos = 29, OK = 185 },
                        new { Pos = 38, OK = 72 },
                        new { Pos = 39, OK = 131 },
                        new { Pos = 40, OK = 192 },
                        new { Pos = 41, OK = 1 },
                        new { Pos = 42, OK = 255 },
                        new { Pos = 43, OK = 225 },
                        new { Pos = 44, OK = 15 },
                        new { Pos = 45, OK = 11 },
                        new { Pos = 46, OK = 72 },
                        new { Pos = 47, OK = 131 },
                        new { Pos = 48, OK = 192 },
                        new { Pos = 49, OK = 4 },
                        new { Pos = 50, OK = 195 }
                    };
            }
            else
            {
                expected = new[]
                    {
                        new { Pos = 0, OK = 49 },
                        new { Pos = 1, OK = 192 },
                        new { Pos = 2, OK = 49 },
                        new { Pos = 3, OK = 201 },
                        new { Pos = 4, OK = 235 },
                        new { Pos = 5, OK = 14 },
                        new { Pos = 6, OK = 15 },
                        new { Pos = 7, OK = 11 },
                        new { Pos = 8, OK = 131 },
                        new { Pos = 9, OK = 192 },
                        new { Pos = 10, OK = 2 },
                        new { Pos = 11, OK = 185 },
                        new { Pos = 16, OK = 255 },
                        new { Pos = 17, OK = 225 },
                        new { Pos = 18, OK = 15 },
                        new { Pos = 19, OK = 11 },
                        new { Pos = 20, OK = 185 },
                        new { Pos = 25, OK = 131 },
                        new { Pos = 26, OK = 192 },
                        new { Pos = 27, OK = 1 },
                        new { Pos = 28, OK = 255 },
                        new { Pos = 29, OK = 225 },
                        new { Pos = 30, OK = 15 },
                        new { Pos = 31, OK = 11 },
                        new { Pos = 32, OK = 131 },
                        new { Pos = 33, OK = 192 },
                        new { Pos = 34, OK = 4 },
                        new { Pos = 35, OK = 195 }
                    };
            }

            using (TestJmpMovLabel2 jmp = new TestJmpMovLabel2())
            {
                foreach (var e in expected)
                {
                    Assert.AreEqual(e.OK, jmp.CodeMemory[e.Pos]);
                }

                var func = jmp.GetDelegate<TestJmpMovLabel2.Func>();
                int ret = func();
                Assert.AreEqual(7, ret);
            }
        }
    }
}
