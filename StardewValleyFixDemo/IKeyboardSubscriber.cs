using System;
using Microsoft.Xna.Framework.Input;

namespace StardewValley
{
	public interface IKeyboardSubscriber
	{
		void RecieveTextInput(char inputChar);

		void RecieveTextInput(string text);

		void RecieveCommandInput(char command);

		void RecieveSpecialInput(Keys key);

		bool Selected { get; set; }
	}
}
