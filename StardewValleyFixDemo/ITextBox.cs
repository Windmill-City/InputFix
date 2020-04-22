using System.Runtime.InteropServices;

namespace StardewValley
{
    //ITextBox interface can be use on LINUX, which enables TextBox's caret move
    //TSF only can be use on Windows, you can change global define in Project->Properties -> Build -> Conditional compilation symbols
    public interface ITextBox : IKeyboardSubscriber
    {
        void SetText(string str);
        string GetText();
        //acpStart and acpEnd should always positive
        void SetSelection(int acpStart, int acpEnd);
        ACP GetSelection();
        //cch - insert text's length
        //return - the selection after insert
        //if we want to insert 3 chars,we need to set result acpEnd = acpStart + 3;
        ACP QueryInsert(ACP acp, uint cch);
        //replace text base on TextBox current acp
        void ReplaceSelection(string text);
        int GetTextLength();
        ACP GetAcpByRange(RECT rect);
        //position compsition window
        RECT GetTextExt(ACP acp);
        bool AllowIME { get;}
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ACP
    {
        public int acpStart;
        public int acpEnd;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    };
}
