using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace StardewValley.Menus
{
    public class ChatTextBox : TextBox
    {
        #region Vars
        public IClickableMenu parentMenu;

        public List<ChatSnippet> finalText = new List<ChatSnippet>();

        public float currentWidth;
        public virtual int X
        {
            get
            {
                return base.X;
            }
            set
            {
                _X = value;
                DrawOrigin.X = _X + 12f;
            }
        }
        public virtual int Y
        {
            get
            {
                return base.Y;
            }
            set
            {
                _Y = value;
                DrawOrigin.Y = _Y + 12f;
            }
        }
        #endregion Vars
        #region ChatTextBox
        public ChatTextBox(Texture2D textBoxTexture, Texture2D caretTexture, SpriteFont font, Color textColor) : base(textBoxTexture, caretTexture, font, textColor)
        {
        }

        public void reset()
        {
            SetText("");
        }
        public void updateWidth()
        {
            currentWidth = 0f;
            foreach (ChatSnippet cs in finalText)
            {
                if (cs.message != null)
                {
                    cs.myLength = Font.MeasureString(cs.message).X;
                }
                currentWidth += cs.myLength;
            }
        }
        public void receiveEmoji(int emoji)
        {
            ReplaceSelection("");
            if (currentWidth + 40f > Width - 66)
            {
                return;
            }
            int index = 0;
            ChatSnippet chatSnippet = new ChatSnippet(emoji);
            for (int i = 0; i < finalText.Count; i++)
            {
                ChatSnippet item = finalText[i];
                index += item.emojiIndex != -1 ? 1 : item.message.Length;
                if (index == acp.Start)//[text message/emoji][caret] 
                {
                    finalText.Insert(i + 1, chatSnippet);
                    goto FinalEmoji;
                }
                else if (index > acp.Start)//[text  [caret]   message]
                {
                    var sep_str1 = new ChatSnippet(item.message.Substring(0, acp.Start - (index - item.message.Length)), LocalizedContentManager.CurrentLanguageCode);
                    var sep_str2 = new ChatSnippet(item.message.Substring(acp.Start - (index - item.message.Length)), LocalizedContentManager.CurrentLanguageCode);
                    finalText[i] = sep_str1;
                    finalText.Insert(i + 1, chatSnippet);
                    finalText.Insert(i + 2, sep_str2);
                    goto FinalEmoji;
                }
            }
            finalText.Add(chatSnippet);
        FinalEmoji:
            updateWidth();
            acp.Start++;
            acp.End++;
#if TSF
            if (Game1.keyboardDispatcher.Subscriber == this)
                Game1.tsf.onTextChange();
#endif
            return;
        }
        #endregion ChatTextBox
        #region ITextBox
        public override void SetText(string str)
        {
            currentWidth = 0;
            finalText.Clear();
            acp.Start = acp.End = 0;
#if TSF
            if (Game1.keyboardDispatcher.Subscriber == this)//changed by other method, except IME
            {
                Game1.tsf.onTextChange();
                Game1.tsf.onSelChange();
            }
#endif
            RecieveTextInput(str);
        }
        public override string GetText()
        {
            StringBuilder sb = new StringBuilder(GetTextLength());
            foreach (var item in finalText)
            {
                if (item.emojiIndex != -1)
                {
                    sb.Append(string.Format("[{0}]", item.emojiIndex));
                }
                else
                {
                    sb.Append(item.message);
                }
            }
            return sb.ToString();
        }
        public override int GetTextLength()
        {
            int len = 0;
            foreach (var item in finalText)
            {
                len += item.emojiIndex != -1 ? 1 : item.message.Length;
            }
            return len;
        }
        public override Acp GetAcpByRange(RECT rect)
        {
            Acp result = new Acp();
            float width = DrawOrigin.X;
            //emoji Menu button 61------>
            if (rect.left <= X + Width - 61 && rect.top <= Y + Height && rect.right >= X && rect.bottom >= Y)//check if overlap textbox
            {
                if (rect.right <= width)
                {
                    result.Start = result.End = 0;
                }
                else if (rect.left >= currentWidth + width)
                {
                    result.Start = result.End = GetTextLength();
                }
                else
                {
                    bool found_start = false;
                    for (int j = 0; j < finalText.Count; j++)
                    {
                        ChatSnippet item = finalText[j];
                        if ((!found_start && width + item.myLength > rect.left) || (found_start && width + item.myLength > rect.right))
                        {
                            if (item.emojiIndex != -1)
                            {
                                width += item.myLength;
                                if (!found_start)
                                {
                                    //divide char from middle, if selection is on the left part, we dont sel this word
                                    result.Start += (width - item.myLength / 2) <= rect.left ? 1 : 0;
                                    result.End = result.Start;
                                    result.End += (((width - item.myLength / 2) < rect.right) && ((width - item.myLength / 2) > rect.left)) ? 1 : 0;
                                    found_start = true;
                                    if (width >= rect.right)
                                    {
                                        return result;
                                    }
                                    continue;
                                }
                                else
                                {
                                    //divide char from middle, if selection is on the left part, we dont sel this word
                                    result.End += (width - item.myLength / 2) < rect.right ? 1 : 0;
                                    return result;
                                }
                            }
                            else
                            {
                                foreach (char ch in item.message)
                                {
                                    var char_x = Font.MeasureString(ch.ToString()).X;
                                    width += char_x;
                                    if (!found_start && width > rect.left)
                                    {
                                        //divide char from middle, if selection is on the left part, we dont sel this word
                                        result.Start += (width - char_x / 2) <= rect.left ? 1 : 0;
                                        result.End = result.Start;
                                        found_start = true;
                                        result.End += (((width - char_x / 2) < rect.right) && ((width - char_x / 2) > rect.left)) ? 1 : 0;
                                        if (width >= rect.right)
                                        {
                                            return result;
                                        }
                                        continue;
                                    }
                                    else if (found_start && width > rect.right)
                                    {
                                        //divide char from middle, if selection is on the left part, we dont sel this word
                                        result.End += (width - char_x / 2) < rect.right ? 1 : 0;
                                        return result;
                                    }
                                    if (found_start)
                                        result.End++;
                                    else
                                        result.Start++;
                                }
                            }
                            continue;
                        }
                        width += item.myLength;
                        if (found_start)
                            result.End += item.emojiIndex != -1 ? 1 : item.message.Length;
                        else
                            result.Start += item.emojiIndex != -1 ? 1 : item.message.Length;
                    }
                }
            }
            else
            {
                result.Start = result.End = -1;
            }
            return result;
        }
        public override RECT GetTextExt(Acp acp)
        {
            var test_len = GetTextLength();
            if (acp.End > test_len)
                acp.End = test_len;
            RECT rect = new RECT();
            var start = Math.Min(acp.Start, acp.End);
            var end = Math.Max(acp.Start, acp.End);
            if (end == 0 || start == GetTextLength())
            {
                return rect;
            }
            rect.left = (int)DrawOrigin.X;
            int index = 0;
            bool foundstart = start == 0;
            if (foundstart)
                rect.right += rect.left;
            foreach (ChatSnippet item in finalText)
            {
                var len = item.emojiIndex != -1 ? 1 : item.message.Length;
                if ((!foundstart && index + len > start) || (foundstart && index + len >= end))
                {
                    if (!foundstart)
                    {
                        if (item.emojiIndex != -1)
                        {
                            rect.right += rect.left;
                            rect.right += (int)item.myLength;
                            index++;
                            if (index == end)
                            {
                                goto Finish;
                            }
                        }
                        else
                        {
                            var sub_len = Math.Min(start - index, item.message.Length);
                            rect.left += (int)Font.MeasureString(item.message.Substring(0, sub_len)).X;
                            rect.right += rect.left;
                            if (index + len >= end)
                            {
                                rect.right += (int)Font.MeasureString(item.message.Substring(sub_len, end - start)).X;
                                goto Finish;
                            }
                            else
                            {
                                rect.right += (int)Font.MeasureString(item.message.Substring(sub_len)).X;
                                index += len;
                            }
                        }
                        foundstart = true;
                        continue;
                    }
                    else
                    {
                        if (item.emojiIndex != -1)
                        {
                            rect.right += (int)item.myLength;
                        }
                        else
                        {
                            var sub_len = end - index;
                            rect.right += (int)Font.MeasureString(item.message.Substring(0, sub_len)).X;
                        }
                        goto Finish;
                    }
                }
                index += len;
                if (!foundstart)
                    rect.left += (int)item.myLength;
                else
                    rect.right += (int)item.myLength;
            }
        Finish:
            rect.top = (int)DrawOrigin.Y;
            rect.bottom = rect.top + 40;//emoji 40

            return rect;
        }

        public override void ReplaceSelection(string _text)
        {
            if (acp.Start != acp.End)//delete selection
            {
                if (acp.End < acp.Start)
                {
                    var temp = acp.Start;
                    acp.Start = acp.End;
                    acp.End = temp;
                }
                int _index = 0;
                for (int i = 0; i < finalText.Count && acp.End - acp.Start > 0; i++)//delete text/emoji before end reach start
                {
                    ChatSnippet item = finalText[i];
                    _index += item.emojiIndex != -1 ? 1 : item.message.Length;
                    if (_index > acp.Start)
                    {
                        if (item.emojiIndex != -1)
                        {
                            finalText.RemoveAt(i);
                            i--;
                            acp.End--;
                            _index--;
                            if (i >= 0 && finalText.Count > i + 1 && finalText[i].emojiIndex == -1 && finalText[i + 1].emojiIndex == -1)
                            {
                                //both text,merge it
                                _index -= finalText[i].message.Length;
                                finalText[i].message += finalText[i + 1].message;
                                finalText[i].myLength += finalText[i + 1].myLength;
                                finalText.RemoveAt(i + 1);
                                //re-handle this snippet
                                i--;
                            }
                        }
                        else
                        {
                            //acp selection may cross snippet, dont out of range
                            var start = acp.Start - (_index - item.message.Length);
                            int len = Math.Min(acp.End - acp.Start, item.message.Length - start);
                            item.message = item.message.Remove(start, len);
                            acp.End -= len;
                            _index -= len;
                            if (item.message.Length == 0)//empty, remove it
                            {
                                finalText.RemoveAt(i);
                                i--;
                            }
                            else
                            {
                                item.myLength = Font.MeasureString(item.message).X;
                            }
                        }
                    }
                }
                updateWidth();
            }
            int index = 0;
            ChatSnippet chatSnippet = new ChatSnippet(_text, LocalizedContentManager.CurrentLanguageCode);
            if (chatSnippet.myLength == 0)
                return;
            for (int i = 0; i < finalText.Count; i++)
            {
                if (chatSnippet.myLength + currentWidth >= Width - 66)
                {
                    acp.End = acp.Start;
                    return;
                }
                ChatSnippet item = finalText[i];
                index += item.emojiIndex != -1 ? 1 : item.message.Length;
                if (index >= acp.Start && item.emojiIndex == -1)//[text  [caret > ]   message][ = caret (index)]
                {
                    item.message = item.message.Insert(acp.Start - (index - item.message.Length), chatSnippet.message);
                    item.myLength += chatSnippet.myLength;
                    goto Final;
                }
                else if (index > acp.Start)//[nothing/emoji][caret here][emoji(now index is here, larger than caret pos)]
                {
                    finalText.Insert(i, chatSnippet);
                    goto Final;
                }
            }
            finalText.Add(chatSnippet);
        Final:
            acp.End = acp.Start + chatSnippet.message.Length;
            updateWidth();
            //IME input dont play sound, english input sound is handled at IKeyboadSubscriber
            //Game1.playSound("cowboy_monsterhit");//TSF may replace some word, which will make the sound strange
        }
        #endregion ITextBox
        #region Draw
        public override void Draw(SpriteBatch spriteBatch, bool drawShadow = true)
        {
            DrawBackGround(spriteBatch);

            float xPositionSoFar = DrawOrigin.X;
            if (Selected)
            {
                DrawByAcp(spriteBatch, new Acp(0, acp.Start), ref xPositionSoFar, TextColor, drawShadow);
                DrawCaret(spriteBatch, ref xPositionSoFar);
                DrawByAcp(spriteBatch, new Acp(acp.Start, GetTextLength()), ref xPositionSoFar, TextColor, drawShadow);
            }
            else
                DrawByAcp(spriteBatch, new Acp(0, GetTextLength()), ref xPositionSoFar, TextColor, drawShadow);
        }
        protected override void DrawByAcp(SpriteBatch spriteBatch, Acp acp, ref float offset, Color color, bool drawShadow = true)
        {
            var start = Math.Min(acp.Start, acp.End);
            var end = Math.Max(acp.Start, acp.End);
            int index = 0;
            if (end == 0 || start == GetTextLength())
            {
                return;
            }
            bool foundstart = start == 0;
            foreach (ChatSnippet item in finalText)
            {
                var len = item.emojiIndex != -1 ? 1 : item.message.Length;
                if ((!foundstart && index + len > start) || (foundstart && index + len >= end))
                {
                    if (!foundstart)
                    {
                        if (item.emojiIndex != -1)
                        {
                            index++;
                            DrawChatSnippet(spriteBatch, item, ref offset, drawShadow);
                            if (index == end)
                            {
                                goto Finish;
                            }
                        }
                        else
                        {
                            var sub_len = Math.Min(start - index, len);
                            if (index + len >= end)
                            {
                                ChatSnippet sep_text = new ChatSnippet(item.message.Substring(sub_len, end - start), LocalizedContentManager.CurrentLanguageCode);
                                DrawChatSnippet(spriteBatch, sep_text, ref offset, drawShadow);
                                goto Finish;
                            }
                            else
                            {
                                ChatSnippet sep_text = new ChatSnippet(item.message.Substring(sub_len), LocalizedContentManager.CurrentLanguageCode);
                                DrawChatSnippet(spriteBatch, sep_text, ref offset, drawShadow);
                                index += len;
                            }
                        }
                        foundstart = true;
                        continue;
                    }
                    else
                    {
                        if (item.emojiIndex != -1)
                        {
                            DrawChatSnippet(spriteBatch, item, ref offset, drawShadow);
                        }
                        else
                        {
                            var sub_len = end - index;
                            ChatSnippet sep_text = new ChatSnippet(item.message.Substring(0, sub_len), LocalizedContentManager.CurrentLanguageCode);
                            DrawChatSnippet(spriteBatch, sep_text, ref offset, drawShadow);
                        }
                        goto Finish;
                    }
                }
                index += len;
                if (foundstart)
                    DrawChatSnippet(spriteBatch, item, ref offset, drawShadow);
            }
        Finish:
            return;
        }
        public virtual void DrawChatSnippet(SpriteBatch spriteBatch, ChatSnippet snippet, ref float offset, bool drawShadow = true)
        {
            if (snippet.emojiIndex != -1)
            {
                spriteBatch.Draw(
                    ChatBox.emojiTexture,
                    new Vector2(offset, DrawOrigin.Y),
                    new Rectangle?(new Rectangle(
                        snippet.emojiIndex * 9 % ChatBox.emojiTexture.Width,
                        snippet.emojiIndex * 9 / ChatBox.emojiTexture.Width * 9,
                        9,
                        9)),
                    Color.White,
                    0f,
                    Vector2.Zero,
                    4f,
                    SpriteEffects.None,
                    0.99f);
            }
            else if (snippet.message != null)
            {
                spriteBatch.DrawString(
                    //ChatBox.messageFont(LocalizedContentManager.CurrentLanguageCode),
                    Font,
                    snippet.message,
                    new Vector2(offset, DrawOrigin.Y),
                    //ChatMessage.getColorFromName(Game1.player.defaultChatColor),
                    TextColor,
                    0f, Vector2.Zero,
                    1f,
                    SpriteEffects.None,
                    0.99f);
            }
            offset += snippet.myLength;
        }
        #endregion Draw
    }
    #region ChatSnippet
    public class ChatSnippet
    {
        public string message;

        public int emojiIndex = -1;

        public float myLength;

        public ChatSnippet(string message, LocalizedContentManager.LanguageCode language)
        {
            //IL_001c: Unknown result type (might be due to invalid IL or missing references)
            this.message = message;
            myLength = Game1.game1.smallFont.MeasureString(message).X;
        }

        public ChatSnippet(int emojiIndex)
        {
            this.emojiIndex = emojiIndex;
            myLength = 40f;
        }
    }
    #endregion ChatSnippet
}
