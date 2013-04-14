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
 * CodeGeneratorRegisterFields.cs
 * Author: mes
 * License: new BSD license http://opensource.org/licenses/BSD-3-Clause
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace XbyakSharp
{
    public partial class CodeGenerator
    {
        public readonly Mmx mm0, mm1, mm2, mm3, mm4, mm5, mm6, mm7;
        public readonly Xmm xmm0, xmm1, xmm2, xmm3, xmm4, xmm5, xmm6, xmm7;
        public readonly Ymm ymm0, ymm1, ymm2, ymm3, ymm4, ymm5, ymm6, ymm7;
        public readonly Reg32 eax, ecx, edx, ebx, esp, ebp, esi, edi;
        public readonly Reg16 ax, cx, dx, bx, sp, bp, si, di;
        public readonly Reg8 al, cl, dl, bl, ah, ch, dh, bh;
        public readonly AddressFrame ptr, @byte, word, dword, qword;
        public readonly Fpu st0, st1, st2, st3, st4, st5, st6, st7;

        #region for x64

        public readonly Reg64 rax, rcx, rdx, rbx, rsp, rbp, rsi, rdi, r8, r9, r10, r11, r12, r13, r14, r15;
        public readonly Reg32 r8d, r9d, r10d, r11d, r12d, r13d, r14d, r15d;
        public readonly Reg16 r8w, r9w, r10w, r11w, r12w, r13w, r14w, r15w;
        public readonly Reg8 r8b, r9b, r10b, r11b, r12b, r13b, r14b, r15b;
        public readonly Reg8 spl, bpl, sil, dil;
        public readonly Xmm xmm8, xmm9, xmm10, xmm11, xmm12, xmm13, xmm14, xmm15;
        public readonly Ymm ymm8, ymm9, ymm10, ymm11, ymm12, ymm13, ymm14, ymm15;
        public readonly RegRip rip;

        #endregion for x64

        void InitializeRegisters()
        {
            SetRegisterUseIndex("mm", typeof(Mmx));
            SetRegisterUseIndex("xmm", typeof(Xmm));
            SetRegisterUseIndex("ymm", typeof(Ymm));
            SetRegisterUseIndex("st", typeof(Fpu));

            SetRegisterUseCodeType(typeof(Reg64));
            SetRegisterUseCodeType(typeof(Reg32));
            SetRegisterUseCodeType(typeof(Reg16));
            SetRegisterUseCodeType(typeof(Reg8));
        }

        void SetRegisterUseIndex(string prefix, Type type)
        {
            FieldInfo[] fields = typeof(CodeGenerator).GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var fieldInfo in fields.Where(x => x.FieldType == type))
            {
                fieldInfo.SetValue(this, Activator.CreateInstance(type, int.Parse(fieldInfo.Name.Replace(prefix, ""))));
            }
        }

        void SetRegisterUseCodeType(Type type)
        {
            FieldInfo[] fields = typeof(CodeGenerator).GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var fieldInfo in fields.Where(x => x.FieldType == type))
            {
                object codeType = Enum.Parse(typeof(Operand.Code), fieldInfo.Name.ToUpper());
                if (codeType == null)
                {
                    throw new InvalidOperationException("code type not found: " + fieldInfo.Name);
                }
                fieldInfo.SetValue(this, Activator.CreateInstance(type, (int)(Operand.Code)codeType));
            }
        }
    }
}
