# Board and Device Simulator — Prototype 0.1

Status: implementation baseline, revision 0.1-r1, consolidated after architectural review on 2026-11-18. This fixes the scope and behavioral contract; implementation findings may justify focused corrections. No implementation or performance validation is implied.

## 1. Purpose

Build the first working slice of an open-source environment for modeling connected electronic boards and devices, conceptually similar to Cisco Packet Tracer.

The prototype must demonstrate this loop:

> Write a controller application in C → run it against a virtual board → change a physical-style input → observe signals, outputs, and the sequence of operations.

The author already has C# experience and wants meaningful C and embedded-related practice. Visible state and connections are important learning aids. This is behavioral simulation, not a claim of electrical or target-MCU accuracy.

## 2. Scope

### Required

- Windows x64 host.
- C# simulation host and a minimal WPF interface on modern .NET.
- A C application compiled for the host computer.
- A separate native worker process executing that application.
- A small synchronous simulated hardware abstraction layer (HAL).
- One fixed board: controller, button, LED, and fixed digital connections.
- Deterministic virtual time with a 1 ms controller polling period.
- Run, Pause, Step, and Reset.
- Observable pin/net states, an ordered trace, and diagnostics.
- Headless scripted execution for meaningful acceptance checks.

### Excluded

Free-form schematic editing; component plugins; a general device-description language; multiple controllers; analog solving; physical PCB layout; voltage/current validation; contact bounce; UART/I2C/SPI; interrupts; RTOS behavior; CPU emulation; source-line or instruction stepping; arbitrary C-variable inspection; execution of untrusted code; cross-platform packaging.

Real-board validation is a follow-up learning milestone, not a prerequisite for completing 0.1.

## 3. Fixed board and digital model

The board contains:

- Controller GPIO `BTN` (logical ID 2).
- Controller GPIO `LED` (logical ID 3).
- A normally open button that connects the BTN net to logical ground when pressed.
- An LED represented as a digital indicator on the LED net.

The button starts released. GPIO pins start internally as `UNCONFIGURED`, electrically released/high impedance without pull-up. Keep the configuration flag separate from electrical drive: an explicitly configured `HAL_INPUT` is electrically similar but is legal to read when its net has a defined level. The controller's setup function configures BTN as input with pull-up and LED as output, then explicitly writes LOW to LED.

On entry to output mode from any other mode, its output latch is initialized to LOW. This is a prototype rule, not a universal MCU reset characteristic. Setting output mode again preserves the current latch.

| Mode transition | Immediate effect |
|---|---|
| Any different mode → INPUT | Release strong drive; disable pull-up; discard output latch |
| Any different mode → INPUT_PULLUP | Release strong drive; enable weak HIGH; discard output latch |
| Any different mode → OUTPUT | Disable pull-up; initialize latch to LOW; drive strong LOW |
| Same mode → same mode | Preserve state, including an existing output latch |

### Pin and net states

Separate a pin's drive from the resolved state observed on its net.

- Drive: released/high impedance (`Z`), LOW, or HIGH.
- Drive strength: strong for active outputs and the pressed button; weak for pull-up.
- Resolved net: LOW, HIGH, Z, or X.

Resolution:

1. Opposing strong drivers produce X and a diagnostic.
2. Otherwise, strong drivers determine the level.
3. Without strong drivers, the pull-up produces HIGH.
4. Without any driver or pull-up, the net is Z.

Only pull-up is required; pull-down and other weak-driver combinations are outside 0.1.

Z means undriven; X means unresolved/conflicting in this model. Neither is a valid binary GPIO read. High impedance is not itself an error.

The LED is ON for HIGH, OFF for LOW, and visibly undefined for Z or X. It does not load the net. Current limiting is assumed; resistance, voltage drop, brightness, and electrical damage are not modeled.

## 4. Controller application contract

The application defines:

```c
void setup(void);
void app_step(void);
```

The worker calls setup once per run, at virtual time 0. It calls app_step once per subsequent controller tick.

