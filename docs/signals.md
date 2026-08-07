# Statuses and signals

SignalMe distinguishes between durable statuses and temporary signals.

A **durable status** stays on until you change it. A **temporary signal** plays an animation and then puts
your durable status back. `ready` is the one exception, and it is deliberate.

## Durable statuses

| Status | Alias | Colour |
| --- | --- | --- |
| `available` | `free` | green |
| `busy` | | yellow |
| `away` | | purple (`#9932CC`) |
| `do-not-disturb` | `dnd` | red |

Setting a status lights every LED in its colour and records it locally, so a later signal knows what to
return to. The status is recorded only once the device has confirmed the change — if the device refuses,
SignalMe fails rather than remembering a colour the LEDs never showed.

The record lives in a single small text file under your local application data
(`%LOCALAPPDATA%\SignalMe\signalme.ini` on Windows). It is written to a temporary file and moved into
place, so an interruption mid-write cannot corrupt it. Delete it and SignalMe simply forgets; nothing else
reads or writes it.

## Temporary signals

### `happy`

**Intent:** a light, celebratory signal.

**Animation:** fades to a pastel version of your current status colour, runs a pastel rainbow wave across
the LEDs, then fades back.

**After completion:** the previous durable status is restored.

### `bored`

**Intent:** idle, nothing happening.

**Animation:** drifts through deep purples, one LED at a time and out of order, then fades every LED to
the status colour.

**After completion:** the previous durable status is restored.

### `desperate`

**Intent:** call for help, with a bit of humour.

**Animation:** blinks S-O-S in white — three short, three long, three short.

**After completion:** the previous durable status is restored.

### `warning`

**Intent:** something needs attention.

**Animation:** alternates red and blue between the front and back LEDs, like an emergency light, five
times.

**After completion:** the previous durable status is restored.

### `alerting`

**Intent:** something needs attention now.

**Animation:** twenty fast red flashes.

**After completion:** the previous durable status is restored.

### `ready`

**Intent:** announce that you are available again.

**Animation:** steps down from `do-not-disturb` through `busy` if that is where you were, then blinks
green.

**After completion:** the durable status becomes `available`.

Unlike every other temporary signal, `ready` intentionally does not restore the previous durable status
after a successful run. If it does not complete, it falls back to the status you started from — you are
only marked available once the signal has actually got there.

## Restoration rules

What happens to your durable status in each case:

| | Result |
| --- | --- |
| Signal succeeds | Status restored. Exit `0`. |
| Signal fails | Status restored, then the original failure is reported. |
| Ctrl+C | Animation stops, status restored. Exit `4`. |
| Restore fails after a successful signal | The command fails with a device error. Leaving the LEDs on an animation colour is not a success. |
| Signal and restore both fail | The original signal failure stays the reported error; the restore failure is reported separately on stderr and never hides it. |

If there was no durable status to begin with, "restored" means the LEDs are turned off.
