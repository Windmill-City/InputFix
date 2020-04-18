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

            RegCommand(helper);

            tsf = new TSF();
            tsf.AssociateFocus(Game1.game1.Window.Handle);

            textbox_h = new TextBoxHelper(tsf, Game1.game1.Window.Handle);

            HarmonyInstance harmony = HarmonyInstance.Create(base.ModManifest.UniqueID);

            MethodInfo m_HookProc = typeof(KeyboardInput).GetMethod("HookProc", BindingFlags.NonPublic | BindingFlags.Static);
            harmony.Patch(m_HookProc, new HarmonyMethod(typeof(Overrides), "KeyboardInput_HookProc"));

            MethodInfo m_selected = typeof(KeyboardDispatcher).GetMethod("set_Subscriber", BindingFlags.Public | BindingFlags.Instance);
            harmony.Patch(m_selected, null, new HarmonyMethod(typeof(Overrides), "Subscriber_Set"));

            MethodInfo m_text = typeof(TextBox).GetMethod("set_Text", BindingFlags.Public | BindingFlags.Instance);
            harmony.Patch(m_text, null, new HarmonyMethod(typeof(Overrides), "TextBox_Text"));

            MethodInfo m_draw = typeof(TextBox).GetMethod("Draw", BindingFlags.Public | BindingFlags.Instance);
            harmony.Patch(m_draw, new HarmonyMethod(typeof(Overrides), "Draw"));

            MethodInfo m_draw2 = typeof(ChatTextBox).GetMethod("Draw", BindingFlags.Public | BindingFlags.Instance);
            harmony.Patch(m_draw2, new HarmonyMethod(typeof(Overrides), "Draw"));

            MethodInfo m_emoji = typeof(ChatTextBox).GetMethod("receiveEmoji", BindingFlags.Public | BindingFlags.Instance);
            harmony.Patch(m_emoji, new HarmonyMethod(typeof(Overrides), "receiveEmoji"));


            FieldInfo host = typeof(Game).GetField("host", BindingFlags.NonPublic | BindingFlags.Instance);
            Type type = host.GetValue(Game1.game1).GetType();
            MethodInfo m_idle = type.GetMethod("ApplicationIdle", BindingFlags.NonPublic | BindingFlags.Instance);
            harmony.Patch(m_idle, null, new HarmonyMethod(typeof(ModEntry), "HandleMsgFirst"));

            //compatible with ChatCommands
            if (Helper.ModRegistry.Get("cat.chatcommands") != null)
            {
                monitor.Log("Compatible with ChatCommands", LogLevel.Info);
                Compatibility.PatchChatCommands(monitor, harmony);
            }
        }

        private void RegCommand(IModHelper helper)
        {
            helper.ConsoleCommands.Add("animal_textbox", "Open animal naming textbox", new Action<string, string[]>((res1, res2) =>
            {
                if (!Game1.debugMode)
                    return;
                if (Game1.gameMode != 3)
                {
                    monitor.Log("Not In Playing Mode", LogLevel.Error);
                    return;
                }
                FarmAnimal animal = new FarmAnimal();
                Game1.activeClickableMenu = new AnimalQueryMenu(animal);
                monitor.Log("Open Succeed", LogLevel.Info);
            }));
            helper.ConsoleCommands.Add("naming_textbox", "Open normal naming textbox", new Action<string, string[]>((res1, res2) =>
            {
                if (!Game1.debugMode)
                    return;
                if (Game1.gameMode != 3)
                {
                    monitor.Log("Not In Playing Mode", LogLevel.Error);
                    return;
                }
                if (res2.Length > 0)
                    Game1.activeClickableMenu = new NamingMenu(new NamingMenu.doneNamingBehavior(new Action<string>((name) =>
                    {
                        monitor.Log(name, LogLevel.Info);
                        Game1.activeClickableMenu = null;
                    })),
                        Game1.content.LoadString("Strings\\Characters:NameYourHorse"), Game1.content.LoadString("Strings\\Characters:DefaultHorseName"));
                else
                    Game1.activeClickableMenu = new NamingMenu(new NamingMenu.doneNamingBehavior(new Action<string>((name) =>
                    {
                        monitor.Log(name, LogLevel.Info);
                        Game1.activeClickableMenu = null;
                    })), Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1236"), Game1.player.IsMale ? (Game1.player.catPerson ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1794") : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1795")) : (Game1.player.catPerson ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1796") : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1797")));
                monitor.Log("Open Succeed", LogLevel.Info);
            }));
            helper.ConsoleCommands.Add("numsel_textbox", "Open number selection textbox", new Action<string, string[]>((res1, res2) =>
            {
                if (!Game1.debugMode)
                    return;
                if (Game1.gameMode != 3)
                {
                    monitor.Log("Not In Playing Mode", LogLevel.Error);
                    return;
                }
                Game1.activeClickableMenu = new NumberSelectionMenu(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1774"),
                    new NumberSelectionMenu.behaviorOnNumberSelect(new Action<int, int, Farmer>((var1, var2, var3) =>
                    {
                        monitor.Log(var1.ToString(), LogLevel.Info);
                    })), 50, 0, 999, 0);
                monitor.Log("Open Succeed", LogLevel.Info);
            }));
            helper.ConsoleCommands.Add("textbox", "Open textbox", new Action<string, string[]>((res1, res2) =>
            {
                if (!Game1.debugMode)
                    return;
                if (Game1.gameMode != 3)
                {
                    ModEntry.monitor.Log("Not In Playing Mode", LogLevel.Error);
                    return;
                }
                Game1.game1.parseDebugInput("warp AnimalShop");
                Game1.activeClickableMenu = new PurchaseAnimalsMenu(Utility.getPurchaseAnimalStock());
                monitor.Log("Open Succeed", LogLevel.Info);
            }));
            helper.ConsoleCommands.Add("inputfix_debug", "Set debug", new Action<string, string[]>((res1, res2) =>
            {
                Game1.debugMode = !Game1.debugMode;
                Program.releaseBuild = !Game1.debugMode;
                string str = String.Format("Debug:{0}", Game1.debugMode);
                monitor.Log(str, LogLevel.Info);
            }));
        }

        private static void HandleMsgFirst()
        {
            tsf.PumpMsg(Game1.game1.Window.Handle);
        }
    }

    public class TextBoxHelper
    {
        TSF tsf;
        public bool _enable = false;

        public SpriteFont font;
        public int X = 0;
        public int Y = 0;
        public int ACP_Start = 0;
        public int ACP_End = 0;
        public TextBox current;
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
            font = current is ChatTextBox ? ChatBox.messageFont(LocalizedContentManager.CurrentLanguageCode) : current.Font;
            resetAcp();
        }

        public int getTextLen()
        {
            if (current is ChatTextBox)
            {
                int len = 0;
                foreach (ChatSnippet item in ((ChatTextBox)ModEntry.textbox_h.current).finalText)
                {
                    len += item.emojiIndex != -1 ? 1 : item.message.Length;
                }
                return len;
            }
            else
                return current.Text.Length;
        }

        public string getText()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (current is ChatTextBox)
            {
                foreach (ChatSnippet item in ((ChatTextBox)ModEntry.textbox_h.current).finalText)
                {
                    stringBuilder.Append(item.emojiIndex != -1 ? "符" : item.message);
                }
                return stringBuilder.ToString();
            }
            else
                return current.Text;
        }

        public void resetAcp()
        {
            ModEntry.textbox_h.ACP_Start = ModEntry.textbox_h.ACP_End = getTextLen();
            tsf.onTextChange();
            tsf.onSelChange();
        }

        public void SetFont(SpriteFont _font)
        {
            font = _font;
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
            tsf.Deactive();
        }
    }
}
