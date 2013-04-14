/*!
 * --- original library license
 * 
	@file xbyak.h
	@brief Xbyak ; JIT assembler for x86(IA32)/x64 by C++
	@author herumi
	@url https://github.com/herumi/xbyak, http://homepage1.nifty.com/herumi/soft/xbyak_e.html
	@note modified new BSD license
	http://opensource.org/licenses/BSD-3-Clause
*/

/*
 * CodeGenerator.cs
 * Author: mes
 * License: new BSD license http://opensource.org/licenses/BSD-3-Clause
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace XbyakSharp
{
    public partial class CodeGenerator : CodeArray
    {
        public enum LabelType
        {
            Short,
            Near,
            Auto
        }

        [Flags]
        public enum AVXType : uint
        {
            PpNone = 0x01,
            Pp_66 = 0x02,
            Pp_F3 = 0x04,
            Pp_F2 = 0x08,
            MmReserved = 0x10,
            Mm_0F = 0x20,
            Mm_0F38 = 0x40,
            Mm_0F3A = 0x80
        }

        private delegate bool IsValidOperand(IOperand op1, IOperand op2);

        public const int DefaultMaxCodeSize = 4096;
        private const int None = 256;

        private static readonly uint i32e;
        private static readonly uint BIT;

        private static bool IsXMM_XMMorMEM(IOperand op1, IOperand op2)
        {
            return op1.IsXMM() && (op2.IsXMM() || op2.IsMEM());
        }

        private static bool IsXMMorMMX_MEM(IOperand op1, IOperand op2)
        {
            return (op1.IsMMX() && (op2.IsMMX() || op2.IsMEM())) || IsXMM_XMMorMEM(op1, op2);
        }

        private static bool IsXMM_MMXorMEM(IOperand op1, IOperand op2)
        {
            return op1.IsXMM() && (op2.IsMMX() || op2.IsMEM());
        }

        private static bool IsMMX_XMMorMEM(IOperand op1, IOperand op2)
        {
            return op1.IsMMX() && (op2.IsXMM() || op2.IsMEM());
        }

        private static bool IsXMM_REG32orMEM(IOperand op1, IOperand op2)
        {
            return op1.IsXMM() && (op2.IsREG(i32e) || op2.IsMEM());
        }

        private static bool IsREG32_XMMorMEM(IOperand op1, IOperand op2)
        {
            return op1.IsREG(i32e) && (op2.IsXMM() || op2.IsMEM());
        }

        static CodeGenerator()
        {
            if (Environment.Is64BitProcess)
            {
                i32e = 32 | 64;
                BIT = 64;
            }
            else
            {
                i32e = 32;
                BIT = 32;
            }
        }

        public CodeGenerator()
            : this(DefaultMaxCodeSize, null, false)
        {
        }

        public CodeGenerator(ulong maxSize)
            : this(maxSize, null, false)
        {
        }

        public CodeGenerator(ulong maxSize, NativeExecutableMemory userPtr)
            : this(maxSize, userPtr, false)
        {
        }

        public CodeGenerator(ulong maxSize, NativeExecutableMemory userPtr, bool useAutoGrow)
            : base(maxSize, userPtr, useAutoGrow)
        {
            InitializeRegisters();
            ptr = new AddressFrame(0);
            @byte = new AddressFrame(8);
            word = new AddressFrame(16);
            dword = new AddressFrame(32);
            qword = new AddressFrame(64);
            spl = new Reg8((int)Operand.Code.SPL, 1);
            bpl = new Reg8((int)Operand.Code.BPL, 1);
            sil = new Reg8((int)Operand.Code.SIL, 1);
            dil = new Reg8((int)Operand.Code.DIL, 1);
            rip = new RegRip();
            label.BaseCodeArray = this;
        }

        private Label label = new Label();

        private void Rex(IOperand op1)
        {
            Rex(op1, new Operand());
        }

        private void Rex(IOperand op1, IOperand op2)
        {
            byte rex = 0;
            if (op1.IsMEM())
            {
                Util.Swap(ref op1, ref op2);
            }
            if (op1.IsMEM())
            {
                throw new ArgumentException("bad combination");
            }
            if (op2.IsMEM())
            {
                Address addr = (Address)op2;
                if (BIT == 64 && addr.Is32Bit)
                {
                    Db(0x67);
                }
                rex = (byte)(addr.Rex | op1.ToReg().GetRex());
            }
            else
            {
                rex = (byte)op2.ToReg().GetRex(op1.ToReg());
            }
            if ((op1.IsBit(16) && !op2.IsBit(i32e)) || (op2.IsBit(16) && !op1.IsBit(i32e)))
            {
                Db(0x66);
            }
            if (rex != 0)
            {
                Db(rex);
            }
        }

        private void Vex(bool r, int idx, bool is256, int type, bool x = false, bool b = false, int w = 1)
        {
            uint pp = (type & (uint)AVXType.Pp_66) != 0 ? 1U : (type & (uint)AVXType.Pp_F3) != 0 ? 2U : (type & (uint)AVXType.Pp_F2) != 0 ? 3U : 0;
            uint vvvv = (uint)((((~idx) & 15) << 3) | (is256 ? 4U : 0) | pp);
            if (!b && !x && w == 0 && (type & (uint)AVXType.Mm_0F) != 0)
            {
                Db(0xC5);
                Db((int)((r ? 0 : 0x80) | vvvv));
            }
            else
            {
                uint mmmm = (type & (uint)AVXType.Mm_0F) != 0 ? 1U : (type & (uint)AVXType.Mm_0F38) != 0 ? 2U : (type & (uint)AVXType.Mm_0F3A) != 0 ? 3U : 0;
                Db(0xC4);
                Db((int)((r ? 0 : 0x80) | (x ? 0 : 0x40) | (b ? 0 : 0x20) | mmmm));
                Db((int)((w << 7) | vvvv));
            }
        }

        private bool IsInDisp16(uint x)
        {
            return 0xFFFF8000 <= x || x <= 0x7FFF;
        }

        private byte GetModRM(int mod, int r1, int r2)
        {
            return unchecked((byte)((mod << 6) | ((r1 & 7) << 3) | (r2 & 7)));
        }

        private void OpModR(Reg reg1, Reg reg2, int code0, int code1 = None, int code2 = None)
        {
            Rex(reg2, reg1);
            Db(code0 | (reg1.IsBit(8) ? 0 : 1));
            if (code1 != None)
            {
                Db(code1);
            }
            if (code2 != None)
            {
                Db(code2);
            }
            Db(GetModRM(3, reg1.IDX, reg2.IDX));
        }

        private void OpModM(Address addr, Reg reg, int code0, int code1 = None, int code2 = None)
        {
            if (addr.Is64BitDisp)
            {
                throw new ArgumentException("cant use 64bit disp", "addr");
            }
            Rex(addr, reg);
            Db(code0 | (reg.IsBit(8) ? 0 : 1));
            if (code1 != None)
            {
                Db(code1);
            }
            if (code2 != None)
            {
                Db(code2);
            }
            addr.UpdateRegField((byte)reg.IDX);
            Db(addr.CodeMemory, unchecked((int)addr.Size));
        }

        private void MakeJmp(uint disp, LabelType type, byte shortCode, byte longCode, byte longPref)
        {
            const uint shortJmpSize = 2;
            uint longHeaderSize = longPref != 0 ? 2U : 1U;
            uint longJmpSize = longHeaderSize + 4;
            if (type != LabelType.Near && Util.IsInDisp8(disp - shortJmpSize))
            {
                Db(shortCode);
                Db((disp - shortJmpSize));
            }
            else
            {
                if (type == LabelType.Short)
                {
                    throw new InvalidOperationException("label is too far");
                }
                if (longPref != 0)
                {
                    Db(longPref);
                }
                Db(longCode);
                Dd(disp - longJmpSize);
            }
        }

        private void OpJmp(string labelName, LabelType type, byte shortCode, byte longCode, byte longPref)
        {
            if (IsAutoGrow && Size + 16 >= MaxSize)
            {
                GrowMemory();
            }
            ulong offset = 0;
            if (label.GetOffset(out offset, labelName))
            {
                MakeJmp(Util.VerifyInInt32(offset - Size), type, shortCode, longCode, longPref);
            }
            else
            {
                JmpLabel jmp = new JmpLabel();
                if (type == LabelType.Near)
                {
                    jmp.JmpSize = 4;
                    if (longPref != 0)
                    {
                        Db(longPref);
                    }
                    Db(longCode);
                    Dd(0);
                }
                else
                {
                    jmp.JmpSize = 1;
                    Db(shortCode);
                    Db(0);
                }
                jmp.Mode = LabelMode.LasIs;
                jmp.EndOfJmp = Size;
                label.AddUndefinedLabel(labelName, jmp);
            }
        }

        private void OpJmpAbs(IntPtr addr, LabelType type, byte shortCode, byte longCode)
        {
            if (IsAutoGrow)
            {
                if (type != LabelType.Near)
                {
                    throw new InvalidOperationException("only LabelType.Near is supported in auto grow");
                }
                if (Size + 16 >= MaxSize)
                {
                    GrowMemory();
                }
                Db(longCode);
                Dd(0);
                Save((int)(Size - 4), addr.ToUInt64() - Size, 4, LabelMode.Labs);
            }
            else
            {
                MakeJmp(Util.VerifyInInt32(addr.ToUInt64() - GetCurrentPointer().ToUInt64()), type, shortCode, longCode, 0);
            }
        }

        private void OpGen(IOperand reg, IOperand op, int code, int pref, IsValidOperand isValid, int imm8 = None, int preCode = None)
        {
            if (isValid != null && !isValid(reg, op))
            {
                throw new ArgumentException("reg and op are bad combination");
            }
            if (pref != None)
            {
                Db(pref);
            }
            if (op.IsMEM())
            {
                OpModM((Address)op, reg.ToReg(), 0x0F, preCode, code);
            }
            else
            {
                OpModR(reg.ToReg(), op.ToReg(), 0x0F, preCode, code);
            }
            if (imm8 != None)
            {
                Db(imm8);
            }
        }

        private void OpMMX_IMM(Mmx mmx, int imm8, int code, int ext)
        {
            if (mmx.IsXMM())
            {
                Db(0x66);
            }
            OpModR(new Reg32(ext), mmx, 0x0F, code);
            Db(imm8);
        }

        private void OpMMX(Mmx mmx, IOperand op, int code, int pref = 0x66, int imm8 = None, int preCode = None)
        {
            OpGen(mmx, op, code, mmx.IsXMM() ? pref : None, IsXMMorMMX_MEM, imm8, preCode);
        }

        private void OpMovXMM(IOperand op1, IOperand op2, int code, int pref)
        {
            if (pref != None)
            {
                Db(pref);
            }
            if (op1.IsXMM() && op2.IsMEM())
            {
                OpModM((Address)op2, op1.ToReg(), 0x0F, code);
            }
            else if (op1.IsMEM() && op2.IsXMM())
            {
                OpModM((Address)op1, op2.ToReg(), 0x0F, code | 1);
            }
            else
            {
                throw new ArgumentException("op1 and op2 are bad combination");
            }
        }

        private void OpExt(IOperand op, Mmx mmx, int code, int imm, bool hasMMX2 = false)
        {
            if (hasMMX2 && op.IsREG(i32e))
            {
                if (mmx.IsXMM())
                {
                    Db(0x66);
                }
                OpModR(op.ToReg(), mmx, 0x0F, (int)BinToHex.B11000101);
                Db(imm);
            }
            else
            {
                OpGen(mmx, op, code, 0x66, IsXMM_REG32orMEM, imm, (int)BinToHex.B00111010);
            }
        }

        private void OpR_ModM(IOperand op, int bit, int ext, int code0, int code1 = None, int code2 = None, bool disableRex = false)
        {
            int opBit = op.Bit;
            if (disableRex && op.Bit == 64)
            {
                opBit = 32;
            }
            Reg tr = new Reg(ext, Operand.KindType.REG, opBit);
            if (op.IsREG((uint)bit))
            {
                OpModR(tr, op.ToReg().ChangeBit(opBit), code0, code1, code2);
            }
            else if (op.IsMEM())
            {
                OpModM((Address)op, tr, code0, code1, code2);
            }
            else
            {
                throw new ArgumentException("bad combination", "op");
            }
        }

        private void OpShift(IOperand op, int imm, int ext)
        {
            VerifyMemHasSize(op);
            OpR_ModM(op, 0, ext, ((int)BinToHex.B11000000 | ((imm == 1 ? 1 : 0) << 4)));
            if (imm != 1)
            {
                Db(imm);
            }
        }

        private void OpShift(IOperand op, Reg8 cl, int ext)
        {
            if (cl.IDX != (int)Operand.Code.CL)
            {
                throw new ArgumentException("op and cl are bad combination");
            }
            OpR_ModM(op, 0, ext, (int)BinToHex.B11010010);
        }

        private void OpModRM(IOperand op1, IOperand op2, bool condR, bool condM, int code0, int code1 = None, int code2 = None)
        {
            if (condR)
            {
                OpModR(op1.ToReg(), op2.ToReg(), code0, code1, code2);
            }
            else if (condM)
            {
                OpModM((Address)op2, op1.ToReg(), code0, code1, code2);
            }
            else
            {
                throw new ArgumentException("condR and condM are bad combination");
            }
        }

        private void OpShxd(IOperand op, Reg reg, byte imm, int code, Reg8 cl = null)
        {
            if (cl != null && cl.IDX != (int)Operand.Code.CL)
            {
                throw new ArgumentException("bad combination", "cl");
            }
            OpModRM(reg, op, op.IsREG(16 | i32e) && op.Bit == reg.Bit, op.IsMEM() && reg.IsREG(16 | i32e), 0x0F, code | (cl != null ? 1 : 0));
            if (cl == null)
            {
                Db(imm);
            }
        }

        private void OpRM_RM(IOperand op1, IOperand op2, int code)
        {
            if (op1.IsREG() && op2.IsMEM())
            {
                OpModM((Address)op2, op1.ToReg(), code | 2);
            }
            else
            {
                OpModRM(op2, op1, op1.IsREG() && op1.Kind == op2.Kind, op1.IsMEM() && op2.IsREG(), code);
            }
        }

        private void OpRM_I(IOperand op, uint imm, int code, int ext)
        {
            VerifyMemHasSize(op);
            uint immBit = Util.IsInDisp8(imm) ? 8U : IsInDisp16(imm) ? 16U : 32U;
            if (op.IsBit(8))
            {
                immBit = 8;
            }
            if (op.Bit < immBit)
            {
                throw new InvalidOperationException("imm is too big");
            }
            if (op.IsBit(32 | 64) && immBit == 16)
            {
                immBit = 32;
            }
            if (op.IsREG() && op.IDX == 0 && (op.Bit == immBit || (op.IsBit(64) && immBit == 32)))
            {
                Rex(op);
                Db(code | 4 | (immBit == 8 ? 0 : 1));
            }
            else
            {
                OpR_ModM(op, 0, ext, (int)BinToHex.B10000000 | (immBit < Math.Min(op.Bit, 32) ? 2 : 0));
            }
            Db(imm, (int)(immBit / 8));
        }

        private void OpIncDec(IOperand op, int code, int ext)
        {
            VerifyMemHasSize(op);
            if (!Environment.Is64BitProcess)
            {
                if (op.IsREG() && !op.IsBit(8))
                {
                    Rex(op);
                    Db(code | op.IDX);
                    return;
                }
            }

            code = (int)BinToHex.B11111110;
            Reg tr = new Reg(ext, Operand.KindType.REG, op.Bit);
            if (op.IsREG())
            {
                OpModR(tr, op.ToReg(), code);
            }
            else
            {
                OpModM((Address)op,tr, code);
            }
        }

        private void OpPushPop(IOperand op, int code, int ext, int alt)
        {
            if (op.IsREG())
            {
                if (op.IsBit(16))
                {
                    Db(0x66);
                }
                if (op.IDX >= 8)
                {
                    Db(0x41);
                }
                Db(alt | (op.IDX & 7));
            }
            else if (op.IsMEM())
            {
                OpModM((Address)op, new Reg(ext, Operand.KindType.REG, op.Bit), code);
            }
            else
            {
                throw new ArgumentException("bad combination", "op");
            }
        }

        private void OpMovxx(Reg reg, IOperand op, byte code)
        {
            if (op.IsBit(32))
            {
                throw new ArgumentException("bad combination", "op");
            }
            int w = op.IsBit(16) ? 1 : 0;
            bool cond = reg.IsREG() && (reg.Bit > op.Bit);
            OpModRM(reg, op, cond && op.IsREG(), cond && op.IsMEM(), 0x0F, code | w);
        }

        private void OpFpuMem(Address addr, byte m16, byte m32, byte m64, byte ext, byte m64Ext)
        {
            if (addr.Is64BitDisp)
            {
                throw new ArgumentException("cant use 64bit disp", "addr");
            }
            byte code = addr.IsBit(16) ? m16 : addr.IsBit(32) ? m32 : addr.IsBit(64) ? m64 : (byte)0;
            if (code == 0)
            {
                throw new ArgumentException("bad mem size");
            }
            if (m64Ext != 0 && addr.IsBit(64))
            {
                ext = m64Ext;
            }

            Rex(addr, st0);
            Db(code);
            addr.UpdateRegField(ext);
            Db(addr.CodeMemory, (int)addr.Size);
        }

        private void OpFpuFpu(Fpu reg1, Fpu reg2, uint code1, uint code2)
        {
            uint code = reg1.IDX == 0 ? code1 : reg2.IDX == 0 ? code2 : 0;
            if (code == 0)
            {
                throw new ArgumentException("reg1 and reg2 are bad combination");
            }
            Db(unchecked((byte)(code >> 8)));
            Db(unchecked((byte)(code | (reg1.IDX | reg2.IDX))));
        }

        private void OpFpu(Fpu reg, byte code1, byte code2)
        {
            Db(code1);
            Db(code2 | reg.IDX);
        }

        public void OpAVX_X_X_XM(Xmm x1, IOperand op1, IOperand op2, AVXType type, int code0, bool supportYMM, int w = -1)
        {
            Xmm x2 = null;
            IOperand op = null;
            if (op2.IsNone())
            {
                x2 = x1;
                op = op1;
            }
            else
            {
                if (!(op1.IsXMM() || (supportYMM && op1.IsYMM())))
                {
                    throw new ArgumentException("bad combination", "op1");
                }
                x2 = (Xmm)op1;
                op = op2;
            }
            if (!((x1.IsXMM() && x2.IsXMM()) || (supportYMM && x1.IsYMM() && x2.IsYMM())))
            {
                throw new ArgumentException("bad combination");
            }
            bool x = false;
            bool b = false;
            if (op.IsMEM())
            {
                Address addr = (Address)op;
                x = (addr.Rex & 2) != 0;
                b = (addr.Rex & 1) != 0;
                if (BIT == 64)
                {
                    if (addr.Is32Bit)
                    {
                        Db(0x67);
                    }
                    if (w == -1)
                    {
                        w = (addr.Rex & 4) != 0 ? 1 : 0;
                    }
                }
            }
            else
            {
                x = false;
                b = op.ToReg().IsExtIdx();
            }
            if (w == -1)
            {
                w = 0;
            }
            Vex(x1.IsExtIdx(), x2.IDX, x1.IsYMM(), (int)type, x, b, w);
            Db(code0);
            if (op.IsMEM())
            {
                Address addr = (Address)op;
                addr.UpdateRegField((byte)x1.IDX);
                Db(addr.CodeMemory, (int)addr.Size);
            }
            else
            {
                Db(GetModRM(3, x1.IDX, op.IDX));
            }
        }

        public void OpAVX_X_X_XMcvt(Xmm x1, IOperand op1, IOperand op2, bool cvt, Operand.KindType kind, AVXType type, int code0, bool supportYMM, int w = -1)
        {
            OpAVX_X_X_XM(x1, op1, cvt ? (kind == Operand.KindType.XMM ? new Xmm(op2.IDX) : new Ymm(op2.IDX)) : op2, type, code0, supportYMM, w);
        }

        public void OpAVX_X_XM_IMM(Xmm x, IOperand op, AVXType type, int code, bool supportYMM, int w = -1, int imm = None)
        {
            OpAVX_X_X_XM(x, x.IsXMM() ? xmm0 : ymm0, op, type, code, supportYMM, w);
            if (imm != None)
            {
                Db((byte)imm);
            }
        }

        public void Reset()
        {
            ResetSize();
            label.Reset();
            label.BaseCodeArray = this;
        }

        public bool HasUndefinedLabel()
        {
            return label.HasUndefinedLabel();
        }

        public void Ready()
        {
            if (HasUndefinedLabel())
            {
                throw new InvalidOperationException("label is not found");
            }
            CalcJmpAddress();
        }

        public void Align(int x = 16)
        {
            if (x == 1)
            {
                return;
            }
            if (x < 1 || (x & (x - 1)) != 0)
            {
                throw new ArgumentException("bad align", "x");
            }
            while ((GetCurrentPointer().ToInt64() % x) != 0)
            {
                nop();
            }
        }

        public T GetDelegate<T>() where T : class
        {
            return Marshal.GetDelegateForFunctionPointer(CodeMemory.DangerousGetHandle(), typeof(T)) as T;
        }

        private void VerifyMemHasSize(IOperand op)
        {
            if (op.IsMEM() && op.Bit == 0)
            {
                throw new ArgumentException("mem size is not specified", "op");
            }
        }
    }
}
