_[Version française](signals-FR.md)_

# Statuses and signals

SignalMe distinguishes between durable statuses and temporary signals.

A **durable status** stays on until you change it. A **temporary signal** plays an animation over it and
then puts it back. `ready` is the one exception, and it is deliberate.

Both are typed at SignalMe's prompt; see the [Command reference](commands.md) for the commands
themselves. This page is about what each one does to the device, and what happens when a signal does not
run to its end.

## Durable statuses

| Status | Alias | Colour |
| --- | --- | --- |
| `available` | `free` | green |
| `busy` | | yellow |
| `do-not-disturb` | `dnd` | red |
| `away` | | purple (`#9932CC`) — **automatic**, while the Windows session is locked |

Setting a status lights every LED in its colour and records it locally, so SignalMe can start from it
next time and a signal knows what to return to. The status is recorded only once the device has confirmed
the change — if the device refuses, SignalMe reports the failure rather than remembering a colour the LEDs
never showed.

### Desired and effective

SignalMe keeps two things apart. The **desired status** is what you asked for. The **effective status** is
what the device shows, and it is computed in this order of priority:

1. SignalMe is off — the device is off, whatever else is going on;
2. the Windows session is locked — the device shows `away`;
3. a signal was asked for — it plays;
4. otherwise the device shows the desired status.

So a lock never changes your desired status: it overrides what is displayed, and the override is lifted
on unlock. This is why `away` cannot be typed any more — it is a presence override, not a choice. A
status typed while the session is locked is recorded and will show once you unlock.

### The record

The record lives in a single small text file under your local application data
(`%LOCALAPPDATA%\SignalMe\signalme.ini` on Windows). It holds the desired status — never the `away` a
lock produces — and is deleted by `off`. It is written to a temporary file and moved into place, so an
interruption mid-write cannot corrupt it. Delete it and SignalMe simply forgets; nothing else reads or
writes it.

A file written by SignalMe 1.x that says `away` is read as "no status": SignalMe 2.0 starts off in that
case, and the next status you type replaces the file.

## Temporary signals

A signal plays over the durable status you have at that moment: the animations start from that status
colour and the status is what comes back when they end. A signal therefore needs a durable
status, and SignalMe refuses one when it is off, or while the session is locked, without touching the
device:

```text
SignalMe is off: 'happy' is not played.
Session locked: 'happy' is not played.
```

### `happy`

**Intent:** a light, celebratory signal.

**Animation:** fades to a pastel version of your status colour, runs a pastel rainbow wave across the
LEDs, then fades back.

**After completion:** the durable status is restored.

### `bored`

**Intent:** idle, nothing happening.

**Animation:** drifts through deep purples, one LED at a time and out of order, then fades every LED to
the status colour.

**After completion:** the durable status is restored.

### `desperate`

**Intent:** call for help, with a bit of humour.

**Animation:** blinks S-O-S in white — three short, three long, three short.

**After completion:** the durable status is restored.

### `warning`

**Intent:** something needs attention.

**Animation:** alternates red and blue between the front and back LEDs, like an emergency light, five
times.

**After completion:** the durable status is restored.

### `alerting`

**Intent:** something needs attention now.

**Animation:** twenty fast red flashes.

**After completion:** the durable status is restored.

### `ready`

**Intent:** announce that you are available again.

**Animation:** steps down from `do-not-disturb` through `busy` if that is where you were, then blinks
green.

**After completion:** the durable status becomes `available`, and is recorded as such.

Unlike every other temporary signal, `ready` intentionally does not restore the previous durable status
after a successful run. If it does not complete, nothing changes: you are only marked available once the
signal has actually got there.

## What happens when a signal ends — or does not

The animations themselves know nothing about restoration: SignalMe's runtime owns the device and decides
what it shows once a signal is over, whichever way it is over.

| | Result |
| --- | --- |
| The signal runs to its end | The durable status is displayed again: `Restored: busy`. After `ready`: `Status: available`, now the durable status. |
| The Windows session is locked during the signal | The animation stops at once, nothing is restored, the device shows `away`. On unlock the durable status comes back; the signal does not resume. |
| Ctrl+C during the signal | The animation stops, the device is turned off, SignalMe exits with `0`. |
| The device refuses a frame, or stops answering | The failure is reported, SignalMe turns the device off as far as it still can and exits with `2`. The durable status stays remembered for the next run. |

A signal always starts from a durable status, so "restored" is never "off": a signal interrupted by a
lock returns to your status on unlock, and `ready` interrupted by anything leaves you where you were.

The runtime has one more rule, which the manual mode cannot reach because its prompt only comes back once
a signal is over: a new request arriving while a signal plays interrupts it — a status is displayed at
once, another signal replaces the running one, off turns the device off. A future mode fed by a presence
service rather than by the keyboard will rely on it.
