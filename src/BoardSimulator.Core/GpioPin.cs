using System;

namespace BoardSimulator
{
    public class GpioPin
    {
        public GpioMode Mode { get; private set; } = GpioMode.Unconfigured;
        public GpioLevel OutputLatch { get; private set; } = GpioLevel.Low;
        public Pin Pin { get; } = new();

        public void SetMode(GpioMode mode)
        {
            if (mode == GpioMode.Unconfigured)
                throw new ArgumentOutOfRangeException(nameof(mode));

            GpioLevel nextLatch = mode == Mode ? OutputLatch : GpioLevel.Low;
            PinElectricalState state = ComputeElectricalState(mode, nextLatch);

            Mode = mode;
            OutputLatch = nextLatch;
            Pin.Drive = state.Drive;
            Pin.PullUpEnabled = state.PullUpEnabled;
        }

        public void Write(GpioLevel level)
        {
            if (Mode != GpioMode.Output)
                throw new InvalidOperationException("Запись разрешена только в режиме Output.");

            PinElectricalState state = ComputeElectricalState(Mode, level);
            OutputLatch = level;
            Pin.Drive = state.Drive;
            Pin.PullUpEnabled = state.PullUpEnabled;
        }

        public static PinElectricalState ComputeElectricalState(GpioMode mode, GpioLevel outputLatch)
        {
            if (outputLatch != GpioLevel.Low && outputLatch != GpioLevel.High)
                throw new ArgumentOutOfRangeException(nameof(outputLatch));

            if (mode == GpioMode.InputPullUp)
                return new PinElectricalState(PinDrive.Z, true);

            if (mode == GpioMode.Input || mode == GpioMode.Unconfigured)
                return new PinElectricalState(PinDrive.Z, false);

            if (mode == GpioMode.Output)
                return new PinElectricalState(outputLatch == GpioLevel.High ? PinDrive.High : PinDrive.Low, false);

            throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }
}