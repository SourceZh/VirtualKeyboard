﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace VirtualKeyboard
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        private ModConfig ModConfig = new ModConfig();
        private List<KeyButton> Buttons = new List<KeyButton>();
        private ClickableTextureComponent? VirtualToggleButton;
        private int EnabledStage = 0;
        private int LastPressTick = 0;
        private bool FirstRender = true;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            this.ModConfig = Helper.ReadConfig<ModConfig>();
            for (int index = 0; index < this.ModConfig.Buttons.Length; ++index)
            {
                this.Buttons.Add(new KeyButton(helper, this.ModConfig.Buttons[index]));
            }

            Texture2D texture = helper.ModContent.Load<Texture2D>("assets/togglebutton.png");
            this.VirtualToggleButton = new ClickableTextureComponent(new Rectangle(this.ModConfig.vToggle.rectangle.X, this.ModConfig.vToggle.rectangle.Y, this.ModConfig.vToggle.rectangle.Width, this.ModConfig.vToggle.rectangle.Height), texture, new Rectangle(0, 0, 16, 16), 4f, false);
            helper.WriteConfig<ModConfig>(this.ModConfig);
            helper.Events.Display.Rendered += this.Rendered;
            helper.Events.Display.MenuChanged += this.OnMenuChanged;
            helper.Events.Input.ButtonPressed += this.VirtualToggleButtonPressed;
        }

        private void VirtualToggleButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;
            Vector2 screenPixels = Utility.ModifyCoordinatesForUIScale(e.Cursor.ScreenPixels);
            if (e.Button == this.ModConfig.vToggle.key || ShouldTrigger(screenPixels))
            {
                foreach (KeyButton keyButton in this.Buttons)
                    keyButton.Hidden = Convert.ToBoolean(this.EnabledStage);
                this.EnabledStage = 1 - this.EnabledStage;
            }
        }
        private bool ShouldTrigger(Vector2 screenPixels)
        {
            if (this.VirtualToggleButton == null) return false;
            int ticks = Game1.ticks;
            if (ticks - this.LastPressTick <= 6 || !((ClickableComponent)this.VirtualToggleButton).containsPoint((int)screenPixels.X, (int)screenPixels.Y))
                return false;
            this.LastPressTick = ticks;
            return true;
        }
        private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            foreach (KeyButton keyButton in this.Buttons)
                keyButton.Hidden = true;
            this.EnabledStage = 0;
        }

        /*********
        ** Private methods
        *********/
        /// <summary>Raised after the game draws to the sprite batch in a draw tick, just before the final sprite batch is rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void Rendered(object? sender, RenderedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            if (this.VirtualToggleButton == null) return;

            if (FirstRender)
            {
                int OffsetX = this.ModConfig.ButtonsOffset.X;
                int OffsetY = this.ModConfig.ButtonsOffset.Y;
                bool all_calc = true;
                for (int index = 0; index < this.Buttons.Count; ++index)
                {
                    if (!Buttons[index].CalcBounds(OffsetX, OffsetY))
                    {
                        all_calc = false;
                        break;
                    }
                    OffsetX = Buttons[index].OutterBounds.X + Buttons[index].OutterBounds.Width + 10;
                }
                FirstRender = !all_calc;
            }

            this.VirtualToggleButton.bounds.X = this.ModConfig.vToggle.rectangle.X;
            this.VirtualToggleButton.bounds.Y = this.ModConfig.vToggle.rectangle.Y;

            float scale = 0.5f + this.EnabledStage * 0.5f;
            this.VirtualToggleButton.draw(e.SpriteBatch, Color.Multiply(Color.White, scale), 1E-06f, 0);
        }
    }
}