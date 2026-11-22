using System;

namespace BoardSimulator.Core
{
    public sealed class Board
    {
        public ButtonComponent Button { get; } = new("button", "Button");
        public LedComponent Led { get; } = new("led", "LED");

        public GpioPin ButtonGpio { get; } = new();
        public GpioPin LedGpio { get; } = new();

        public Net ButtonNet { get; } = new();
        public Net LedNet { get; } = new();

        public Board()
        {
            ButtonNet.Pins.Add(ButtonGpio.Pin);
            ButtonNet.Pins.Add(Button.Signal);

            LedNet.Pins.Add(LedGpio.Pin);
            LedNet.Pins.Add(Led.Signal);
        }

        public GpioLevel ReadGpio(int pinId)
        {
            (GpioPin gpio, Net net) = pinId switch
            {
                2 => (ButtonGpio, ButtonNet),
                3 => (LedGpio, LedNet),
                _ => throw new ArgumentOutOfRangeException(nameof(pinId))
            };

            if (gpio.Mode == GpioMode.Unconfigured)
                throw new InvalidOperationException($"GPIO is not configured.");

            return net.Resolve() switch
            {
                NetState.Low => GpioLevel.Low,
                NetState.High => GpioLevel.High,
                NetState.Z => throw new InvalidOperationException("Z_NET_READ"),
                NetState.X => throw new InvalidOperationException("X_NET_READ"),
                _ => throw new InvalidOperationException("Invalid net state.")
            };
        }
    }
}
