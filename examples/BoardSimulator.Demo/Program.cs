using BoardSimulator.Core;
using System;

namespace BoardSimulator.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            Pin first = new Pin { Drive = PinDrive.High };
            Pin second = new Pin();
            Net net = new Net();
            net.Pins.Add(first);
            net.Pins.Add(second);

            Console.WriteLine($"Подключено выводов: {net.Pins.Count}");
            Console.WriteLine($"Состояние цепи: {net.Resolve()}");

            ShowElectricalStates();
            ShowLedState();
        }

        static void ShowElectricalStates()
        {
            foreach (GpioMode mode in Enum.GetValues<GpioMode>())
                foreach (GpioLevel latch in Enum.GetValues<GpioLevel>())
                    Console.WriteLine($"{mode}, {latch} → {GpioPin.ComputeElectricalState(mode, latch)}");
        }

        static void ShowLedState()
        {
            LedComponent led = new LedComponent("led", "LED");
            GpioPin gpioPin = new GpioPin();
            Net net = new Net();
            net.Pins.Add(gpioPin.Pin);
            net.Pins.Add(led.Signal);
            Console.WriteLine($"Состояние светодиода до вмешательства: {led.GetState(net.Resolve())}");
            gpioPin.SetMode(GpioMode.Output);
            Console.WriteLine($"Состояние светодиода после изменения выхода: {led.GetState(net.Resolve())}");
            gpioPin.Write(GpioLevel.High);
            Console.WriteLine($"Состояние светодиода после установки уровня: {led.GetState(net.Resolve())}");
        }
    }
}
