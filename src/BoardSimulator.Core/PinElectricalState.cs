namespace BoardSimulator.Core
{
    public readonly record struct PinElectricalState(PinDrive Drive, bool PullUpEnabled);
}
