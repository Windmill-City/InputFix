using Harmony;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace InputFix
{
    public class Compatibility
    {
        public static void PatchChatCommands(IMonitor monitor, HarmonyInstance harmony)
        {
            Type CCTB = AccessTools.TypeByName("ChatCommands.ClassReplacements.CommandChatTextBox");
            if (CCTB != null)
            {
                monitor.Log("Patching CommandChatTextBox", LogLevel.Info);
                MethodInfo m_draw2 = AccessTools.Method(CCTB, "Draw");
                harmony.Patch(m_draw2, new HarmonyMethod(typeof(Compatibility), "CommandChatTextBoxDrawStart"));

                MethodInfo m_leftarrow = AccessTools.Method(CCTB, "OnLeftArrowPress");
                harmony.Patch(m_leftarrow, new HarmonyMethod(typeof(Compatibility), "CommandChatTextBoxOnArrow"));

                MethodInfo m_rightarrow = AccessTools.Method(CCTB, "OnRightArrowPress");
                harmony.Patch(m_rightarrow, new HarmonyMethod(typeof(Compatibility), "CommandChatTextBoxOnArrow"));

                List<ConstructorInfo> m_ctor = AccessTools.GetDeclaredConstructors(CCTB);
                harmony.Patch(m_ctor[1], null, new HarmonyMethod(typeof(Compatibility), "onConstructChatBox"));
            }
            else
            {
                monitor.Log("CommandChatTextBox NOT FOUND", LogLevel.Error);
            }
        }

        public static void onConstructChatBox(ChatBox __instance)
        {
            //replace ChatTextBox
            Texture2D texture2D = Game1.content.Load<Texture2D>("LooseSprites\\chatBox");
            ChatTextBox_ chatTextBox_ = new ChatTextBox_(texture2D, null, Game1.smallFont, Color.White);
            //chatTextBox_.OnEnterPressed += new TextBoxEvent());
            chatTextBox_.X = __instance.chatBox.X;
            chatTextBox_.Y = __instance.chatBox.Y;
            chatTextBox_.Width = __instance.chatBox.Width;
            chatTextBox_.Height = __instance.chatBox.Height;
            __instance.chatBox = chatTextBox_;
        }

        public static bool CommandChatTextBoxDrawStart()
        {
            return false;
        }

        public static bool CommandChatTextBoxOnArrow()
        {
            return false;
        }
    }
}