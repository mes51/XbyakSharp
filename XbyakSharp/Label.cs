using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XbyakSharp
{
    public struct JmpLabel
    {
        public ulong EndOfJmp { get; set; }

        public int JmpSize { get; set; }

        public LabelMode Mode { get; set; }
    }

    public class Label
    {
        private const int MaxStack = 10;

        public static string ToLabelIndex(int num)
        {
            return "." + num.ToString("x8");
        }

        private int anonymouseCount = 0;
        private Stack<int> stack = new Stack<int>(MaxStack);
        private int usedCount = 0;
        private int localCount = 0;
        private Dictionary<string, ulong> definedList = new Dictionary<string, ulong>();
        private Dictionary<string, List<JmpLabel>> undefinedList = new Dictionary<string, List<JmpLabel>>();

        public CodeArray BaseCodeArray { get; set; }

        public void Reset()
        {
            BaseCodeArray = null;
            anonymouseCount = 0;
            stack.Clear();
            usedCount = 0;
            localCount = 0;
            definedList.Clear();
            undefinedList.Clear();
        }

        public void EnterLocal()
        {
            if (stack.Count >= MaxStack)
            {
                throw new InvalidOperationException("over local label");
            }
            usedCount++;
            localCount = usedCount;
            stack.Push(usedCount);
        }

        public void LeaveLocal()
        {
            if (stack.Count <= 0)
            {
                throw new InvalidOperationException("under local label");
            }
            localCount = stack.Pop();
        }

        public void Define(string label, ulong addrOffset, IntPtr addr)
        {
            if (label == "@@")
            {
                anonymouseCount++;
                label += ToLabelIndex(anonymouseCount);
            }
            else if (label[0] == '.')
            {
                label += ToLabelIndex(localCount);
            }

            definedList.Add(label, addrOffset);
            if (!this.undefinedList.ContainsKey(label))
            {
                return;
            }

            foreach (var undefinedLabel in this.undefinedList[label])
            {
                ulong disp = 0;
                if (undefinedLabel.Mode == LabelMode.LaddTop)
                {
                    disp = addrOffset;
                }
                else if (undefinedLabel.Mode == LabelMode.Labs)
                {
                    disp = addr.ToUInt64();
                }
                else
                {
                    disp = addrOffset - undefinedLabel.EndOfJmp;
                    if (undefinedLabel.JmpSize <= 4)
                    {
                        disp = Util.VerifyInInt32(disp);
                    }
                    if (undefinedLabel.JmpSize == 1 && !Util.IsInDisp8((uint)disp))
                    {
                        throw new InvalidOperationException("label is too far");
                    }
                }

                int offset = (int)undefinedLabel.EndOfJmp - undefinedLabel.JmpSize;
                if (BaseCodeArray.IsAutoGrow)
                {
                    BaseCodeArray.Save(offset, disp, undefinedLabel.JmpSize, undefinedLabel.Mode);
                }
                else
                {
                    BaseCodeArray.Rewrite(offset, disp, undefinedLabel.JmpSize);
                }
            }
            undefinedList.Remove(label);
        }

        public bool GetOffset(out ulong offset, string label)
        {
            label = ConvertLabel(label);
            if (definedList.ContainsKey(label))
            {
                offset = definedList[label];
                return true;
            }
            else
            {
                offset = 0;
                return false;
            }
        }

        public void AddUndefinedLabel(string label, JmpLabel jmp)
        {
            string newLabel = ConvertLabel(label);
            if (!undefinedList.ContainsKey(newLabel))
            {
                undefinedList.Add(newLabel, new List<JmpLabel>());
            }
            undefinedList[newLabel].Add(jmp);
        }

        public bool HasUndefinedLabel()
        {
            return undefinedList.Count != 0;
        }

        private string ConvertLabel(string label)
        {
            if (label == "@f" || label == "@F")
            {
                label = "@@" + ToLabelIndex(anonymouseCount + 1);
            }
            else if (label == "@b" || label == "@B")
            {
                label = "@@" + ToLabelIndex(anonymouseCount);
            }
            else if (label[0] == '.')
            {
                label += ToLabelIndex(localCount);
            }

            return label;
        }
    }
}
