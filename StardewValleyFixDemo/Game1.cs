#define XNA

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
        public static LocalizedContentManager content;
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        public static int gameMode = 0;
        public static KeyboardDispatcher keyboardDispatcher;
        public static Game1 game1;
        public static TextBox textBox;
        public static Texture2D staminaRect;
        public static Texture2D chatboxtexture;
        public SpriteFont smallFont;
        public static Options options;
        public static bool lastCursorMotionWasMouse;
        public static InputState input = new InputState();
#if TSF
        public static TSF tsf;
#endif

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.GraphicsProfile = GraphicsProfile.HiDef;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            game1 = this;
        }

        protected override void Initialize()
        {
            keyboardDispatcher = new KeyboardDispatcher(this.Window);
#if TSF
            InitTSF();
#endif
            base.Initialize();
        }

#if TSF

        private void InitTSF()
        {
            tsf = new TSF();//Need to init at STA Thread
            tsf.AssociateFocus(this.Window.Handle);//Window need to create in a STA Thread
            tsf.Active();
            tsf.CreateContext(this.Window.Handle);
            tsf.PushContext();
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
        }

#endif
#if TSF

        private static void HandleMsgFirst()
        {
            tsf.PumpMsg(Game1.game1.Window.Handle);
        }

#endif

        protected internal virtual LocalizedContentManager CreateContentManager(IServiceProvider serviceProvider, string rootDirectory)
        {
            return new LocalizedContentManager(serviceProvider, rootDirectory);
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            content = this.CreateContentManager(base.Content.ServiceProvider, base.Content.RootDirectory);
            chatboxtexture = content.Load<Texture2D>("chatBox");
            smallFont = Content.Load<SpriteFont>("SmallFont.zh-CN");
            ChatBox.emojiTexture = content.Load<Texture2D>("emojis");
            staminaRect = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            Game1.options = new Options();
            Color[] colors = new Color[staminaRect.Width * staminaRect.Height];
            staminaRect.GetData(colors);
            for (int i = 0; i < colors.Length; i++)
            {
                Color color = new Color(255f, 255f, 255f, 1f);
                colors[i] = color;
            }
            staminaRect.SetData(colors);
        }

        protected override void UnloadContent()
        {
        }

        private bool rightmouse = false;

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (textBox == null)
            {
                textBox = new ChatTextBox(chatboxtexture, null, smallFont, Color.White);
                textBox.Width = 896 / 2;
                textBox.Height = 56;
                textBox.Selected = true;
            }
            if (input.GetMouseState().RightButton == ButtonState.Pressed && !rightmouse)
            {
                rightmouse = true;
                var chat = textBox as ChatTextBox;
                chat.receiveEmoji(1);
            }
            if (input.GetMouseState().RightButton == ButtonState.Released)
            {
                rightmouse = false;
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
            textBox.Draw(spriteBatch);
            spriteBatch.End();
            base.Draw(gameTime);
        }

        public static KeyboardState GetKeyboardState()
        {
            KeyboardState keyState = Keyboard.GetState();
            return keyState;
        }

        public static int getMouseX()
        {
            return (int)((float)Game1.input.GetMouseState().X * (1f / Game1.options.zoomLevel));
        }

        public static int getMouseY()
        {
            return (int)((float)Game1.input.GetMouseState().Y * (1f / Game1.options.zoomLevel));
        }

        public static void showTextEntry(StardewValley.Menus.TextBox text_box)
        {
        }

        public static void playSound(string cueName)
        {
        }

        public static void SetFreeCursorDrag()
        {
        }

        internal static void drawDialogueBox(int v1, int v2, int v3, int height, bool v4, bool v5, object p, bool v6, bool v7, int v8, int v9, int v10)
        {
            throw new NotImplementedException();
        }
    }
}