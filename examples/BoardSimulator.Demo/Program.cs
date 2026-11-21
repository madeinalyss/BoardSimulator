using System;

namespace BoardSimulator
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

            second.Drive = PinDrive.Low;
            Console.WriteLine($"Добавили сильный Low: {net.Resolve()}");
            first.Drive = PinDrive.Z;
            Console.WriteLine($"Отключили первый выход: {net.Resolve()}");
            second.Drive = PinDrive.Z;
            Console.WriteLine($"Отключили второй выход: {net.Resolve()}");

            ShowElectricalStates();
            ShowGpioMode();
        }

        static void ShowGpioMode()
        {
            GpioPin gpio = new();
            gpio.SetMode(GpioMode.InputPullUp);
            Console.WriteLine($"{gpio.Mode}: Drive={gpio.Pin.Drive}, PullUp={gpio.Pin.PullUpEnabled}");
            gpio.SetMode(GpioMode.Output);
            Console.WriteLine($"Вход в Output: {gpio.OutputLatch}, {gpio.Pin.Drive}");
            gpio.Write(GpioLevel.High);
            Console.WriteLine($"Запись High: {gpio.OutputLatch}, {gpio.Pin.Drive}");
            gpio.SetMode(GpioMode.Output);
            Console.WriteLine($"Повтор Output: {gpio.OutputLatch}, {gpio.Pin.Drive}");
            gpio.SetMode(GpioMode.Input);
            Console.WriteLine($"Вход: Drive={gpio.Pin.Drive}, PullUp={gpio.Pin.PullUpEnabled}");
            gpio.SetMode(GpioMode.Output);
            Console.WriteLine($"Снова Output: {gpio.OutputLatch}, {gpio.Pin.Drive}");
        }

        static void ShowElectricalStates()
        {
            foreach (GpioMode mode in Enum.GetValues<GpioMode>())
                foreach (GpioLevel latch in Enum.GetValues<GpioLevel>())
                    Console.WriteLine($"{mode}, {latch} → {GpioPin.ComputeElectricalState(mode, latch)}");
        }
    }
}