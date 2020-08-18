using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace InputFix
{
    public class Overrides
    {
        public static void Subscriber_Set()
        {
            if (Game1.keyboardDispatcher.Subscriber is ITextBox && (Game1.keyboardDispatcher.Subscriber as ITextBox).AllowIME)
            {
                ImeSharp.InputMethod.Enabled = true;
            }
            else
            {
                ImeSharp.InputMethod.Enabled = false;
            }
        }

        public static bool CommandChatTextBoxDrawStart(TextBox __instance, SpriteBatch spriteBatch, Texture2D ____textBoxTexture, bool drawShadow = true)
        {
            return false;
        }

        public static bool CommandChatTextBoxOnArrow(TextBox __instance)
        {
            return false;
        }
    }
}