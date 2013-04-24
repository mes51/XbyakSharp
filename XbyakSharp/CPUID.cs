using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace XbyakSharp
{
    public class CPUID : CodeGenerator
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void GetCPUIDDelegate(int level, int[] result);

        private static CPUID instance = null;
        private static GetCPUIDDelegate func = null;

        static CPUID()
        {
            instance = new CPUID();
            func = instance.GetDelegate<GetCPUIDDelegate>();
        }

        private CPUID()
        {
            if (Environment.Is64BitProcess)
            {
                mov(r9, rdx);
                mov(r10, rbx);
                mov(rax, rcx);
                cpuid();
                mov(dword[r9], eax);
                mov(dword[r9 + 4], ebx);
                mov(dword[r9 + 8], ecx);
                mov(dword[r9 + 12], edx);
                mov(rbx, r10);
            }
            else
            {
                push(ebx);
                push(esi);
                mov(eax, dword[esp + 8 + 4]);
                cpuid();
                mov(esi, dword[esp + 8 + 8]);
                mov(dword[esi], eax);
                mov(dword[esi + 4], ebx);
                mov(dword[esi + 8], ecx);
                mov(dword[esi + 12], edx);
                pop(esi);
                pop(ebx);
            }
            ret();
        }

        public static int[] Exec(int level)
        {
            int[] result = new int[4];
            func(level, result);

            return result;
        }
    }
}
