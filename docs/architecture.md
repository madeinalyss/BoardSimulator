# Architecture and accepted implementation changes

This document records the implementation direction as of 2026-11-22. It is not
a claim that the complete prototype has been implemented or tested.

## Baseline

The [prototype specification, revision 0.1-r1](specification/board-simulator-prototype-0.1-spec.md)
was fixed on 2026-11-18. The copy in this repository preserves that text, including
its original implementation order and its then-pending licensing decision.

The following decisions were accepted afterward:

1. Shared model code lives in a `net10.0` class library, separate from WPF.
2. A minimal visual view is introduced earlier to support incremental development.
   The electrical and scheduling rules remain in the model/host, not in the view.
3. WPF is the current Windows view. Another view may reuse Core in-process.
   A separate UI-to-simulation service protocol is not planned.
4. The owner selected MIT for original project code and documentation on
   2026-11-22. The root `LICENSE` records that decision; the older proposed-license
   wording in the preserved baseline is historical.

These changes do not make Linux a verified target. They do not add plugins,
CPU emulation, or a general execution-backend API to prototype 0.1.

## Current projects

```text
BoardSimulator.Demo ----> BoardSimulator.Core <---- BoardSimulator.Wpf
```

- **Core** owns digital pin/net and GPIO behavior. It has no reference to the
  presentation projects and no WPF or native-process dependency.
- **Demo** is a console application using Core. Its output is a demonstration,
  not a test framework.
- **Wpf** uses Core to display an initial GPIO/net state. Its current tooltip is
  created at initialization, with no button interaction or live refresh yet.

The current public pin setters and connection list are temporary. Validated,
owned mutations, stable IDs, and immutable trace snapshots are future work.

## Planned session and worker boundary

Core will hold authoritative simulation state and synchronous rules for applying
validated operations. A later host-side coordinator will drive serialized ticks
and the native worker lifecycle. Its exact implementation contract is deferred
until the first worker exchange.

The coordinator must service individual HAL requests while awaiting callback
completion. HAL requests must not be queued behind the tick command whose
completion depends on those same requests. Core must not reference a later
Runtime assembly. Shared data contracts, if needed, must not introduce a circular
project dependency.

The planned native worker executes trusted local C code. The host owns virtual
time; reset starts a new process. Stdout is reserved for the protocol, stderr is
drained independently, and every HAL call counts toward the callback budget.
Worker deadlines and process containment remain unimplemented.

## Portability and extensions

The shared library is a dependency boundary, not proof of Linux support. Native
worker compilation, runtime-library discovery, process containment, and UI
behavior must be implemented and checked for every supported host platform.

Runtime and PluginApi projects do not exist yet. Device behavior, controller
capabilities, and execution backends are distinct future extension concerns.
Do not commit to plugin discovery, unloading, API compatibility, or universal
emulation before a concrete use case requires it.

## Current verification limits

The model console demonstrations have been run on Windows. The initial WPF
window and GPIO tooltip have been observed locally. There are no automated
tests, native-worker checks, or Linux execution results yet. Follow the baseline
acceptance criteria as the missing features are implemented.
