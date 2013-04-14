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
 * NativeMethods.cs
 * Author: mes
 * License: new BSD license http://opensource.org/licenses/BSD-3-Clause
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace XbyakSharp
{
    static class WindowsNativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        public static extern WindowsNativeExecutableMemory VirtualAlloc(IntPtr lpAddress, SizeT dwSize, VirtialAllocType flAllocationType, MemoryProtectionType flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        [SuppressUnmanagedCodeSecurity]
        public static extern bool VirtualFree(IntPtr lpAddress, SizeT dwSize, VirtualFreeType dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        public static extern void CopyMemory(IntPtr destination, IntPtr source, SizeT length);
    }

    [Flags]
    internal enum VirtialAllocType : uint
    {
        MemCommit = 0x1000,
        MemReserve = 0x2000,
        MemPhysical = 0x400000,
        MemReset = 0x80000,
        MemTopDown = 0x100000,
        MemWriteWatch = 0x200000,
        MemLargePages = 0x20000000,
    }

    [Flags]
    internal enum MemoryProtectionType : uint
    {
        PageNoaccess = 0x01,
        PageReadonly = 0x02,
        PageReadwrite = 0x04,
        PageWritecopy = 0x08,
        PageExecute = 0x10,
        PageExecuteRead = 0x20,
        PageExecuteReadwrite = 0x40,
        PageExecuteWritecopy = 0x80,
        PageGuard = 0x100,
        PageNocache = 0x200,
        PageWritecombine = 0x400,
    }

    internal enum VirtualFreeType : uint
    {
        MemDecommit = 0x4000,
        MemRelease = 0x8000,
    }

    public abstract class NativeExecutableMemory : SafeHandle
    {
        public static NativeExecutableMemory Allocate(SizeT size)
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32Windows:
                case PlatformID.Win32NT:
                    return WindowsNativeMethods.VirtualAlloc(IntPtr.Zero, size, VirtialAllocType.MemCommit, MemoryProtectionType.PageExecuteReadwrite);
            }
            return null;
        }

        internal NativeExecutableMemory()
            : base(IntPtr.Zero, true)
        {
        }

        public byte this[int index]
        {
            get { return Marshal.ReadByte(DangerousGetHandle(), index); }
            set { Marshal.WriteByte(DangerousGetHandle(), index, value); }
        }

        public abstract void Copy(NativeExecutableMemory dst, SizeT length);

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }
    }

    public class WindowsNativeExecutableMemory : NativeExecutableMemory
    {
        public override void Copy(NativeExecutableMemory dst, SizeT length)
        {
            WindowsNativeMethods.CopyMemory(dst.DangerousGetHandle(), DangerousGetHandle(), length);
        }

        protected override bool ReleaseHandle()
        {
            return WindowsNativeMethods.VirtualFree(handle, 0, VirtualFreeType.MemRelease);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct SizeT
    {
        public SizeT(int value)
            : this(unchecked((uint)value))
        {
        }

        public SizeT(long value)
            : this(unchecked((ulong)value))
        {
        }

        public SizeT(IntPtr value)
            : this(unchecked((ulong)value.ToInt64()))
        {
        }

        public SizeT(uint value)
        {
            num = new UIntPtr(value);
        }

        public SizeT(ulong value)
        {
            num = new UIntPtr(value);
        }

        public SizeT(UIntPtr value)
        {
            num = value;
        }

        private readonly UIntPtr num;

        public uint UInt32Value
        {
            get { return num.ToUInt32(); }
        }

        public ulong UInt64Value
        {
            get { return num.ToUInt64(); }
        }

        public UIntPtr UIntPtrValue
        {
            get { return num; }
        }

        public bool Equals(SizeT obj)
        {
            return num == obj.num;
        }

        public bool Equals(uint obj)
        {
            return UInt64Value == obj;
        }

        public bool Equals(ulong obj)
        {
            return UInt64Value == obj;
        }

        public bool Equals(UIntPtr obj)
        {
            return num == obj;
        }

        public override bool Equals(object obj)
        {
            if (obj is SizeT)
            {
                return Equals((SizeT)obj);
            }
            else if (obj is uint)
            {
                return Equals((uint)obj);
            }
            else if (obj is ulong)
            {
                return Equals((ulong)obj);
            }
            else if (obj is UIntPtr)
            {
                return Equals((UIntPtr)obj);
            }
            return false;
        }

        public override string ToString()
        {
            return num.ToString();
        }

        public override int GetHashCode()
        {
            return num.GetHashCode() ^ 0x7FFFFFFF;
        }

        public static implicit operator uint(SizeT value)
        {
            return value.UInt32Value;
        }

        public static implicit operator ulong(SizeT value)
        {
            return value.UInt64Value;
        }

        public static implicit operator UIntPtr(SizeT value)
        {
            return value.UIntPtrValue;
        }

        public static implicit operator SizeT(uint value)
        {
            return new SizeT(value);
        }

        public static implicit operator SizeT(ulong value)
        {
            return new SizeT(value);
        }

        public static implicit operator SizeT(UIntPtr value)
        {
            return new SizeT(value);
        }

        public static explicit operator int(SizeT value)
        {
            return unchecked((int)value.UInt64Value);
        }

        public static explicit operator long(SizeT value)
        {
            return unchecked((long)value.UInt64Value);
        }

        public static explicit operator IntPtr(SizeT value)
        {
            return new IntPtr(value);
        }

        public static explicit operator SizeT(int value)
        {
            return new SizeT(value);
        }

        public static explicit operator SizeT(long value)
        {
            return new SizeT(value);
        }

        public static explicit operator SizeT(IntPtr value)
        {
            return new SizeT(value);
        }

        public static SizeT operator +(SizeT a, SizeT b)
        {
            return new SizeT(a.UInt64Value + b.UInt64Value);
        }

        public static SizeT operator +(SizeT a, uint b)
        {
            return new SizeT(a.UInt64Value + b);
        }

        public static SizeT operator +(SizeT a, ulong b)
        {
            return new SizeT(a.UInt64Value + b);
        }

        public static SizeT operator +(SizeT a, UIntPtr b)
        {
            return new SizeT(a.UInt64Value + b.ToUInt64());
        }

        public static SizeT operator +(SizeT a, int b)
        {
            if (b > -1)
            {
                return a + (uint)b;
            }
            else
            {
                return a - unchecked((uint)-b);
            }
        }

        public static SizeT operator +(SizeT a, long b)
        {
            if (b > -1)
            {
                return a + (ulong)b;
            }
            else
            {
                return a - unchecked((ulong)-b);
            }
        }

        public static SizeT operator +(uint a, SizeT b)
        {
            return new SizeT(a + b.UInt64Value);
        }

        public static SizeT operator +(ulong a, SizeT b)
        {
            return new SizeT(a + b.UInt64Value);
        }

        public static SizeT operator +(UIntPtr a, SizeT b)
        {
            return new SizeT(a.ToUInt64() + b.UInt64Value);
        }

        public static SizeT operator +(int a, SizeT b)
        {
            if (a > -1)
            {
                return b + (uint)a;
            }
            else
            {
                return b - unchecked((uint)-a);
            }
        }

        public static SizeT operator +(long a, SizeT b)
        {
            if (a > -1)
            {
                return b + (ulong)a;
            }
            else
            {
                return b - unchecked((ulong)-a);
            }
        }

        public static SizeT operator -(SizeT a, SizeT b)
        {
            return new SizeT(a.UInt64Value - b.UInt64Value);
        }

        public static SizeT operator -(SizeT a, uint b)
        {
            return new SizeT(a.UInt64Value - b);
        }

        public static SizeT operator -(SizeT a, ulong b)
        {
            return new SizeT(a.UInt64Value - b);
        }

        public static SizeT operator -(SizeT a, UIntPtr b)
        {
            return new SizeT(a.UInt64Value - b.ToUInt64());
        }

        public static SizeT operator -(SizeT a, int b)
        {
            if (b > -1)
            {
                return a - (uint)b;
            }
            else
            {
                return a + -b;
            }
        }

        public static SizeT operator -(SizeT a, long b)
        {
            if (b > -1)
            {
                return a - (uint)b;
            }
            else
            {
                return a + -b;
            }
        }

        public static SizeT operator -(UIntPtr a, SizeT b)
        {
            return new SizeT(a.ToUInt64() - b.UInt64Value);
        }

        public static SizeT operator -(int a, SizeT b)
        {
            if (a > -1)
            {
                return b - (uint)a;
            }
            else
            {
                return b + -a;
            }
        }

        public static SizeT operator -(long a, SizeT b)
        {
            if (a > -1)
            {
                return b - (uint)a;
            }
            else
            {
                return b + -a;
            }
        }

        public static SizeT operator -(uint a, SizeT b)
        {
            return new SizeT(a - b.UInt64Value);
        }

        public static SizeT operator -(ulong a, SizeT b)
        {
            return new SizeT(a - b.UInt64Value);
        }

        public static SizeT operator *(SizeT a, SizeT b)
        {
            return new SizeT(a.UInt64Value * b.UInt64Value);
        }

        public static SizeT operator *(SizeT a, uint b)
        {
            return new SizeT(a.UInt64Value * b);
        }

        public static SizeT operator *(SizeT a, ulong b)
        {
            return new SizeT(a.UInt64Value * b);
        }

        public static SizeT operator *(SizeT a, UIntPtr b)
        {
            return new SizeT(a.UInt64Value * b.ToUInt64());
        }

        public static SizeT operator *(uint a, SizeT b)
        {
            return new SizeT(a * b.UInt64Value);
        }

        public static SizeT operator *(ulong a, SizeT b)
        {
            return new SizeT(a * b.UInt64Value);
        }

        public static SizeT operator *(UIntPtr a, SizeT b)
        {
            return new SizeT(a.ToUInt64() * b.UInt64Value);
        }

        public static SizeT operator /(SizeT a, SizeT b)
        {
            return new SizeT(a.UInt64Value / b.UInt64Value);
        }

        public static SizeT operator /(SizeT a, uint b)
        {
            return new SizeT(a.UInt64Value / b);
        }

        public static SizeT operator /(SizeT a, ulong b)
        {
            return new SizeT(a.UInt64Value / b);
        }

        public static SizeT operator /(SizeT a, UIntPtr b)
        {
            return new SizeT(a.UInt64Value / b.ToUInt64());
        }

        public static SizeT operator /(uint a, SizeT b)
        {
            return new SizeT(a / b.UInt64Value);
        }

        public static SizeT operator /(ulong a, SizeT b)
        {
            return new SizeT(a / b.UInt64Value);
        }

        public static SizeT operator /(UIntPtr a, SizeT b)
        {
            return new SizeT(a.ToUInt64() / b.UInt64Value);
        }

        public static SizeT operator %(SizeT a, SizeT b)
        {
            return new SizeT(a.UInt64Value % b.UInt64Value);
        }

        public static SizeT operator %(SizeT a, uint b)
        {
            return new SizeT(a.UInt64Value % b);
        }

        public static SizeT operator %(SizeT a, ulong b)
        {
            return new SizeT(a.UInt64Value % b);
        }

        public static SizeT operator %(SizeT a, UIntPtr b)
        {
            return new SizeT(a.UInt64Value % b.ToUInt64());
        }

        public static SizeT operator %(uint a, SizeT b)
        {
            return new SizeT(a % b.UInt64Value);
        }

        public static SizeT operator %(ulong a, SizeT b)
        {
            return new SizeT(a % b.UInt64Value);
        }

        public static SizeT operator %(UIntPtr a, SizeT b)
        {
            return new SizeT(a.ToUInt64() % b.UInt64Value);
        }

        public static SizeT operator >>(SizeT a, int b)
        {
            return new SizeT(a.UInt64Value >> b);
        }

        public static SizeT operator <<(SizeT a, int b)
        {
            return new SizeT(a.UInt64Value << b);
        }

        public static bool operator ==(SizeT a, SizeT b)
        {
            return a.Equals(b);
        }

        public static bool operator ==(SizeT a, uint b)
        {
            return a.Equals(b);
        }

        public static bool operator ==(SizeT a, ulong b)
        {
            return a.Equals(b);
        }

        public static bool operator ==(SizeT a, UIntPtr b)
        {
            return a.Equals(b);
        }

        public static bool operator ==(uint a, SizeT b)
        {
            return b.Equals(a);
        }

        public static bool operator ==(ulong a, SizeT b)
        {
            return b.Equals(a);
        }

        public static bool operator ==(UIntPtr a, SizeT b)
        {
            return b.Equals(a);
        }

        public static bool operator !=(SizeT a, SizeT b)
        {
            return !a.Equals(b);
        }

        public static bool operator !=(SizeT a, uint b)
        {
            return !a.Equals(b);
        }

        public static bool operator !=(SizeT a, ulong b)
        {
            return !a.Equals(b);
        }

        public static bool operator !=(SizeT a, UIntPtr b)
        {
            return !a.Equals(b);
        }

        public static bool operator !=(uint a, SizeT b)
        {
            return !b.Equals(a);
        }

        public static bool operator !=(ulong a, SizeT b)
        {
            return !b.Equals(a);
        }

        public static bool operator !=(UIntPtr a, SizeT b)
        {
            return !b.Equals(a);
        }
    }
}
