using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace XbyakSharp.Test
{
    [TestClass]
    public class MnemonicTest
    {
        private delegate object OpCodeArgument();

        static class XorShift
        {
            private static uint x = 123456789;
            private static uint y = 362436069;
            private static uint z = 521288629;
            private static uint w = 88675123;

            public static void Init()
            {
                x = 123456789;
                y = 362436069;
                z = 521288629;
                w = 88675123;
            }

            public static uint Rand()
            {
                uint t = (x ^ (x << 11));
                x = y;
                y = z;
                z = w;
                w = (w ^ (w >> 19)) ^ (t ^ (t >> 8));
                return w;
            }
        }

        private class MnemonicTestCode : CodeGenerator
        {
            private const int BitEnd = 64;

            private const ulong MMX = 1UL << 0;
            private const ulong _XMM = 1UL << 1;
            private const ulong _MEM = 1UL << 2;
            private const ulong _REG32 = 1UL << 3;
            private const ulong EAX = 1UL << 4;
            private const ulong IMM32 = 1UL << 5;
            private const ulong IMM8 = 1UL << 6;
            private const ulong _REG8 = 1UL << 7;
            private const ulong _REG16 = 1UL << 8;
            private const ulong NEG8 = 1UL << 9;
            private const ulong IMM16 = 1UL << 10;
            private const ulong NEG16 = 1UL << 11;
            private const ulong AX = 1UL << 12;
            private const ulong AL = 1UL << 13;
            private const ulong IMM_1 = 1UL << 14;
            private const ulong MEM8 = 1UL << 15;
            private const ulong MEM16 = 1UL << 16;
            private const ulong MEM32 = 1UL << 17;
            private const ulong NEG = 1UL << 18;
            private const ulong ONE = 1UL << 19;
            private const ulong CL = 1UL << 20;
            private const ulong MEM_ONLY_DISP = 1UL << 21;
            private const ulong NEG32 = 1UL << 23;
            private const ulong _YMM = 1UL << 24;

            private static readonly ulong _MEMe;
            private static readonly ulong REG32_2;
            private static readonly ulong REG16_2;
            private static readonly ulong REG8_2;
            private static readonly ulong REG8_3;
            private static readonly ulong _REG64;
            private static readonly ulong _REG64_2;
            private static readonly ulong RAX;
            private static readonly ulong _XMM2;
            private static readonly ulong _YMM2;

            private static readonly ulong REG64;
            private static readonly ulong REG32;
            private static readonly ulong REG16;
            private static readonly ulong REG32e;
            private static readonly ulong REG8;
            private static readonly ulong MEM;
            private const ulong MEM64 = 1UL << 35;
            private const ulong ST0 = 1UL << 36;
            private const ulong STi = 1UL << 37;
            private const ulong IMM_2 = 1UL << 38;
            private const ulong IMM = IMM_1 | IMM_2;
            private static readonly ulong XMM;
            private static readonly ulong YMM;
            private const ulong NOPARA = 1UL << (BitEnd - 1);

            static MnemonicTestCode()
            {
                if (Environment.Is64BitProcess)
                {
                    _MEMe = 1UL << 25;
                    REG32_2 = 1UL << 26;
                    REG16_2 = 1UL << 27;
                    REG8_2 = 1UL << 28;
                    REG8_3 = 1UL << 29;
                    _REG64 = 1UL << 30;
                    _REG64_2 = 1UL << 31;
                    RAX = 1UL << 32;
                    _XMM2 = 1UL << 33;
                    _YMM2 = 1UL << 34;
                }
                else
                {
                    _MEMe = 0;
                    REG32_2 = 0;
                    REG16_2 = 0;
                    REG8_2 = 0;
                    REG8_3 = 0;
                    _REG64 = 0;
                    _REG64_2 = 0;
                    RAX = 0;
                    _XMM2 = 0;
                    _YMM2 = 0;
                }

                REG64 = _REG64 | _REG64_2 | RAX;
                REG32 = _REG32 | REG32_2 | EAX;
                REG16 = _REG16 | REG16_2 | AX;
                REG32e = REG32 | REG64;
                REG8 = _REG8 | REG8_2 | AL;
                MEM = _MEM | _MEMe;
                XMM = _XMM | _XMM2;
                YMM = _YMM | _YMM2;
            }

            #region TestCodeGenerators

            public IEnumerable<string> GenJmp()
            {
                var results = Enumerable.Empty<string>();

                if (Environment.Is64BitProcess)
                {
                    results = results.Concat(GenerateCode("jmp", REG64));
                    results = results.Concat(GenerateCode("call", REG64));
                }
                else
                {
                    results = results.Concat(GenerateCode("jmp", REG32));
                    results = results.Concat(GenerateCode("call", REG32));
                }
                results = results.Concat(GenerateCode("jmp", MEM));
                results = results.Concat(GenerateCode("jmp", MEM));
                results = results.Concat(GenerateCode("jmp", MEM));
                results = results.Concat(GenerateCode("call", REG16 | MEM | MEM_ONLY_DISP));
                results = results.Concat(GenerateCode("call", () => CodeMemory.DangerousGetHandle() + 5));

                if (Environment.Is64BitProcess)
                {
                    results = results.Concat(GenerateCode("jmp", () => ptr[new IntPtr(0x12345678)]));
                    results = results.Concat(GenerateCode("call", () => ptr[new IntPtr(0x12345678)]));
                }

                return results;
            }

            public IEnumerable<string> GenSimple()
            {
                string[] opCodes = null;
                if (Environment.Is64BitProcess)
                {
                    opCodes = new string[]
                        {
                            "cdqe", "cqo"
                        };
                }
                else
                {
                    opCodes = new string[]
                        {
                            "aaa",
                            "aad",
                            "aam",
                            "aas",
                            "daa",
                            "das",
                            "popad",
                            "popfd",
                            "pusha",
                            "pushad",
                            "pushfd",
                            "popa"
                        };
                }
                var results = opCodes.Concat(new string[]
                    {
                        "cbw",
                        "cdq",
                        "clc",
                        "cld",
                        "cli",
                        "cmc",

                        "cpuid",
                        "cwd",
                        "cwde",

                        "lahf",
                        "nop",

                        "sahf",
                        "stc",
                        "std",
                        "sti",

                        "emms",
                        "pause",
                        "sfence",
                        "lfence",
                        "mfence",
                        "monitor",
                        "mwait",

                        "rdmsr",
                        "rdpmc",
                        "rdtsc",
                        "rdtscp",
                        "ud2",
                        "wait",
                        "fwait",
                        "wbinvd",
                        "wrmsr",
                        "xlatb",

                        "popf",
                        "pushf",

                        "xgetbv",
                        "vzeroall",
                        "vzeroupper",

                        "f2xm1",
                        "fabs",
                        "faddp",
                        "fchs",
                        "fcom",
                        "fcomp",
                        "fcompp",
                        "fcos",
                        "fdecstp",
                        "fdivp",
                        "fdivrp",
                        "fincstp",
                        "finit",
                        "fninit",
                        "fld1",
                        "fldl2t",
                        "fldl2e",
                        "fldpi",
                        "fldlg2",
                        "fldln2",
                        "fldz",
                        "fmulp",
                        "fnop",
                        "fpatan",
                        "fprem",
                        "fprem1",
                        "fptan",
                        "frndint",
                        "fscale",
                        "fsin",
                        "fsincos",
                        "fsqrt",
                        "fsubp",
                        "fsubrp",
                        "ftst",
                        "fucom",
                        "fucomp",
                        "fucompp",
                        "fxam",
                        "fxch",
                        "fxtract",
                        "fyl2x",
                        "fyl2xp1"
                    }).Select(x => GenerateCode(x)).SelectMany(x => x);

                results = results.Concat(GenerateCode("bswap", REG32e));
                results = results.Concat(GenerateCode("lea", REG32e, MEM));
                results = results.Concat(GenerateCode("fldcw", MEM));
                results = results.Concat(GenerateCode("fstcw", MEM));

                return results;
            }

            public IEnumerable<string> GenReg1()
            {
                List<IEnumerable<string>> results = new List<IEnumerable<string>>();

                string[] opCodes = new string[]
                    {
                        "adc",
                        "add",
                        "and",
                        "cmp",
                        "or",
                        "sbb",
                        "sub",
                        "xor"
                    };
                foreach (var opCode in opCodes)
                {
                    results.Add(GenerateCode(opCode, REG32, REG32 | MEM));
                    results.Add(GenerateCode(opCode, REG64, REG64 | MEM));
                    results.Add(GenerateCode(opCode, REG16, REG16 | MEM));
                    results.Add(GenerateCode(opCode, REG8 | REG8_3, REG8 | MEM));
                    results.Add(GenerateCode(opCode, MEM, REG32e | REG16 | REG8 | REG8_3));
                    results.Add(GenerateCode(opCode, MEM8, IMM8 | NEG8));
                    results.Add(GenerateCode(opCode, MEM16, IMM8 | IMM16 | NEG8 | NEG16));
                    results.Add(GenerateCode(opCode, MEM32, IMM8 | IMM32 | NEG8 | NEG32));
                    results.Add(GenerateCode(opCode, REG64 | RAX, IMM8 | NEG8));
                    results.Add(GenerateCode(opCode, REG64 | RAX, () => 0x12345678));
                    results.Add(GenerateCode(opCode, REG64 | RAX, () => 192));
                    results.Add(GenerateCode(opCode, REG64 | RAX, () => 0x1234));
                    results.Add(GenerateCode(opCode, REG32 | EAX, IMM8 | IMM32 | NEG8));
                    results.Add(GenerateCode(opCode, REG16 | AX, IMM8 | IMM16 | NEG8 | NEG16));
                    results.Add(GenerateCode(opCode, REG8 | REG8_3 | AL, IMM | NEG8));
                }

                return results.SelectMany(x => x);
            }

            public IEnumerable<string> GenRorM()
            {
                List<IEnumerable<string>> results = new List<IEnumerable<string>>();

                string[] opCodes = new string[]
                    {
                        "inc",
                        "dec",
                        "div",
                        "idiv",
                        "imul",
                        "mul",
                        "neg",
                        "not"
                    };
                foreach (var opCode in opCodes)
                {
                    results.Add(GenerateCode(opCode, REG32e | REG16 | REG8 | REG8_3));
                    results.Add(GenerateCode(opCode, MEM32 | MEM16 | MEM8));
                }
                results.Add(GenerateCode("imul", REG16, REG16 | MEM16));
                results.Add(GenerateCode("imul", REG32, REG32 | MEM32));
                results.Add(GenerateCode("imul", REG64, REG64 | MEM));
                results.Add(GenerateCode("imul", REG16, REG16 | MEM, IMM8 | IMM16));
                results.Add(GenerateCode("imul", REG32, REG32 | MEM, IMM8 | IMM32));
                results.Add(GenerateCode("imul", REG64, REG64 | MEM, IMM8 | IMM32));

                return results.SelectMany(x => x);
            }

            public IEnumerable<string> GenPushPop()
            {
                var results = Enumerable.Empty<string>();

                results = results.Concat(GenerateCode("push", REG16));
                results = results.Concat(GenerateCode("push", IMM8));
                results = results.Concat(GenerateCode("push", MEM16));
                results = results.Concat(GenerateCode("pop", REG16 | MEM16));

                if (Environment.Is64BitProcess)
                {
                    results = results.Concat(GenerateCode("push", REG64));
                    results = results.Concat(GenerateCode("pop", REG64));
                }
                else
                {
                    results = results.Concat(GenerateCode("push", REG32 | IMM32 | MEM32));
                    results = results.Concat(GenerateCode("pop", REG32 | MEM32));
                }

                return results;
            }

            public IEnumerable<string> GenTest()
            {
                return new[]
                    {
                        new { Op1 = REG32 | MEM, Op2 = REG32 },
                        new { Op1 = REG64 | MEM, Op2 = REG64 },
                        new { Op1 = REG16 | MEM, Op2 = REG16 },
                        new { Op1 = REG8 | REG8_3 | MEM, Op2 = REG8 | REG8_3 },
                        new { Op1 = REG32e | REG16 | REG8 | REG8_3 | EAX | AX | AL | MEM32 | MEM16 | MEM8, Op2 = IMM },
                    }.SelectMany(x => GenerateCode("test", x.Op1, x.Op2));
            }

            public IEnumerable<string> GenEtc()
            {
                var results = Enumerable.Empty<string>();

                results = results.Concat(GenerateCode("ret"));
                results = results.Concat(GenerateCode("ret", IMM));
                results = results.Concat(
                    new[]
                        {
                            new { Op1 = EAX | REG32 | MEM | MEM_ONLY_DISP, Op2 = REG32 | EAX },
                            new { Op1 = REG64 | MEM | MEM_ONLY_DISP, Op2 = REG64|RAX },
                            new { Op1 = AX | REG16 | MEM | MEM_ONLY_DISP, Op2 = REG16 | AX },
                            new { Op1 = AL | REG8 | REG8_3 | MEM | MEM_ONLY_DISP, Op2 = REG8 | REG8_3 | AL },
                            new { Op1 = REG32e | REG16 | REG8 | RAX | EAX | AX | AL, Op2 = MEM | MEM_ONLY_DISP },
                            new { Op1 = MEM32 | MEM16 | MEM8, Op2 = IMM }
                        }.SelectMany(x => GenerateCode("mov", x.Op1, x.Op2))
                    );
                results = results.Concat(GenerateCode("mov", REG64, () => 0x1234567890abcdefUL));

                if (Environment.Is64BitProcess)
                {
                    results = results.Concat(GenerateCode("mov", RAX | EAX | AX | AL, () => ptr[0x1234567890abcdefUL]));
                    results = results.Concat(GenerateCode("mov", () => ptr[0x1234567890abcdefUL], RAX | EAX | AX | AL));
                    results = results.Concat(
                        new OpCodeArgument[]
                            {
                                () => new object[] { qword[rax], 0 },
                                () => new object[] { qword[rax], 0x12 },
                                () => new object[] { qword[rax], 0x1234 },
                                () => new object[] { qword[rax], 0x12345678 },
                                () => new object[] { qword[rax], 1000000 },
                                () => new object[] { rdx, qword[rax] }
                            }.SelectMany(x => GenerateCode("mov", x))
                        );
                }

                results = results.Concat(
                    new string[] { "movsx", "movzx" }
                        .SelectMany(x => new[]
                            {
                                new { Op1 = REG64, Op2 = REG16 | REG8 | MEM8 | MEM16 },
                                new { Op1 = REG32, Op2 = REG16 | REG8 | MEM8 | MEM16 },
                                new { Op1 = REG16, Op2 = REG8 | MEM8 }
                            }, (c, a) => new { Code = c, a.Op1, a.Op2 })
                        .SelectMany(x => GenerateCode(x.Code, x.Op1, x.Op2))
                    );

                if (Environment.Is64BitProcess)
                {
                    results = results.Concat(GenerateCode("movsxd", REG64, REG32 | MEM32));
                }

                results = results.Concat(GenerateCode("cmpxchg8b", MEM));

                if (Environment.Is64BitProcess)
                {
                    results = results.Concat(GenerateCode("cmpxchg16b", MEM));
                }

                results = results.Concat(
                    new[]
                        {
                            new { Op1 = REG8 | MEM, Op2 = REG8 },
                            new { Op1 = REG16 | MEM, Op2 = REG16 },
                            new { Op1 = REG32 | MEM, Op2 = REG32 },
                            new { Op1 = REG64 | MEM, Op2 = REG64 }
                        }.SelectMany(x => GenerateCode("xadd", x.Op1, x.Op2))
                    );
                results = results.Concat(
                    new[]
                        {
                            new { Op1 = AL | REG8, Op2 = AL | REG8 | MEM },
                            new { Op1 = MEM, Op2 = AL | REG8 },
                            new { Op1 = AX | REG16, Op2 = AX | REG16 | MEM },
                            new { Op1 = MEM, Op2 = AX | REG16 },
                            new { Op1 = EAX | REG32, Op2 = EAX | REG32 | MEM },
                            new { Op1 = MEM, Op2 = EAX | REG32 },
                            new { Op1 = REG64, Op2 = REG64 | MEM },
                        }.SelectMany(x => GenerateCode("xchg", x.Op1, x.Op2))
                    );

                return results;
            }

            public IEnumerable<string> GenShift()
            {
                return new string[]
                    {
                        "rcl",
                        "rcr",
                        "rol",
                        "ror",
                        "sar",
                        "shl",
                        "shr",
                        "sal"
                    }.SelectMany(x => GenerateCode(x, REG32e | REG16 | REG8 | MEM32 | MEM16 | MEM8, ONE | CL | IMM));
            }

            public IEnumerable<string> GenShxd()
            {
                return new string[] { "shld", "shrd" }
                    .SelectMany(
                        x => new[]
                            {
                                new { Op1 = REG64 | MEM, Op2 = REG64, Op3 = IMM | CL },
                                new { Op1 = REG32 | MEM, Op2 = REG32, Op3 = IMM | CL },
                                new { Op1 = REG16 | MEM, Op2 = REG16, Op3 = IMM | CL },
                            },
                        (c, a) => new { Code = c, a.Op1, a.Op2, a.Op3 }
                    ).SelectMany(x => GenerateCode(x.Code, x.Op1, x.Op2, x.Op3));
            }

            public IEnumerable<string> GenBs()
            {
                return new string[] { "bsr", "bsf" }
                    .SelectMany(
                        x => new[]
                            {
                                new { Op1 = REG64, Op2 = REG64 | MEM },
                                new { Op1 = REG32, Op2 = REG32 | MEM },
                                new { Op1 = REG16, Op2 = REG16 | MEM },
                            },
                        (c, a) => new { Code = c, a.Op1, a.Op2 }
                    ).SelectMany(x => GenerateCode(x.Code, x.Op1, x.Op2));
            }

            public IEnumerable<string> GenMMX1()
            {
                var results = Enumerable.Empty<string>();

                results = results.Concat(GenerateCode("ldmxcsr", MEM));
                results = results.Concat(GenerateCode("movmskps", REG32e, XMM));
                results = results.Concat(GenerateCode("movmskpd", REG32e, XMM));
                results = results.Concat(GenerateCode("stmxcsr", MEM));
                results = results.Concat(GenerateCode("maskmovq", MMX, MMX));
                results = results.Concat(GenerateCode("movntps", MEM, XMM));
                results = results.Concat(GenerateCode("movntq", MEM, MMX));
                results = results.Concat(GenerateCode("prefetcht0", MEM));
                results = results.Concat(GenerateCode("prefetcht1", MEM));
                results = results.Concat(GenerateCode("prefetcht2", MEM));
                results = results.Concat(GenerateCode("prefetchnta", MEM));
                results = results.Concat(GenerateCode("maskmovdqu", XMM, XMM));
                results = results.Concat(GenerateCode("movntpd", MEM, XMM));
                results = results.Concat(GenerateCode("movntdq", MEM, XMM));
                results = results.Concat(GenerateCode("movnti", MEM, REG32));
                results = results.Concat(GenerateCode("movhlps", XMM, XMM));
                results = results.Concat(GenerateCode("movlhps", XMM, XMM));
                results = results.Concat(GenerateCode("movd", MEM | MEM32 | REG32, MMX | XMM));
                results = results.Concat(GenerateCode("movd", MMX | XMM, MEM | REG32 | MEM32));
                results = results.Concat(GenerateCode("movq", MMX, MMX | MEM));
                results = results.Concat(GenerateCode("movq", MEM, MMX));
                results = results.Concat(GenerateCode("movq", XMM, XMM | MEM));
                results = results.Concat(GenerateCode("movq", MEM, XMM));
                results = results.Concat(GenerateCode("movq", XMM | MMX, () => qword[eax]));
                results = results.Concat(GenerateCode("movq", XMM | MMX, () => ptr[eax]));
                results = results.Concat(GenerateCode("movq", () => qword[eax], XMM | MMX));
                results = results.Concat(GenerateCode("movq", () => ptr[eax], XMM | MMX));

                if (Environment.Is64BitProcess)
                {
                    results = results.Concat(GenerateCode("movq", REG64, XMM | MMX));
                    results = results.Concat(GenerateCode("movq", XMM | MMX, REG64));
                }

                results = results.Concat(GenerateCode("lddqu", XMM, MEM));

                return results;
            }

            public IEnumerable<string> GenMMX2()
            {
                return new string[]
                    {
                        "packssdw",
                        "packsswb",
                        "packuswb",
                        "pand",
                        "pandn",
                        "pmaddwd",
                        "pmulhuw",
                        "pmulhw",
                        "pmullw",
                        "por",
                        "punpckhbw",
                        "punpckhwd",
                        "punpckhdq",
                        "punpcklbw",
                        "punpcklwd",
                        "punpckldq",
                        "pxor",
                        "paddb",
                        "paddw",
                        "paddd",
                        "paddsb",
                        "paddsw",
                        "paddusb",
                        "paddusw",
                        "pcmpeqb",
                        "pcmpeqw",
                        "pcmpeqd",
                        "pcmpgtb",
                        "pcmpgtw",
                        "pcmpgtd",
                        "psllw",
                        "pslld",
                        "psllq",
                        "psraw",
                        "psrad",
                        "psrlw",
                        "psrld",
                        "psrlq",
                        "psubb",
                        "psubw",
                        "psubd",
                        "psubsb",
                        "psubsw",
                        "psubusb",
                        "psubusw",
                        "pavgb",
                        "pavgw",
                        "pmaxsw",
                        "pmaxub",
                        "pminsw",
                        "pminub",
                        "psadbw",
                        "paddq",
                        "pmuludq",
                        "psubq"
                    }.SelectMany(
                        x => new[]
                            {
                                new { Op1 = MMX, Op2 = MMX | MEM },
                                new { Op1 = XMM, Op2 = XMM | MEM }
                            }, (c, a) => new { Code = c, a.Op1, a.Op2 }
                    ).SelectMany(x => GenerateCode(x.Code, x.Op1, x.Op2));
            }

            public IEnumerable<string> GenMMX3()
            {
                var results = Enumerable.Empty<string>();

                results = results.Concat(
                    new string[]
                        {
                            "psllw",
                            "pslld",
                            "psllq",
                            "psraw",
                            "psrad",
                            "psrlw",
                            "psrld",
                            "psrlq"
                        }.SelectMany(x => GenerateCode(x, MMX | XMM, IMM))
                    );
                results = results.Concat(GenerateCode("pslldq", XMM, IMM));
                results = results.Concat(GenerateCode("psrldq", XMM, IMM));
                results = results.Concat(GenerateCode("pmovmskb", REG32, MMX | XMM));
                results = results.Concat(GenerateCode("pextrw", REG32, MMX | XMM, IMM));
                results = results.Concat(GenerateCode("pinsrw", MMX | XMM, REG32 | MEM, IMM));

                return results;
            }

            public IEnumerable<string> GenMMX4()
            {
                var results = Enumerable.Empty<string>();

                results = results.Concat(GenerateCode("pshufw", MMX, MMX | MEM, IMM));
                results = results.Concat(GenerateCode("pshuflw", XMM, XMM | MEM, IMM));
                results = results.Concat(GenerateCode("pshufhw", XMM, XMM | MEM, IMM));
                results = results.Concat(GenerateCode("pshufd", XMM, XMM | MEM, IMM));

                return results;
            }

            public IEnumerable<string> GenMMX5()
            {
                var results = Enumerable.Empty<string>();

                results = results.Concat(
                    new string[]
                        {
                            "movdqa",
                            "movdqu",
                            "movaps",
                            "movss",
                            "movups",
                            "movapd",
                            "movsd",
                            "movupd"
                        }.SelectMany(
                            x => new[]
                                {
                                    new { Op1 = XMM, Op2 = XMM|MEM },
                                    new { Op1 = MEM, Op2 = XMM }
                                }, (c, a) => new { Code = c, a.Op1, a.Op2 }
                        ).SelectMany(x => GenerateCode(x.Code, x.Op1, x.Op2))
                    );
                results = results.Concat(GenerateCode("movq2dq", XMM, MMX));
                results = results.Concat(GenerateCode("movdq2q", MMX, XMM));

                return results;
            }

            public IEnumerable<string> GenXMM1()
            {
                string PS = "ps";
                string SS = "ss";
                string PD = "pd";
                string SD = "sd";

                return new[]
                    {
                        new { Suffixes = new string[] { PS, SS, PD, SD }, Code = "add", HasImm = false },
                        new { Suffixes = new string[] { PS, PD }, Code = "andn", HasImm = false },
                        new { Suffixes = new string[] { PS, PD }, Code = "and", HasImm = false },
                        new { Suffixes = new string[] { PS, SS, PD, SD }, Code = "cmp", HasImm = true },
                        new { Suffixes = new string[] { PS, SS, PD, SD }, Code = "div", HasImm = false },
                        new { Suffixes = new string[] { PS, SS, PD, SD }, Code = "max", HasImm = false },
                        new { Suffixes = new string[] { PS, SS, PD, SD }, Code = "min", HasImm = false },
                        new { Suffixes = new string[] { PS, SS, PD, SD }, Code = "mul", HasImm = false },
                        new { Suffixes = new string[] { PS, PD }, Code = "or", HasImm = false },
                        new { Suffixes = new string[] { PS, SS }, Code = "rcp", HasImm = false },
                        new { Suffixes = new string[] { PS, SS }, Code = "rsqrt", HasImm = false },
                        new { Suffixes = new string[] { PS, PD }, Code = "shuf", HasImm = true },
                        new { Suffixes = new string[] { PS, SS, PD, SD }, Code = "sqrt", HasImm = false },
                        new { Suffixes = new string[] { PS, SS, PD, SD }, Code = "sub", HasImm = false },
                        new { Suffixes = new string[] { PS, PD }, Code = "unpckh", HasImm = false },
                        new { Suffixes = new string[] { PS, PD }, Code = "unpckl", HasImm = false },
                        new { Suffixes = new string[] { PS, PD }, Code = "xor", HasImm = false }

                    }.SelectMany(x => x.Suffixes, (x, s) => new { Code = x.Code + s, x.HasImm })
                     .SelectMany(x => GenerateCode(x.Code, XMM, XMM | MEM, x.HasImm ? IMM : NOPARA));
            }

            public IEnumerable<string> GenXMM2()
            {
                return new string[]
                    {
                        "punpckhqdq",
                        "punpcklqdq",
                        "comiss",
                        "ucomiss",
                        "comisd",
                        "ucomisd",
                        "cvtpd2ps",
                        "cvtps2pd",
                        "cvtsd2ss",
                        "cvtss2sd",
                        "cvtpd2dq",
                        "cvttpd2dq",
                        "cvtdq2pd",
                        "cvtps2dq",
                        "cvttps2dq",
                        "cvtdq2ps",
                        "addsubpd",
                        "addsubps",
                        "haddpd",
                        "haddps",
                        "hsubpd",
                        "hsubps",
                        "movddup",
                        "movshdup",
                        "movsldup"
                    }.SelectMany(x => GenerateCode(x, XMM, XMM | MEM));
            }

            public IEnumerable<string> GenXMM3()
            {
                return new[]
                    {
                        new { Code = "cvtpi2ps", Op1 = XMM, Op2 = MMX | MEM },
                        new { Code = "cvtps2pi", Op1 = MMX, Op2 = XMM | MEM },
                        new { Code = "cvtsi2ss", Op1 = XMM, Op2 = REG32 | MEM },
                        new { Code = "cvtss2si", Op1 = REG32, Op2 = XMM | MEM },
                        new { Code = "cvttps2pi", Op1 = MMX, Op2 = XMM | MEM },
                        new { Code = "cvttss2si", Op1 = REG32, Op2 = XMM | MEM },
                        new { Code = "cvtpi2pd", Op1 = XMM, Op2 = MMX | MEM },
                        new { Code = "cvtpd2pi", Op1 = MMX, Op2 = XMM | MEM },
                        new { Code = "cvtsi2sd", Op1 = XMM, Op2 = REG32 | MEM },
                        new { Code = "cvtsd2si", Op1 = REG32, Op2 = XMM | MEM },
                        new { Code = "cvttpd2pi", Op1 = MMX, Op2 = XMM | MEM },
                        new { Code = "cvttsd2si", Op1 = REG32, Op2 = XMM | MEM }
                    }.SelectMany(x => GenerateCode(x.Code, x.Op1, x.Op2));
            }

            public IEnumerable<string> GenXMM4()
            {
                return new string[]
                    {
                        "movhps",
                        "movlps",
                        "movhpd",
                        "movlpd"
                    }.SelectMany(
                        x => new[]
                            {
                                new { Op1 = XMM, Op2 = MEM },
                                new { Op1 = MEM, Op2 = XMM }
                            },
                        (c, a) => new { Code = c, a.Op1, a.Op2 })
                     .SelectMany(x => GenerateCode(x.Code, x.Op1, x.Op2));
            }

            public IEnumerable<string> GenCmov()
            {
                return new string[]
                    {
                        "o",
                        "no",
                        "b",
                        "c",
                        "nae",
                        "nb",
                        "nc",
                        "ae",
                        "e",
                        "z",
                        "ne",
                        "nz",
                        "be",
                        "na",
                        "nbe",
                        "a",
                        "s",
                        "ns",
                        "p",
                        "pe",
                        "np",
                        "po",
                        "l",
                        "nge",
                        "nl",
                        "ge",
                        "le",
                        "ng",
                        "nle",
                        "g"
                    }.SelectMany(
                        x => new[]
                            {
                                new { Prefix = "cmov", Op1 = REG32, Op2 = REG32 | MEM },
                                new { Prefix = "cmov", Op1 = REG64, Op2 = REG64 | MEM },
                                new { Prefix = "set", Op1 = REG8 | REG8_3 | MEM, Op2 = NOPARA }
                            },
                        (s, p) => new { Code = p.Prefix + s, p.Op1, p.Op2 })
                     .SelectMany(x => GenerateCode(x.Code, x.Op1, x.Op2));
            }

            public IEnumerable<string> GenFpuMem16_32()
            {
                return new string[]
                    {
                        "fiadd",
                        "fidiv",
                        "fidivr",
                        "ficom",
                        "ficomp",
                        "fimul",
                        "fist",
                        "fisub",
                        "fisubr"
                    }.SelectMany(x => GenerateCode(x, MEM16 | MEM32));
            }

            public IEnumerable<string> GenFpuMem32_64()
            {
                return new string[]
                    {
                        "fadd",
                        "fcom",
                        "fcomp",
                        "fdiv",
                        "fdivr",
                        "fld",
                        "fmul",
                        "fst",
                        "fstp",
                        "fsub",
                        "fsubr"
                    }.SelectMany(x => GenerateCode(x, MEM32 | MEM64));
            }

            public IEnumerable<string> GenFpuMem16_32_64()
            {
                return new string[]
                    {
                        "fild",
                        "fistp",
                        "fisttp"
                    }.SelectMany(x => GenerateCode(x, MEM16 | MEM32 | MEM64));
            }

            public IEnumerable<string> GenClflush()
            {
                return GenerateCode("clflush", MEM);
            }

            public IEnumerable<string> GenFpu()
            {
                return new string[]
                    {
                        "fcom",
                        "fcomp",
                        "ffree",
                        "fld",
                        "fst",
                        "fstp",
                        "fucom",
                        "fucomp",
                        "fxch"
                    }.SelectMany(x => GenerateCode(x, STi));
            }

            public IEnumerable<string> GenFpuFpu()
            {
                var mode1Params = new { Op1 = ST0, Op2 = STi };
                var mode2Params = new { Op1 = STi, Op2 = ST0 };
                var defaultParams = new { Op1 = STi, Op2 = NOPARA };
                var operands = new[]
                    {
                        new[] { mode1Params, defaultParams },
                        new[] { mode2Params, defaultParams },
                        new[] { mode1Params, mode2Params, defaultParams }
                    };
                return new[]
                    {
                        new { Code = "fadd", Mode = 2 },
                        new { Code = "faddp", Mode = 1 },
                        new { Code = "fcmovb", Mode = 0 },
                        new { Code = "fcmove", Mode = 0 },
                        new { Code = "fcmovbe", Mode = 0 },
                        new { Code = "fcmovu", Mode = 0 },
                        new { Code = "fcmovnb", Mode = 0 },
                        new { Code = "fcmovne", Mode = 0 },
                        new { Code = "fcmovnbe", Mode = 0 },
                        new { Code = "fcmovnu", Mode = 0 },
                        new { Code = "fcomi", Mode = 0 },
                        new { Code = "fcomip", Mode = 0 },
                        new { Code = "fucomi", Mode = 0 },
                        new { Code = "fucomip", Mode = 0 },
                        new { Code = "fdiv", Mode = 2 },
                        new { Code = "fdivp", Mode = 1 },
                        new { Code = "fdivr", Mode = 2 },
                        new { Code = "fdivrp", Mode = 1 },
                        new { Code = "fmul", Mode = 2 },
                        new { Code = "fmulp", Mode = 1 },
                        new { Code = "fsub", Mode = 2 },
                        new { Code = "fsubp", Mode = 1 },
                        new { Code = "fsubr", Mode = 2 },
                        new { Code = "fsubrp", Mode = 1 }
                    }.SelectMany(x => operands[x.Mode], (c, p) => new { c.Code, p.Op1, p.Op2 })
                     .SelectMany(x => GenerateCode(x.Code, x.Op1, x.Op2));
            }

            public IEnumerable<string> GenCmp()
            {
                string[] predict = new string[]
                    {
                        "eq",
                        "lt",
                        "le",
                        "unord",
                        "neq",
                        "nlt",
                        "nle",
                        "ord",
                        "eq_uq",
                        "nge",
                        "ngt",
                        "false",
                        "neq_oq",
                        "ge",
                        "gt",
                        "true",
                        "eq_os",
                        "lt_oq",
                        "le_oq",
                        "unord_s",
                        "neq_us",
                        "nlt_uq",
                        "nle_uq",
                        "ord_s",
                        "eq_us",
                        "nge_uq",
                        "ngt_uq",
                        "false_os",
                        "neq_os",
                        "ge_oq",
                        "gt_oq",
                        "true_us"
                    };
                string[] suffixes = new string[] { "pd", "ps", "sd", "ss" };

                var results = Enumerable.Empty<string>();

                for (int i = 0; i < suffixes.Length; i++)
                {
                    for (int n = 0; n < predict.Length; n++)
                    {
                        if (n < 8)
                        {
                            results = results.Concat(GenerateCode("cmp" + predict[n] + suffixes[i], XMM, XMM | MEM));
                        }
                        string code = "vcmp" + predict[n] + suffixes[i];
                        results = results.Concat(GenerateCode(code, XMM, XMM | MEM));
                        results = results.Concat(GenerateCode(code, XMM, XMM, XMM | MEM));
                        if (i < 2)
                        {
                            results = results.Concat(GenerateCode(code, YMM, YMM | MEM));
                            results = results.Concat(GenerateCode(code, YMM, YMM, YMM | MEM));
                        }
                    }
                }

                return results;
            }

            public IEnumerable<string> GenAVX1()
            {
                var code = new[]
                    {
                        new { Code = "add", OnlyPdPs = false },
                        new { Code = "sub", OnlyPdPs = false },
                        new { Code = "mul", OnlyPdPs = false },
                        new { Code = "div", OnlyPdPs = false },
                        new { Code = "max", OnlyPdPs = false },
                        new { Code = "min", OnlyPdPs = false },
                        new { Code = "and", OnlyPdPs = true },
                        new { Code = "andn", OnlyPdPs = true },
                        new { Code = "or", OnlyPdPs = true },
                        new { Code = "xor", OnlyPdPs = true },
                        new { Code = "addsub", OnlyPdPs = true },
                        new { Code = "hadd", OnlyPdPs = true },
                        new { Code = "hsub", OnlyPdPs = true }
                    };
                var suffixes = new[]
                    {
                        new { Suffix = "pd", SupportYMM = true },
                        new { Suffix = "ps", SupportYMM = true },
                        new { Suffix = "sd", SupportYMM = false },
                        new { Suffix = "ss", SupportYMM = false }
                    };

                var results = Enumerable.Empty<string>();

                foreach (var c in code)
                {
                    for (int i = 0; i < suffixes.Length; i++)
                    {
                        if (c.OnlyPdPs && i == 2)
                        {
                            break;
                        }
                        string name = "v" + c.Code + suffixes[i].Suffix;
                        results = results.Concat(GenerateCode(name, XMM, XMM | MEM));
                        results = results.Concat(GenerateCode(name, XMM, XMM, XMM | MEM));
                        if (suffixes[i].SupportYMM)
                        {
                            results = results.Concat(GenerateCode(name, YMM, YMM | MEM));
                            results = results.Concat(GenerateCode(name, YMM, YMM, YMM | MEM));
                        }
                    }
                }

                return results;
            }

            public IEnumerable<string> GenAVX2()
            {
                var results = Enumerable.Empty<string>();

                results = results.Concat(GenerateCode("vextractps", REG32 | MEM, XMM, IMM));
                results = results.Concat(GenerateCode("vldmxcsr", MEM));
                results = results.Concat(GenerateCode("vstmxcsr", MEM));
                results = results.Concat(GenerateCode("vmaskmovdqu", XMM, XMM));
                results = results.Concat(GenerateCode("vmovd", XMM, REG32 | MEM));
                results = results.Concat(GenerateCode("vmovd", REG32 | MEM, XMM));
                results = results.Concat(GenerateCode("vmovq", XMM, XMM | MEM));
                results = results.Concat(GenerateCode("vmovq", MEM, XMM));
                results = results.Concat(GenerateCode("vmovhlps", XMM, XMM));
                results = results.Concat(GenerateCode("vmovhlps", XMM, XMM, XMM));
                results = results.Concat(GenerateCode("vmovlhps", XMM, XMM));
                results = results.Concat(GenerateCode("vmovlhps", XMM, XMM, XMM));

                results = results.Concat(
                    new string[]
                        {
                            "vmovhpd",
                            "vmovhps",
                            "vmovlpd",
                            "vmovlps"
                        }.SelectMany(
                            x => new[]
                                {
                                    new { Op1 = XMM, Op2 = XMM, Op3 = MEM },
                                    new { Op1 = XMM, Op2 = MEM, Op3 = NOPARA },
                                    new { Op1 = MEM, Op2 = XMM, Op3 = NOPARA }
                                },
                            (c, p) => new { Code = c, p.Op1, p.Op2, p.Op3 }
                        ).SelectMany(x => GenerateCode(x.Code, x.Op1, x.Op2, x.Op3))
                    );

                results = results.Concat(GenerateCode("vmovmskpd", REG32e, XMM | YMM));
                results = results.Concat(GenerateCode("vmovmskps", REG32e, XMM | YMM));
                results = results.Concat(GenerateCode("vmovntdq", MEM, XMM | YMM));
                results = results.Concat(GenerateCode("vmovntpd", MEM, XMM | YMM));
                results = results.Concat(GenerateCode("vmovntps", MEM, XMM | YMM));
                results = results.Concat(GenerateCode("vmovntdqa", XMM, MEM));

                results = results.Concat(
                    new string[]
                        {
                            "vmovsd",
                            "vmovss"
                        }.SelectMany(
                            x => new[]
                                {
                                    new { Op1 = XMM, Op2 = XMM, Op3 = XMM },
                                    new { Op1 = XMM, Op2 = XMM | MEM, Op3 = NOPARA },
                                    new { Op1 = MEM, Op2 = XMM, Op3 = NOPARA }
                                },
                            (c, p) => new { Code = c, p.Op1, p.Op2, p.Op3 }
                        ).SelectMany(x => GenerateCode(x.Code, x.Op1, x.Op2, x.Op3))
                    );

                results = results.Concat(GenerateCode("vpextrb", REG32e | MEM, XMM, IMM));
                results = results.Concat(GenerateCode("vpextrd", REG32 | MEM, XMM, IMM));

                results = results.Concat(
                    new string[]
                        {
                            "vpinsrb",
                            "vpinsrw",
                            "vpinsrd"
                        }.SelectMany(
                            x => new[]
                                {
                                    new { Op1 = XMM, Op2 = XMM, Op3 = REG32|MEM, Op4 = IMM },
                                    new { Op1 = XMM, Op2 = REG32|MEM, Op3 = IMM, Op4 = NOPARA },
                                },
                            (c, p) => new { Code = c, p.Op1, p.Op2, p.Op3, p.Op4 }
                        ).SelectMany(x => GenerateCode(x.Code, x.Op1, x.Op2, x.Op3, x.Op4))
                    );

                results = results.Concat(GenerateCode("vpmovmskb", REG32e, XMM));

                results = results.Concat(
                    new[]
                        {
                            new { Code = "vblendvpd", SupportYMM = true },
                            new { Code = "vblendvps", SupportYMM = true },
                            new { Code = "vpblendvb", SupportYMM = false },
                        }.Select(
                            x => new
                                {
                                    x.Code,
                                    x.SupportYMM,
                                    Params = new[]
                                        {
                                            new { Op1 = XMM, Op2 = XMM, Op3 = XMM | MEM, Op4 = XMM },
                                            new { Op1 = XMM, Op2 = XMM | MEM, Op3 = XMM, Op4 = NOPARA },
                                        }
                                }
                        ).Select(
                            x => new
                                {
                                    x.Code,
                                    Params = x.SupportYMM ? x.Params.Concat(
                                        new[]
                                            {
                                                new { Op1 = YMM, Op2 = YMM, Op3 = YMM | MEM, Op4 = YMM },
                                                new { Op1 = YMM, Op2 = YMM | MEM, Op3 = YMM, Op4 = NOPARA },
                                            }
                                                                ) : x.Params
                                }
                        ).SelectMany(x => x.Params.SelectMany(p => GenerateCode(x.Code, p.Op1, p.Op2, p.Op3, p.Op4)))
                    );

                results = results.Concat(GenerateCode("vcvtss2si", REG32e, XMM | MEM));
                results = results.Concat(GenerateCode("vcvttss2si", REG32e, XMM | MEM));
                results = results.Concat(GenerateCode("vcvtsd2si", REG32e, XMM | MEM));
                results = results.Concat(GenerateCode("vcvttsd2si", REG32e, XMM | MEM));
                results = results.Concat(GenerateCode("vcvtsi2ss", XMM, XMM, REG32e | MEM));
                results = results.Concat(GenerateCode("vcvtsi2ss", XMM, REG32e | MEM));
                results = results.Concat(GenerateCode("vcvtsi2sd", XMM, XMM, REG32e | MEM));
                results = results.Concat(GenerateCode("vcvtsi2sd", XMM, REG32e | MEM));
                results = results.Concat(GenerateCode("vcvtps2pd", XMM | YMM, XMM | MEM));
                results = results.Concat(GenerateCode("vcvtdq2pd", XMM | YMM, XMM | MEM));
                results = results.Concat(GenerateCode("vcvtpd2ps", XMM, XMM | YMM | MEM));
                results = results.Concat(GenerateCode("vcvtpd2dq", XMM, XMM | YMM | MEM));
                results = results.Concat(GenerateCode("vcvttpd2dq", XMM, XMM | YMM | MEM));

                if (Environment.Is64BitProcess)
                {
                    results = results.Concat(GenerateCode("vmovq", XMM, REG64));
                    results = results.Concat(GenerateCode("vmovq", REG64, XMM));
                    results = results.Concat(GenerateCode("vpextrq", REG64 | MEM, XMM, IMM));
                    results = results.Concat(GenerateCode("vpinsrq", XMM, XMM, REG64 | MEM, IMM));
                    results = results.Concat(GenerateCode("vpinsrq", XMM, REG64 | MEM, IMM));
                }

                return results;
            }

            public IEnumerable<string> GenAVX_X_X_XM_Omit()
            {
                var code = new[]
                    {
                        new { Code = "vaesenc", SupportYMM = false },
                        new { Code = "vaesenclast", SupportYMM = false },
                        new { Code = "vaesdec", SupportYMM = false },
                        new { Code = "vaesdeclast", SupportYMM = false },
                        new { Code = "vcvtsd2ss", SupportYMM = false },
                        new { Code = "vcvtss2sd", SupportYMM = false },
                        new { Code = "vpacksswb", SupportYMM = false },
                        new { Code = "vpackssdw", SupportYMM = false },
                        new { Code = "vpackuswb", SupportYMM = false },
                        new { Code = "vpackusdw", SupportYMM = false },
                        new { Code = "vpaddb", SupportYMM = false },
                        new { Code = "vpaddw", SupportYMM = false },
                        new { Code = "vpaddd", SupportYMM = false },
                        new { Code = "vpaddq", SupportYMM = false },
                        new { Code = "vpaddsb", SupportYMM = false },
                        new { Code = "vpaddsw", SupportYMM = false },
                        new { Code = "vpaddusb", SupportYMM = false },
                        new { Code = "vpaddusw", SupportYMM = false },
                        new { Code = "vpand", SupportYMM = false },
                        new { Code = "vpandn", SupportYMM = false },
                        new { Code = "vpavgb", SupportYMM = false },
                        new { Code = "vpavgw", SupportYMM = false },
                        new { Code = "vpcmpeqb", SupportYMM = false },
                        new { Code = "vpcmpeqw", SupportYMM = false },
                        new { Code = "vpcmpeqd", SupportYMM = false },
                        new { Code = "vpcmpgtb", SupportYMM = false },
                        new { Code = "vpcmpgtw", SupportYMM = false },
                        new { Code = "vpcmpgtd", SupportYMM = false },
                        new { Code = "vphaddw", SupportYMM = false },
                        new { Code = "vphaddd", SupportYMM = false },
                        new { Code = "vphaddsw", SupportYMM = false },
                        new { Code = "vphsubw", SupportYMM = false },
                        new { Code = "vphsubd", SupportYMM = false },
                        new { Code = "vphsubsw", SupportYMM = false },
                        new { Code = "vpmaddwd", SupportYMM = false },
                        new { Code = "vpmaddubsw", SupportYMM = false },
                        new { Code = "vpmaxsb", SupportYMM = false },
                        new { Code = "vpmaxsw", SupportYMM = false },
                        new { Code = "vpmaxsd", SupportYMM = false },
                        new { Code = "vpmaxub", SupportYMM = false },
                        new { Code = "vpmaxuw", SupportYMM = false },
                        new { Code = "vpmaxud", SupportYMM = false },
                        new { Code = "vpminsb", SupportYMM = false },
                        new { Code = "vpminsw", SupportYMM = false },
                        new { Code = "vpminsd", SupportYMM = false },
                        new { Code = "vpminub", SupportYMM = false },
                        new { Code = "vpminuw", SupportYMM = false },
                        new { Code = "vpminud", SupportYMM = false },
                        new { Code = "vpmulhuw", SupportYMM = false },
                        new { Code = "vpmulhrsw", SupportYMM = false },
                        new { Code = "vpmulhw", SupportYMM = false },
                        new { Code = "vpmullw", SupportYMM = false },
                        new { Code = "vpmulld", SupportYMM = false },
                        new { Code = "vpmuludq", SupportYMM = false },
                        new { Code = "vpmuldq", SupportYMM = false },
                        new { Code = "vpor", SupportYMM = false },
                        new { Code = "vpsadbw", SupportYMM = false },
                        new { Code = "vpsignb", SupportYMM = false },
                        new { Code = "vpsignw", SupportYMM = false },
                        new { Code = "vpsignd", SupportYMM = false },
                        new { Code = "vpsllw", SupportYMM = false },
                        new { Code = "vpslld", SupportYMM = false },
                        new { Code = "vpsllq", SupportYMM = false },
                        new { Code = "vpsraw", SupportYMM = false },
                        new { Code = "vpsrad", SupportYMM = false },
                        new { Code = "vpsrlw", SupportYMM = false },
                        new { Code = "vpsrld", SupportYMM = false },
                        new { Code = "vpsrlq", SupportYMM = false },
                        new { Code = "vpsubb", SupportYMM = false },
                        new { Code = "vpsubw", SupportYMM = false },
                        new { Code = "vpsubd", SupportYMM = false },
                        new { Code = "vpsubq", SupportYMM = false },
                        new { Code = "vpsubsb", SupportYMM = false },
                        new { Code = "vpsubsw", SupportYMM = false },
                        new { Code = "vpsubusb", SupportYMM = false },
                        new { Code = "vpsubusw", SupportYMM = false },
                        new { Code = "vpunpckhbw", SupportYMM = false },
                        new { Code = "vpunpckhwd", SupportYMM = false },
                        new { Code = "vpunpckhdq", SupportYMM = false },
                        new { Code = "vpunpckhqdq", SupportYMM = false },
                        new { Code = "vpunpcklbw", SupportYMM = false },
                        new { Code = "vpunpcklwd", SupportYMM = false },
                        new { Code = "vpunpckldq", SupportYMM = false },
                        new { Code = "vpunpcklqdq", SupportYMM = false },
                        new { Code = "vpxor", SupportYMM = false },
                        new { Code = "vsqrtsd", SupportYMM = false },
                        new { Code = "vsqrtss", SupportYMM = false },
                        new { Code = "vunpckhpd", SupportYMM = true },
                        new { Code = "vunpckhps", SupportYMM = true },
                        new { Code = "vunpcklpd", SupportYMM = true },
                        new { Code = "vunpcklps", SupportYMM = true }
                    };

                var results = Enumerable.Empty<string>();

                foreach (var c in code)
                {
                    results = results.Concat(GenerateCode(c.Code, XMM, XMM | MEM));
                    results = results.Concat(GenerateCode(c.Code, XMM, XMM, XMM | MEM));
                    if (c.SupportYMM)
                    {
                        results = results.Concat(GenerateCode(c.Code, YMM, YMM | MEM));
                        results = results.Concat(GenerateCode(c.Code, YMM, YMM, YMM | MEM));
                    }
                }

                return results;
            }

            public IEnumerable<string> GenAVX_X_X_XM_IMM()
            {
                var code = new[]
                    {
                        new { Code = "vblendpd", SupportYMM = true },
                        new { Code = "vblendps", SupportYMM = true },
                        new { Code = "vdppd", SupportYMM = false },
                        new { Code = "vdpps", SupportYMM = true },
                        new { Code = "vmpsadbw", SupportYMM = false },
                        new { Code = "vpblendw", SupportYMM = false },
                        new { Code = "vroundsd", SupportYMM = false },
                        new { Code = "vroundss", SupportYMM = false },
                        new { Code = "vpclmulqdq", SupportYMM = false },
                        new { Code = "vcmppd", SupportYMM = true },
                        new { Code = "vcmpps", SupportYMM = true },
                        new { Code = "vcmpsd", SupportYMM = false },
                        new { Code = "vcmpss", SupportYMM = false },
                        new { Code = "vinsertps", SupportYMM = false },
                        new { Code = "vpalignr", SupportYMM = false },
                        new { Code = "vshufpd", SupportYMM = true },
                        new { Code = "vshufps", SupportYMM = true },
                    };

                var results = Enumerable.Empty<string>();

                foreach (var c in code)
                {
                    results = results.Concat(GenerateCode(c.Code, XMM, XMM, XMM | MEM, IMM));
                    results = results.Concat(GenerateCode(c.Code, XMM, XMM | MEM, IMM));
                    if (c.SupportYMM)
                    {
                        results = results.Concat(GenerateCode(c.Code, YMM, YMM, YMM | MEM, IMM));
                        results = results.Concat(GenerateCode(c.Code, YMM, YMM | MEM, IMM));
                    }
                }

                return results;
            }

            public IEnumerable<string> GenAVX_X_XM_IMM()
            {
                var code = new[]
                    {
                        new { Code = "vroundpd", SupportYMM = true },
                        new { Code = "vroundps", SupportYMM = true },
                        new { Code = "vpcmpestri", SupportYMM = false },
                        new { Code = "vpcmpestrm", SupportYMM = false },
                        new { Code = "vpcmpistri", SupportYMM = false },
                        new { Code = "vpcmpistrm", SupportYMM = false },
                        new { Code = "vpermilpd", SupportYMM = true },
                        new { Code = "vpermilps", SupportYMM = true },
                        new { Code = "vaeskeygenassist", SupportYMM = false },
                        new { Code = "vpshufd", SupportYMM = false },
                        new { Code = "vpshufhw", SupportYMM = false },
                        new { Code = "vpshuflw", SupportYMM = false }
                    };

                var results = Enumerable.Empty<string>();

                foreach (var c in code)
                {
                    results = results.Concat(GenerateCode(c.Code, XMM, XMM | MEM, IMM));
                    if (c.SupportYMM)
                    {
                        results = results.Concat(GenerateCode(c.Code, YMM, YMM | MEM, IMM));
                    }
                }

                return results;
            }

            public IEnumerable<string> GenAVX_X_X_XM()
            {
                var code = new[]
                    {
                        new { Code = "vpermilpd", SupportYMM = true },
                        new { Code = "vpermilps", SupportYMM = true },
                        new { Code = "vpshufb", SupportYMM = false }
                    };

                var results = Enumerable.Empty<string>();

                foreach (var c in code)
                {
                    results = results.Concat(GenerateCode(c.Code, XMM, XMM, XMM | MEM));
                    if (c.SupportYMM)
                    {
                        results = results.Concat(GenerateCode(c.Code, YMM, YMM, YMM | MEM));
                    }
                }

                return results;
            }

            public IEnumerable<string> GenAVX_X_XM()
            {
                var code = new[]
                    {
                        new { Code = "vaesimc", SupportYMM = false },
                        new { Code = "vtestps", SupportYMM = true },
                        new { Code = "vtestpd", SupportYMM = true },
                        new { Code = "vcomisd", SupportYMM = false },
                        new { Code = "vcomiss", SupportYMM = false },
                        new { Code = "vcvtdq2ps", SupportYMM = true },
                        new { Code = "vcvtps2dq", SupportYMM = true },
                        new { Code = "vcvttps2dq", SupportYMM = true },
                        new { Code = "vmovapd", SupportYMM = true },
                        new { Code = "vmovaps", SupportYMM = true },
                        new { Code = "vmovddup", SupportYMM = true },
                        new { Code = "vmovdqa", SupportYMM = true },
                        new { Code = "vmovdqu", SupportYMM = true },
                        new { Code = "vmovupd", SupportYMM = true },
                        new { Code = "vmovups", SupportYMM = true },
                        new { Code = "vpabsb", SupportYMM = false },
                        new { Code = "vpabsw", SupportYMM = false },
                        new { Code = "vpabsd", SupportYMM = false },
                        new { Code = "vphminposuw", SupportYMM = false },
                        new { Code = "vpmovsxbw", SupportYMM = false },
                        new { Code = "vpmovsxbd", SupportYMM = false },
                        new { Code = "vpmovsxbq", SupportYMM = false },
                        new { Code = "vpmovsxwd", SupportYMM = false },
                        new { Code = "vpmovsxwq", SupportYMM = false },
                        new { Code = "vpmovsxdq", SupportYMM = false },
                        new { Code = "vpmovzxbw", SupportYMM = false },
                        new { Code = "vpmovzxbd", SupportYMM = false },
                        new { Code = "vpmovzxbq", SupportYMM = false },
                        new { Code = "vpmovzxwd", SupportYMM = false },
                        new { Code = "vpmovzxwq", SupportYMM = false },
                        new { Code = "vpmovzxdq", SupportYMM = false },
                        new { Code = "vptest", SupportYMM = false },
                        new { Code = "vrcpps", SupportYMM = true },
                        new { Code = "vrcpss", SupportYMM = false },
                        new { Code = "vrsqrtps", SupportYMM = true },
                        new { Code = "vrsqrtss", SupportYMM = false },
                        new { Code = "vsqrtpd", SupportYMM = true },
                        new { Code = "vsqrtps", SupportYMM = true },
                        new { Code = "vucomisd", SupportYMM = false },
                        new { Code = "vucomiss", SupportYMM = false }
                    };

                var results = Enumerable.Empty<string>();

                foreach (var c in code)
                {
                    results = results.Concat(GenerateCode(c.Code, XMM, XMM | MEM));
                    if (c.SupportYMM)
                    {
                        results = results.Concat(GenerateCode(c.Code, YMM, YMM | MEM));
                    }
                }

                return results;
            }

            public IEnumerable<string> GenAVX_M_X()
            {
                var code = new[]
                    {
                        new { Code = "vmovapd", SupportYMM = true },
                        new { Code = "vmovaps", SupportYMM = true },
                        new { Code = "vmovdqa", SupportYMM = true },
                        new { Code = "vmovdqu", SupportYMM = true },
                        new { Code = "vmovupd", SupportYMM = true },
                        new { Code = "vmovups", SupportYMM = true }
                    };

                var results = Enumerable.Empty<string>();

                foreach (var c in code)
                {
                    results = results.Concat(GenerateCode(c.Code, MEM, XMM));
                    if (c.SupportYMM)
                    {
                        results = results.Concat(GenerateCode(c.Code, MEM, YMM));
                    }
                }

                return results;
            }

            public IEnumerable<string> GenAVX_X_X_IMM_Omit()
            {
                return new string[]
                    {
                        "vpslldq",
                        "vpsrldq",
                        "vpsllw",
                        "vpslld",
                        "vpsllq",
                        "vpsraw",
                        "vpsrad",
                        "vpsrlw",
                        "vpsrld",
                        "vpsrlq"
                    }.SelectMany(
                        x => new[]
                            {
                                new { Op1 = XMM, Op2 = XMM, Op3 = IMM },
                                new { Op1 = XMM, Op2 = IMM, Op3 = NOPARA }
                            },
                        (c, p) => new { Code = c, p.Op1, p.Op2, p.Op3 }
                    ).SelectMany(x => GenerateCode(x.Code, x.Op1, x.Op2, x.Op3));
            }

            public IEnumerable<string> GenFMA()
            {
                var pattern = new[]
                    {
                        new { Code = "vfmadd", SupportYMM = true },
                        new { Code = "vfmadd", SupportYMM = false },
                        new { Code = "vfmaddsub", SupportYMM = true },
                        new { Code = "vfmsubadd", SupportYMM = true },
                        new { Code = "vfmsub", SupportYMM = true },
                        new { Code = "vfmsub", SupportYMM = false },
                        new { Code = "vfnmadd", SupportYMM = true },
                        new { Code = "vfnmadd", SupportYMM = false },
                        new { Code = "vfnmsub", SupportYMM = true },
                        new { Code = "vfnmsub", SupportYMM = false }
                    }.SelectMany(
                        x => new string[] { "132", "213", "231" },
                        (c, o) => new { c.Code, c.SupportYMM, Order = o }
                    ).SelectMany(
                        x => new[]
                            {
                                new { Suffixes = new string[] { "pd", "ps" }, SupportYMM = true },
                                new { Suffixes = new string[] { "sd", "ss" }, SupportYMM = false }
                            },
                        (x, s) => new { x.Code, x.SupportYMM, x.Order, Suffixes = x.SupportYMM == s.SupportYMM ? s.Suffixes : null }
                    ).Where(x => x.Suffixes != null)
                     .SelectMany(
                         x => new[]
                             {
                                 new { Code = x.Code + x.Order + x.Suffixes[0], x.SupportYMM },
                                 new { Code = x.Code + x.Order + x.Suffixes[1], x.SupportYMM },
                             }
                    );

                var results = Enumerable.Empty<string>();

                foreach (var p in pattern)
                {
                    results = results.Concat(GenerateCode(p.Code, XMM, XMM, XMM | MEM));
                    if (p.SupportYMM)
                    {
                        results = results.Concat(GenerateCode(p.Code, YMM, YMM, YMM | MEM));
                    }
                }

                return results;
            }

            public IEnumerable<string> GenFMA2()
            {
                var results = Enumerable.Empty<string>();

                results = results.Concat(GenerateCode("vmaskmovps", XMM, XMM, MEM));
                results = results.Concat(GenerateCode("vmaskmovps", YMM, YMM, MEM));
                results = results.Concat(GenerateCode("vmaskmovpd", YMM, YMM, MEM));
                results = results.Concat(GenerateCode("vmaskmovpd", XMM, XMM, MEM));
                results = results.Concat(GenerateCode("vmaskmovps", MEM, XMM, XMM));
                results = results.Concat(GenerateCode("vmaskmovpd", MEM, XMM, XMM));
                results = results.Concat(GenerateCode("vbroadcastf128", YMM, MEM));
                results = results.Concat(GenerateCode("vbroadcastsd", YMM, MEM));
                results = results.Concat(GenerateCode("vbroadcastss", XMM | YMM, MEM));
                results = results.Concat(GenerateCode("vinsertf128", YMM, YMM, XMM | MEM, IMM8));
                results = results.Concat(GenerateCode("vperm2f128", YMM, YMM, YMM | MEM, IMM8));

                return results;
            }

            #endregion TestCodeGenerators

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < (int)Size; i++)
                {
                    sb.Append(CodeMemory[i].ToString("X2"));
                }
                return sb.ToString();
            }

            private IEnumerable<string> GenerateCode(string opCode, ulong op1 = NOPARA, ulong op2 = NOPARA, ulong op3 = NOPARA, ulong op4 = NOPARA)
            {
                for (int i = 0; i < BitEnd; i++)
                {
                    if ((op1 & (1UL << i)) == 0)
                    {
                        continue;
                    }
                    for (int n = 0; n < BitEnd; n++)
                    {
                        if ((op2 & (1UL << n)) == 0)
                        {
                            continue;
                        }
                        for (int k = 0; k < BitEnd; k++)
                        {
                            if ((op3 & (1UL << k)) == 0)
                            {
                                continue;
                            }
                            for (int s = 0; s < BitEnd; s++)
                            {
                                if ((op4 & (1UL << s)) == 0)
                                {
                                    continue;
                                }
                                var args = new List<object>();
                                if ((op1 & NOPARA) == 0)
                                {
                                    args.Add(GetOperand(1UL << i));
                                }
                                if ((op2 & NOPARA) == 0)
                                {
                                    args.Add(GetOperand(1UL << n));
                                }
                                if ((op3 & NOPARA) == 0)
                                {
                                    args.Add(GetOperand(1UL << k));
                                }
                                if ((op4 & NOPARA) == 0)
                                {
                                    args.Add(GetOperand(1UL << s));
                                }
                                ExecMethod(opCode, args);
                                yield return ToString();
                                ResetSize();
                            }
                        }
                    }
                }
            }

            private IEnumerable<string> GenerateCode(string opCode, OpCodeArgument value, ulong op = NOPARA)
            {
                for (int i = 0; i < BitEnd; i++)
                {
                    if ((op & (1UL << i)) == 0)
                    {
                        continue;
                    }
                    var args = new List<object>();
                    object valueResult = value();
                    if (valueResult is IEnumerable<object>)
                    {
                        args.AddRange((IEnumerable<object>)valueResult);
                    }
                    else
                    {
                        args.Add(valueResult);
                    }
                    if ((op & NOPARA) == 0)
                    {
                        args.Add(GetOperand(1UL << i));
                    }
                    ExecMethod(opCode, args);
                    yield return ToString();
                    ResetSize();
                }
            }

            private IEnumerable<string> GenerateCode(string opCode, ulong op, OpCodeArgument value)
            {
                for (int i = 0; i < BitEnd; i++)
                {
                    if ((op & (1UL << i)) == 0)
                    {
                        continue;
                    }
                    var args = new List<object>();
                    if ((op & NOPARA) == 0)
                    {
                        args.Add(GetOperand(1UL << i));
                    }
                    object valueResult = value();
                    if (value.GetType().IsArray)
                    {
                        args.AddRange((object[])valueResult);
                    }
                    else
                    {
                        args.Add(valueResult);
                    }
                    ExecMethod(opCode, args);
                    yield return ToString();
                    ResetSize();
                }
            }

            private object GetOperand(ulong type)
            {
                uint rand = XorShift.Rand();
                int idx = (int)((rand / 31) & 7);

                if (type == ST0)
                {
                    return st0;
                }
                if (type == STi)
                {
                    return st2;
                }
                if (Environment.Is64BitProcess)
                {
                    if (type == _XMM2)
                    {
                        return GetRegisterByName("xmm", idx + 8);
                    }
                    if (type == _YMM2)
                    {
                        return GetRegisterByName("ymm", idx);
                    }
                    if (type == _REG64)
                    {
                        // rax以外
                        return new IOperand[]
                            {
                                rax, rcx, rdx, rbx, rsp, rbp, rsi, rdi
                            }[(idx % 7) + 1];
                    }
                    if (type == _REG64_2)
                    {
                        return GetRegisterByName("r", idx + 8);
                    }
                    if (type == REG32_2)
                    {
                        return GetRegisterByName("r", idx + 8, "d");
                    }
                    if (type == REG16_2)
                    {
                        return GetRegisterByName("r", idx + 8, "w");
                    }
                    if (type == REG8_2)
                    {
                        return GetRegisterByName("r", idx + 8, "b");
                    }
                    if (type == REG8_3)
                    {
                        return new IOperand[]
                            {
                                spl, bpl, sil, dil, spl, bpl, sil, dil
                            }[idx];
                    }
                    if (type == RAX)
                    {
                        return rax;
                    }
                }
                if (type == _MEMe)
                {
                    return ptr[rdx + r15 + 0x12];
                }
                switch (type)
                {
                    case MMX:
                        {
                            return GetRegisterByName("mm", idx);
                        }
                    case _XMM:
                        {
                            return GetRegisterByName("xmm", idx);
                        }
                    case _YMM:
                        {
                            return GetRegisterByName("ymm", idx);
                        }
                    case _MEM:
                        {
                            return ptr[eax + ecx + 3];
                        }
                    case MEM8:
                        {
                            return @byte[eax + edx];
                        }
                    case MEM16:
                        {
                            return word[esi];
                        }
                    case MEM32:
                        {
                            return dword[ebp * 2];
                        }
                    case MEM64:
                        {
                            return qword[eax + ecx * 8];
                        }
                    case MEM_ONLY_DISP:
                        {
                            return ptr[new IntPtr(0x123)];
                        }
                    case _REG16:
                        {
                            // ax以外
                            return new IOperand[]
                                {
                                    ax, cx, dx, bx, sp, bp, si, di
                                }[(idx % 7) + 1];
                        }
                    case _REG8:
                        {
                            // al以外
                            var reg = new IOperand[] { al, cl, dl, bl };
                            if (Environment.Is64BitProcess)
                            {
                                reg = reg.Concat(reg).ToArray();
                            }
                            else
                            {
                                reg = reg.Concat(new IOperand[] { ah, ch, dh, bh }).ToArray();
                            }
                            return reg[(idx % 7) + 1];
                        }
                    case _REG32:
                        {
                            // eax以外
                            return new IOperand[]
                                {
                                    eax, ecx, edx, ebx, esp, ebp, esi, edi
                                }[(idx % 7) + 1];
                        }
                    case EAX:
                        {
                            return eax;
                        }
                    case AX:
                        {
                            return ax;
                        }
                    case AL:
                        {
                            return al;
                        }
                    case CL:
                        {
                            return cl;
                        }
                    case ONE:
                        {
                            return 1;
                        }
                    case IMM32:
                        {
                            return 12345678;
                        }
                    case IMM16:
                        {
                            return 1000;
                        }
                    case IMM8:
                        {
                            return 4;
                        }
                    case NEG8:
                        {
                            return -30;
                        }
                    case NEG16:
                        {
                            return -1000;
                        }
                    case NEG32:
                        {
                            return -100000;
                        }
                    case IMM_1:
                        {
                            return 4;
                        }
                    case IMM_2:
                        {
                            return 0xda;
                        }
                    case NEG:
                        {
                            return -5;
                        }
                }

                return 0;
            }

            private IOperand GetRegisterByName(string prefix, int id, string suffix = "")
            {
                FieldInfo info = typeof(CodeGenerator).GetField(prefix + id.ToString() + suffix, BindingFlags.Instance | BindingFlags.Public);
                return (IOperand)info.GetValue(this);
            }

            private void ExecMethod(string name, IEnumerable<object> args)
            {
                MethodInfo info = typeof(CodeGenerator).GetMethod(name, args.Select(x => x.GetType()).ToArray());
                object[] argumentArray = args.ToArray();
                if (info != null)
                {
                    info.Invoke(this, argumentArray);
                    return;
                }

                var methods = typeof(CodeGenerator)
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Where(x => x.Name == name && x.GetParameters().Length >= args.Count())
                    .Select(
                        x => new
                            {
                                Method = x,
                                ParameterType = x.GetParameters().Select(p => p.ParameterType),
                                DefaultValue = x.GetParameters().Select(p => p.RawDefaultValue)
                            }
                    ).ToArray();
                var hits = methods.Where(
                    x => x.ParameterType
                          .Take(argumentArray.Length)
                          .Zip(argumentArray, (t, a) => new { Type = t, Arg = a })
                          .All(v => v.Type.IsInstanceOfType(v.Arg))
                    );
                if (!hits.Any())
                {
                    hits = methods.Where(
                        x => x.ParameterType
                              .Take(argumentArray.Length)
                              .Select(t => t == typeof(byte) || t == typeof(uint) || t == typeof(ulong) ? typeof(int) : t)
                              .Zip(argumentArray, (t, a) => new { Arg = a, Type = t })
                              .All(v => v.Type.IsInstanceOfType(v.Arg))
                        );
                }

                var hit = hits.First();
                info = hit.Method;
                argumentArray = argumentArray.Concat(hit.DefaultValue.Skip(argumentArray.Length))
                                             .Zip(hit.ParameterType, (a, t) => new { Arg = a, Type = t })
                                             .Select(v => v.Arg is int ? ConvertNumber((int)v.Arg, v.Type) : v.Arg)
                                             .ToArray();
                info.Invoke(this, argumentArray);
            }

            private object ConvertNumber(int p, Type type)
            {
                if (type == typeof(uint))
                {
                    return unchecked((uint)p);
                }
                else if (type == typeof(ulong))
                {
                    return unchecked((ulong)p);
                }
                else if (type == typeof(byte))
                {
                    return unchecked((byte)p);
                }
                return p;
            }

            bool CheckParameterType(Type parameterType, Type argType)
            {
                if (parameterType != argType)
                {
                    if (argType == typeof(object))
                    {
                        return false;
                    }
                    if (parameterType.IsInterface)
                    {
                        return argType.GetInterface(parameterType.Name) != null;
                    }
                    return CheckParameterType(parameterType, argType.BaseType);
                }
                return true;
            }
        }

        [TestMethod]
        public void Test()
        {
            XorShift.Init();
            using (MnemonicTestCode code = new MnemonicTestCode())
            {
                var results = code.GenJmp();
                results = results.Concat(code.GenSimple());
                results = results.Concat(code.GenReg1());
                results = results.Concat(code.GenRorM());
                results = results.Concat(code.GenPushPop());
                results = results.Concat(code.GenTest());
                results = results.Concat(code.GenEtc());
                results = results.Concat(code.GenShift());
                results = results.Concat(code.GenShxd());
                results = results.Concat(code.GenBs());
                results = results.Concat(code.GenMMX1());
                results = results.Concat(code.GenMMX2());
                results = results.Concat(code.GenMMX3());
                results = results.Concat(code.GenMMX4());
                results = results.Concat(code.GenMMX5());
                results = results.Concat(code.GenXMM1());
                results = results.Concat(code.GenXMM2());
                results = results.Concat(code.GenXMM3());
                results = results.Concat(code.GenXMM4());
                results = results.Concat(code.GenCmov());
                results = results.Concat(code.GenFpuMem16_32());
                results = results.Concat(code.GenFpuMem32_64());
                results = results.Concat(code.GenFpuMem16_32_64());
                results = results.Concat(code.GenClflush());
                results = results.Concat(code.GenFpu());
                results = results.Concat(code.GenFpuFpu());
                results = results.Concat(code.GenCmp());

                string expectedData = null;
                if (Environment.Is64BitProcess)
                {
                    expectedData = ExpectedData.mnemonic_64bit;
                }
                else
                {
                    expectedData = ExpectedData.mnemonic_32bit;
                }
                string[] expected = expectedData.Split('\n').Select(x => x.Replace("\r", "")).ToArray();
                int line = 0;
                foreach (var result in results)
                {
                    Assert.AreEqual(expected[line], result);
                    line++;
                }
            }
        }

        [TestMethod]
        public void AVXTest()
        {
            XorShift.Init();
            using (MnemonicTestCode code = new MnemonicTestCode())
            {
                var results = code.GenAVX1();
                results = results.Concat(code.GenAVX2());
                results = results.Concat(code.GenAVX_X_X_XM_Omit());
                results = results.Concat(code.GenAVX_X_X_XM_IMM());
                results = results.Concat(code.GenAVX_X_XM_IMM());
                results = results.Concat(code.GenAVX_X_X_XM());
                results = results.Concat(code.GenAVX_X_XM());
                results = results.Concat(code.GenAVX_M_X());
                results = results.Concat(code.GenAVX_X_X_IMM_Omit());
                results = results.Concat(code.GenFMA());
                results = results.Concat(code.GenFMA2());

                string expectedData = null;
                if (Environment.Is64BitProcess)
                {
                    expectedData = ExpectedData.mnemonic_avx_64bit;
                }
                else
                {
                    expectedData = ExpectedData.mnemonic_avx_32bit;
                }
                string[] expected = expectedData.Split('\n').Select(x => x.Replace("\r", "")).ToArray();
                int line = 0;
                foreach (var result in results)
                {
                    Assert.AreEqual(expected[line], result);
                    line++;
                }
            }
        }
    }
}
