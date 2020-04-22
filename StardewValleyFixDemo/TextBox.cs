using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Text;

namespace StardewValley.Menus
{
    public class TextBox : ITextBox
    {
        SpriteFont _font;
        public SpriteFont Font
        {
            get
            {
                return _font;
            }
        }
        Color _textColor;
        public Color TextColor
        {
            get
            {
                return this._textColor;
            }
        }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool PasswordBox { get; set; }
        bool _selected;
        public bool Selected
        {
            get
            {
                return _selected;
            }
            set
            {
                if (_selected != value)
                {
                    if (_selected)
                    {
                        if (Game1.keyboardDispatcher.Subscriber == this)
                            Game1.keyboardDispatcher.Subscriber = null;
                    }
                    else
                    {
                        Game1.keyboardDispatcher.Subscriber = this;
                    }
                    _selected = value;
                }
            }
        }
        public bool AllowIME
        {
            get
            {
                return !numbersOnly;
            }
        }

        public bool numbersOnly = false;
        public int textLimit = -1;

        ACP acp = new ACP();
        StringBuilder text = new StringBuilder("你好");

        public TextBox(SpriteFont font, Color color)
        {
            _font = font;
            _textColor = color;
        }

        public ACP GetSelection()
        {
            return acp;
        }

        public void SetSelection(int acpStart, int acpEnd)
        {
            acp.acpStart = acpStart;
            acp.acpEnd = acpEnd;

            int len = GetTextLength();
            if (acp.acpStart > len || acp.acpEnd > len)//out of range
            {
                acp.acpEnd = acp.acpStart = len;//reset caret to the tail
            }
        }

        public string GetText()
        {
            return text.ToString();
        }
#if TSF

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, ExactSpelling = true)]
        internal static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, ref RECT pt, int cPoints);
