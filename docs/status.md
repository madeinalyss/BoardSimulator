# Current status

Source snapshot checked on **2026-11-22**. Requirements are in
[prototype 0.1-r2](specification/board-simulator-prototype-0.1-r2-spec.md).
This file distinguishes code that exists from behavior actually exercised.

## Implemented

### Core

- `PinDrive`, `NetState`, `GpioMode`, and `GpioLevel`.
- Mutable `Pin` drive/pull-up state and on-demand `Net.Resolve()`.
- `PinElectricalState` as an immutable value pair.
- `GpioPin` mode transitions, output latch, output writes, and pure electrical
  state derivation. Validation precedes mutation.
- `BoardComponent` with immutable ID/display name and ID validation.
- `ButtonComponent.SetPressed`: Low when pressed, Z when released.
- Passive `LedComponent.Signal`, with `GetState(NetState)` returning
  On/Off/Undefined.

### WPF demonstration

- Controller outline, BTN marker, press/release control, and LED marker.
- Tooltip refresh after interaction, showing GPIO mode, drive, pull-up, and net.
- Colors derived from model state; UI text is English.
- Two separate nets, assembled directly in the window constructor.
- Temporary C# logic turns the LED on while the button is pressed.

This is immediate demo input, not a paused/running simulation. It has no tick
queue or 2000 ms timer. No C application controls the UI yet.

### Native worker

- Visual Studio Windows application project using MSVC v145 and C17 for x64.
- `main.c` writes `Worker started` to stderr and `HELLO 1` to stdout, flushes
  stdout, and returns 0.
- Output: `artifacts/native/BoardSimulator.Worker/x64/Debug/BoardSimulator.Worker.exe`.
- Intermediate output: `artifacts/obj/BoardSimulator.Worker/x64/Debug/`.

There is no command reader, callback dispatch, HAL, or start gate yet. The hello
line is a greeting demonstration, not a negotiated handshake.

### Runtime

- `WorkerLauncher.CreateStartInfo`: direct launch settings, redirected stdout
  and stderr, and no separate window.
- `ReadStreamAsync`: cancellable whole-stream text capture.
- `WorkerResult`: stdout, stderr, and exit code for a completed process.
- `RunToExitAsync`: starts both reads before awaiting exit, uses a shared
  five-second deadline, and attempts termination/cleanup if still running.

The method is present in source. The checked Demo does not call it. It has not
been verified by an end-to-end C#-launch experiment, a hang fixture, or an
excess-output fixture. It is not yet a long-lived worker session or a bounded
protocol reader. Captured output is not yet size-limited.

## Observed verification

- Earlier managed-project builds and console examples were run locally.
- The owner ran the WPF button/LED demonstration and checked tooltips/colors.
- The owner built and ran the native greeting program successfully.
- Recent namespace changes were inspected: Core, Runtime, Demo, and Wpf are
  consistently separated.
- Installed SDK `11.0.100` and MSVC toolset `14.52.37025` were inspected locally.

The complete solution was not rebuilt as part of this documentation update.
Do not treat previous builds as proof of all current behavior. There is still
no automated test project, acceptance-suite result, or Linux execution result.

## Missing before the first complete scenario

1. Call `RunToExitAsync` from Demo; confirm captured hello, stderr, and exit code.
   Exercise timeout and cleanup on a controlled fixture.
2. Introduce fixed-board/controller composition, stable pin/net identities,
   connection ownership, and validated GPIO reads. Keep changes reviewable;
   some model work can precede or accompany the HAL exercise.
3. Build the bounded protocol reader/writer, stdin commands, callback/request
   IDs, explicit terminal outcomes, EOF handling, and independent stderr drain.
4. Establish and test containment/start gating before invoking app callbacks.
5. Add the public C HAL header, runner callback dispatch, and HAL stubs. First
   real round trip: configure BTN pull-up, then immediately read High.
6. Add host-owned 1 ms virtual ticks, ordered input queue, run lifecycle, trace,
   callback deadlines, operation budget, and fresh-process reset.
7. Implement the reference C app: sampled new press, LED on for 2000 ms,
   retrigger, no repeat while held, and new-press priority at expiry.
8. Replace the WPF demo shortcut with queued input and C-driven GPIO output.
   Add the required controls and focused headless acceptance checks.

Automatic user-source compilation and program selection must eventually obey
build identity and stale-build rules. A broad preset library or code editor is
not required before the reference example works.

## Immediate next teaching step

Continue from `WorkerLauncher.RunToExitAsync` and `WorkerResult`: call it from
the existing Demo asynchronously and inspect the three result fields. The
owner has just worked through why process exit and stream completion are
separate events, and why cancellation does not itself terminate a process.
Review their code and output before moving to the next dependent step.

After completing the first prototype, repeat the reference application on the
owner's real STM32 board. Identify its exact model before board-specific setup;
do not infer it from an earlier illustrative board photo.
