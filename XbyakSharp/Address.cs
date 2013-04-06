using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XbyakSharp
{
    public class Address : CodeArray, IOperand
    {
        public Address(int sizeBit, bool isOnlyDisp, ulong disp, bool is32Bit, bool is64BitDisp = false)
            : base(6)
        {
            #region Operand ctor

            IDX = 0;
            Kind = Operand.KindType.MEM;
            Bit = sizeBit;

            #endregion Operand ctor

            Disp = disp;
            IsOnlyDisp = isOnlyDisp;
            Is32Bit = is32Bit;
            Is64BitDisp = is64BitDisp;
            Rex = 0;
        }

        public ulong Disp { get; private set; }

        public byte Rex { get; set; }

        public int IDX { get; private set; }

        public Operand.KindType Kind { get; private set; }

        public int Bit { get; private set; }

        public int Ext8Bit { get; private set; }

        public bool Is32Bit { get; private set; }

        public bool IsOnlyDisp { get; private set; }

        public bool Is64BitDisp { get; private set; }

        public Reg ToReg()
        {
            throw new NotImplementedException();
        }
    }

    public class AddressFrame
    {
        public AddressFrame(int bit)
        {
            Bit = bit;
        }

        public int Bit { get; private set; }

        public Address this[IntPtr disp]
        {
            get
            {
                ulong addr = disp.ToUInt64();
                if (Environment.Is64BitProcess)
                {
                    if (addr > uint.MaxValue)
                    {
                        throw new ArgumentException("offset is too big", "disp");
                    }
                }
                Reg32e r = new Reg32e(new Reg(), new Reg(), 0, (uint)addr);
                return this[r];
            }
        }

        public Address this[Reg32e rIn]
        {
            get
            {
                Reg32e r = rIn.Optimize();
                Address frame = new Address(Bit, r.IsNone() && r.Index.IsNone(), r.Disp, r.IsBit(32) || r.Index.IsBit(32));
                int mod = 0;
                if (r.IsNone() || ((r.IDX & 7) != (int)Operand.Code.EBP && r.Disp == 0))
                {
                    mod = 0;
                }
                else if (Util.IsInDisp8(r.Disp))
                {
                    mod = 1;
                }
                else
                {
                    mod = 2;
                }
                int based = r.IsNone() ? (int)Operand.Code.EBP : (r.IDX & 7);
                bool hasSIB = !r.Index.IsNone() || (r.IDX & 7) == (int)Operand.Code.ESP;
                if (Environment.Is64BitProcess)
                {
                    hasSIB |= r.IsNone() && r.Index.IsNone();
                }
                if (!hasSIB)
                {
                    frame.Db((mod << 6) | based);
                }
                else
                {
                    frame.Db((mod << 6) | (int)Operand.Code.ESP);
                    int index = r.Index.IsNone() ? (int)Operand.Code.ESP : (r.Index.IDX & 7);
                    int ss = r.Scale == 8 ? 3 : (r.Scale == 4 ? 2 : (r.Scale == 2 ? 1 : 0));
                    frame.Db((ss << 6) | (index << 3) | based);
                }
                if (mod == 1)
                {
                    frame.Db(r.Disp);
                }
                else if (mod == 2 || (mod == 0 && r.IsNone()))
                {
                    frame.Dd(r.Disp);
                }
                byte rex = (byte)(((r.IDX | r.Index.IDX) < 8) ? 0 : (0x40 | ((r.Index.IDX >> 3) << 1) | (r.IDX >> 3)) & 0xFF);
                frame.Rex = rex;

                return frame;
            }
        }

        #region for x64

        public Address this[RegRip addr]
        {
            get
            {
                Address frame = new Address(Bit, true, addr.Disp, false);
                frame.Db((int)BinToHex.B00000101);
                frame.Dd(addr.Disp);
                return frame;
            }
        }

        public Address this[ulong disp]
        {
            get
            {
                return new Address(64, true, disp, false, true);
            }
        }

        #endregion for x64
    }
}