#endif
        public RECT GetTextExt(ACP acp)
        {

            RECT rect = new RECT();
#if TSF
            string text = GetText();
            rect.left += (int)Font.MeasureString(text.Substring(0, acp.acpStart)).X;
            rect.top = Y;

            rect.right = rect.left + (int)Font.MeasureString(" ").X * (acp.acpEnd - acp.acpStart);
            rect.bottom = rect.top + (int)Font.MeasureString(" ").Y + 8;

            MapWindowPoints(Game1.game1.Window.Handle, (IntPtr)0, ref rect, 2);
#endif
            return rect;
        }

        public int GetTextLength()
        {
            return text.Length;
        }

        public ACP QueryInsert(ACP acp, uint cch)
        {
            if (textLimit != -1)
                acp.acpEnd = Math.Min(textLimit, acp.acpEnd);
            return acp;
        }

        public void SetText(string str)
        {
            text.Clear();
            text.Append(str);
            acp.acpStart = acp.acpEnd = text.Length;
#if TSF
            if (Game1.keyboardDispatcher.Subscriber == this)//changed by other method, except IME
            {
                Game1.tsf.onTextChange();
                Game1.tsf.onSelChange();
            }
#endif
        }

        public void ReplaceSelection(string _text)
        {
            if(acp.acpEnd < acp.acpStart)//it means delete and insert text
            {
                text.Remove(acp.acpEnd, acp.acpStart - acp.acpEnd);
                acp.acpStart = acp.acpEnd;
            }
            if ((Font.MeasureString(text).X + Font.MeasureString(_text).X) < Width)//insert
            {
                text.Insert(acp.acpStart, _text);
                //after insert index
                acp.acpEnd = acp.acpStart + _text.Length;
            }
        }

        public ACP GetAcpByRange(RECT rect)
        {
            ACP result = new ACP();
            var text = GetText();
            float width = X;
            if(rect.left <= X + Font.MeasureString(text).X && rect.right >= X && rect.bottom <= Y && rect.top >= Font.MeasureString(text).Y + Y)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    var char_x = Font.MeasureString(text[i].ToString()).X;
                    width += char_x;
                    result.acpStart++;
                    if (width > rect.left)
                    {
                        result.acpEnd = result.acpStart;
                        for(i++; i < text.Length; i++)
                        {
                            char_x = Font.MeasureString(text[i].ToString()).X;
                            width += char_x;
                            result.acpEnd++;
                            if (width > rect.right)
                                break;
                        }
                        break;
                    }
                }
            }
            else
            {
                result.acpStart = result.acpEnd = -1;
            }
            return result;
        }

        public void RecieveCommandInput(char command)//IME will handle key event first, so these method just for english input(if it is using IME, we need to notify TSF)
        {
            switch (command)
            {
                case '\b':
                    if (acp.acpStart == acp.acpEnd && acp.acpEnd > 0)//if not, means it alradey have something selected, we just delete it
                    {
                        acp.acpEnd--;//it selected nothing, reduce end to delete a char
                    }
                    if (acp.acpStart != acp.acpEnd)
                    {
                        ReplaceSelection("");
#if TSF
                        if (Game1.keyboardDispatcher.Subscriber == this)
                        {
                            Game1.tsf.onTextChange();//not changed by IME, should notify
                            Game1.tsf.onSelChange();
                        }
#endif
                    }
                    break;
                case '\r':
                    //onEnterPressed
                    break;
                case '\t':
                    //onTabPressed
                    break;
                default:
                    break;
            }
        }

        public void RecieveSpecialInput(Keys key)//IME will handle key event first, so these method just for english input(if it is using IME, we need to notify TSF)
        {
            switch (key)
            {
                case Keys.Left:
                    if(acp.acpStart > 0 || acp.acpEnd > 0)
                    {
                        if (acp.acpStart != acp.acpEnd)
                            acp.acpEnd = acp.acpStart = Math.Min(acp.acpEnd, acp.acpStart);//have selected sth, go to the left most
                        else
                            acp.acpStart = --acp.acpEnd;//left move caret
#if TSF
                        if (Game1.keyboardDispatcher.Subscriber == this)
                            Game1.tsf.onSelChange();
#endif
                    }
                    break;
                case Keys.Right:
                    var len = GetTextLength();
                    if (acp.acpStart < len || acp.acpEnd < len)
                    {
                        if (acp.acpStart != acp.acpEnd)
                            acp.acpEnd = acp.acpStart = Math.Max(acp.acpEnd, acp.acpStart);//have selected sth, go to the right most
                        else
                            acp.acpStart = ++acp.acpEnd;//right move caret
#if TSF
                        if (Game1.keyboardDispatcher.Subscriber == this)
                            Game1.tsf.onSelChange();
#endif
                    }
                    break;
                default:
                    break;
            }
        }

        public void RecieveTextInput(char inputChar)//IME will handle key event first, so these method just for english input(if it is using IME, we need to notify TSF)
        {
            RecieveTextInput(inputChar.ToString());
        }

        public void RecieveTextInput(string text)//IME will handle key event first, so these method just for english input(if it is using IME, we need to notify TSF)
        {
            ACP temp_acp = new ACP();
            temp_acp.acpEnd = acp.acpEnd + text.Length;
            temp_acp.acpStart = acp.acpStart;

            acp = QueryInsert(temp_acp, (uint)text.Length);
            ReplaceSelection(text);
#if TSF
            if (Game1.keyboardDispatcher.Subscriber == this)
                Game1.tsf.onTextChange();//not changed by IME, should notify
#endif
            acp.acpStart = acp.acpEnd;
#if TSF
            if (Game1.keyboardDispatcher.Subscriber == this)
                Game1.tsf.onSelChange();
#endif

        }

        public virtual void Draw(SpriteBatch spriteBatch, bool drawShadow = true)
        {
           
            bool caretVisible = DateTime.UtcNow.Millisecond % 1000 >= 500;
                
            string toDraw = PasswordBox ? new string('*', text.Length) : text.ToString();

            int offset = X + 16;

            var sep_str1 = toDraw.Substring(0, acp.acpStart);
            var sep_str2 = toDraw.Substring(acp.acpStart);
            var sep1_len = Font.MeasureString(sep_str1).X;

            if (caretVisible)
            {
                //caret width = 4
                spriteBatch.DrawString(Font, "|", new Vector2(offset + sep1_len, Y + (8)), TextColor);
            }
            spriteBatch.DrawString(Font, sep_str1, new Vector2(offset, Y + (8)), TextColor);
            spriteBatch.DrawString(Font, sep_str2, new Vector2(offset + sep1_len + 4, Y + 8), TextColor);

        }
    }
}
