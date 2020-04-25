using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewValley.Menus;
using System;
using System.Runtime.InteropServices;

namespace StardewValley
{
    public delegate void KeyEventHandler(object sender, KeyEventArgs e);
    public delegate void CharEnteredHandler(object sender, CharacterEventArgs e);
    public static class KeyboardInput
    {
        public static event CharEnteredHandler CharEntered;

        public static event KeyEventHandler KeyDown;

        public static event KeyEventHandler KeyUp;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        internal static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, ref RECT pt, int cPoints);

        private const int TF_UNLOCKED = 0x060F;
        private const int TF_LOCKED = 0x0606;
        private const int TF_GETTEXTLENGTH = 0x060E;
        private const int TF_GETTEXT = 0x060D;
        private const int TF_CLEARTEXT = 0x060C;
        private const int TF_GETTEXTEXT = 0x060B;
        private const int TF_QUERYINSERT = 0x060A;
        private const int TF_GETSELSTATE = 0x0609;

        private const int EM_REPLACESEL = 0x00C2;
        private const int EM_SETSEL = 0x00B1;
        private const int EM_GETSEL = 0x00B0;

        private const int WM_KILLFOCUS = 0x008;

        private const int WM_KEYDOWN = 0x100;
        private const int WM_KEYUP = 0x101;
        private const int WM_CHAR = 0x102;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_MOUSELEAVE = 0x02A3;


        private const int DLGC_WANTALLKEYS = 4;
        private const int WM_GETDLGCODE = 135;
        private const int GWL_WNDPROC = -4;
        public static void Initialize(GameWindow window)
        {
            if (KeyboardInput.initialized)
            {
                throw new InvalidOperationException("KeyboardInput.Initialize can only be called once!");
            }
            KeyboardInput.hookProcDelegate = new KeyboardInput.WndProc(KeyboardInput.HookProc);
            KeyboardInput.prevWndProc = (IntPtr)KeyboardInput.SetWindowLong(window.Handle, GWL_WNDPROC, (int)Marshal.GetFunctionPointerForDelegate(KeyboardInput.hookProcDelegate));
            KeyboardInput.initialized = true;
        }

        static RECT MouseSelection = new RECT();
        static bool Selecting = false;