Both functions must return. Blocking delay, waiting for virtual time to advance inside a callback, and waiting for an input to change inside a callback are unsupported. Time and external inputs remain fixed during a callback.

The reference application uses static state, edge detection, and unsigned elapsed-time arithmetic. It must not depend on the number of callback invocations to measure duration.

### Reference behavior

- Released BTN reads HIGH; pressed BTN reads LOW.
- A sampled HIGH-to-LOW transition is a new press.
- A new press turns LED ON and records that tick as the latest press time.
- Another sampled press restarts the interval.
- LED turns OFF when at least 2000 virtual milliseconds have elapsed since the latest sampled press.
- Holding the button causes no repeated activation.
- Releasing the button does not turn LED off or restart the timer.
- Initially, the application considers the button released.
- When a new press and timer expiry coincide, the new press takes precedence: LED stays ON without an intermediate OFF write.

Reference application order: sample BTN; compare with the previous sample; on a new press update the latest press time and turn LED on; otherwise turn it off if its interval has expired; finally retain the current sample. Repeating a write of the same level is legal and does not create a net-change event.

This is polling. A complete press/release between two samples may be missed. The simulator must not synthesize a callback or interrupt to make it visible to the application.

## 5. Minimal HAL

The following is the proposed logical API; names can change before implementation, but semantics must remain explicit.

```c
#include <stdint.h>

typedef uint8_t hal_pin_t;

typedef enum {
    HAL_INPUT,
    HAL_INPUT_PULLUP,
    HAL_OUTPUT
} hal_pin_mode_t;

typedef enum {
    HAL_LOW = 0,
    HAL_HIGH = 1
} hal_level_t;

void hal_gpio_mode(hal_pin_t pin, hal_pin_mode_t mode);
void hal_gpio_write(hal_pin_t pin, hal_level_t level);
hal_level_t hal_gpio_read(hal_pin_t pin);
uint32_t hal_millis(void);
```

### Semantics

- GPIO calls are synchronous and ordered.
- A mode change or write takes effect before the next HAL operation.
- A read returns the resolved net level, including effects of preceding calls in the same callback.
- Configuring pull-up on a released BTN and immediately reading it returns HIGH.
- Repeated reads are allowed and see unchanged external input unless preceding HAL operations alter the net.
- Output reads are allowed and return the resolved net level, not simply the output latch.
- Configuring the same mode twice preserves its state. Mode changes disable incompatible drive/pull-up behavior.
- Writing is permitted only in output mode. GPIO mode must be explicitly configured before application reads or writes.
- `hal_millis()` returns the low 32 bits of virtual milliseconds since reset; unsigned subtraction is used for elapsed intervals.
- HAL operations consume zero virtual time in 0.1.
- HAL calls are supported only during setup/app_step and from the controller's single execution thread.

### Diagnostic policy

| Condition | Result |
|---|---|
| Net becomes X due to opposing strong drivers | Nonfatal diagnostic on entry into X; simulation continues unless an application read faults |
| Net is Z without an application read | Valid modeled state, not automatically an error |
| Read of Z / read of X | Fault with distinct `Z_NET_READ` / `X_NET_READ` codes and pin/net IDs |
| Read/write before explicit mode configuration; invalid pin/mode/level; write to input | Fault with a specific validation code |
| HAL use outside an active callback | Fault; unsupported additional application threads remain outside the trusted execution contract |
| Protocol failure, worker exit, callback/startup timeout, HAL-operation budget exhaustion | Operational fault |

Validate a requested operation before mutating state. An invalid operation has no model effects. A valid write that produces X is applied and diagnosed under the nonfatal conflict rule. Effects of earlier successful operations are retained if a later operation faults.

The host owns the authoritative run state. On a rejected HAL request it records the fault and sends a fault response; the worker stub exits through a controlled nonzero return/termination path without returning a fabricated GPIO value to application code. The host terminates a nonresponsive worker. Do not use an assertion dialog or `abort()` as the normal fault path.

