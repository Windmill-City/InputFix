using Harmony;
using StardewModdingAPI;
using System;
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

                MethodInfo m_emoji = AccessTools.Method(CCTB, "ReceiveEmoji");
                harmony.Patch(m_emoji, new HarmonyMethod(typeof(Compatibility), "receiveEmoji"));

                MethodInfo m_leftarrow = AccessTools.Method(CCTB, "OnLeftArrowPress");
                harmony.Patch(m_leftarrow, new HarmonyMethod(typeof(Compatibility), "CommandChatTextBoxOnArrow"));

                MethodInfo m_rightarrow = AccessTools.Method(CCTB, "OnRightArrowPress");
                harmony.Patch(m_rightarrow, new HarmonyMethod(typeof(Compatibility), "CommandChatTextBoxOnArrow"));
            }
            else
            {
                monitor.Log("CommandChatTextBox NOT FOUND", LogLevel.Error);
            }
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