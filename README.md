# BoardSimulator

A little board and device simulator I'm building in C#.

The idea is to connect virtual devices, write a program in C, and see what happens
to the signals. Something like Packet Tracer, but for electronic boards.

I'm starting with a controller, a button, and an LED. Press the button, turn the
LED on for two seconds, and watch the states change.

It's still very early. There is a basic pin and connection model, console
examples, and a window where a button changes an LED. That example still runs
in C#. We also have a small C program that prints a greeting, and we're working
on connecting it to the simulator. The two-second C-controlled example is next.

## Running it

You'll need Windows and Visual Studio with **.NET desktop development** and
**Desktop development with C++** installed. The selected .NET SDK is listed in
[global.json](global.json); the C worker uses MSVC and C17.

1. Open `BoardSimulator.slnx`.
2. Set `BoardSimulator.Wpf` as the startup project.
3. Run it.

You can also run `BoardSimulator.Demo` for the console examples.

## What's where

- `src/BoardSimulator.Core` — how the simulated pins and connections work.
- `src/BoardSimulator.Wpf` — the window and everything you see in it.
- `src/BoardSimulator.Runtime` — launching the C program and reading its output.
- `native/BoardSimulator.Worker` — the small native C program.
- `examples` — small examples to try things out.
- `docs` — more detailed plans and notes.
- `tests` — a place for tests; none have been added yet.

The window currently works on Windows only. I'd like to keep the shared logic
usable for a Linux version later.

The [current specification](docs/specification/board-simulator-prototype-0.1-r2-spec.md)
has the detailed plan. [Current status](docs/status.md) lists what works so far.

## License

[MIT](LICENSE).