Stopping on an undefined read is an educational policy, not a claim about physical GPIO behavior.

## 6. Time and scheduling

The host owns time. Use checked unsigned 64-bit integer virtual milliseconds; `hal_millis()` exposes the low 32 bits. Host scheduling delays do not change simulated results.

### Initialization

1. Create the fixed board at t=0, button released.
2. Start a fresh worker and complete the protocol handshake.
3. Execute setup at t=0, servicing HAL calls synchronously.
4. Finish initialization in Paused state.
5. The first app_step executes at t=1 ms.

### One tick

For target time t:

1. Advance virtual time to t.
2. Apply external input events assigned to t, in stable sequence order; resolve and trace each change.
3. Execute app_step exactly once. Service each HAL operation sequentially, immediately resolving affected nets and updating derived device states.
4. Finish the tick and expose its completed state to the UI.

No external input is inserted halfway through a callback. Tick execution is serialized; the host may service protocol traffic while awaiting callback completion but must not start another tick.

Multiple input changes at the same timestamp are all traced, but the callback observes their final resulting state. A press and release assigned to the same tick may therefore be missed. Script timestamps are integer milliseconds >= 1; scripted input at t=0 is rejected. Initial board state is fixed separately.

### Controls and live input

- Step executes one complete next tick, then pauses.
- Run executes successive ticks, with a nominal real-time playback pace. A slow host may fall behind; ticks and inputs are not skipped to catch up.
- Pause takes effect after the current tick completes. It is not source-level suspension of C code.
- UI input is assigned to the next tick that has not begun when the serialized host command is accepted. Input arriving during a tick is queued for the following tick; input accepted between ticks targets the immediately upcoming tick.
- Input received while paused is queued for the next tick and does not immediately alter the authoritative board state. The UI shows the pending button state separately from the sampled board state.
- Input assignment is serialized by the host; each recorded input receives a virtual timestamp and stable order number.
- Reset discards pending live input and starts a fresh run.

Use an explicit press/release toggle for the first UI: one click queues a press and a later click queues release. Do not automatically generate both edges from one click. Multiple toggles before a tick retain their actual queue order at that tick; they are not silently spread across future ticks. The pending-state display and log must explain when the final sampled state hides intermediate edges.

A general discrete-event scheduler is not required for 0.1. The selected model is a fixed-period polling simulator; a later event-driven engine must preserve or deliberately revise its semantics.

## 7. Worker, build, and failure handling

Compile the application together with a small C runner and HAL stubs into a native x64 worker executable. The baseline compiler is MinGW-w64 GCC from the MSYS2 UCRT64 environment, using C11 and debug/warning options (`-std=c11 -Wall -Wextra -O0 -g`). Invoke the documented native compiler executable directly; WSL, a Linux worker, and a CMake dependency are not required. Record the actual compiler/package version when the first working build is established; do not invent a version pin before verification. Toolchain installation and redistribution are outside 0.1. Document runtime DLL discovery for worker launch rather than assuming the UI inherits an MSYS2 shell environment.

The C# host launches the worker. A versioned structured protocol over redirected stdin/stdout supports:

- Initialization/handshake.
- setup and app_step commands with callback ID and virtual time.
- Synchronous GPIO requests and host responses.
- Callback completion and structured fault reporting.

`hal_millis()` may use the callback timestamp cached in the worker. GPIO operations are serviced by the host individually; an input-snapshot/output-batch optimization is excluded until equivalent sequential semantics are demonstrated.

The host must service requests while waiting for callback completion. It must not wait for completion before reading requests. Simulation orchestration and process I/O must not block the WPF UI thread; UI commands enter a serialized simulation command queue.

