using Harmony;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace InputFix
{
    [HarmonyPatch(typeof(KeyboardDispatcher))]
    [HarmonyPatch("Subscriber", MethodType.Setter)]
    internal class PatchSubScriber
    {
        private static void Postfix()
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
    }

    [HarmonyPatchAll]
    [HarmonyPatch(typeof(ChatBox))]
    internal class PatchChatBox
    {
        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor)]
        private static void onConstruct(ChatBox __instance)
        {
            //replace ChatTextBox
            Texture2D texture2D = Game1.content.Load<Texture2D>("LooseSprites\\chatBox");
            ChatTextBox_ chatTextBox_ = new ChatTextBox_(texture2D, null, Game1.smallFont, Color.White);
            chatTextBox_.OnEnterPressed += new TextBoxEvent(__instance.textBoxEnter);
            chatTextBox_.X = __instance.chatBox.X;
            chatTextBox_.Y = __instance.chatBox.Y;
            chatTextBox_.Width = __instance.chatBox.Width;
            chatTextBox_.Height = __instance.chatBox.Height;
            __instance.chatBox = chatTextBox_;
        }

        [HarmonyPostfix]
        [HarmonyPatch("updatePosition")]
        private static void onUpdatePosition(ChatBox __instance)
        {
            if (__instance.chatBox is ChatTextBox_)
            {
                (__instance.chatBox as ChatTextBox_).X = __instance.chatBox.X;
                (__instance.chatBox as ChatTextBox_).Y = __instance.chatBox.Y;
            }
        }
    }
}