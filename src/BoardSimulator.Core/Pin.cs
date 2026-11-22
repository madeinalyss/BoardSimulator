namespace BoardSimulator.Core
{
    public class Pin
    {
        public PinDrive Drive { get; set; } = PinDrive.Z;
        public bool PullUpEnabled { get; set; }
    }
}