Protocol version 1 uses bounded ASCII line messages, newline framing, stable operation tags, callback IDs, request sequence IDs, and integer fields. There is one outstanding HAL request at a time. Readers handle partial reads and accept LF or CRLF; maximum message size is 4096 bytes. Unknown, malformed, out-of-order, oversized, or mismatched-ID messages cause a protocol fault. Flush each complete message. Request/response message spellings are an implementation detail to document and check with the first worker exchange, not a new architecture decision.

Stdout is reserved for protocol traffic. Application `printf` to stdout is explicitly unsupported and may produce a diagnosed protocol fault; that limitation is accepted for trusted prototype programs. Do not add an extra IPC channel or stream redirection scheme solely to support it in 0.1. Debug text may be written to stderr, which the host drains continuously and asynchronously with bounded retention, independently of callback completion. Stderr is not part of the canonical simulation trace.

Worker EOF on the host command stream ends the worker. A protocol disconnect, unexpected EOF, or worker exit during a run faults the host run. A completed/faulted callback has an explicit terminal protocol outcome; a process failure can substitute for a missing outcome and marks the callback incomplete. Late messages from an ended run are ignored using an internal run generation ID.

Build stops and cleans up any existing paused/faulted worker before compiling. Build is disabled while Running until Pause completes. Builds use separate output locations and show compiler diagnostics. A failed build leaves no worker and clears the currently runnable build selection; an older executable may remain on disk but cannot be executed by Run, Step, or Reset. A successful build becomes the selected build and initializes a fresh run. Record the source/build identity; editing source does not silently change the selected binary.

Handshake and every callback have separate configurable wall-clock deadlines, initially 5 seconds each for a debug prototype. The callback deadline runs from command dispatch until completion and is never restarted by HAL traffic. A separate per-request timeout is not required: all protocol reads/writes are cancellable and remain covered by the active handshake/callback deadline and host shutdown.

Every HAL invocation, including locally serviced `hal_millis()`, counts against a per-callback budget of 10,000 calls. The worker checks before executing call 10,001 and reports a budget fault; the host also validates received request counts. The budget resets at setup/app_step entry. This limits repeated HAL polling; computation with no HAL calls still requires the wall-clock deadline. Budget exhaustion is a deterministic policy limit, not proof that the application contains an infinite loop.

A timeout, worker crash, protocol violation, or fatal HAL diagnostic transitions the run to Faulted and causes worker termination/cleanup. The UI and trace remain available. Continuing that worker from the fault location is unsupported; Reset or rebuilding starts a new run.

Use explicit process cleanup on normal shutdown, reset, build, and fault. For host-crash cleanup, assign the worker to a host-owned Windows Job Object with kill-on-job-close and a noninherited host-owned job handle before allowing application execution. Failure to establish that containment is a startup fault. Do not let controlled failures display modal native error dialogs. Verify these paths during implementation rather than treating the API choice as tested behavior.

Wall-clock timeouts are operational safeguards, not simulated MCU watchdogs or guarantees of deterministic fault timing.

Partial HAL effects before a fault remain visible and are marked as belonging to an incomplete callback. Do not roll them back or advance to another tick.

Worker separation is failure containment, not a security sandbox. Only trusted, locally authored programs are supported. Determinism assumes application code does not use system time, unseeded randomness, background threads, external I/O, or undefined C behavior. The prototype does not enforce this as a security boundary.

## 8. Observability and lifecycle

The trace records ordered events with virtual time, sequence ID, callback ID where applicable, event type, subject, details, and a direct source/trigger reference where known. Trace sequence IDs are globally monotonic within one run, starting at 1; callback IDs start at 0 for setup and increase by one for each app_step. Input-script sequence IDs are separate and only order inputs. All trace insertion is serialized; sequence ID is the definitive trace order, with nondecreasing virtual timestamps.

Canonical comparison uses structured records with the same fixed fields and stable board/pin/net IDs. Absent fields are null. It excludes localized/rendered text, wall-clock timestamps/durations, run generation IDs, paths, PIDs, and operational-failure records. Compare canonical sequences for successfully completed deterministic scenarios; do not promise canonical equality for runs interrupted by host timing or process failure. Record build identity, configuration, tick period, policy budgets, initial state, and input script alongside the trace, not as host-dependent event text.

