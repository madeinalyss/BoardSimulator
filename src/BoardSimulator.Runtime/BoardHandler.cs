using BoardSimulator.Core;
using System;
using System.IO;

namespace BoardSimulator.Runtime
{
    public sealed class BoardHandler
    {
        private readonly Board _board;

        public BoardHandler(Board board)
        {
            _board = board;
        }

        public string Handle(string request)
        {
            string[] parts = request.Split(' ');

            if (parts.Length == 3 && parts[0] == "HAL" && parts[1] == "READ")
            {
                int pinId = parts[2] switch
                {
                    "2" => 2,
                    "3" => 3,
                    _ => throw new InvalidDataException($"Invalid GPIO pin.")
                };

                GpioLevel level = _board.ReadGpio(pinId);
                return level == GpioLevel.High ? "VALUE 1" : "VALUE 0";
            }

            if (parts.Length != 4 || parts[0] != "HAL" || (parts[1] != "MODE" && parts[1] != "WRITE"))
                throw new InvalidDataException($"Invalid HAL mode request.");

            GpioPin pin  = parts[2] switch
            {
                "2" => _board.ButtonGpio,
                "3" => _board.LedGpio,
                _ => throw new InvalidDataException($"Invalid GPIO pin.")
            };

            if (parts[1] == "WRITE")
            {
                GpioLevel level = parts[3] switch
                {
                    "0" => GpioLevel.Low,
                    "1" => GpioLevel.High,
                    _ => throw new InvalidDataException($"Invalid GPIO level.")
                };

                pin.Write(level);
                return "OK";
            }

            GpioMode mode = parts[3] switch
            {
                "0" => GpioMode.Input,
                "1" => GpioMode.InputPullUp,
                "2" => GpioMode.Output,
                _ => throw new InvalidDataException($"Invalid GPIO mode.")
            };

            pin.SetMode(mode);
            return "OK";
        }
    }
}
