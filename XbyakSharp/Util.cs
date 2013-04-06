using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XbyakSharp
{
    static class Util
    {
        public const int AlignPageSize = 4096;

        private static readonly string[] ErrorMessage = new string[]
            {
                "none",
                "bad addressing",
                "code is too big",
                "bad scale",
                "esp can't be index",
                "bad combination",
                "bad size of register",
                "imm is too big",
                "bad align",
                "label is redefined",
                "label is too far",
                "label is not found",
                "code is not copyable",
                "bad parameter",
                "can't protect",
                "can't use 64bit disp(use (void*))",
                "offset is too big",
                "MEM size is not specified",
                "bad mem size",
                "bad st combination",
                "over local label",
                "under local label",
                "can't alloc",
                "T_SHORT is not supported in AutoGrow",
                "bad protect mode",
                "internal error",
            };

        public static string ConvertErrorToString(Error error)
        {
            return ErrorMessage[(int)error];
        }

        public static bool IsInDisp8(uint x)
        {
            return 0xFFFFFF80 <= x || x <= 0x7F;
        }

        public static bool IsInInt32(ulong x)
        {
            return ~0x7fffffffUL <= x || x <= 0x7FFFFFFFU;
        }

        public static uint VerifyInInt32(ulong x)
        {
            if (Environment.Is64BitProcess)
            {
                if (!IsInInt32(x))
                {
                    throw new ErrorException(Error.ErrOffsetIsTooBig);
                }
            }
            return unchecked((uint)x);
        }

        public static void Swap<T>(ref T t1, ref T t2)
        {
            T tmp = t1;
            t1 = t2;
            t2 = tmp;
        }

        public static ulong ToUInt64(this IntPtr ptr)
        {
            return (ulong)ptr;
        }
    }
}