Record inputs, callback boundaries, GPIO operations, mode/drive/net changes, LED changes, and faults. The UI may filter unchanged reads and repetitive callback entries without changing the underlying trace used for verification.

Retain at most 100,000 ordinary trace records per run, plus a reserved terminal diagnostic. Reaching that configured limit stops the run with `TRACE_LIMIT_REACHED` rather than silently dropping canonical records. The UI displays a bounded recent view (up to 2,000 records). These are prototype resource limits recorded in run metadata, not performance claims. High-volume stderr/compiler output is continuously drained with bounded display retention and a truncation notice.

Do not claim to infer arbitrary C data dependencies. A trace can prove that a read preceded a write within one callback; it cannot explain which C expression caused the write without instrumentation/debugging.

Directly observable: modeled pins/nets, device states, virtual time, inputs, and HAL operations. Not automatically observable: local/static C variables, call stack, current source line, registers, or target execution time.

Minimum UI: fixed labeled board diagram, clickable press/release button, LED, pin/net table, virtual time, filtered trace, diagnostics, source/build action, and Run/Pause/Step/Reset. No embedded IDE is required.

Run states and enabled commands:

| State | Allowed user commands / next transition |
|---|---|
| NoProgram | Build → Building |
| Building | Controls disabled; failure → BuildFailed; success → Initializing |
| Initializing | Controls disabled; successful setup → Paused; failure → Faulted at t=0 |
| Paused | Run → Running; Step → one tick then Paused/Faulted; Reset → Resetting; Build → Building |
| Running | Pause → Paused after current tick; fault → Faulted; Build/Step/Reset disabled until pause |
| Resetting | Controls disabled; cleanup then Initializing |
| Faulted | Reset → Resetting when a selected successful build exists; Build → Building |
| BuildFailed | Build → Building; Run/Step/Reset disabled |

Closing the application is available from every state and cancels work/cleans up processes. Live board input is accepted only while Paused or Running. A tick in progress is internally busy; duplicate control commands cannot launch another tick. Fault takes precedence over a pending Pause request.

Reset first terminates and cleans up the old worker, then recreates board state, clears time/events/pending input and the old visible trace, and launches a fresh worker for the selected successful build. It then runs setup, records a new initialization trace, and pauses. A fresh run therefore has setup records, not a permanently empty trace. This reinitializes C static/global state through process startup rather than assuming setup resets all variables.

Reset does not silently replay previous input. A separate headless replay operation starts clean and supplies a recorded script.

## 9. Acceptance criteria

