/*
 * --- original code description
 * 
	@author herumi

	JPEG quantize sample
	This program generates a quantization routine by using fast division algorithm in run-time.

	time(sec)
	quality  1(low) 10     50   100(high)
	VC2005   8.0     8.0   8.0  8.0
	Xbyak    1.6     0.8   0.5  0.5


; generated code at q = 100
    push        esi
    push        edi
    mov         edi,dword ptr [esp+0Ch]
    mov         esi,dword ptr [esp+10h]
    mov         eax,dword ptr [esi]
    shr         eax,4
    mov         dword ptr [edi],eax
    mov         eax,dword ptr [esi+4]
    mov         edx,0BA2E8BA3h
    mul         eax,edx
    shr         edx,3
    ...

; generated code at q = 100
     push        esi
     push        edi
     mov         edi,dword ptr [esp+0Ch]
     mov         esi,dword ptr [esp+10h]
     mov         eax,dword ptr [esi]
     mov         dword ptr [edi],eax
     mov         eax,dword ptr [esi+4]
     mov         dword ptr [edi+4],eax
     mov         eax,dword ptr [esi+8]
     mov         dword ptr [edi+8],eax
     mov         eax,dword ptr [esi+0Ch]
	 ...

*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace XbyakSharp.Sample.Quantize
{
    class Program
    {
        private const int N = 64;

        private const int Loop = 10000000;

        static void Main(string[] args)
        {
            Console.Write("input quantize = ");
            uint q = uint.Parse(Console.ReadLine());

            uint[] qTbl = new uint[]
                {
                    16, 11, 10, 16, 24, 40, 51, 61,
                    12, 12, 14, 19, 26, 58, 60, 55,
                    14, 13, 16, 24, 40, 57, 69, 56,
                    14, 17, 22, 29, 51, 87, 80, 62,
                    18, 22, 37, 56, 68, 109, 103, 77,
                    24, 35, 55, 64, 81, 104, 113, 92,
                    49, 64, 78, 87, 103, 121, 120, 101,
                    72, 92, 95, 98, 112, 100, 103, 99
                };

            for (int i = 0; i < N; i++)
            {
                qTbl[i] /= q;
                if (qTbl[i] == 0)
                {
                    qTbl[i] = 1;
                }
            }

            Random r = new Random();
            uint[] src = Enumerable.Range(0, N).Select(x => (uint)r.Next(2048)).ToArray();
            uint[] dst = new uint[N];
            uint[] dst2 = new uint[N];

            Stopwatch sw = new Stopwatch();

            Console.Write("Managed: ");
            sw.Start();
            for (int i = 0; i < Loop; i++)
            {
                Quantize(dst, src, qTbl);
            }
            sw.Stop();
            Console.WriteLine(sw.Elapsed.ToString());

            Console.Write("Generated: ");
            sw.Reset();
            sw.Start();
            using (QuantizeCode code = new QuantizeCode(qTbl))
            {
                QuantizeCode.QuantizeMethod m = code.GetDelegate<QuantizeCode.QuantizeMethod>();
                for (int i = 0; i < Loop; i++)
                {
                    m(dst2, src, qTbl);
                }
                sw.Stop();
                Console.WriteLine(sw.Elapsed.ToString());
            }

            Console.WriteLine(dst.SequenceEqual(dst2));

            Console.ReadKey();
        }

        static void Quantize(uint[] dst, uint[] src, uint[] tbl)
        {
            for (int i = 0; i < N; i++)
            {
                dst[i] = src[i] / tbl[i];
            }
        }

        private class QuantizeCode : CodeGenerator
        {
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void QuantizeMethod(uint[] dst, uint[] src, uint[] tbl);

            public QuantizeCode(uint[] qTbl)
            {
                push(esi);
                push(edi);
                uint P_ = 4 * 2;
                mov(edi, ptr[esp + P_ + 4]);
                mov(esi, ptr[esp + P_ + 8]);
                for (int i = 0; i < N; i++)
                {
                    udiv(qTbl[i], i * 4);
                    mov(ptr[edi + (uint)i * 4], eax);
                }
                pop(edi);
                pop(esi);
                ret();
            }

            private void udiv(uint dividend, int offset)
            {
                mov(eax, ptr[esi + (uint)offset]);

                /* dividend = odd x 2^exponent */
                int exponent = 0, odd = (int)dividend;
                while ((odd & 1) == 0)
                {
                    odd >>= 1;
                    exponent++;
                }

                if (odd == 1)
                {
                    // trivial case
                    if (exponent != 0)
                    {
                        shr(eax, exponent);
                    }
                    return;
                }

                ulong mLow, mHigh;
                int len = ilog2(odd) + 1;
                {
                    ulong roundUp = 1UL << (32 + len);
                    ulong k = roundUp / (ulong)(0xFFFFFFFFL - (0xFFFFFFFFL % odd));
                    mLow = roundUp / (uint)odd;
                    mHigh = (roundUp + k) / (uint)odd;
                }

                while (((mLow >> 1) < (mHigh >> 1)) && (len > 0))
                {
                    mLow >>= 1;
                    mHigh >>= 1;
                    len--;
                }

                ulong m;
                int a;
                if ((mHigh >> 32) == 0)
                {
                    m = mHigh;
                    a = 0;
                }
                else
                {
                    len = ilog2(odd);
                    ulong roundDown = 1UL << (32 + len);
                    mLow = roundDown / (uint)odd;
                    int r = (int)(roundDown % (uint)odd);
                    m = (r <= (odd >> 1)) ? mLow : mLow + 1;
                    a = 1;
                }
                while ((m & 1) == 0)
                {
                    m >>= 1;
                    len--;
                }
                len += exponent;

                mov(edx, m);
                mul(edx);
                if (a != 0)
                {
                    add(eax, (uint)m);
                    adc(edx, 0);
                }
                if (len != 0)
                {
                    shr(edx, len);
                }
                mov(eax, edx);
            }

            private int ilog2(int x)
            {
                int shift = 0;
                while ((1 << shift) <= x)
                {
                    shift++;
                }
                return shift - 1;
            }
        }
    }
}
