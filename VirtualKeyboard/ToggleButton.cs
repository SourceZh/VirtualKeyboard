using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace VirtualKeyboard
{
    internal class ToggleButton : KeyButton
    {
        Action ButtonPressedAction;
        public ToggleButton(ModEntry modEntry, IModHelper helper, ModConfig.VirtualButton buttonDefine, int aboveMenu, Action action) : base(modEntry, helper, buttonDefine, aboveMenu)
        {
            ButtonPressedAction = action;
        }

        public override void ButtonPressed()
        {
            ButtonPressedAction();
        }

        public override void OnRenderedCloseButton(RenderedEventArgs e)
        {

        }

        public override void OnRenderedActiveMenuCloseButton(RenderedActiveMenuEventArgs e)
        {

        }
    }
}