1. Valid C application builds and initializes. Invalid source shows build diagnostics and cannot execute a stale build.
2. After setup: BTN is HIGH, LED is LOW/OFF, time is 0, state is Paused.
3. Scripted press at t=100 ms and release at t=150 ms: LED turns ON at 100 and OFF at 2100 ms.
4. Add a second press at 1000 ms and release at 1050 ms: LED remains ON until 3000 ms, without an intermediate OFF change.
5. Hold from 100 to 5000 ms: LED is ON only from 100 through just before 2100 ms. Holding/release causes no new activation.
6. Press at 100, release at 150, press again at 2100, release at 2150: no OFF glitch at 2100; LED turns OFF at 4100 ms.
7. Press and release in one tick produce trace entries but no sampled press if the final level is released.
8. Mode change to pull-up followed immediately by read returns HIGH on a released BTN. An output write followed by read observes the updated resolved net.
9. A configured input without pull-up faults on reading Z. BTN configured OUTPUT HIGH followed by button press resolves X and emits a nonfatal diagnostic; reading it then faults with X_NET_READ. A separate fixture reads an UNCONFIGURED pin and receives the distinct configuration fault. These are headless fixtures, not editor features.
10. Reset during illumination restores initialization state, button release, time, and a fresh setup trace. A C fixture with a static callback counter exposes its state through a different LED pattern if it survives reset; its post-reset behavior must match a fresh process, without requiring variable inspection.
11. Identical binary, configuration, initial state, and timestamped input script produce identical canonical simulation traces. Wall-clock durations, process IDs, run IDs, and host-generated failure timing are excluded from canonical comparison.
12. Repeated Steps and Run produce the same result for the same scripted inputs. Paused time does not advance.
13. A non-returning setup/app_step is terminated after timeout without closing the UI. A deliberately abnormal worker exit is diagnosed. Do not rely on undefined C behavior as the crash test.
14. Cleaned-up worker processes do not remain running after Reset, fault, closing the application, or a deliberately terminated host after worker containment has been established.
15. Reference application elapsed-time logic is checked across uint32 wraparound using a focused C test fixture, without simulating 49 days in real time.
16. A repeated-HAL-call fixture faults before call 10,001; a loop containing only hal_millis is also covered. Pure computation with no HAL calls is covered by the callback deadline.
17. Excess stderr does not block execution or freeze the UI; malformed application stdout is diagnosed as a protocol failure. Startup/handshake failure also times out without freezing the UI.
18. Trace-limit exhaustion preserves the bounded trace and emits its terminal diagnostic. Failed builds, input timing on pause, GPIO mode transitions, and stale responses are covered by focused checks where their effects are not already exercised above.

## 10. Learning limits and follow-up

This prototype practices C, nonblocking state machines, GPIO abstractions, elapsed-time logic, host/native integration, and trace-based diagnosis.

It does not reproduce a specific MCU's registers, interrupt behavior, stack/RAM limits, instruction timing, or electrical behavior.

After 0.1, reproduce the reference application on one real development board through a board-specific HAL. Compare logical behavior and explain differences such as bounce, startup state, sampling frequency, and real timing. Do this before building a broad component library.

Keep UI, simulation model, and controller execution separate, but do not promise a universal emulator interface. A future MCU emulator is a separate design exercise with concrete target/peripheral requirements.

## 11. Baseline decisions and implementation order

The architectural review phase is complete for 0.1. Do not reopen the general architecture because another implementation is possible. Change this baseline only for a demonstrated contradiction, implementation blocker, or an explicit scope change by the author.

Chosen technical defaults: Windows x64, modern .NET/WPF, MSYS2 UCRT64 GCC for C11, an external C worker, sequential synchronous GPIO, 1 ms fixed polling, and strict fault-on-undefined-read behavior. Exact .NET SDK/compiler versions are recorded when the first development environment is verified. No software has been installed by writing this specification.

Implementation order (small, reviewable increments):

1. Headless C# pin/net model and fixed board, including mode transitions and a few direct behavioral checks.
2. Build one C worker; handshake; setup/one callback; synchronous HAL; deadline and clean termination. No graphical editor.
3. Complete the reference C application and scripted timing/reset/fault scenarios.
4. Add the WPF fixed-board view, command controls, pending button state, and bounded trace view.
5. Finish acceptance checks and document the toolchain, limitations, and how to reproduce the example.

Headless scenarios are required; saving/loading scripts from the UI is excluded. No IDE, timeline graph, general protocol library, additional IPC channel, or universal emulator backend is required for 0.1.

Licensing is a separate publication decision, not an architecture blocker. MIT is the proposed default for the author's original code, pending the author's choice before public release. This document does not license code or select third-party dependencies. Record dependency/component licenses before including or distributing them; toolchain executable licensing and worker runtime redistribution must be checked separately if packaging is introduced.

Primary references used to check the technical baseline:

- [MSYS2 environments](https://www.msys2.org/docs/environments/) — UCRT64 toolchain environment.
- [WPF documentation](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/) — Windows desktop UI on .NET.
- [Windows Job Objects](https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects) — process grouping and kill-on-job-close behavior.

These references support platform choices, not claims that this simulator has been implemented or validated.
