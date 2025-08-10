using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using static VirtualKeyboard.ModConfig;

namespace VirtualKeyboard
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        private ModConfig ModConfig = new ModConfig();
        private List<KeyButton> Buttons = new List<KeyButton>();
        private List<KeyButton> ControlButtons = new List<KeyButton>();
        private ClickableTextureComponent? VirtualToggleButton;
        private int EnabledStage = 0;
        private int LastPressTick = 0;
        private bool FirstRender = true;
        private bool ToolbarAlignTop = false;
        private bool ToolbarVertical = false;
        private int ToolbarItemSlotSize = 0;
        private int ToolbarHeight = 0;
        private Rectangle VirtualToggleButtonBound;
        private bool EnableMenu = false;
        private bool EnableEditButton = false;
        private IModHelper Helper;
        public Point ToolbarOffset = new Point(0);

        public void UpdateAllButtons()
        {
            int index = 0;
            while(index < this.ModConfig.Buttons.Count)
            {
                if (this.Buttons[index].Deleted)
                {
                    this.Buttons.RemoveAt(index);
                    this.ModConfig.Buttons.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }

            this.Helper.WriteConfig<ModConfig>(this.ModConfig);
        }

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            this.Helper = helper;
            this.ModConfig = Helper.ReadConfig<ModConfig>();
            int buttonsLineNumber = this.ModConfig.Buttons.Count;
            for (int index = 0; index < buttonsLineNumber; index++)
            {
                this.Buttons.Add(new KeyButton(this, helper, this.ModConfig.Buttons[index], this.ModConfig.AboveMenu));
            }

            VirtualButton EditVirtualButton = new VirtualButton(0, new Pos(0, 0), "edit button");
            this.ControlButtons.Add(new ControlButton(this, helper, EditVirtualButton, this.ModConfig.AboveMenu, EditButtonPressed));
            VirtualButton AddVirtualButton = new VirtualButton(0, new Pos(0, 0), "add button");
            this.ControlButtons.Add(new ControlButton(this, helper, AddVirtualButton, this.ModConfig.AboveMenu, AddButtonPressed));

            Texture2D texture = helper.ModContent.Load<Texture2D>("assets/togglebutton.png");
            VirtualToggleButtonBound = new Rectangle(this.ModConfig.vToggle.rectangle.X, this.ModConfig.vToggle.rectangle.Y, this.ModConfig.vToggle.rectangle.Width, this.ModConfig.vToggle.rectangle.Height);
            this.VirtualToggleButton = new ClickableTextureComponent(VirtualToggleButtonBound, texture, new Rectangle(0, 0, 16, 16), 4f, false);

            //helper.WriteConfig<ModConfig>(this.ModConfig);

            helper.Events.Display.Rendered += this.Rendered;
            helper.Events.Display.RenderedActiveMenu += this.OnRenderedActiveMenu;
            helper.Events.Display.MenuChanged += this.OnMenuChanged;
            helper.Events.Input.ButtonPressed += this.VirtualToggleButtonPressed;
        }

        private void ChangeEditButton(bool isChange)
        {
            EnableEditButton = isChange;
            foreach (KeyButton keyButton in this.Buttons)
                keyButton.EditButton = EnableEditButton;
        }

        private void EditButtonPressed()
        {
            ChangeEditButton(!EnableEditButton);
            this.Helper.Input.Suppress(SButton.MouseLeft);
        }

        private void AddButtonPressed()
        {
            this.Helper.Input.Suppress(SButton.MouseLeft);
        }

        private void ShowAllButtons(bool isShow)
        {
            foreach (KeyButton keyButton in this.Buttons)
                keyButton.Hidden = isShow;
            foreach (ControlButton controlButton in this.ControlButtons)
                controlButton.Hidden = isShow;
        }

        private void VirtualToggleButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;
            // ignore if menu open
            if (EnableMenu && this.ModConfig.AboveMenu == 0)
                return;

            Vector2 screenPixels = Utility.ModifyCoordinatesForUIScale(e.Cursor.ScreenPixels);
            if (e.Button == this.ModConfig.vToggle.key || ShouldTrigger(VirtualToggleButtonBound, screenPixels))
            {
                ShowAllButtons(Convert.ToBoolean(this.EnabledStage));
                this.EnabledStage = 1 - this.EnabledStage;
                this.Helper.Input.Suppress(SButton.MouseLeft);
                if (this.EnabledStage == 0)
                {
                    ChangeEditButton(false);
                }
            }
        }

        private bool ShouldTrigger(Rectangle bound, Vector2 screenPixels)
        {
            if (this.VirtualToggleButton == null) return false;
            int ticks = Game1.ticks;
            if (ticks - this.LastPressTick <= 6 || !bound.Contains(screenPixels.X, screenPixels.Y))
                return false;
            this.LastPressTick = ticks;
            return true;
        }

        private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            ShowAllButtons(true);
            this.EnabledStage = 0;
            EnableMenu = e.NewMenu != null;
        }

        private bool CalToolbarSize(bool needRecal)
        {
            if (Constants.TargetPlatform != GamePlatform.Android)
                return needRecal;

            bool RecalButtonPosition = needRecal;

            Type Game1OptionType = Game1.options.GetType();
            FieldInfo? verticalToolbarField = Game1OptionType.GetField("verticalToolbar");

            if (verticalToolbarField != null)
            {
                bool currentToolbarVertical = Convert.ToBoolean(verticalToolbarField.GetValue(Game1.options));
                RecalButtonPosition |= (ToolbarVertical != currentToolbarVertical);
                ToolbarVertical = currentToolbarVertical;
            }

            foreach (IClickableMenu onScreenMenu in Game1.onScreenMenus)
            {
                if (onScreenMenu is Toolbar)
                {
                    Toolbar toolbar = (Toolbar)onScreenMenu;
                    Type ToolbarType = toolbar.GetType();
                    FieldInfo? alignTopField = ToolbarType.GetField("alignTop");
                    if (alignTopField != null)
                    {
                        bool currentAlignTop = Convert.ToBoolean(alignTopField.GetValue(toolbar));
                        RecalButtonPosition |= (ToolbarAlignTop != currentAlignTop);
                        ToolbarAlignTop = currentAlignTop;
                    }

                    PropertyInfo? itemSlotSizeProperty = ToolbarType.GetProperty("itemSlotSize");
                    if (itemSlotSizeProperty != null)
                    {
                        int currentItemSlotSize = Convert.ToInt32(itemSlotSizeProperty.GetValue(toolbar));
                        RecalButtonPosition |= (ToolbarItemSlotSize != currentItemSlotSize);
                        ToolbarItemSlotSize = currentItemSlotSize;
                    }

                    //FieldInfo? toolbarHeightField = ToolbarType.GetField("toolbarHeight");
                    //if (toolbarHeightField != null)
                    //{
                    //    int currentToolbarHeight = Convert.ToInt32(toolbarHeightField.GetValue(toolbar));
                    //    RecalButtonPosition |= (ToolbarHeight != currentToolbarHeight);
                    //    ToolbarHeight = currentToolbarHeight;
                    //}
                    ToolbarHeight = this.Helper.Reflection.GetField<int>(toolbar, "toolbarHeight").GetValue();

                    break;
                }
            }
            
            return RecalButtonPosition;
        }

        private bool CalButtonBounds(int X, int Y, ref List<KeyButton> buttons, bool saveConfig = false)
        {
            bool allCalc = true;
            int lineOffsetX = X;
            for (int index = 0; index < buttons.Count; ++index)
            {
                if (!buttons[index].CalcBounds(lineOffsetX, Y))
                {
                    allCalc = false;
                    break;
                }
                if (saveConfig)
                {
                    this.ModConfig.Buttons[index].pos = new Pos(buttons[index].OutterBounds.X, buttons[index].OutterBounds.Y);
                }
                lineOffsetX = buttons[index].OutterBounds.X + buttons[index].OutterBounds.Width + 10;
            }
            return allCalc;
        }

        private bool CalButtonBounds(ref List<KeyButton> buttons)
        {
            bool allCalc = true;
            foreach (KeyButton keyButton in this.Buttons)
            {
                if (!keyButton.CalcBounds(keyButton.OutterBounds.X, keyButton.OutterBounds.Y))
                {
                    allCalc = false;
                    break;
                }
            }
            return allCalc;
        }

        private void CalVirtualToggleButtonPosition()
        {
            bool RecalButtonPosition = CalToolbarSize(FirstRender);

            if (RecalButtonPosition)
            {
                int currentToolbarPaddingX = 0;
                Type Game1Type = typeof(Game1);
                FieldInfo? toolbarPaddingXField = Game1Type.GetField("toolbarPaddingX", BindingFlags.Public | BindingFlags.Static);
                currentToolbarPaddingX = (toolbarPaddingXField != null) ? Convert.ToInt32(toolbarPaddingXField.GetValue(null)) : 0;

                ToolbarOffset.X = ToolbarVertical ? currentToolbarPaddingX + ToolbarItemSlotSize + 20 : 0;
                ToolbarOffset.Y = (!ToolbarVertical && ToolbarAlignTop) ? ToolbarHeight + 16 : 0;

                VirtualToggleButtonBound.X = ToolbarOffset.X + this.ModConfig.vToggle.rectangle.X;
                VirtualToggleButtonBound.Y = ToolbarOffset.Y + this.ModConfig.vToggle.rectangle.Y;

                int controlButtonsOffsetX = this.ModConfig.vToggle.rectangle.X + VirtualToggleButtonBound.Width + 10;
                int controlButtonsOffsetY = this.ModConfig.vToggle.rectangle.Y;
                CalButtonBounds(controlButtonsOffsetX, controlButtonsOffsetY, ref this.ControlButtons);

                int buttonsOffsetX = this.ModConfig.vToggle.rectangle.X;
                int buttonsOffsetY = this.ModConfig.vToggle.rectangle.Y + this.ModConfig.vToggle.rectangle.Height + 4;
                bool calSucceed = this.ModConfig.Init ? CalButtonBounds(ref this.Buttons) : CalButtonBounds(buttonsOffsetX, buttonsOffsetY, ref this.Buttons, true);
                FirstRender = !calSucceed;
                if (!FirstRender)
                {
                    this.ModConfig.Init = true;
                    this.Helper.WriteConfig<ModConfig>(this.ModConfig);
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

        /*********
        ** Private methods
        *********/
        /// <summary>Raised after the game draws to the sprite batch in a draw tick, just before the final sprite batch is rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void Rendered(object? sender, RenderedEventArgs e)
        {
            if (this.ModConfig.AboveMenu != 0)
                return;
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            if (this.VirtualToggleButton == null)
                return;

            CalVirtualToggleButtonPosition();

            this.VirtualToggleButton.bounds = CalBoundFromUIScale(VirtualToggleButtonBound);
            this.VirtualToggleButton.scale = Utility.ModifyCoordinateFromUIScale(4.0f);
            this.VirtualToggleButton.baseScale = this.VirtualToggleButton.scale;

            float scale = 0.5f + this.EnabledStage * 0.5f;
            this.VirtualToggleButton.draw(e.SpriteBatch, Color.Multiply(Color.White, scale), 1E-06f, 0);
        }

        private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
        {
            if (this.ModConfig.AboveMenu == 0)
                return;

            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            if (this.VirtualToggleButton == null)
                return;

            CalVirtualToggleButtonPosition();

            this.VirtualToggleButton.bounds = VirtualToggleButtonBound;
            this.VirtualToggleButton.scale = 4.0f;
            this.VirtualToggleButton.baseScale = this.VirtualToggleButton.scale;

            float scale = 0.5f + this.EnabledStage * 0.5f;
            this.VirtualToggleButton.draw(e.SpriteBatch, Color.Multiply(Color.White, scale), 1E-06f, 0);
        }
    }
}