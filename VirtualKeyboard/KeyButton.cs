using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewModdingAPI;
using StardewValley;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace VirtualKeyboard
{
    internal class KeyButton
    {
        public const int PixelBorderSize = 12;
        public bool Deleted;
        public bool Hidden;
        public bool EditButton;
        public Rectangle TextBounds;
        public SButton ButtonKey;
        private readonly float Transparency;
        public string Alias;
        private string PaddingAlias;
        private readonly IModHelper Helper;
        private readonly float ButtonScale;
        private int AboveMenu;
        private Rectangle CloseButtonBounds;
        private ModEntry ModEntry;
        private Vector2 MouseOffset = new Vector2(0);
        private bool SelectButton = false;
        private int LastPressTick = 0;
        private Rectangle BeforeOutterBounds = new Rectangle(0, 0, 0, 0);

        public KeyButton(ModEntry modEntry, IModHelper helper, ModConfig.VirtualButton buttonDefine, int aboveMenu)
        {
            this.ModEntry = modEntry;
            this.Deleted = false;
            this.Hidden = true;
            this.EditButton = false;
            this.ButtonKey = buttonDefine.key;
            this.Alias = buttonDefine.alias != "" ? buttonDefine.alias : this.ButtonKey.ToString();
            this.PaddingAlias = this.Alias;
            this.TextBounds.X = buttonDefine.pos.X;
            this.TextBounds.Y = buttonDefine.pos.Y;
            this.Helper = helper;

            this.ButtonScale = Helper.ReadConfig<ModConfig>().ButtonScale;
            this.AboveMenu = aboveMenu;
 
            helper.Events.Display.Rendered += this.OnRendered;
            helper.Events.Display.RenderedActiveMenu += this.OnRenderedActiveMenu;
            helper.Events.Input.ButtonPressed += this.EventInputButtonPressed;
            helper.Events.Input.ButtonReleased += this.EventInputButtonReleased;
            helper.Events.Input.CursorMoved += this.OnCursorMoved;
        }

        public bool CalcBounds(int x, int y)
        {
            this.TextBounds.X = x;
            this.TextBounds.Y = y;

            if (Game1.smallFont == null)
            {
                return false;
            }

            Vector2 textSize = Game1.smallFont.MeasureString(this.PaddingAlias);
            while (textSize.X < textSize.Y)
            {
                string paddingAlias = " " + this.PaddingAlias + " ";
                Vector2 paddingBounds = Game1.smallFont.MeasureString(paddingAlias);
                if (paddingBounds.X < paddingBounds.Y)
                {
                    this.PaddingAlias = paddingAlias;
                    textSize = paddingBounds;
                }
                else
                {
                    break;
                }
            }

            this.TextBounds.Width = (int)(textSize.X * this.ButtonScale) + 1;
            this.TextBounds.Height = (int)(textSize.Y * this.ButtonScale) + 1;

            int CloseButtonSize = 20;
            this.CloseButtonBounds.X = this.TextBounds.X + TextBounds.Width + PixelBorderSize / 2 - CloseButtonSize / 2;
            this.CloseButtonBounds.Y = this.TextBounds.Y - PixelBorderSize / 2 - CloseButtonSize / 2;
            this.CloseButtonBounds.Width = CloseButtonSize;
            this.CloseButtonBounds.Height = CloseButtonSize;

            return true;
        }

        public bool IsShowButton()
        {
            if (Deleted)
                return false;
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return false;
            if (this.Hidden)
                return false;
            return true;
        }

        private bool ShouldTrigger(Vector2 screenPixels, Rectangle bound)
        {
            Rectangle checkBound = bound;
            checkBound.X += this.ModEntry.ToolbarOffset.X;
            checkBound.Y += this.ModEntry.ToolbarOffset.Y;
            int ticks = Game1.ticks;
            if (ticks - this.LastPressTick <= 6 || !checkBound.Contains(screenPixels.X, screenPixels.Y))
                return false;

            LastPressTick = ticks;
            MouseOffset.X = screenPixels.X - bound.X;
            MouseOffset.Y = screenPixels.Y - bound.Y;
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
            if (e.Button != SButton.MouseLeft)
                return;
            if (!IsShowButton())
                return;
            Vector2 screenPixels = Utility.ModifyCoordinatesForUIScale(e.Cursor.ScreenPixels);

            if (this.EditButton)
            {
                // delete button
                if (ShouldTrigger(screenPixels, this.CloseButtonBounds))
                {
                    this.Deleted = true;
                    this.ModEntry.UpdateAllButtons();
                    this.Helper.Input.Suppress(SButton.MouseLeft);
                }
                else
                {
                    // move button
                    this.SelectButton = ShouldTrigger(screenPixels, this.TextBounds);
                    if (this.SelectButton) {
                        this.BeforeOutterBounds.X = this.TextBounds.X;
                        this.BeforeOutterBounds.Y = this.TextBounds.Y;
                    }
                    this.ModEntry.Monitor.Log("EventInputButtonPressed" + Alias, LogLevel.Debug);
                }
            }
            else
            {
                if (ShouldTrigger(screenPixels, this.TextBounds))
                {
                    ButtonPressed();
                }
            }
        }

        public void ChangeButtonValue()
        {
            if (Constants.TargetPlatform != GamePlatform.Android)
                return;
            
            this.ModEntry.Monitor.Log("EventInputButtonReleased Android", LogLevel.Debug);
            Assembly monoGameAssembly = Assembly.Load("MonoGame.Framework");
            Type? keyboardInputType = monoGameAssembly.GetType("Microsoft.Xna.Framework.Input.KeyboardInput");
            if (keyboardInputType == null) return;
            this.ModEntry.Monitor.Log("EventInputButtonReleased keyboardInputType", LogLevel.Debug);
            MethodInfo? showAndroidKeyborad = keyboardInputType.GetMethod("Show", BindingFlags.Public | BindingFlags.Static);
            if (showAndroidKeyborad == null) return;
            this.ModEntry.Monitor.Log("EventInputButtonReleased showAndroidKeyborad", LogLevel.Debug);
            Task<string>? key_task = showAndroidKeyborad.Invoke(null, new object[] { "Key", "Enum Keys", this.ButtonKey.ToString(), false }) as Task<string>;
            if (key_task == null) return;
            this.ModEntry.Monitor.Log("EventInputButtonReleased Invoke task", LogLevel.Debug);
            key_task.ContinueWith(s =>
            {
                if (Enum.TryParse(s.Result, ignoreCase: true, out SButton buttonKey))
                {
                    this.ButtonKey = buttonKey;
                    this.Alias = this.ButtonKey.ToString();
                    this.PaddingAlias = this.Alias;
                    Task<string>? descript_task = showAndroidKeyborad.Invoke(null, new object[] { "Description", "description", Alias, false }) as Task<string>;
                    descript_task.ContinueWith(ss => {
                        this.Alias = ss.Result;
                        this.PaddingAlias = Alias;
                        CalcBounds(this.TextBounds.X, this.TextBounds.Y);
                        ModEntry.UpdateAllButtons();
                    });
                }
            });
        }

        private void EventInputButtonReleased(object? sender, ButtonReleasedEventArgs e)
        {
            if (e.Button != SButton.MouseLeft)
                return;
            if (!IsShowButton())
                return;
            if (!this.EditButton)
                return;
            if (!this.SelectButton)
                return;
            this.ModEntry.Monitor.Log("EventInputButtonReleased" + Alias, LogLevel.Debug);
            this.SelectButton = false;
            int ticks = Game1.ticks;
            if (ticks - this.LastPressTick > 24 && this.BeforeOutterBounds.X == this.TextBounds.X && this.BeforeOutterBounds.Y == this.TextBounds.Y)
            {
                ChangeButtonValue();
            }
            else
            {
                this.ModEntry.UpdateAllButtons();
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
            Rectangle offsetCloseButtonBounds = this.CloseButtonBounds;
            offsetCloseButtonBounds.X += this.ModEntry.ToolbarOffset.X;
            offsetCloseButtonBounds.Y += this.ModEntry.ToolbarOffset.Y;
            Rectangle UIScaleCloseButtonBoundsRectangle = CalBoundFromUIScale(offsetCloseButtonBounds);
            e.SpriteBatch.Draw(Game1.mouseCursors, UIScaleCloseButtonBoundsRectangle, new Rectangle(337, 494, 12, 12), Color.White, 0, new Vector2(0, 0), SpriteEffects.None, 1E-06f);
        }

        private void OnRendered(object? sender, RenderedEventArgs e)
        {
            if (this.AboveMenu != 0)
                return;
            if (!IsShowButton())
                return;

            //e.SpriteBatch.Draw(Game1.menuTexture, OutterBounds, new Rectangle(0, 256, 60, 60), Color.White);
            Rectangle offsetOutterBounds = this.TextBounds;
            offsetOutterBounds.X += this.ModEntry.ToolbarOffset.X;
            offsetOutterBounds.Y += this.ModEntry.ToolbarOffset.Y;
            Rectangle UIScaleOutterBoundsRectangle = CalBoundFromUIScale(offsetOutterBounds);
            //e.SpriteBatch.Draw(Game1.menuTexture, UIScaleOutterBoundsRectangle, new Rectangle(0, 256, 60, 60), Color.White, 0, new Vector2(0, 0), SpriteEffects.None, 1E-06f);
            OnRenderedDrawOutterBound(e, UIScaleOutterBoundsRectangle);

            //e.SpriteBatch.DrawString(Game1.smallFont, this.Alias, new Vector2(this.InnerBounds.X, this.InnerBounds.Y), Game1.textColor);
            float UIScale = Utility.ModifyCoordinateFromUIScale(this.ButtonScale);
            Vector2 offsetInnerBounds = new Vector2(this.ModEntry.ToolbarOffset.X + this.TextBounds.X, this.ModEntry.ToolbarOffset.Y + this.TextBounds.Y);
            Vector2 UIScaleInnerBounds = Utility.ModifyCoordinatesFromUIScale(offsetInnerBounds);
            e.SpriteBatch.DrawString(Game1.smallFont, this.PaddingAlias, UIScaleInnerBounds, Game1.textColor, 0, new Vector2(0, 0), UIScale, SpriteEffects.None, 1E-06f);

            OnRenderedCloseButton(e);
        }

        public virtual void OnRenderedActiveMenuCloseButton(RenderedActiveMenuEventArgs e)
        {
            if (!this.EditButton)
                return;
            Rectangle UIScaleCloseButtonBoundsRectangle = this.CloseButtonBounds;
            UIScaleCloseButtonBoundsRectangle.X += this.ModEntry.ToolbarOffset.X;
            UIScaleCloseButtonBoundsRectangle.Y += this.ModEntry.ToolbarOffset.Y;
            e.SpriteBatch.Draw(Game1.mouseCursors, UIScaleCloseButtonBoundsRectangle, new Rectangle(337, 494, 12, 12), Color.White, 0, new Vector2(0, 0), SpriteEffects.None, 1E-06f);
        }

        private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
        {
            if (this.AboveMenu == 0)
                return;
            if (!IsShowButton())
                return;

            //e.SpriteBatch.Draw(Game1.menuTexture, OutterBounds, new Rectangle(0, 256, 60, 60), Color.White);
            Rectangle UIScaleOutterBoundsRectangle = this.TextBounds;
            UIScaleOutterBoundsRectangle.X += this.ModEntry.ToolbarOffset.X;
            UIScaleOutterBoundsRectangle.Y += this.ModEntry.ToolbarOffset.Y;
            //e.SpriteBatch.Draw(Game1.menuTexture, UIScaleOutterBoundsRectangle, new Rectangle(0, 256, 60, 60), Color.White, 0, new Vector2(0, 0), SpriteEffects.None, 1E-06f);
            OnRenderedActiveMenuDrawOutterBound(e, UIScaleOutterBoundsRectangle);

            //e.SpriteBatch.DrawString(Game1.smallFont, this.Alias, new Vector2(this.InnerBounds.X, this.InnerBounds.Y), Game1.textColor);
            float UIScale = this.ButtonScale;
            Vector2 UIScaleInnerBounds = new Vector2(this.ModEntry.ToolbarOffset.X + this.TextBounds.X, this.ModEntry.ToolbarOffset.Y + this.TextBounds.Y);
            e.SpriteBatch.DrawString(Game1.smallFont, this.PaddingAlias, UIScaleInnerBounds, Game1.textColor, 0, new Vector2(0, 0), UIScale, SpriteEffects.None, 1E-06f);

            OnRenderedActiveMenuCloseButton(e);
        }

        private void OnCursorMoved(object? sender, CursorMovedEventArgs e)
        {
            if (!IsShowButton())
                return;
            if (!this.EditButton)
                return;
            if (!this.SelectButton)
                return;
            Vector2 screenPixels = Utility.ModifyCoordinatesForUIScale(e.NewPosition.ScreenPixels);
            this.TextBounds.X = (int)(screenPixels.X - MouseOffset.X);
            this.TextBounds.Y = (int)(screenPixels.Y - MouseOffset.Y);
            CalcBounds(this.TextBounds.X, this.TextBounds.Y);
        }

        private void OnRenderedDrawOutterBound(RenderedEventArgs e, Rectangle BoundsRectangle)
        {
            int BorderSize = (int)Utility.ModifyCoordinateFromUIScale(PixelBorderSize);

            Rectangle OuterBorderRect = new Rectangle(
                BoundsRectangle.X - BorderSize,
                BoundsRectangle.Y - BorderSize,
                BoundsRectangle.Width + 2 * BorderSize,
                BoundsRectangle.Height + 2 * BorderSize
            );

            // left
            e.SpriteBatch.Draw(Game1.menuTexture,
                new Rectangle(OuterBorderRect.X, OuterBorderRect.Y, BorderSize, OuterBorderRect.Height),
                new Rectangle(0, 256, PixelBorderSize, 60), 
                Color.White, 0, Vector2.Zero, SpriteEffects.None, 1E-06f);

            // right
            e.SpriteBatch.Draw(Game1.menuTexture,
                new Rectangle(OuterBorderRect.Right - BorderSize, OuterBorderRect.Y, BorderSize, OuterBorderRect.Height),
                new Rectangle(60 - PixelBorderSize, 256, PixelBorderSize, 60), 
                Color.White, 0, Vector2.Zero, SpriteEffects.None, 1E-06f);

            // top
            e.SpriteBatch.Draw(Game1.menuTexture,
                new Rectangle(OuterBorderRect.X + BorderSize, OuterBorderRect.Y, OuterBorderRect.Width - 2 * BorderSize, BorderSize),
                new Rectangle(PixelBorderSize, 256, 60 - 2 * PixelBorderSize, PixelBorderSize),
                Color.White, 0, Vector2.Zero, SpriteEffects.None, 1E-06f);

            // bottom
            e.SpriteBatch.Draw(Game1.menuTexture,
                new Rectangle(OuterBorderRect.X + BorderSize, OuterBorderRect.Bottom - BorderSize, OuterBorderRect.Width - 2 * BorderSize, BorderSize),
                new Rectangle(PixelBorderSize, 256 + 60 - PixelBorderSize, 60 - 2 * PixelBorderSize, PixelBorderSize),
                Color.White, 0, Vector2.Zero, SpriteEffects.None, 1E-06f);

            e.SpriteBatch.Draw(Game1.menuTexture, BoundsRectangle, new Rectangle(PixelBorderSize, 256 + PixelBorderSize, 60 - 2 * PixelBorderSize, 60 - 2 * PixelBorderSize), Color.White, 0, new Vector2(0, 0), SpriteEffects.None, 1E-06f);
        }

        private void OnRenderedActiveMenuDrawOutterBound(RenderedActiveMenuEventArgs e, Rectangle BoundsRectangle)
        {
            int BorderSize = PixelBorderSize;

            Rectangle OuterBorderRect = new Rectangle(
                BoundsRectangle.X - BorderSize,
                BoundsRectangle.Y - BorderSize,
                BoundsRectangle.Width + 2 * BorderSize,
                BoundsRectangle.Height + 2 * BorderSize
            );

            // left
            e.SpriteBatch.Draw(Game1.menuTexture,
                new Rectangle(OuterBorderRect.X, OuterBorderRect.Y, BorderSize, OuterBorderRect.Height),
                new Rectangle(0, 256, PixelBorderSize, 60),
                Color.White, 0, Vector2.Zero, SpriteEffects.None, 1E-06f);

            // right
            e.SpriteBatch.Draw(Game1.menuTexture,
                new Rectangle(OuterBorderRect.Right - BorderSize, OuterBorderRect.Y, BorderSize, OuterBorderRect.Height),
                new Rectangle(60 - PixelBorderSize, 256, PixelBorderSize, 60),
                Color.White, 0, Vector2.Zero, SpriteEffects.None, 1E-06f);

            // top
            e.SpriteBatch.Draw(Game1.menuTexture,
                new Rectangle(OuterBorderRect.X + BorderSize, OuterBorderRect.Y, OuterBorderRect.Width - 2 * BorderSize, BorderSize),
                new Rectangle(PixelBorderSize, 256, 60 - 2 * PixelBorderSize, PixelBorderSize),
                Color.White, 0, Vector2.Zero, SpriteEffects.None, 1E-06f);

            // bottom
            e.SpriteBatch.Draw(Game1.menuTexture,
                new Rectangle(OuterBorderRect.X + BorderSize, OuterBorderRect.Bottom - BorderSize, OuterBorderRect.Width - 2 * BorderSize, BorderSize),
                new Rectangle(PixelBorderSize, 256 + 60 - PixelBorderSize, 60 - 2 * PixelBorderSize, PixelBorderSize),
                Color.White, 0, Vector2.Zero, SpriteEffects.None, 1E-06f);

            e.SpriteBatch.Draw(Game1.menuTexture, BoundsRectangle, new Rectangle(PixelBorderSize, 256 + PixelBorderSize, 60 - 2 * PixelBorderSize, 60 - 2 * PixelBorderSize), Color.White, 0, new Vector2(0, 0), SpriteEffects.None, 1E-06f);
        }
    }
}
