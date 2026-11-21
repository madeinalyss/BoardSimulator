using System.Collections.Generic;
using System.Linq;

namespace BoardSimulator
{
    public class Net
    {
        public List<Pin> Pins { get; } = new();

        public NetState Resolve()
        {
            bool hasStrongLow = Pins.Any(pin => pin.Drive == PinDrive.Low);
            bool hasStrongHigh = Pins.Any(pin => pin.Drive == PinDrive.High);
            bool hasPullUp = Pins.Any(pin => pin.PullUpEnabled);

            if (hasStrongLow && hasStrongHigh) return NetState.X;
            if (hasStrongLow) return NetState.Low;
            if (hasStrongHigh) return NetState.High;
            if (hasPullUp) return NetState.High;
            return NetState.Z;
        }
    }
}