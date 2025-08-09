using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewModdingAPI;
using StardewValley;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;

namespace VirtualKeyboard
{
    internal class KeyButton
    {
        public const int ButtonBorderWidth = 4;
        public Rectangle OutterBounds;
        private Rectangle InnerBounds;
        private readonly SButton ButtonKey;
        private readonly float Transparency;
        private string Alias;
        private readonly IModHelper Helper;
        private readonly float ButtonScale;
        public bool Hidden;
        public int AboveMenu;

        public KeyButton(IModHelper helper, ModConfig.VirtualButton buttonDefine, int AboveMenu)
        {
            this.Hidden = true;
            this.ButtonKey = buttonDefine.key;
            this.Alias = buttonDefine.alias != "" ? buttonDefine.alias : this.ButtonKey.ToString();
            this.Helper = helper;

            this.ButtonScale = Helper.ReadConfig<ModConfig>().ButtonScale;
            this.AboveMenu = AboveMenu;
            if (this.AboveMenu == 0)
            {
                helper.Events.Display.Rendered += this.OnRendered;
            }
            else
            {
                helper.Events.Display.RenderedActiveMenu += this.OnRenderedActiveMenu;
            }
            helper.Events.Input.ButtonPressed += this.EventInputButtonPressed;
        }

        public bool CalcBounds(int x, int y)
        {
            this.OutterBounds.X = x;
            this.OutterBounds.Y = y;

            if (Game1.smallFont == null)
            {
                return false;
            }

            Vector2 bounds = Game1.smallFont.MeasureString(this.Alias);
            while (bounds.X < bounds.Y)
            {
                string padding_alias = " " + this.Alias + " ";
                Vector2 padding_bounds = Game1.smallFont.MeasureString(padding_alias);
                if (padding_bounds.X < padding_bounds.Y)
                {
                    this.Alias = padding_alias;
                    bounds = padding_bounds;
                }
                else
                {
                    break;
                }
            }

            this.InnerBounds.X = OutterBounds.X + ButtonBorderWidth;
            this.InnerBounds.Y = OutterBounds.Y + ButtonBorderWidth;
            this.InnerBounds.Width = (int)(bounds.X * this.ButtonScale) + 1;
            this.InnerBounds.Height = (int)(bounds.Y * this.ButtonScale) + 1;

            this.OutterBounds.Width = InnerBounds.Width + ButtonBorderWidth * 2;
            this.OutterBounds.Height = InnerBounds.Height + ButtonBorderWidth * 2;
            return true;
        }
        private bool ShouldTrigger(Vector2 screenPixels, SButton button)
        {
            if (!this.OutterBounds.Contains(screenPixels.X, screenPixels.Y) || this.Hidden)
                return false;
            return true;
        }

        public virtual void ButtonPressed()
        {
            MethodInfo? overrideButton = Game1.input.GetType().GetMethod("OverrideButton");
            if (overrideButton != null)
            {
                overrideButton.Invoke(Game1.input, new object[] { ButtonKey, true });
                this.Helper.Input.Suppress(SButton.MouseLeft);
            }
        }
        private void EventInputButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;
            if (this.Hidden)
                return;
            Vector2 screenPixels = Utility.ModifyCoordinatesForUIScale(e.Cursor.ScreenPixels);
            if (ShouldTrigger(screenPixels, e.Button))
            {
                ButtonPressed();
            }
        }

        private void OnRendered(object? sender, RenderedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;
            if (this.Hidden)
                return;

            //e.SpriteBatch.Draw(Game1.menuTexture, OutterBounds, new Rectangle(0, 256, 60, 60), Color.White);
            Vector2 UIScaleOutterBounds = Utility.ModifyCoordinatesFromUIScale(new Vector2(this.OutterBounds.X, this.OutterBounds.Y));
            Rectangle UIScaleOutterBoundsRectangle;
            UIScaleOutterBoundsRectangle.X = (int)UIScaleOutterBounds.X;
            UIScaleOutterBoundsRectangle.Y = (int)UIScaleOutterBounds.Y;
            UIScaleOutterBoundsRectangle.Height = (int)Utility.ModifyCoordinateFromUIScale(OutterBounds.Height);
            UIScaleOutterBoundsRectangle.Width = (int)Utility.ModifyCoordinateFromUIScale(OutterBounds.Width);
            e.SpriteBatch.Draw(Game1.menuTexture, UIScaleOutterBoundsRectangle, new Rectangle(0, 256, 60, 60), Color.White, 0, new Vector2(0, 0), SpriteEffects.None, 1E-06f);

            //e.SpriteBatch.DrawString(Game1.smallFont, this.Alias, new Vector2(this.InnerBounds.X, this.InnerBounds.Y), Game1.textColor);
            float UIScale = Utility.ModifyCoordinateFromUIScale(this.ButtonScale);
            Vector2 UIScaleInnerBounds = Utility.ModifyCoordinatesFromUIScale(new Vector2(this.InnerBounds.X, this.InnerBounds.Y));
            e.SpriteBatch.DrawString(Game1.smallFont, this.Alias, UIScaleInnerBounds, Game1.textColor, 0, new Vector2(0, 0), UIScale, SpriteEffects.None, 1E-06f);
        }

        private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;
            if (this.Hidden)
                return;

            //e.SpriteBatch.Draw(Game1.menuTexture, OutterBounds, new Rectangle(0, 256, 60, 60), Color.White);
            Rectangle UIScaleOutterBoundsRectangle = OutterBounds;
            e.SpriteBatch.Draw(Game1.menuTexture, UIScaleOutterBoundsRectangle, new Rectangle(0, 256, 60, 60), Color.White, 0, new Vector2(0, 0), SpriteEffects.None, 1E-06f);

            //e.SpriteBatch.DrawString(Game1.smallFont, this.Alias, new Vector2(this.InnerBounds.X, this.InnerBounds.Y), Game1.textColor);
            float UIScale = this.ButtonScale;
            Vector2 UIScaleInnerBounds = new Vector2(this.InnerBounds.X, this.InnerBounds.Y);
            e.SpriteBatch.DrawString(Game1.smallFont, this.Alias, UIScaleInnerBounds, Game1.textColor, 0, new Vector2(0, 0), UIScale, SpriteEffects.None, 1E-06f);
        }
    }
}
