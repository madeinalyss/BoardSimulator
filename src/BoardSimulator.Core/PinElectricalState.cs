namespace BoardSimulator
{
    public readonly record struct PinElectricalState(PinDrive Drive, bool PullUpEnabled);
}