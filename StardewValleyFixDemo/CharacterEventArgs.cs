using Microsoft.Xna.Framework.Input;
using System;

namespace StardewValley
{
    public class CharacterEventArgs : EventArgs
    {
        public CharacterEventArgs(char character, int lParam)
        {
            this.character = character;
            this.lParam = lParam;
        }

        public char Character
        {
            get
            {
                return this.character;
            }
        }

        public int Param
        {
            get
            {
                return this.lParam;
            }
        }

        public int RepeatCount
        {
            get
            {
                return this.lParam & 65535;
            }
        }

        public bool ExtendedKey
        {
            get
            {
                return (this.lParam & 16777216) > 0;
            }
        }

        public bool AltPressed
        {
            get
            {
                return (this.lParam & 536870912) > 0;
            }
        }

        public bool PreviousState
        {
            get
            {
                return (this.lParam & 1073741824) > 0;
            }
        }

        public bool TransitionState
        {
            get
            {
                return (this.lParam & int.MinValue) > 0;
            }
        }

        private readonly char character;

        private readonly int lParam;
    }
    public class KeyEventArgs : EventArgs
    {
        // Token: 0x0600046E RID: 1134 RVA: 0x0005A9E0 File Offset: 0x00058BE0
        public KeyEventArgs(Keys keyCode)
        {
            this.keyCode = keyCode;
        }

        // Token: 0x17000095 RID: 149
        // (get) Token: 0x0600046F RID: 1135 RVA: 0x0005A9EF File Offset: 0x00058BEF
        public Keys KeyCode
        {
            get
            {
                return this.keyCode;
            }
        }

        // Token: 0x0400038F RID: 911
        private Keys keyCode;
    }
}
