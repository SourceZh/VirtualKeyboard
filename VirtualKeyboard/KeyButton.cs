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
        public bool Deleted;
        public bool Hidden;
        public bool EditButton;
        public Rectangle OutterBounds;
        private Rectangle InnerBounds;
        private readonly SButton ButtonKey;
        private readonly float Transparency;
        private string Alias;
        private readonly IModHelper Helper;
        private readonly float ButtonScale;
        private int AboveMenu;
        private Rectangle CloseButtonBounds;
        private ModEntry ModEntry;

        public KeyButton(ModEntry modEntry, IModHelper helper, ModConfig.VirtualButton buttonDefine, int aboveMenu)
        {
            this.ModEntry = modEntry;
            this.Deleted = false;
            this.Hidden = true;
            this.EditButton = false;
            this.ButtonKey = buttonDefine.key;
            this.Alias = buttonDefine.alias != "" ? buttonDefine.alias : this.ButtonKey.ToString();
            this.Helper = helper;

            this.ButtonScale = Helper.ReadConfig<ModConfig>().ButtonScale;
            this.AboveMenu = aboveMenu;
 
            helper.Events.Display.Rendered += this.OnRendered;
            helper.Events.Display.RenderedActiveMenu += this.OnRenderedActiveMenu;
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

            int CloseButtonSize = 20;
            this.CloseButtonBounds.X = this.OutterBounds.X + InnerBounds.Width - CloseButtonSize / 2;
            this.CloseButtonBounds.Y = this.OutterBounds.Y - CloseButtonSize / 2;
            this.CloseButtonBounds.Width = CloseButtonSize;
            this.CloseButtonBounds.Height = CloseButtonSize;

            return true;
        }

        private bool ShouldTrigger(Vector2 screenPixels, Rectangle bound)
        {
            if (!bound.Contains(screenPixels.X, screenPixels.Y))
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
            if (Deleted)
                return;
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;
            if (this.Hidden)
                return;
            Vector2 screenPixels = Utility.ModifyCoordinatesForUIScale(e.Cursor.ScreenPixels);

            if (this.EditButton)
            {
                if (ShouldTrigger(screenPixels, this.CloseButtonBounds))
                {
                    this.Deleted = true;
                    ModEntry.UpdateAllButtons();
                }
            }
            else
            {
                if (ShouldTrigger(screenPixels, this.OutterBounds))
                {
                    ButtonPressed();
                }
            }
        }

        private Rectangle CalBoundFromUIScale(Rectangle bound)
        {
            Rectangle CalBound;
            Vector2 UIScalePos = Utility.ModifyCoordinatesFromUIScale(new Vector2(bound.X, bound.Y));
            CalBound.X = (int)UIScalePos.X;
            CalBound.Y = (int)UIScalePos.Y;
            CalBound.Height = (int)Utility.ModifyCoordinateFromUIScale(bound.Height);
            CalBound.Width = (int)Utility.ModifyCoordinateFromUIScale(bound.Width);
            return CalBound;
        }

        public virtual void OnRenderedCloseButton(RenderedEventArgs e)
        {
            if (!this.EditButton)
                return;
            Rectangle UIScaleCloseButtonBoundsRectangle = CalBoundFromUIScale(this.CloseButtonBounds);
            e.SpriteBatch.Draw(Game1.mouseCursors, UIScaleCloseButtonBoundsRectangle, new Rectangle(337, 494, 12, 12), Color.White, 0, new Vector2(0, 0), SpriteEffects.None, 1E-06f);
        }

        private void OnRendered(object? sender, RenderedEventArgs e)
        {
            if (this.AboveMenu != 0)
                return;
            if (Deleted)
                return;
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;
            if (this.Hidden)
                return;

            //e.SpriteBatch.Draw(Game1.menuTexture, OutterBounds, new Rectangle(0, 256, 60, 60), Color.White);
            Rectangle UIScaleOutterBoundsRectangle = CalBoundFromUIScale(this.OutterBounds);
            e.SpriteBatch.Draw(Game1.menuTexture, UIScaleOutterBoundsRectangle, new Rectangle(0, 256, 60, 60), Color.White, 0, new Vector2(0, 0), SpriteEffects.None, 1E-06f);

            //e.SpriteBatch.DrawString(Game1.smallFont, this.Alias, new Vector2(this.InnerBounds.X, this.InnerBounds.Y), Game1.textColor);
            float UIScale = Utility.ModifyCoordinateFromUIScale(this.ButtonScale);
            Vector2 UIScaleInnerBounds = Utility.ModifyCoordinatesFromUIScale(new Vector2(this.InnerBounds.X, this.InnerBounds.Y));
            e.SpriteBatch.DrawString(Game1.smallFont, this.Alias, UIScaleInnerBounds, Game1.textColor, 0, new Vector2(0, 0), UIScale, SpriteEffects.None, 1E-06f);

            OnRenderedCloseButton(e);
        }

        public virtual void OnRenderedActiveMenuCloseButton(RenderedActiveMenuEventArgs e)
        {
            if (!this.EditButton)
                return;
            Rectangle UIScaleCloseButtonBoundsRectangle = this.CloseButtonBounds;
            e.SpriteBatch.Draw(Game1.mouseCursors, UIScaleCloseButtonBoundsRectangle, new Rectangle(337, 494, 12, 12), Color.White, 0, new Vector2(0, 0), SpriteEffects.None, 1E-06f);
        }

        private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
        {
            if (this.AboveMenu == 0)
                return;
            if (Deleted)
                return;
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;
            if (this.Hidden)
                return;

            //e.SpriteBatch.Draw(Game1.menuTexture, OutterBounds, new Rectangle(0, 256, 60, 60), Color.White);
            Rectangle UIScaleOutterBoundsRectangle = this.OutterBounds;
            e.SpriteBatch.Draw(Game1.menuTexture, UIScaleOutterBoundsRectangle, new Rectangle(0, 256, 60, 60), Color.White, 0, new Vector2(0, 0), SpriteEffects.None, 1E-06f);

            //e.SpriteBatch.DrawString(Game1.smallFont, this.Alias, new Vector2(this.InnerBounds.X, this.InnerBounds.Y), Game1.textColor);
            float UIScale = this.ButtonScale;
            Vector2 UIScaleInnerBounds = new Vector2(this.InnerBounds.X, this.InnerBounds.Y);
            e.SpriteBatch.DrawString(Game1.smallFont, this.Alias, UIScaleInnerBounds, Game1.textColor, 0, new Vector2(0, 0), UIScale, SpriteEffects.None, 1E-06f);

            OnRenderedActiveMenuCloseButton(e);
        }
    }
}
