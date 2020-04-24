using System.Runtime.InteropServices;

namespace StardewValley.Menus
{
    //TSF only can be use on Windows, you can change global define in Project->Properties -> Build -> Conditional compilation symbols
    /// <summary>
    /// The ITextBox interface can be use on LINUX and Windows, which enables TextBox's caret move and text insert
    /// </summary>
    public interface ITextBox : IKeyboardSubscriber
    {
        /// <summary>
        /// Clear and Set TextBox's Text
        /// </summary>
        /// <param name="str"></param>
        void SetText(string str);
        string GetText();
        /// <summary>
        /// The SetSelection method selects text within the TextBox.
        /// </summary>
        /// <param name="acpStart"></param>
        /// <param name="acpEnd"></param>
        void SetSelection(int acpStart, int acpEnd);
        Acp GetSelection();
        /// <summary>
        /// The QueryInsert method determines whether the specified start and end character positions are valid.
        /// Use this method to adjust an edit to a document before executing the edit. The method must not return values outside the range of the document.
        /// </summary>
        /// <param name="acp">
        /// acpStart
        ///     Starting application character position for inserted text.
        /// acpEnd
        ///     Ending application character position for the inserted text.This value is equal to acpTextStart if the text is inserted at a point instead of replacing selected text.
        /// </param>
        /// <param name="cch">Length of replacement text.</param>
        /// <returns>
        /// acpResultStart
        /// Returns the new starting application character position of the inserted text.
        /// If this parameter is -1, then text cannot be inserted at the specified position.This value cannot be outside the document range.
        /// acpResultEnd
        /// Returns the new ending application character position of the inserted text.
        /// If this parameter is -1, then pacpResultStart is set to -1 and text cannot be inserted at the specified position. This value cannot be outside the document range.
        /// </returns>
        Acp QueryInsert(Acp acp, uint cch);
        /// <summary>
        /// Replace TextBox's Text base on its current selection
        /// </summary>
        /// <param name="text">Replace text</param>
        void ReplaceSelection(string text);
        /// <summary>
        /// Get TextBox's Text Length
        /// </summary>
        /// <returns>Text Length</returns>
        int GetTextLength();
        /// <summary>
        /// Get Text ACP Position by Screen pos Rect
        /// </summary>
        /// <param name="rect"></param>
        /// <returns>
        /// acpStart = acpEnd = -1 means rect not overlap TextBox
        /// if overlap TextBox and the left pos > string's right pos, return acpStart = acpEnd = text.length
        /// if overlap TextBox and the right pos < string's left pos, return acpStart = acpEnd = 0
        /// </returns>
        Acp GetAcpByRange(RECT rect);
        /// <summary>
        /// The GetTextExt method returns the bounding box, in world coordinates, of the text at a specified character position.
        /// </summary>
        /// <param name="acp">
        /// Specifies the character position of the text to get in the document.    
        /// </param>
        /// <returns>
        /// the bounding box in screen coordinates of the text at the specified character positions.
        /// </returns>
        RECT GetTextExt(Acp acp);
        /// <summary>
        /// Allow IME start composition
        /// </summary>
        bool AllowIME { get; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Acp
    {
        public int Start;
        public int End;

        public Acp(int start, int end)
        {
            Start = start;
            End = end;
        }
        public Acp(Acp acp)
        {
            Start = acp.Start;
            End = acp.End;
        }
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
