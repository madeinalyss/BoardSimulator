namespace BoardSimulator.Core
{
    public class ButtonComponent : BoardComponent
    {
        public bool IsPressed { get; private set; }
        public Pin Signal { get; } = new();
        public ButtonComponent(string id, string displayName)
            : base(id, displayName) { }

        public void SetPressed(bool pressed)
        {
            IsPressed = pressed;
            Signal.Drive = pressed ? PinDrive.Low : PinDrive.Z;
        }
    }
}
