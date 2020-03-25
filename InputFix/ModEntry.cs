using Harmony;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Reflection;

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
        }
    }

    public class TextBoxHelper
    {
        TSF tsf;
        bool _enable = false;
        public TextBoxHelper(TSF _tsf, IntPtr _hWnd)
        {
            tsf = _tsf;
            tsf.CreateContext(_hWnd);
            tsf.PushContext();
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

        ~TextBoxHelper()
        {
            tsf.PopContext();
            tsf.ReleaseContext();
        }
    }
}
