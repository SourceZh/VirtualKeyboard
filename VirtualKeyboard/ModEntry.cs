using System;
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
        private List<List<KeyButton>> Buttons = new List<List<KeyButton>>();
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
        private Rectangle EditButtonBound, AddButtonBound;
        private ToggleButton EditButton, AddButton;
        private bool EnableEditButton = false;
        private bool ShowKeyBoard = false;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            this.ModConfig = Helper.ReadConfig<ModConfig>();
            int buttonsLineNumber = this.ModConfig.Buttons.Length;
            for (int line = 0; line < buttonsLineNumber; line++)
            {
                this.Buttons.Add(new List<KeyButton>());
                for (int index = 0; index < this.ModConfig.Buttons[line].Length; ++index)
                {
                    this.Buttons[line].Add(new KeyButton(helper, this.ModConfig.Buttons[line][index], this.ModConfig.AboveMenu));
                }
            }

            Texture2D texture = helper.ModContent.Load<Texture2D>("assets/togglebutton.png");
            VirtualToggleButtonBound = new Rectangle(this.ModConfig.vToggle.rectangle.X, this.ModConfig.vToggle.rectangle.Y, this.ModConfig.vToggle.rectangle.Width, this.ModConfig.vToggle.rectangle.Height);
            this.VirtualToggleButton = new ClickableTextureComponent(VirtualToggleButtonBound, texture, new Rectangle(0, 0, 16, 16), 4f, false);
            VirtualButton EditVirtualButton = new VirtualButton(0, "edit button");
            this.EditButton = new ToggleButton(helper, EditVirtualButton, this.ModConfig.AboveMenu, EditButtonPressed);
            VirtualButton AddVirtualButton = new VirtualButton(0, "add button");
            this.AddButton = new ToggleButton(helper, AddVirtualButton, this.ModConfig.AboveMenu, AddButtonPressed);

            helper.WriteConfig<ModConfig>(this.ModConfig);

            helper.Events.Display.Rendered += this.Rendered;
            helper.Events.Display.RenderedActiveMenu += this.OnRenderedActiveMenu;
            helper.Events.Display.MenuChanged += this.OnMenuChanged;
            helper.Events.Input.ButtonPressed += this.VirtualToggleButtonPressed;
        }

        private void EditButtonPressed()
        {
            EnableEditButton = !EnableEditButton;
            this.Helper.Input.Suppress(SButton.MouseLeft);
        }

        private void AddButtonPressed()
        {
            this.Helper.Input.Suppress(SButton.MouseLeft);
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
                foreach (List<KeyButton> keyButtonList in this.Buttons)
                    foreach (KeyButton keyButton in keyButtonList)
                        keyButton.Hidden = Convert.ToBoolean(this.EnabledStage);
                EditButton.Hidden = Convert.ToBoolean(this.EnabledStage);
                AddButton.Hidden = Convert.ToBoolean(this.EnabledStage);
                this.EnabledStage = 1 - this.EnabledStage;
                this.Helper.Input.Suppress(SButton.MouseLeft);
                if (this.EnabledStage == 0)
                {
                    EnableEditButton = false;
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
            foreach (List<KeyButton> keyButtonList in this.Buttons)
                foreach (KeyButton keyButton in keyButtonList)
                    keyButton.Hidden = true;
            EditButton.Hidden = true;
            AddButton.Hidden = true;
            this.EnabledStage = 0;
            EnableMenu = e.NewMenu != null;
        }

        private void CalVirtualToggleButtonPosition()
        {
            bool RecalButtonPosition = FirstRender;
            if (Constants.TargetPlatform == GamePlatform.Android)
            {
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
            }

            if (RecalButtonPosition)
            {
                int currentToolbarPaddingX = 0;
                Type Game1Type = typeof(Game1);
                FieldInfo? toolbarPaddingXField = Game1Type.GetField("toolbarPaddingX", BindingFlags.Public | BindingFlags.Static);
                if (toolbarPaddingXField != null)
                {
                    currentToolbarPaddingX = Convert.ToInt32(toolbarPaddingXField.GetValue(null));
                }

                int OffsetX = this.ModConfig.vToggle.rectangle.X;
                if (ToolbarVertical)
                {
                    OffsetX += currentToolbarPaddingX + ToolbarItemSlotSize + 20;
                }
                VirtualToggleButtonBound.X = OffsetX;

                int OffsetY = this.ModConfig.vToggle.rectangle.Y;
                if (ToolbarAlignTop && !ToolbarVertical)
                {
                    OffsetY += ToolbarHeight + 16;
                }
                VirtualToggleButtonBound.Y = OffsetY;
                OffsetY += this.ModConfig.vToggle.rectangle.Height + 4;

                EditButtonBound.X = VirtualToggleButtonBound.X + VirtualToggleButtonBound.Width + 10;
                EditButtonBound.Y = VirtualToggleButtonBound.Y;
                if (this.EditButton.CalcBounds(EditButtonBound.X, EditButtonBound.Y))
                {
                    EditButtonBound = this.EditButton.OutterBounds;
                }

                AddButtonBound.X = EditButtonBound.X + EditButtonBound.Width + 10;
                AddButtonBound.Y = EditButtonBound.Y;
                if (this.AddButton.CalcBounds(AddButtonBound.X, AddButtonBound.Y))
                {
                    AddButtonBound = this.AddButton.OutterBounds;
                }

                bool all_calc = true;
                for (int line = 0; line < this.Buttons.Count; ++line)
                {
                    int LineOffsetX = OffsetX;
                    for (int index = 0; index < this.Buttons[line].Count; ++index)
                    {
                        if (!Buttons[line][index].CalcBounds(LineOffsetX, OffsetY))
                        {
                            all_calc = false;
                            break;
                        }
                        LineOffsetX = Buttons[line][index].OutterBounds.X + Buttons[line][index].OutterBounds.Width + 10;
                    }
                    OffsetY = Buttons[line][0].OutterBounds.Y + Buttons[line][0].OutterBounds.Height + 10;
                }
                FirstRender = !all_calc;
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