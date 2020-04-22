using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;
using System;
using System.Collections.Generic;

namespace StardewValley.Menus
{
    public class ChatTextBox : TextBox
    {
        public IClickableMenu parentMenu;

        public List<ChatSnippet> finalText = new List<ChatSnippet>();

        public float currentWidth;
        public ChatTextBox(SpriteFont font, Color textColor) : base(font, textColor)
        {
        }


    }
}
