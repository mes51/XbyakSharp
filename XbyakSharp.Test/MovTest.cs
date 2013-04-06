using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace XbyakSharp.Test
{
    [TestClass]
    public class MovTest
    {
        class MovCode : CodeGenerator
        {
            public IEnumerable<string> GenerateMachineCodeList(Reg32e[] regs)
            {
                Array.Resize(ref regs, regs.Length + 1);
                int[] scales = new int[] { 0, 1, 2, 4, 8 };
                int[] disps = new int[] { 0, 1, 1000, -1, -1000 };

                foreach (var reg1 in regs)
                {
                    foreach (var reg2 in regs)
                    {
                        if (reg2 == esp || reg2 == rsp)
                        {
                            continue;
                        }
                        foreach (var scale in scales)
                        {
                            foreach (var disp in disps)
                            {
                                Reg32e r = null;
                                if (reg1 != null)
                                {
                                    r = reg1;
                                }
                                if (reg2 != null)
                                {
                                    Reg32e tr2 = reg2;
                                    if (scale > 0)
                                    {
                                        tr2 *= scale;
                                    }
                                    if (r == null)
                                    {
                                        r = tr2;
                                    }
                                    else
                                    {
                                        r += tr2;
                                    }
                                }
                                if (r != null)
                                {
                                    mov(ecx, disp > -1 ? ptr[r + (uint)disp] : ptr[r - (uint)(-disp)]);
                                }
                                else
                                {
                                    // 64bitでは-1はulong.MaxValueになる & 32bitではuint.MaxValueは
                                    // OverfrowExceptionが発生する(intで-1のまま渡す必要がある)ため条件分けする
                                    if (Environment.Is64BitProcess)
                                    {
                                        mov(ecx, ptr[new IntPtr((uint)disp)]);
                                    }
                                    else
                                    {
                                        mov(ecx, ptr[new IntPtr(disp)]);
                                    }
                                }
                                yield return ToString();
                                ResetSize();
                            }
                        }
                    }
                }
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < (int)Size; i++)
                {
                    sb.Append(CodeMemory[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        [TestMethod]
        public void Test()
        {
            string expectedData = null;
            if (Environment.Is64BitProcess)
            {
                expectedData = ExpectedData.mov_expected_64bit_1;
            }
            else
            {
                expectedData = ExpectedData.mov_expected_32bit;
            }
            string[] expected = expectedData.Split('\n').Select(x => x.Replace("\r", "")).ToArray();
            using (MovCode mov = new MovCode())
            {
                Reg32e[] regs = new Reg32e[] { mov.eax, mov.ecx, mov.edx, mov.ebx, mov.esp, mov.ebp, mov.esi, mov.edi };
                if (Environment.Is64BitProcess)
                {
                    regs = regs.Concat(new Reg32e[] { mov.r9d, mov.r10d, mov.r11d, mov.r12d, mov.r13d, mov.r14d, mov.r15d }).ToArray();
                }
                int line = 0;
                foreach (string code in mov.GenerateMachineCodeList(regs))
                {
                    Assert.AreEqual(expected[line], code);
                    line++;
                }
            }

            if (Environment.Is64BitProcess)
            {
                expected = ExpectedData.mov_expected_64bit_2.Split('\n').Select(x => x.Replace("\r", "")).ToArray();
                using (MovCode mov = new MovCode())
                {
                    Reg32e[] regs = new Reg32e[] { mov.rax, mov.rcx, mov.rdx, mov.rbx, mov.rsp, mov.rbp, mov.rsi, mov.rdi, mov.r9, mov.r10, mov.r11, mov.r12, mov.r13, mov.r14, mov.r15 };
                    int line = 0;
                    foreach (string code in mov.GenerateMachineCodeList(regs))
                    {
                        Assert.AreEqual(expected[line], code);
                        line++;
                    }
                }
            }
        }
    }
}
