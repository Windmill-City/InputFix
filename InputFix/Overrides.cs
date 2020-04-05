using StardewValley;
using StardewValley.Menus;
using System;
using System.Runtime.InteropServices;


namespace InputFix
{
    public class Overrides
    {
        [DllImport("Imm32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const int WM_IME_SETCONTEXT = 0x0281;

        private const int WM_IME_STARTCOMPOSITION = 269;
        private const int WM_IME_COMPOSITION = 271;
        private const int WM_IME_ENDCOMPOSITION = 0x10E;

        private const int WM_INPUTLANGCHANGE = 81;

        private const int WM_KILLFOCUS = 0x008;
        public static bool KeyboardInput_HookProc(ref IntPtr __result, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, IntPtr ___prevWndProc, ref IntPtr ___hIMC)
        {
            //ModEntry.monitor.Log("MSG:" + msg, StardewModdingAPI.LogLevel.Debug);
            if (___hIMC != (IntPtr)0)
            {
                ImmReleaseContext(___prevWndProc, ___hIMC);
                ___hIMC = (IntPtr)0;
            }
            switch (msg)
            {
                case WM_KILLFOCUS:
                    ModEntry.tsf.TerminateComposition();
                    break;
                //IMEs
                case WM_IME_STARTCOMPOSITION:
                    ModEntry.monitor.Log("StartComposition", StardewModdingAPI.LogLevel.Trace);
                    __result = (IntPtr)1;
                    goto Handled;
                case WM_IME_COMPOSITION:
                    ModEntry.textbox_h.text.Clear();
                    if ((int)lParam != -1)
                    {
                        var comp = Marshal.PtrToStringAuto(wParam);
                        if ((int)lParam == 1)//result
                        {
                            for (int i = 0; i < comp.Length; i++)
                            {
                                char ch = comp[i];
                                Game1.keyboardDispatcher.Subscriber?.RecieveTextInput(ch);
                            }
                        }
                        else//comp
                        {
                            ModEntry.textbox_h.text.Insert(0, comp);
                        }
                        ModEntry.monitor.Log("UpdateComposition:" + comp + "&" + (int)lParam, StardewModdingAPI.LogLevel.Trace);
                    }
                    else
                        ModEntry.monitor.Log("UpdateComposition:HRESULT:" + (int)wParam, StardewModdingAPI.LogLevel.Trace);
                    __result = (IntPtr)1;
                    goto Handled;
                case WM_IME_ENDCOMPOSITION:
                    ModEntry.monitor.Log("EndComposition", StardewModdingAPI.LogLevel.Trace);
                    __result = (IntPtr)1;
                    goto Handled;
                case WM_IME_SETCONTEXT:
                case WM_INPUTLANGCHANGE:
                    __result = (IntPtr)1;
                    goto Handled;
            }
            return true;
        Handled:
            return false;
        }
        static bool DuringDraw = false;
        public static void Subscriber_Set()
        {
            if (Game1.gameMode != 6
                && !DuringDraw//compatible with CommandChatTextBox
                )//cant change input state during loading,or the game will struck
            {
                if (Game1.keyboardDispatcher.Subscriber != null && Game1.keyboardDispatcher.Subscriber is TextBox)
                {
                    ModEntry.textbox_h.enableInput(true);
                    ModEntry.textbox_h.SetTextBox((TextBox)Game1.keyboardDispatcher.Subscriber);
                    ModEntry.tsf.TerminateComposition();
                    ModEntry.tsf.ClearText();
                    ModEntry.textbox_h.text.Clear();

                }
                else
                {
                    ModEntry.textbox_h.enableInput(false);
                    ModEntry.tsf.TerminateComposition();
                    ModEntry.tsf.ClearText();
                    ModEntry.textbox_h.text.Clear();
                }
            }
        }
        public static void TextBox_Text(TextBox __instance, string ____text)
        {
            if (__instance == ModEntry.textbox_h.current)
            {
                var length = __instance.Font.MeasureString(____text).X;
                ModEntry.textbox_h.SetCaretX((int)length + 16);
            }

        }

        public static void ChatTextBox_CaretUpdate(ChatTextBox __instance, float ___currentWidth)
        {
            if (__instance == ModEntry.textbox_h.current)
            {
                ModEntry.textbox_h.SetCaretX((int)___currentWidth + 16);
            }
        }

        public static void DrawComposition(Game1 __instance)
        {
            ModEntry.textbox_h.drawComposition();
        }

        public static void CommandChatTextBoxDrawStart()
        {
            DuringDraw = true;
        }

        public static void CommandChatTextBoxDrawEnd()
        {
            DuringDraw = false;
        }
    }
}
