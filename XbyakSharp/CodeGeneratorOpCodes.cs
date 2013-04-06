using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XbyakSharp
{
    public partial class CodeGenerator
    {
        public void L(string labelName)
        {
            label.Define(labelName, Size, GetCurrentPointer());
        }

        public void InLocalLabel()
        {
            label.EnterLocal();
        }

        public void OutLocalLabel()
        {
            label.LeaveLocal();
        }

        public void jmp(string labelName, LabelType type = LabelType.Auto)
        {
            OpJmp(labelName, type, (byte)BinToHex.B11101011, (byte)BinToHex.B11101001, 0);
        }

        public void jmp(IntPtr addr, LabelType type = LabelType.Auto)
        {
            OpJmpAbs(addr, type, (byte)BinToHex.B11101011, (byte)BinToHex.B11101001);
        }

        public void jmp(IOperand op)
        {
            OpR_ModM(op, (int)BIT, 4, 0xFF, None, None, true);
        }

        public void call(IOperand op)
        {
            OpR_ModM(op, 16 | (int)i32e, 2, 0xFF, None, None, true);
        }

        public void call(string labelName)
        {
            OpJmp(labelName, LabelType.Near, 0, (byte)BinToHex.B11101000, 0);
        }

        public void call(IntPtr addr)
        {
            OpJmpAbs(addr, LabelType.Near, 0, (byte)BinToHex.B11101000);
        }

        public void test(IOperand op, Reg reg)
        {
            OpModRM(reg, op, op.IsREG() && (op.Kind == reg.Kind), op.IsMEM(), (int)BinToHex.B10000100);
        }

        public void test(IOperand op, uint imm)
        {
            VerifyMemHasSize(op);
            if (op.IsREG() && op.IDX == 0)
            {
                Rex(op);
                Db((int)BinToHex.B10101000 | (op.IsBit(8) ? 0 : 1));
            }
            else
            {
                OpR_ModM(op, 0, 0, (int)BinToHex.B11110110);
            }
            Db(imm, Math.Min(op.Bit / 8, 4));
        }

        public void ret(int imm = 0)
        {
            if (imm != 0)
            {
                Db((int)BinToHex.B11000010);
                Dw(unchecked((uint)imm));
            }
            else
            {
                Db((int)BinToHex.B11000011);
            }
        }

        public void imul(Reg reg, IOperand op)
        {
            OpModRM(reg, op, op.IsREG() && (reg.Kind == op.Kind), op.IsMEM(), 0x0F, (int)BinToHex.B10101111);
        }

        public void imul(Reg reg, IOperand op, int imm)
        {
            int s = Util.IsInDisp8(unchecked((uint)imm)) ? 1 : 0;
            OpModRM(reg, op, op.IsREG() && (reg.Kind == op.Kind), op.IsMEM(), (int)BinToHex.B01101001 | (s << 1));
            int size = s != 0 ? 1 : reg.IsREG(16) ? 2 : 4;
            Db(unchecked((uint)imm), size);
        }

        public void poo(IOperand op)
        {
            OpPushPop(op, (int)BinToHex.B10001111, 0, (int)BinToHex.B01011000);
        }

        public void push(IOperand op)
        {
            OpPushPop(op, (int)BinToHex.B11111111, 6, (int)BinToHex.B01010000);
        }

        public void push(AddressFrame af, uint imm)
        {
            if (af.Bit == 8 && Util.IsInDisp8(imm))
            {
                Db((int)BinToHex.B01101010);
                Db(imm);
            }
            else if (af.Bit == 16 && IsInDisp16(imm))
            {
                Db(0x66);
                Db((int)BinToHex.B01101000);
                Dw(imm);
            }
            else
            {
                Db((int)BinToHex.B01101000);
                Dd(imm);
            }
        }

        public void push(uint imm)
        {
            push(Util.IsInDisp8(imm) ? @byte : dword, imm);
        }

        public void bswap(Reg32e reg)
        {
            OpModR(new Reg32(1), reg, 0x0F);
        }

        public void mov(IOperand reg1, IOperand reg2)
        {
            Reg r = null;
            Address addr = null;
            byte code = 0;
            if (reg1.IsREG() && reg1.IDX == 0 && reg2.IsMEM())
            {
                r = reg1.ToReg();
                addr = (Address)reg2;
                code = (byte)BinToHex.B10100000;
            }
            else
            {
                if (reg1.IsMEM() && reg2.IsREG() && reg2.IDX == 0)
                {
                    r = reg2.ToReg();
                    addr = (Address)reg1;
                    code = (byte)BinToHex.B10100010;
                }
                if (Environment.Is64BitProcess)
                {
                    if (addr != null && addr.Is64BitDisp)
                    {
                        if (code != 0)
                        {
                            Rex(r);
                            Db(reg1.IsREG(8) ? 0xA0 : reg1.IsREG() ? 0xA1 : reg2.IsREG(8) ? 0xA2 : 0xA3);
                            Db(addr.Disp, 8);
                        }
                        else
                        {
                            throw new ArgumentException("reg1 and reg2 are bad combination");
                        }
                    }
                    else
                    {
                        OpRM_RM(reg1, reg2, (int)BinToHex.B10001000);
                    }
                }
                else
                {
                    if (code != 0 && addr.IsOnlyDisp)
                    {
                        Rex(r, addr);
                        Db(code | (r.IsBit(8) ? 0 : 1));
                        Dd(unchecked((uint)addr.Disp));
                    }
                    else
                    {
                        OpRM_RM(reg1, reg2, (int)BinToHex.B10001000);
                    }
                }
            }
        }

        public void mov(IOperand op, ulong imm, bool opti = true)
        {
            if (!Environment.Is64BitProcess && imm > uint.MaxValue)
            {
                throw new ArgumentException("imm is must be less than uint.MaxValue in 32bit process");
            }
            VerifyMemHasSize(op);
            if (op.IsREG())
            {
                Rex(op);
                int code = 0;
                int size = 0;
                bool notOpti = !(Environment.Is64BitProcess && (opti && op.IsBit(64) && Util.IsInInt32(imm)));
                if (!notOpti)
                {
                    Db((int)BinToHex.B11000111);
                    code = (int)BinToHex.B11000000;
                    size = 4;
                }
                else
                {
                    code = (int)BinToHex.B10110000 | ((op.IsBit(8) ? 0 : 1) << 3);
                    size = op.Bit / 8;
                }
                Db(code | (op.IDX & 7));
                Db(imm, size);
            }
            else if (op.IsMEM())
            {
                OpModM((Address)op, new Reg(0, Operand.KindType.REG, op.Bit), (int)BinToHex.B11000110);
                int size = Math.Min(op.Bit / 8, 4);
                Db(unchecked((uint)imm), size);
            }
            else
            {
                throw new ArgumentException("op and imm are bad combination");
            }
        }

        public void mov(Reg32e reg, string labelName)
        {
            if ((Environment.Is64BitProcess && reg.GetType() != typeof (Reg64)) || (!Environment.Is64BitProcess && reg.GetType() != typeof (Reg32)))
            {
                throw new ArgumentException("invalid register type", "reg");
            }
            if (labelName == null)
            {
                mov(reg, 0, true);
                return;
            }

            int jmpSize = IntPtr.Size;
            ulong dummyAddr = 0x12345678;
            if (Environment.Is64BitProcess)
            {
                dummyAddr = 0x1122334455667788UL;
            }
            if (IsAutoGrow && Size + 16 >= MaxSize)
            {
                GrowMemory();
            }
            ulong offset = 0;
            if (label.GetOffset(out offset, labelName))
            {
                if (IsAutoGrow)
                {
                    mov(reg, dummyAddr);
                    Save((int)Size - jmpSize, offset, jmpSize, LabelMode.LaddTop);
                }
                else
                {
                    mov(reg, Top + offset, false);
                }
                return;
            }
            mov(reg, dummyAddr);
            JmpLabel jmp = new JmpLabel()
                {
                    EndOfJmp = Size,
                    JmpSize = jmpSize,
                    Mode = IsAutoGrow ? LabelMode.LaddTop : LabelMode.Labs
                };
            label.AddUndefinedLabel(labelName, jmp);
        }

        public void cmpxchg8b(Address addr)
        {
            OpModM(addr, new Reg32(1), 0x0F, (int)BinToHex.B11000111);
        }

        public void cmpxchg16b(Address addr)
        {
            OpModM(addr, new Reg64(1), 0x0F, (int)BinToHex.B11000111);
        }

        public void xadd(IOperand op, Reg reg)
        {
            OpModRM(reg, op, op.IsREG() && reg.IsREG() && op.Bit == reg.Bit, op.IsMEM(), 0x0F, (int)BinToHex.B11000000 | (reg.IsBit(8) ? 0 : 1));
        }

        public void xchg(IOperand op1, IOperand op2)
        {
            if (op1.IsMEM() || (op2.IsREG(16 | i32e) && op2.IDX == 0))
            {
                Util.Swap(ref op1, ref op2);
            }
            if (op1.IsMEM())
            {
                throw new ArgumentException("op1 and op2 are bad combination");
            }
            if (op2.IsREG() && (op1.IsREG(16 | i32e) && op1.IDX == 0) && (!Environment.Is64BitProcess || (op2.IDX != 0 || !op1.IsREG(32))))
            {
                Rex(op2, op1);
                Db(0x90 | (op2.IDX & 7));
                return;
            }
            OpModRM(op1, op2, op1.IsREG() && op2.IsREG() && op1.Bit == op2.Bit, op2.IsMEM(), (int)BinToHex.B10000110 | (op1.IsBit(8) ? 0 : 1));
        }

        public void movd(Address addr, Mmx mmx)
        {
            if (mmx.IsXMM())
            {
                Db(0x66);
            }
            OpModM(addr, mmx, 0x0F, (int)BinToHex.B01111110);
        }

        public void movd(Reg32 reg, Mmx mmx)
        {
            if (mmx.IsXMM())
            {
                Db(0x66);
            }
            OpModR(mmx, reg, 0x0F, (int)BinToHex.B01111110);
        }

        public void movd(Mmx mmx, Address addr)
        {
            if (mmx.IsXMM())
            {
                Db(0x66);
            }
            OpModM(addr, mmx, 0x0F, (int)BinToHex.B01101110);
        }

        public void movd(Mmx mmx, Reg32 reg)
        {
            if (mmx.IsXMM())
            {
                Db(0x66);
            }
            OpModR(mmx, reg, 0x0F, (int)BinToHex.B01101110);
        }

        public void movq2dq(Xmm xmm, Mmx mmx)
        {
            Db(0xF3);
            OpModR(xmm, mmx, 0x0F, (int)BinToHex.B11010110);
        }

        public void movdq2q(Mmx mmx, Xmm xmm)
        {
            Db(0xF2);
            OpModR(mmx, xmm, 0x0F, (int)BinToHex.B11010110);
        }

        public void movq(Mmx mmx, IOperand op)
        {
            if (mmx.IsXMM())
            {
                Db(0xF3);
            }
            OpModRM(mmx, op, mmx.Kind == op.Kind, op.IsMEM(), 0x0F, (int)(mmx.IsXMM() ? BinToHex.B01111110 : BinToHex.B01101111));
        }

        public void movq(Address addr, Mmx mmx)
        {
            if (mmx.IsXMM())
            {
                Db(0x66);
            }
            OpModM(addr, mmx, 0x0F, (int)(mmx.IsXMM() ? BinToHex.B11010110 : BinToHex.B01111111));
        }

        public void pextrw(IOperand op, Mmx xmm, byte imm)
        {
            OpExt(op, xmm, 0x15, imm, true);
        }

        public void pextrb(IOperand op, Xmm xmm, byte imm)
        {
            OpExt(op, xmm, 0x14, imm);
        }

        public void pextrd(IOperand op, Xmm xmm, byte imm)
        {
            OpExt(op, xmm, 0x16, imm);
        }

        public void extractps(IOperand op, Xmm xmm, byte imm)
        {
            OpExt(op, xmm, 0x17, imm);
        }

        public void pinsrw(Mmx mmx, IOperand op, int imm)
        {
            if (!op.IsREG(32) && !op.IsMEM())
            {
                throw new ArgumentException("bad combination", "op");
            }
            OpGen(mmx, op, (int)BinToHex.B11000100, mmx.IsXMM() ? 0x66 : None, null, imm);
        }

        public void insertps(Xmm xmm, IOperand op, byte imm)
        {
            OpGen(xmm, op, 0x21, 0x66, IsXMM_XMMorMEM, imm, (int)BinToHex.B00111010);
        }

        public void pinsrb(Xmm xmm, IOperand op, byte imm)
        {
            OpGen(xmm, op, 0x20, 0x66, IsXMM_REG32orMEM, imm, (int)BinToHex.B00111010);
        }

        public void pinsrd(Xmm xmm, IOperand op, byte imm)
        {
            OpGen(xmm, op, 0x22, 0x66, IsXMM_REG32orMEM, imm, (int)BinToHex.B00111010);
        }

        public void pmovmskb(Reg32e reg, Mmx mmx)
        {
            if (mmx.IsXMM())
            {
                Db(0x66);
            }
            OpModR(reg, mmx, 0x0F, (int)BinToHex.B11010111);
        }

        public void maskmovq(Mmx reg1, Mmx reg2)
        {
            if (!reg1.IsMMX() || !reg2.IsMMX())
            {
                throw new ArgumentException("reg1 and reg2 are bad combination");
            }
            OpModR(reg1, reg2, 0x0F, (int)BinToHex.B11110111);
        }

        public void lea(Reg32e reg, Address addr)
        {
            OpModM(addr, reg, (int)BinToHex.B10001101);
        }

        public void movmskps(Reg32e reg, Xmm xmm)
        {
            OpModR(reg, xmm, 0x0F, (int)BinToHex.B01010000);
        }

        public void movmskpd(Reg32e reg, Xmm xmm)
        {
            Db(0x66);
            movmskps(reg, xmm);
        }

        public void movntps(Address addr, Xmm xmm)
        {
            OpModM(addr, new Mmx(xmm.IDX), 0x0F, (int)BinToHex.B00101011);
        }

        public void movntdqa(Xmm xmm, Address addr)
        {
            Db(0x66);
            OpModM(addr, xmm, 0x0F, 0x38, 0x2A);
        }

        public void lddqu(Xmm xmm, Address addr)
        {
            Db(0xF2);
            OpModM(addr, xmm, 0x0F, (int)BinToHex.B11110000);
        }

        public void movnti(Address addr, Reg32e reg)
        {
            OpModM(addr, reg, 0x0F, (int)BinToHex.B11000011);
        }

        public void movntq(Address addr, Mmx mmx)
        {
            if (!mmx.IsMMX())
            {
                throw new ArgumentException("bad combination", "mmx");
            }
            OpModM(addr, mmx, 0x0F, (int)BinToHex.B11100111);
        }

        public void popcnt(Reg reg, IOperand op)
        {
            bool is16Bit = reg.IsREG(16) && (op.IsREG(16) || op.IsMEM());
            if (!is16Bit && !(reg.IsREG(i32e) && (op.IsREG(i32e) || op.IsMEM())))
            {
                throw new ArgumentException("reg and op are bad combination");
            }
            if (is16Bit)
            {
                Db(0x66);
            }
            Db(0xF3);
            OpModRM(reg.ChangeBit(i32e == 32 ? 32 : reg.Bit), op, op.IsREG(), true, 0x0F, 0xB8);
        }

        public void crc32(Reg32e reg, IOperand op)
        {
            if (reg.IsBit(32) && op.IsBit(16))
            {
                Db(0x66);
            }
            Db(0xF2);
            OpModRM(reg, op, op.IsREG(), op.IsMEM(), 0x0F, 0x38, 0xF0 | (op.IsBit(8) ? 0 : 1));
        }


        public void setz(IOperand op)
        {
            OpR_ModM(op, 8, 0, 0x0F, (int)BinToHex.B10010000 | 4);
        }


        public void jnz(string labelName, LabelType type = LabelType.Auto)
        {
            OpJmp(labelName, type, 0x75, 0x85, 0x0F);
        }


        public void jcxz(string label)
        {
            if (Environment.Is64BitProcess)
            {
                throw new InvalidOperationException("cant use in x64 process");
            }
            Db(0x67);
            OpJmp(label, LabelType.Short, 0xe3, 0, 0);
        }

        public void jecxz(string label)
        {
            if (Environment.Is64BitProcess)
            {
                Db(0x67);   
            }
            OpJmp(label, LabelType.Short, 0xe3, 0, 0);
        }

        public void jrcxz(string label)
        {
            if (!Environment.Is64BitProcess)
            {
                throw new InvalidOperationException("cant use in x86 process");
            }
            OpJmp(label, LabelType.Short, 0xe3, 0, 0);
        }


        public void ud2()
        {
            Db(0x0F);
            Db(0x0B);
        }


        public void adc(IOperand op1, IOperand op2)
        {
            OpRM_RM(op1, op2, 0x10);
        }

        public void adc(IOperand op, uint imm)
        {
            OpRM_I(op, imm, 0x10, 2);
        }

        public void add(IOperand op1, IOperand op2)
        {
            OpRM_RM(op1, op2, 0x00);
        }

        public void add(IOperand op, uint imm)
        {
            OpRM_I(op, imm, 0x00, 0);
        }

        public void and(IOperand op1, IOperand op2)
        {
            OpRM_RM(op1, op2, 0x20);
        }

        public void and(IOperand op, uint imm)
        {
            OpRM_I(op, imm, 0x20, 4);
        }

        public void cmp(IOperand op1, IOperand op2)
        {
            OpRM_RM(op1, op2, 0x38);
        }

        public void cmp(IOperand op, uint imm)
        {
            OpRM_I(op, imm, 0x38, 7);
        }

        public void or(IOperand op1, IOperand op2)
        {
            OpRM_RM(op1, op2, 0x08);
        }

        public void or(IOperand op, uint imm)
        {
            OpRM_I(op, imm, 0x08, 1);
        }

        public void sbb(IOperand op1, IOperand op2)
        {
            OpRM_RM(op1, op2, 0x18);
        }

        public void sbb(IOperand op, uint imm)
        {
            OpRM_I(op, imm, 0x18, 3);
        }

        public void sub(IOperand op1, IOperand op2)
        {
            OpRM_RM(op1, op2, 0x28);
        }

        public void sub(IOperand op, uint imm)
        {
            OpRM_I(op, imm, 0x28, 5);
        }

        public void xor(IOperand op1, IOperand op2)
        {
            OpRM_RM(op1, op2, 0x30);
        }

        public void xor(IOperand op, uint imm)
        {
            OpRM_I(op, imm, 0x30, 6);
        }



        public void nop()
        {
            Db(0x90);
        }

        #region for x64

        public void movq(Reg64 reg, Mmx mmx)
        {
            if (mmx.IsXMM())
            {
                Db(0x66);
            }
            OpModR(mmx, reg, 0x0F, (int)BinToHex.B01111110);
        }

        public void movq(Mmx mmx, Reg64 reg)
        {
            if (mmx.IsXMM())
            {
                Db(0x66);
            }
            OpModR(mmx, reg, 0x0F, (int)BinToHex.B01101110);
        }

        public void pextrq(IOperand op, Xmm xmm, byte imm)
        {
            if (!op.IsREG(64) && !op.IsMEM())
            {
                throw new ArgumentException("bad combination", "op");
            }
            OpGen(new Reg64(xmm.IDX), op, 0x16, 0x66, null, imm, (int)BinToHex.B00111010);
        }

        public void pinsrq(Xmm xmm, IOperand op, byte imm)
        {
            if (!op.IsREG(64) && !op.IsMEM())
            {
                throw new ArgumentException("bad combination", "op");
            }
            OpGen(new Reg64(xmm.IDX), op, 0x22, 0x66, null, imm, (int)BinToHex.B00111010);
        }

        public void movsxd(Reg64 reg, IOperand op)
        {
            if (!op.IsBit(32))
            {
                throw new ArgumentException("bad combination", "op");
            }
            OpModRM(reg, op, op.IsREG(), op.IsMEM(), 0x63);
        }

        #endregion for x64
    }
}
