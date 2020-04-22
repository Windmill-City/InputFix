using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewValley.Menus;
using System;
using System.Threading;

namespace StardewValley
{
	public class KeyboardDispatcher
	{
		private IKeyboardSubscriber _subscriber;
		public KeyboardDispatcher(GameWindow window)
		{
			KeyboardInput.Initialize(window);
			KeyboardInput.CharEntered += this.EventInput_CharEntered;
			KeyboardInput.KeyDown += this.EventInput_KeyDown;
		}

		private void Event_KeyDown(object sender, Keys key)
		{
			if (this._subscriber == null)
			{
				return;
			}
			if (key == Keys.Back)
			{
				this._subscriber.RecieveCommandInput('\b');
			}
			if (key == Keys.Enter)
			{
				this._subscriber.RecieveCommandInput('\r');
			}
			if (key == Keys.Tab)
			{
				this._subscriber.RecieveCommandInput('\t');
			}
			this._subscriber.RecieveSpecialInput(key);
		}

		private void EventInput_KeyDown(object sender, KeyEventArgs e)
		{
			_subscriber?.RecieveSpecialInput(e.KeyCode);
		}

		private void EventInput_CharEntered(object sender, CharacterEventArgs e)
		{
			char ch = e.Character;
			IKeyboardSubscriber subscriber = Game1.keyboardDispatcher.Subscriber;
			if (subscriber != null)
			{
				if (!char.IsControl(ch))
				{
					subscriber.RecieveTextInput(ch);
				}
				else if (ch == '\u0016')//paste
				{
					if (System.Windows.Forms.Clipboard.ContainsText())
					{
						subscriber.RecieveTextInput(System.Windows.Forms.Clipboard.GetText());
					}
				}
				else
				{
					subscriber.RecieveCommandInput(ch);
				}
			}
		}

		public IKeyboardSubscriber Subscriber
		{
			get
			{
				return this._subscriber;
			}
			set
			{
				if (this._subscriber == value)
				{
					return;
				}
				if (this._subscriber != null)
				{
					this._subscriber.Selected = false;
				}
				this._subscriber = value;
				if (this._subscriber != null)
				{
					this._subscriber.Selected = true;
#if TSF
					if (Game1.gameMode != 6)//cant change input state during loading,or the game will struck
						Game1.tsf.SetEnable(_subscriber is ITextBox && ((ITextBox)_subscriber).AllowIME);//set if allow IME composition
					return;
#endif
				}
#if TSF
				else
				{
					if (Game1.gameMode != 6)//cant change input state during loading,or the game will struck
						Game1.tsf.SetEnable(false);
				}
#endif
			}
		}
	}
}
