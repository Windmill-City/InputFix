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

        private const int WM_SETFOCUS = 0x0007;
        private const int WM_KILLFOCUS = 0x0008;

        private const int EM_REPLACESEL = 0x00C2;
        private const int EM_SETSEL = 0x00B1;
        private const int EM_GETSEL = 0x00B0;

        private const int TF_GETTEXTLENGTH = 0x060E;
        private const int TF_GETTEXT = 0x060D;
        private const int TF_UNLOCKED = 0x60F;
        private const int TF_LOCKED = 0x606;

        private static string temptext;
        private static int sel_Start;
        private static int sel_End;
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
                case EM_REPLACESEL:
                    temptext = Marshal.PtrToStringAuto(lParam);
                    ModEntry.monitor.Log("TempText:" + temptext, StardewModdingAPI.LogLevel.Debug);
                    sel_End = sel_Start + temptext.Length;
                    ModEntry.textbox_h.text.Length = sel_End;//Ensure len
                    int k = 0;
                    for (int i = sel_Start; i < sel_End; i++)
                    {
                        var ch = temptext[k];
                        ModEntry.textbox_h.text[i] = ch;
                        k++;
                    }
                    ModEntry.monitor.Log("AfterReplace:AcpStart:" + sel_Start + "AcpEnd:" + sel_End, StardewModdingAPI.LogLevel.Debug);
                    __result = (IntPtr)1;
                    goto Handled;
                case EM_SETSEL:
                    if ((int)wParam > (int)lParam)//if start > end, reverse it
                    {
                        sel_Start = (int)lParam;
                        sel_End = (int)wParam;
                    }
                    else
                    {
                        sel_Start = (int)wParam;
                        sel_End = (int)lParam;
                    }
                    ModEntry.monitor.Log("SetSelection:AcpStart:" + sel_Start + "AcpEnd:" + sel_End, StardewModdingAPI.LogLevel.Debug);

                    ModEntry.textbox_h.text.Length = sel_End;//Ensure len
                    __result = (IntPtr)1;
                    goto Handled;
                //GetSelection
                case EM_GETSEL:
                    ModEntry.monitor.Log("GetSelection:AcpStart:" + sel_Start + "AcpEnd:" + sel_End, StardewModdingAPI.LogLevel.Debug);
                    Marshal.WriteInt32(wParam, sel_Start);//acpstart
                    Marshal.WriteInt32(lParam, sel_End);//acpend
                    __result = (IntPtr)1;
                    goto Handled;
                //Doc lock
                case TF_LOCKED:
                    __result = (IntPtr)1;
                    goto Handled;
                //Doc unlock
                case TF_UNLOCKED:
                    __result = (IntPtr)1;
                    goto Handled;
                //GetText
                case TF_GETTEXTLENGTH:
                    Marshal.WriteInt32(lParam, ModEntry.textbox_h.text.Length);//textlen
                    __result = (IntPtr)1;
                    goto Handled;
                case TF_GETTEXT:
                    int len = (int)lParam;//max len
                    char[] _text = ModEntry.textbox_h.text.ToString().ToCharArray();
                    Marshal.Copy(_text, 0, wParam, Math.Min(len, _text.Length));
                    __result = (IntPtr)1;
                    goto Handled;
                //IMEs
                case WM_IME_STARTCOMPOSITION:
                    ModEntry.monitor.Log("StartComposition", StardewModdingAPI.LogLevel.Debug);
                    __result = (IntPtr)1;
                    goto Handled;
                case WM_IME_COMPOSITION:
                    ModEntry.monitor.Log("UpdateComposition", StardewModdingAPI.LogLevel.Debug);
                    __result = (IntPtr)1;
                    goto Handled;
                case WM_IME_ENDCOMPOSITION:
                    ModEntry.monitor.Log("EndComposition", StardewModdingAPI.LogLevel.Debug);
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

        public static void TextBox_Selected(TextBox __instance, string ____text, bool ____selected)
        {
            if (Game1.keyboardDispatcher.Subscriber != null && Game1.keyboardDispatcher.Subscriber == __instance && ____selected)
            {
                ModEntry.textbox_h.enableInput(true);
                ModEntry.textbox_h.SetTextBox(__instance);
                ModEntry.tsf.TerminateComposition();
                ModEntry.tsf.ClearText();

            }
            else if (Game1.keyboardDispatcher.Subscriber == null)
            {
                ModEntry.textbox_h.enableInput(false);
                ModEntry.tsf.TerminateComposition();
                ModEntry.tsf.ClearText();
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

        public static void DrawComposition(Game1 __instance)
        {
            ModEntry.textbox_h.drawComposition();
        }
    }
}
