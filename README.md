# BoardSimulator

A little board and device simulator I'm building in C#.

The idea is to connect virtual devices, write a program in C, and see what happens
to the signals. Something like Packet Tracer, but for electronic boards.

I'm starting with a controller, a button, and an LED. Press the button, turn the
LED on for two seconds, and watch the states change.

It's still very early. So far, there is a basic pin and connection model, some
console examples, and a simple window with information about one pin. Running C
programs and the button-and-LED example are still ahead.

## Running it

You'll need Windows, the .NET 10 SDK, and Visual Studio with **.NET desktop
development** installed. The SDK version is listed in [global.json](global.json).

1. Open `BoardSimulator.slnx`.
2. Set `BoardSimulator.Wpf` as the startup project.
3. Run it.

You can also run `BoardSimulator.Demo` for the console examples.

## What's where

- `src/BoardSimulator.Core` — how the simulated pins and connections work.
- `src/BoardSimulator.Wpf` — the window and everything you see in it.
- `examples` — small examples to try things out.
- `docs` — more detailed plans and notes.
- `tests` — a place for tests; none have been added yet.

The window currently works on Windows only. I'd like to keep the shared logic
usable for a Linux version later.

## License

[MIT](LICENSE).
