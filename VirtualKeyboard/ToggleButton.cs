using StardewModdingAPI;

namespace VirtualKeyboard
{
    internal class ToggleButton : KeyButton
    {
        Action ButtonPressedAction;
        public ToggleButton(IModHelper helper, ModConfig.VirtualButton buttonDefine, int AboveMenu, Action action) : base(helper, buttonDefine, AboveMenu)
        {
            ButtonPressedAction = action;
        }

        public override void ButtonPressed()
        {
            ButtonPressedAction();
        }
    }
}
