using StardewModdingAPI;
using System.Diagnostics;

namespace VirtualKeyboard
{
    internal class ModConfig
    {
        public Toggle vToggle { get; set; } = new Toggle((SButton)0, new Rect(Constants.TargetPlatform == GamePlatform.Android ? 96 : 36, 12, 64, 64));
        public int AboveMenu { get; set; } = 0;
        public float ButtonScale { get; set; } = 1.0f;
        public List<List<VirtualButton>> Buttons { get; set; } = new List<List<VirtualButton>>
        {
            new List<VirtualButton>{
              new VirtualButton((SButton) 80),
              new VirtualButton((SButton) 73),
              new VirtualButton((SButton) 79),
              new VirtualButton((SButton) 81)
            }
        };

        internal class Rect
        {
            public int X;
            public int Y;
            public int Width;
            public int Height;

            public Rect(int x, int y, int width, int height)
            {
                this.X = x;
                this.Y = y;
                this.Width = width;
                this.Height = height;
            }
        }
        internal class Toggle
        {
            public SButton key { get; set; }

            public Rect rectangle { get; set; }

            public Toggle(SButton key, Rect rectangle)
            {
                this.key = key;
                this.rectangle = rectangle;
            }
        }
        internal class VirtualButton
        {
            public SButton key { get; set; }
            public string alias { get; set; }
            public VirtualButton(
              SButton key,
              string alias = "")
            {
                this.key = key;
                this.alias = alias;
            }
        }
    }
}