        private static IntPtr HookProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            IntPtr returnCode = KeyboardInput.CallWindowProc(KeyboardInput.prevWndProc, hWnd, msg, wParam, lParam); ;
            switch (msg)
            {
                case WM_GETDLGCODE:
                    returnCode = (IntPtr)DLGC_WANTALLKEYS;
                    break;
                case WM_CHAR:
                    CharEntered?.Invoke(null, new CharacterEventArgs((char)wParam, (int)lParam));
                    break;
                case WM_KEYDOWN:
                    KeyDown?.Invoke(null, new KeyEventArgs((Keys)wParam));
                    break;
                case WM_KEYUP:
                    KeyUp?.Invoke(null, new KeyEventArgs((Keys)wParam));
                    break;
                case WM_LBUTTONDOWN:
                    MouseSelection.left = (int)lParam & 0xffff;
                    MouseSelection.top = (int)lParam >> 16;
                    MouseSelection.right = MouseSelection.left;
                    MouseSelection.bottom = MouseSelection.top;
                    if (Game1.keyboardDispatcher.Subscriber is ITextBox)
                    {
                        ITextBox textBox = Game1.keyboardDispatcher.Subscriber as ITextBox;
                        Acp acp = textBox.GetAcpByRange(MouseSelection);
                        if (acp.Start >= 0)
                        {
                            textBox.SetSelection(acp.Start, acp.End);
                            Console.WriteLine("ACPStart:{0},ACPEnd:{1}", acp.Start, acp.End);
                            Console.WriteLine("MouseDown:Left:{0},TOP:{1}RIGHT:{2}BOTTOM:{3}", MouseSelection.left, MouseSelection.top, MouseSelection.right, MouseSelection.bottom);
                            Selecting = true;
                        }
                    }
                    break;
                case WM_MOUSEMOVE:
                    MouseSelection.right = (int)lParam & 0xffff;
                    MouseSelection.bottom = (int)lParam >> 16;
                    if (Selecting && Game1.keyboardDispatcher.Subscriber is ITextBox)
                    {
                        RECT range = new RECT();
                        range.left = Math.Min(MouseSelection.left, MouseSelection.right);
                        range.top = Math.Max(MouseSelection.top, MouseSelection.bottom);
                        range.right = Math.Max(MouseSelection.left, MouseSelection.right);
                        range.bottom = Math.Min(MouseSelection.top, MouseSelection.bottom);
                        ITextBox textBox = Game1.keyboardDispatcher.Subscriber as ITextBox;
                        Acp acp = textBox.GetAcpByRange(range);
                        if (acp.Start >= 0)
                        {
                            textBox.SetSelection(acp.Start, acp.End);
                            textBox.SetSelState(MouseSelection.left > MouseSelection.right ? SelState.SEL_AE_END : SelState.SEL_AE_START);
                            Console.WriteLine("ACPStart:{0},ACPEnd:{1}", acp.Start, acp.End);
                            Console.WriteLine("MouseMove:Left:{0},TOP:{1}RIGHT:{2}BOTTOM:{3}", MouseSelection.left, MouseSelection.top, MouseSelection.right, MouseSelection.bottom);
                        }
                    }
                    //handle IsMouseVisable
                    returnCode = KeyboardInput.CallWindowProc(KeyboardInput.prevWndProc, hWnd, msg, wParam, lParam);
                    break;
                case WM_LBUTTONUP:
                    Selecting = false;
                    break;
#if TSF
                case EM_GETSEL:
                    if (Game1.keyboardDispatcher.Subscriber is ITextBox)
                    {
                        ITextBox textBox = Game1.keyboardDispatcher.Subscriber as ITextBox;
                        Acp acp = textBox.GetSelection();
                        Marshal.WriteInt32(wParam, acp.Start);
                        Marshal.WriteInt32(lParam, acp.End);
                    }
                    break;
                case EM_SETSEL:
                    if (Game1.keyboardDispatcher.Subscriber is ITextBox)
                    {
                        ITextBox textBox = Game1.keyboardDispatcher.Subscriber as ITextBox;
                        textBox.SetSelection((int)wParam, (int)lParam);
                    }
                    break;
                case EM_REPLACESEL:
                    if (Game1.keyboardDispatcher.Subscriber is ITextBox)
                    {
                        ITextBox textBox = Game1.keyboardDispatcher.Subscriber as ITextBox;
                        textBox.ReplaceSelection(Marshal.PtrToStringAuto(lParam));
                    }
                    break;
                case TF_GETSELSTATE:
                    if (Game1.keyboardDispatcher.Subscriber is ITextBox)
                    {
                        ITextBox textBox = Game1.keyboardDispatcher.Subscriber as ITextBox;
                        returnCode = (IntPtr)textBox.GetSelState();
                    }
                    break;
                case TF_GETTEXT:
                    if (Game1.keyboardDispatcher.Subscriber is ITextBox)
                    {
                        ITextBox textBox = Game1.keyboardDispatcher.Subscriber as ITextBox;
                        var text = textBox.GetText();
                        Marshal.Copy(text.ToCharArray(), 0, wParam, Math.Min(text.Length, (int)lParam));
                    }
                    break;
                case TF_GETTEXTLENGTH:
                    if (Game1.keyboardDispatcher.Subscriber is ITextBox)
                    {
                        ITextBox textBox = Game1.keyboardDispatcher.Subscriber as ITextBox;
                        returnCode = (IntPtr)textBox.GetTextLength();
                    }
                    break;
                case TF_GETTEXTEXT:
                    if (Game1.keyboardDispatcher.Subscriber is ITextBox)
                    {
                        ITextBox textBox = Game1.keyboardDispatcher.Subscriber as ITextBox;
                        Acp acp = (Acp)Marshal.PtrToStructure(lParam, typeof(Acp));
                        RECT rect = textBox.GetTextExt(acp);
                        MapWindowPoints(Game1.game1.Window.Handle, (IntPtr)0, ref rect, 2);//to screen coord
                        Marshal.StructureToPtr(rect, wParam, false);//text ext

                        returnCode = (IntPtr)0;//if the rect clipped
                    }
                    break;
                case TF_QUERYINSERT:
                    if (Game1.keyboardDispatcher.Subscriber is ITextBox)
                    {
                        ITextBox textBox = Game1.keyboardDispatcher.Subscriber as ITextBox;
                        Acp acp = (Acp)Marshal.PtrToStructure(wParam, typeof(Acp));
                        textBox.QueryInsert(acp, (uint)lParam);
                        Marshal.StructureToPtr(acp, wParam, false);
                    }
                    break;
                case WM_KILLFOCUS:
                    Game1.tsf.TerminateComposition();
                    break;
#endif
                default:
                    break;
            }
            return returnCode;
        }

        private static bool initialized;

        private static IntPtr prevWndProc;

        private static KeyboardInput.WndProc hookProcDelegate;

        private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    }
}
