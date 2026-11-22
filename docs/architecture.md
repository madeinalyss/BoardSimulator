# Architecture and decisions

Updated on 2026-11-22 after reviewing the current source files.

The current behavioral contract is [prototype 0.1-r2](specification/board-simulator-prototype-0.1-r2-spec.md).
The [r1 document](specification/board-simulator-prototype-0.1-spec.md) is preserved
as history. See [status.md](status.md) for what exists and what still needs work.

## What we are building

A small digital board simulator: connect devices, run a controller program, and
observe signals. The first complete example is a button that activates an LED
for 2000 virtual milliseconds. It is not an emulator of an STM32 chip or an
analog circuit solver.

The owner has a real STM32 board. After the first complete prototype, reproduce
the reference behavior on that board through a board-specific HAL. The exact
board and chip are not identified yet. Learning C and embedded programming is a
project goal alongside building the simulator; do not defer controller practice
behind a broad device library or editor.

## Projects and dependencies

```text
BoardSimulator.Demo ----> BoardSimulator.Core <---- BoardSimulator.Wpf
        |
        +--------------> BoardSimulator.Runtime

BoardSimulator.Worker: separate native executable
```

- **Core** contains pin/net rules, GPIO behavior, and component models. It has
  no WPF or process-management dependency.
- **Runtime** contains process-launch and stream-reading code. It currently has
  no reference to Core. Later host orchestration can use Core's validated
  operations without introducing a Core-to-Runtime dependency.
- **Demo** is a console application for small executable examples.
- **Wpf** displays model state and currently applies demo input directly.
- **Worker** is a native Windows x64 C application built by Visual Studio.

Namespaces follow the project names: `BoardSimulator.Core`, `.Runtime`, `.Demo`,
and `.Wpf`. Projects use explicit `using` directives and nullable analysis.

## Components and electrical state

- `BoardComponent` provides an immutable ID and display name. It does not define
  shared electrical behavior or a universal `IsEnabled`.
- `ButtonComponent` stores pressed/released state and owns a signal endpoint.
  Pressed drives strong Low; released drives Z. The same connection remains.
- `LedComponent` owns a passive signal endpoint. It derives On/Off/Undefined
  from an observed net; it does not set that net's level.
- `GpioPin` represents one controller GPIO, not a whole controller component.
  Its mode and output latch determine strong drive and pull-up state.
- `PinElectricalState` groups drive and pull-up values. It adds no new electrical
  behavior. `NetState` is the result for the connected net, not a pin's intent.

The component ID convention is ASCII `[A-Za-z][A-Za-z0-9_]*`, checked with absolute
string anchors. This is a project convention, not a property of electronics.
Names are human labels. Stable pin/net identities and uniqueness checks for a
board have not been implemented.

Current public pin setters and `Net.Pins` are temporary. For buttons, callers
must use `SetPressed`, not write the signal drive independently. A pull-based
`Pin` source delegate was discussed but not selected or implemented.

A future fixed-board composition will own components, nets, and wiring. A
controller component and topology ownership are not implemented. Decide the
pin-to-net lookup when introducing that composition; do not treat a suggested
map or back-reference as an existing API. Component list order does not itself
define simulation update order.

## UI direction

WPF is the current Windows UI. It was introduced earlier than r1's original
order because a visible diagram helps with development and learning. Detailed
pin information appears on hover; colors are presentation choices. UI text and
repository documentation are English. The README and contributing guide use
simple, personal language.

The current press/release control immediately changes the demonstration model
and directly drives the LED from C#. It has no simulation clock or Pause/Step
controls. This is temporary demonstration behavior, not the final controller
program or the r2 queued-input semantics. Replace the direct C# decision with
the C program, and apply live input at tick boundaries when scheduling arrives.

## Native C and build decisions

The owner chose the installed Visual Studio tools rather than installing MSYS2:
**MSVC v145, C17, x64** supersedes r1's GCC/C11 default. The inspected compiler
toolset is `14.52.37025`. The selected .NET SDK is `11.0.100`; managed projects
still target .NET 10. WPF explicitly targets Windows 10 build 17763 (1809) or
newer; that configured minimum has not been verified by running on Windows 10.

The worker's `.vcxproj` is the development build. Later, selecting a user's C
source will build that source together with runner and HAL stubs into a native
worker executable. Compiler discovery, MSVC environment setup, dynamic source
selection, build identity, and runtime dependency handling are not implemented.
No CMake dependency or extra UI-to-host service is planned.

The runner owns `main`. Controller programs provide `setup()` and `app_step()`
and call the public HAL. These are host-native controller programs, not MCU
firmware images. C++ is not required for the learning goal; Arduino-like ease
of use does not imply Arduino source/API compatibility.

The product direction is supplied examples plus user C programs using the same
contract and execution path. The first reference example comes first. A larger
preset library, configurable mappings, and an embedded editor are later ideas,
not extra prerequisites for prototype 0.1.

## Runtime and orchestration

The existing `RunToExitAsync` reads both output streams concurrently and returns
`WorkerResult` after exit. It has a five-second deadline and a bounded cleanup
wait. It has not been called by the checked Demo. It is not the interactive
protocol/session implementation and has no Job Object containment.

The real coordinator will drive one serialized tick at a time, service HAL
messages while awaiting callback completion, and call Core's state-owning
operations. HAL work must not be queued behind the very tick awaiting it.
Shared metadata contracts must not make Core depend on Runtime types.

Before any controller callback executes, establish a Windows Job Object with
kill-on-close and a noninherited host-owned handle. The runner must wait for a
host command that is sent only after containment succeeds. A hello line alone
does not authorize execution. Test cleanup, deadlines, partial effects, and
protocol failures; do not assume process separation is a security sandbox.

Stdout is protocol traffic, stderr is diagnostic output. The eventual reader
must bound messages to 4096 bytes, reject malformed input, and support LF/CRLF
and partial reads. Whole-stream capture is only the current finite-process
exercise. The worker counts every HAL call, including local `hal_millis()`;
the host can independently check only requests it receives.

## Deferred directions

Core is intended for reuse by another UI in-process. Linux build/run support,
process containment, and native dependencies still need their own implementation
and validation. Neither a portable library nor a different UI proves Linux
support. No decision to replace WPF with Avalonia or Uno has been made.

Analog resistors/capacitors, bounce, pull-down, buses, plugins, and real MCU
emulation remain outside 0.1. Role-specific electrical approximations and an
external solver were discussed as future possibilities, not commitments. Keep
component identity separate from behavior; do not add an electrical property bag
or universal plugin/backend contract now.

## License

The owner selected MIT on 2026-11-22. The root `LICENSE` applies to original
project code and documentation. Third-party licenses and any native runtime
redistribution obligations must be checked for what is actually shipped.
