using Harmony;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Reflection;
using System.Text;

namespace InputFix
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        public static TSF tsf;
        public static IMonitor monitor;
        public static TextBoxHelper textbox_h;
        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            monitor = this.Monitor;
            tsf = new TSF();
            tsf.AssociateFocus(Game1.game1.Window.Handle);

            textbox_h = new TextBoxHelper(tsf, Game1.game1.Window.Handle);

            HarmonyInstance harmony = HarmonyInstance.Create(base.ModManifest.UniqueID);

            MethodInfo m_HookProc = typeof(KeyboardInput).GetMethod("HookProc", BindingFlags.NonPublic | BindingFlags.Static);
            harmony.Patch(m_HookProc, new HarmonyMethod(typeof(Overrides), "KeyboardInput_HookProc"));

            MethodInfo m_selected = typeof(TextBox).GetMethod("set_Selected", BindingFlags.Public | BindingFlags.Instance);
            harmony.Patch(m_selected, null, new HarmonyMethod(typeof(Overrides), "TextBox_Selected"));

            MethodInfo m_text = typeof(TextBox).GetMethod("set_Text", BindingFlags.Public | BindingFlags.Instance);
            harmony.Patch(m_text, null, new HarmonyMethod(typeof(Overrides), "TextBox_Text"));

            MethodInfo m_draw = typeof(Game1).GetMethod("drawOverlays", BindingFlags.NonPublic | BindingFlags.Instance);
            harmony.Patch(m_draw, null, new HarmonyMethod(typeof(Overrides), "DrawComposition"));
        }
    }

    public class TextBoxHelper
    {
        TSF tsf;
        public bool _enable = false;

        SpriteFont font;
        Color textColor;
        int Caret_X = 0;
        int X = 0;
        int Y = 0;
        public TextBox current;

        public StringBuilder text = new StringBuilder(32);
        public TextBoxHelper(TSF _tsf, IntPtr _hWnd)
        {
            tsf = _tsf;
            tsf.Active();
            tsf.CreateContext(_hWnd);
            tsf.PushContext();
        }

        public void SetTextBox(TextBox textBox)
        {
            current = textBox;
            X = current.X;
            Y = current.Y;
            font = current.Font;
            textColor = current.TextColor;
            SetTextExt(X, X + current.Width, Y, Y + current.Height);
            var length = current.Font.MeasureString(current.Text).X;
            SetCaretX((int)length + 16);
        }

        public void SetCaretX(int x)
        {
            Caret_X = x;
            tsf.SetCaretX(x);
        }

        public void SetFont(SpriteFont _font)
        {
            font = _font;
        }

        public void SetTextExt(int left, int right, int top, int bottom)
        {
            tsf.SetTextExt(left, right, top, bottom);
        }

        public void enableInput(bool enable)
        {
            if (enable != _enable)
            {
                _enable = enable;
                tsf.SetEnable(enable);
            }
        }

        public void drawComposition()
        {
            if (_enable && !text.Equals(""))
            {
                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
                Game1.spriteBatch.DrawString(font, text, new Vector2(X + Caret_X + 6f, Y + 12f), Color.Gray);
                Game1.spriteBatch.End();
            }
        }

        ~TextBoxHelper()
        {
            tsf.PopContext();
            tsf.ReleaseContext();
            tsf.Deactive();
        }
    }
}
