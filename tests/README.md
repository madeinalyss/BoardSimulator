# Tests

This directory reserves the location for automated checks. No test project or
test runner has been selected or added yet.

The console application in `examples/BoardSimulator.Demo` provides demonstrations
only. A successful solution build does not establish that the prototype’s
acceptance criteria pass.

The first checks should cover the existing Core behavior:

- Net resolution: no drive, strong Low/High, opposing strong drivers, and pull-up.
- GPIO state derivation for each defined mode and output-latch level.
- Mode transitions: pull-up removal, output initialization to Low, and same-mode
  preservation of the output latch.
- Output writes and rejection of invalid operations before changing state.

Add GPIO-read checks when reads exist, and connection-ownership checks when that
invariant is implemented. Tick timing, reset, worker failures, and canonical
traces belong to later checks as their implementations become available.

When an automated test project is added, record the chosen runner and exact
commands here.
