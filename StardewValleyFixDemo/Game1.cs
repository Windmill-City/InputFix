#define Mono
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Menus;
using System;
using System.Reflection;

namespace StardewValley
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        public static int gameMode = 0;
        public static KeyboardDispatcher keyboardDispatcher;
        public static Game1 game1;
        public static TextBox textBox;
        public static Texture2D staminaRect;
        SpriteFont smallFont;
#if TSF
        public static TSF tsf;
#endif

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            game1 = this;
        }

        protected override void Initialize()
        {
            keyboardDispatcher = new KeyboardDispatcher(this.Window);
#if TSF
            tsf = new TSF();//Need to init at STA Thread
            tsf.AssociateFocus(this.Window.Handle);
            tsf.Active();
            tsf.CreateContext(this.Window.Handle);
            tsf.PushContext();
#endif
#if XNA
            FieldInfo host = typeof(Game).GetField("host", BindingFlags.NonPublic | BindingFlags.Instance);
            Type type = host.GetValue(Game1.game1).GetType();
            MethodInfo m_idle = type.GetMethod("ApplicationIdle", BindingFlags.NonPublic | BindingFlags.Instance);
            Harmony harmony = new Harmony("StardewValley_TSF");
            harmony.Patch(m_idle, null, new HarmonyMethod(typeof(Game1), "HandleMsgFirst"));
#endif
#if Mono
            Type type = Game1.game1.Window.GetType();
            MethodInfo m_idle = type.GetMethod("TickOnIdle", BindingFlags.NonPublic | BindingFlags.Instance);
            Harmony harmony = new Harmony("StardewValley_TSF");
            harmony.Patch(m_idle, null, new HarmonyMethod(typeof(Game1), "HandleMsgFirst"));//need to handle msg first, or the game will struck after IME actived
#endif
            base.Initialize();
        }
#if TSF
        private static void HandleMsgFirst()
        {
            tsf.PumpMsg(Game1.game1.Window.Handle);
        }
#endif

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            smallFont = Content.Load<SpriteFont>("SmallFont.zh-CN");
            staminaRect = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if(textBox == null)
            {
                textBox = new TextBox(smallFont,Color.Black);
                textBox.Width = 800;
                textBox.Height = 100;
                keyboardDispatcher.Subscriber = textBox;
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin();
            textBox.Draw(spriteBatch);
            spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
