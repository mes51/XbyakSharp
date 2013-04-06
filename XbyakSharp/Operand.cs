using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XbyakSharp
{
    public interface IOperand
    {
        int IDX { get; }

        Operand.KindType Kind { get; }

        int Bit { get; }

        int Ext8Bit { get; }

        Reg ToReg();
    }

    public class Operand : IOperand
    {
        public Operand()
            : this(0, KindType.None, 0, 0)
        {
        }

        public Operand(int idx, KindType kind, int bit, int ext8Bit = 0)
        {
            if ((bit & (bit - 1)) != 0)
            {
                throw new ArgumentException("bit must be power of two", "bit");
            }
            IDX = (byte)idx;
            Kind = kind;
            Bit = (byte)bit;
            Ext8Bit = (byte)ext8Bit;
        }

        public int IDX { get; private set; }

        public KindType Kind { get; private set; }

        public int Bit { get; private set; }

        public int Ext8Bit { get; private set; }

        public enum KindType : byte
        {
            None = 0x00,
            MEM = 0x02,
            IMM = 0x04,
            REG = 0x08,
            MMX = 0x10,
            XMM = 0x20,
            FPU = 0x40,
            YMM = 0x80
        }

        public enum Code : uint
        {
            #region for x64
            RAX = 0,
            RCX = 1,
            RDX = 2,
            RBX = 3,
            RSP = 4,
            RBP = 5,
            RSI = 6,
            RDI = 7,
            R8 = 8,
            R9 = 9,
            R10 = 10,
            R11 = 11,
            R12 = 12,
            R13 = 13,
            R14 = 14,
            R15 = 15,
            R8D = 8,
            R9D = 9,
            R10D = 10,
            R11D = 11,
            R12D = 12,
            R13D = 13,
            R14D = 14,
            R15D = 15,
            R8W = 8,
            R9W = 9,
            R10W = 10,
            R11W = 11,
            R12W = 12,
            R13W = 13,
            R14W = 14,
            R15W = 15,
            R8B = 8,
            R9B = 9,
            R10B = 10,
            R11B = 11,
            R12B = 12,
            R13B = 13,
            R14B = 14,
            R15B = 15,
            SPL = 4,
            BPL = 5,
            SIL = 6,
            DIL = 7,
            #endregion for x64

            EAX = 0,
            ECX = 1,
            EDX = 2,
            EBX = 3,
            ESP = 4,
            EBP = 5,
            ESI = 6,
            EDI = 7,
            AX = 0,
            CX = 1,
            DX = 2,
            BX = 3,
            SP = 4,
            BP = 5,
            SI = 6,
            DI = 7,
            AL = 0,
            CL = 1,
            DL = 2,
            BL = 3,
            AH = 4,
            CH = 5,
            DH = 6,
            BH = 7
        }

        public virtual Reg ToReg()
        {
            return new Reg(IDX, Kind, Bit, Ext8Bit);
        }
    }

    public static class OperandMethods
    {

        public static bool IsNone(this IOperand op)
        {
            return op.Kind == Operand.KindType.None;
        }

        public static bool IsMMX(this IOperand op)
        {
            return op.Kind == Operand.KindType.MMX;
        }

        public static bool IsXMM(this IOperand op)
        {
            return op.Kind == Operand.KindType.XMM;
        }

        public static bool IsYMM(this IOperand op)
        {
            return op.Kind == Operand.KindType.YMM;
        }

        public static bool IsREG(this IOperand op, uint bit = 0)
        {
            return op.Kind == Operand.KindType.REG && (bit == 0 || (op.Bit & bit) != 0);
        }

        public static bool IsMEM(this IOperand op, uint bit = 0)
        {
            return op.Kind == Operand.KindType.MEM && (bit == 0 || (op.Bit & bit) != 0);
        }

        public static bool IsFPU(this IOperand op)
        {
            return op.Kind == Operand.KindType.FPU;
        }

        public static bool IsExt8Bit(this IOperand op)
        {
            return op.Ext8Bit != 0;
        }

        public static bool IsBit(this IOperand op, uint bit)
        {
            return (op.Bit & bit) != 0;
        }
    }
}
