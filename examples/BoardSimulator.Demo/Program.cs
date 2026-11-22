using BoardSimulator.Core;
using BoardSimulator.Runtime;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BoardSimulator.Demo
{
    class Program
    {
        static async Task Main(string[] args)
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

            try
            {
                WorkerResult result = await RunWorkerDemoAsync(@"D:\Projects\BoardSimulator\artifacts\native\BoardSimulator.Worker\x64\Debug\BoardSimulator.Worker.exe", new[] { "PING", "TEST", "PONG" });
                Console.WriteLine($"Worker exited with code: {result.ExitCode}");
                Console.WriteLine($"stdout: {result.StandardOutput.TrimEnd()}");
                Console.WriteLine($"stderr: {result.StandardError.TrimEnd()}");
            }
            catch (IOException exception)
            {
                Console.WriteLine($"Worker communication failed: {exception.Message}");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Worker timed out.");
            }
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

        static async Task<WorkerResult> RunWorkerDemoAsync(string executablePath, string[] commands)
        {
            using CancellationTokenSource deadline = new(TimeSpan.FromSeconds(5));
            await using WorkerSession session = await WorkerLauncher.StartAsync(executablePath, deadline.Token);
            StringBuilder stdout = new();
            foreach (string command in commands)
            {
                string response = await session.SendCommandAsync(command, deadline.Token);
                stdout.AppendLine(response);
            }
            int exitCode = await session.CompleteAsync(deadline.Token);
            string errorOutput = await session.GetStandardErrorAsync(deadline.Token);
            return new WorkerResult(stdout.ToString(), errorOutput, exitCode);
        }
    }
}
