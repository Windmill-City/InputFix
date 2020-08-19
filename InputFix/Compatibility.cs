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
            Type CCTB = getByFullName("ChatCommands.ClassReplacements.CommandChatTextBox");
            if (CCTB != null)
            {
                monitor.Log("Patching CommandChatTextBox", LogLevel.Info);
                MethodInfo m_draw2 = CCTB.GetMethod("Draw", BindingFlags.Public | BindingFlags.Instance);
                harmony.Patch(m_draw2, new HarmonyMethod(typeof(Compatibility), "CommandChatTextBoxDrawStart"));

                MethodInfo m_emoji = CCTB.GetMethod("ReceiveEmoji", BindingFlags.Public | BindingFlags.Instance);
                harmony.Patch(m_emoji, new HarmonyMethod(typeof(Compatibility), "receiveEmoji"));

                MethodInfo m_leftarrow = CCTB.GetMethod("OnLeftArrowPress", BindingFlags.Public | BindingFlags.Instance);
                harmony.Patch(m_leftarrow, new HarmonyMethod(typeof(Compatibility), "CommandChatTextBoxOnArrow"));

                MethodInfo m_rightarrow = CCTB.GetMethod("OnRightArrowPress", BindingFlags.Public | BindingFlags.Instance);
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

        public static Type getByFullName(string typeName)
        {
            Type type = null;
            Assembly[] assemblyArray = AppDomain.CurrentDomain.GetAssemblies();
            int assemblyArrayLength = assemblyArray.Length;
            for (int i = 0; i < assemblyArrayLength; ++i)
            {
                type = assemblyArray[i].GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            for (int i = 0; (i < assemblyArrayLength); ++i)
            {
                Type[] typeArray = assemblyArray[i].GetTypes();
                int typeArrayLength = typeArray.Length;
                for (int j = 0; j < typeArrayLength; ++j)
                {
                    if (typeArray[j].Name.Equals(typeName))
                    {
                        return typeArray[j];
                    }
                }
            }
            return type;
        }
    }
}