using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualKeyboard
{
    internal class VirtualKeyboardMenu : IClickableMenu
    {
        private Color Background;
        public VirtualKeyboardMenu(int X, int Y, int Width, int Height) : base(0, 0, Game1.viewport.Width, Game1.viewport.Height)
        {
            this.Background = new Color(0, 0, 0, 0);
        }

        public override void draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, width, height), Background);
            this.drawMouse(spriteBatch);
            base.draw(spriteBatch); 
        }
    }
}
