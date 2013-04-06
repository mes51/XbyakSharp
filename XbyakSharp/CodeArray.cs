using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace XbyakSharp
{
    public class CodeArray : IDisposable
    {
        internal class AddrInfo
        {
            public AddrInfo(int codeOffset, ulong jmpAddr, int jmpSize, LabelMode mode)
            {
                CodeOffset = codeOffset;
                JmpAddr = jmpAddr;
                JmpSize = jmpSize;
                Mode = mode;
            }

            public int CodeOffset { get; private set; }

            public ulong JmpAddr { get; private set; }

            public int JmpSize { get; private set; }

            public LabelMode Mode { get; private set; }

            public ulong GetVal(ulong top)
            {
                ulong disp = Mode == LabelMode.LaddTop ? JmpAddr + top : (Mode == LabelMode.LasIs ? JmpAddr : JmpAddr - top);
                if (JmpSize == 4)
                {
                    disp = Util.VerifyInInt32(disp);
                }
                return disp;
            }
        }

        private const int MaxFixedBufSize = 8;

        internal enum MemoryType
        {
            FixedBuf, // use buf_(non alignment, non protect)
            UserBuf, // use userPtr(non alignment, non protect)
            AllocBuf, // use new(alignment, protect)
            AutoGrow // automatically move and grow memory if necessary
        }

        public static IntPtr GetAlignedAddress(IntPtr addr, uint alignedSize = 16)
        {
            ulong ptr = addr.ToUInt64();
            return new IntPtr(unchecked((long)((ptr + alignedSize - 1) & ~(alignedSize - 1))));
        }

        internal static bool IsAllocType(MemoryType type)
        {
            return type == MemoryType.AllocBuf || type == MemoryType.AutoGrow;
        }

        private LinkedList<AddrInfo> addrInfoList = new LinkedList<AddrInfo>();
        private MemoryType type;

        public CodeArray()
            : this(MaxFixedBufSize, null)
        {
        }

        public CodeArray(ulong maxSize)
            : this(maxSize, null)
        {
        }

        public CodeArray(ulong maxSize, NativeExecutableMemory userPtr, bool useAutoGrow = false)
        {
            type = GetMemoryType(maxSize, userPtr, useAutoGrow);
            MaxSize = maxSize;
            if (IsAllocType(type))
            {
                CodeMemory = NativeExecutableMemory.Allocate(maxSize);
            }
            else
            {
                if (type == MemoryType.UserBuf)
                {
                    CodeMemory = userPtr;
                }
                else
                {
                    // fixed buffer
                    CodeMemory = NativeExecutableMemory.Allocate(MaxFixedBufSize);
                }
            }
            if (maxSize > 0 && CodeMemory == null)
            {
                throw new ArgumentException("cant alloc", "userPtr");
            }
            CodeSize = 0;
        }

        public CodeArray(CodeArray rhs)
        {
            if (rhs.type != MemoryType.FixedBuf)
            {
                throw new ArgumentException("code isnt copyable", "rhs");
            }
            type = rhs.type;
            MaxSize = rhs.MaxSize;
            CodeSize = rhs.CodeSize;
            CodeMemory = NativeExecutableMemory.Allocate(MaxFixedBufSize);
            rhs.CodeMemory.Copy(CodeMemory, MaxFixedBufSize);
        }

        public NativeExecutableMemory CodeMemory { get; private set; }

        public ulong Size
        {
            get { return CodeSize; }
            set
            {
                if (MaxSize <= value)
                {
                    throw new ArgumentException("offset is too big"); 
                }
                CodeSize = value;
            }
        }

        public bool IsAutoGrow { get { return type == MemoryType.AutoGrow; } }

        protected ulong MaxSize { get; set; }

        protected ulong Top
        {
            get { return CodeMemory.DangerousGetHandle().ToUInt64(); }
        }

        protected ulong CodeSize { get; set; }

        public void ResetSize()
        {
            CodeSize = 0;
            addrInfoList.Clear();
        }

        public void Db(uint code)
        {
            Db(unchecked((int)code));
        }

        public void Db(int code)
        {
            if (CodeSize >= MaxSize)
            {
                if (type == MemoryType.AutoGrow)
                {
                    GrowMemory();
                }
                else
                {
                    throw new OutOfMemoryException("code is too big");
                }
            }
            CodeMemory[(int)CodeSize++] = unchecked((byte)code);
        }

        public void Db(NativeExecutableMemory code, int codeSize)
        {
            for (int i = 0; i < codeSize; i++)
            {
                CodeMemory[(int)CodeSize++] = code[i];
            }
        }

        public void Db(ulong code, int codeSize)
        {
            if (codeSize > 8)
            {
                throw new ArgumentException("code is too big", "codeSize");
            }
            for (int i = 0; i < codeSize; i++)
            {
                Db((byte)((code >> (i * 8)) & 0xFF));
            }
        }

        public void Dw(uint code)
        {
            Db(code, 2);
        }

        public void Dd(uint code)
        {
            Db(code, 4);
        }

        public IntPtr GetCurrentPointer()
        {
            return new IntPtr(CodeMemory.DangerousGetHandle().ToInt64() + (long)CodeSize);
        }

        public void Rewrite(int offset, ulong disp, int size)
        {
            if ((ulong)offset >= MaxSize)
            {
                throw new ArgumentException("offset is too big", "offset");
            }
            if (size != 1 && size != 2 && size != 4 && size != 8)
            {
                throw new ArgumentException("size is one of 1, 2, 4 and 8", "size");
            }
            for (int i = 0; i < size; i++)
            {
                CodeMemory[offset + i] = (byte)((disp >> (i * 8)) & 0xFF);
            }
        }

        public void Save(int offset, ulong val, int size, LabelMode mode)
        {
            addrInfoList.AddLast(new AddrInfo(offset, val, size, mode));
        }

        public void UpdateRegField(byte regIdx)
        {
            CodeMemory[0] = unchecked((byte)((CodeMemory[0] & (uint)BinToHex.B11000111) | ((regIdx << 3) & (uint)BinToHex.B00111000)));
        }

        protected void GrowMemory()
        {
            ulong newSize = MaxSize * 2;
            NativeExecutableMemory newMemory = NativeExecutableMemory.Allocate(newSize);
            CodeMemory.Copy(newMemory, CodeSize);
            CodeMemory.Dispose();
            CodeMemory = newMemory;
            MaxSize = newSize;
        }

        protected void CalcJmpAddress()
        {
            foreach (var addrInfo in addrInfoList)
            {
                ulong disp = addrInfo.GetVal(Top);
                Rewrite(addrInfo.CodeOffset, disp, addrInfo.JmpSize);
            }
        }

        MemoryType GetMemoryType(ulong maxSize, NativeExecutableMemory userPtr, bool useAutoGrow)
        {
            if (useAutoGrow)
            {
                return MemoryType.AutoGrow;
            }
            else if (userPtr != null)
            {
                return MemoryType.UserBuf;
            }
            else if (maxSize <= MaxFixedBufSize)
            {
                return MemoryType.FixedBuf;
            }
            return MemoryType.AllocBuf;
        }

        public override string ToString()
        {
            byte[] tmp = new byte[Size];
            Marshal.Copy(CodeMemory.DangerousGetHandle(), tmp, 0, tmp.Length);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < tmp.Length; i += 16)
            {
                sb.AppendLine(string.Join(" ", tmp.Skip(i).Take(16).Select(x => x.ToString("X2"))));
            }
            return sb.ToString();
        }

        public void Dispose()
        {
            if (type != MemoryType.UserBuf)
            {
                CodeMemory.Dispose();
            }
        }
    }
}
