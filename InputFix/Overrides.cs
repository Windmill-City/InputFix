using StardewValley;
using StardewValley.Menus;
using System;
using System.Runtime.InteropServices;


namespace InputFix
{
    public class Overrides
    {
        [DllImport("Imm32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr ImmGetContext(IntPtr hWnd);

        [DllImport("Imm32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);
        [DllImport("Imm32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr ImmAssociateContext(IntPtr hWnd, IntPtr hIMC);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private const int WM_IME_COMPOSITION = 271;
        private const int WM_IME_STARTCOMPOSITION = 269;
        private const int WM_INPUTLANGCHANGE = 81;
        private const int WM_IME_SETCONTEXT = 0x0281;
        private const int WM_SETFOCUS = 0x0007;

        public static bool KeyboardInput_HookProc(ref IntPtr __result, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, IntPtr ___prevWndProc, ref IntPtr ___hIMC)
        {
            ModEntry.monitor.Log("MSG:" + msg, StardewModdingAPI.LogLevel.Debug);
            if (___hIMC != (IntPtr)0)
            {
                ImmReleaseContext(___prevWndProc, ___hIMC);
                ___hIMC = (IntPtr)0;
            }
            switch (msg)
            {
                case WM_SETFOCUS:
                    //ModEntry.tsf.SetFocus();
                    break;
                case WM_IME_STARTCOMPOSITION:
                    __result = (IntPtr)1;
                    goto Handled;
                case WM_IME_COMPOSITION:
                    __result = (IntPtr)1;
                    goto Handled;
                case WM_IME_SETCONTEXT:
                    __result = (IntPtr)1;
                    goto Handled;
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
                var text = __instance.Font.MeasureString(____text);
                var _char = __instance.Font.MeasureString("字");
                int X = __instance.X + (int)text.X;
                int Y = __instance.Y + (int)text.Y;
                ModEntry.textbox_h.SetTextExt(X, X + (int)_char.X, Y, Y + (int)_char.Y);
            }
            else if (Game1.keyboardDispatcher.Subscriber == null)
                ModEntry.textbox_h.enableInput(false);
        }
        public static void TextBox_Text(TextBox __instance, string ____text)
        {
            var text = __instance.Font.MeasureString(____text);
            var _char = __instance.Font.MeasureString("字");
            int X = __instance.X + (int)text.X;
            int Y = __instance.Y + (int)text.Y;
            ModEntry.textbox_h.SetTextExt(X, X + (int)_char.X, Y, Y + (int)_char.Y);
        }
    }
}
