_[Version française](commands-FR.md)_

# Command reference

The options, interactive commands, messages and exit codes on this page are the public contract of
SignalMe 2.0. Compatible additions will come in minor versions; anything incompatible would require a new
major version.

SignalMe has two levels of commands. The **command line** starts it:

```shell
signalme [--mode <MODE>]
signalme --help       # or -h
signalme --version    # or -v
```

Once it runs, the **interactive commands** are typed at its prompt:

```text
> busy
> happy
> status
> off
> help
```

## Command line

### `--mode <MODE>`, `-m <MODE>`

The mode decides where the status comes from. SignalMe 2.0 knows one mode, `manual`, in which you type
the status yourself; it is the default, so `signalme` and `signalme --mode manual` are the same thing. The
name is trimmed and compared regardless of case.

An unknown mode is a usage error, reported before any device is touched:

```text
$ signalme --mode foo
Unknown mode: 'foo'.
Available modes: manual
```

### `--help` (`-h`), `--version` (`-v`)

`--help` prints the usage, `--version` prints the version; `-h` and `-v` are their short forms. Neither
opens a device, so both work on any machine, Windows or not.

### Anything else

An unknown option, an argument SignalMe does not expect, or a `--mode` with no value is a usage error,
exit code `1`:

```text
$ signalme --bogus
Unknown option: '--bogus'.
Type 'signalme --help' for usage.

$ signalme extra
Unknown command 'extra'.
Type 'signalme --help' for usage.
```

SignalMe has no sub-commands: `Unknown command` on the command line means an argument it did not expect
(one placed after `--` is reported as `Unexpected argument: 'extra'.` instead). It is not the
`Unknown command: '…'.` of the prompt, which is about a line typed once SignalMe runs.

## Startup

```text
$ signalme
SignalMe 2.0.0-preview.1
Luxafor device detected.
Mode: manual
Status: busy
Commands: help
Press Ctrl+C to stop.

> 
```

In order:

1. **The device.** SignalMe looks for every Luxafor device once, at startup. With exactly one, it is used
   and `Luxafor device detected.` is printed. With several, SignalMe lists them and asks which one to use,
   identifying each candidate with a short light wave — see
   [Hardware and platform support](hardware.md#multiple-devices). With none, SignalMe exits with a
   device error.
2. **The mode**, `Mode: manual`.
3. **The initial status.** SignalMe starts from the durable status it remembered from the last run, and
   shows it on the device before the prompt appears; with nothing remembered it prints `Status: off` and
   the device stays dark. If the Windows session is already locked, the device shows `away` instead and
   a second line says so: `Effective status: away`.
4. **The prompt.** `Commands: help` and `Press Ctrl+C to stop.` come from the manual mode, then `> `
   waits for you.

## Interactive commands

Input is trimmed and lower-cased, so ` BUSY ` means `busy`. An empty line is ignored. Anything SignalMe
does not recognise is reported and SignalMe keeps running:

```text
> buzy
Unknown command: 'buzy'.
Type 'help' to list available commands.
```

Every command is carried out before the next prompt: a signal, for instance, plays to its end (or until
something interrupts it) before `> ` comes back.

### Durable statuses

Stay on until you change them.

| Command | Alias | Colour |
| --- | --- | --- |
| `available` | `free` | green |
| `busy` | | yellow |
| `do-not-disturb` | `dnd` | red |

```text
> busy
Status: busy
```

The status is remembered only once the device has shown it. Typed while the session is locked, it is
remembered all the same but the device keeps showing `away` until you unlock, and SignalMe says so:

```text
> busy
Status: busy
Effective status: away
```

### `away`

`away` is no longer a command. It is the **presence override** SignalMe applies by itself: while your
Windows session is locked the device is purple, whatever your status, and when you unlock it your status
comes back exactly as it was. Typing `away` is an unknown command.

### Temporary signals

Play an animation over your durable status, then put it back.

| Command | Ends on |
| --- | --- |
| `happy` | your durable status |
| `bored` | your durable status |
| `desperate` | your durable status |
| `warning` | your durable status |
| `alerting` | your durable status |
| `ready` | **`available`** |

```text
> happy
Playing: happy
Restored: busy

> ready
Playing: ready
Status: available
```

A signal needs a durable status to play over. It is refused, with no change to the device, when SignalMe
is off:

```text
> happy
SignalMe is off: 'happy' is not played.
```

and when the session is locked:

```text
> happy
Session locked: 'happy' is not played.
```

A signal that is playing is interrupted by a lock (the device goes to `away`, nothing is restored, the
signal does not resume on unlock) and by Ctrl+C. The prompt only comes back once the signal is over, so
anything you type meanwhile is applied afterwards. See [Statuses and signals](signals.md) for what each
signal looks like and the exact rules.

### `status`

Prints where things stand:

```text
> status
Mode: manual
Desired status: busy
Effective status: away
Session: locked
```

- **Desired status** is the durable status you asked for (`none` when SignalMe is off).
- **Effective status** is what the device shows: `off`, `away`, `available`, `busy` or
  `do-not-disturb`.
- **Session** is `active` or `locked`.

It reports SignalMe's own state. Luxafor devices cannot report their LEDs back, so if another
application drove the device meanwhile, SignalMe has no way to know.

### `off`

Turns every LED off and forgets the durable status:

```text
> off
Status: off
```

Until you type a status again, signals are refused and a locked session changes nothing: off has
priority over everything.

### `help`

Lists the commands of the current mode:

```text
> help
Statuses:
  available
  free
  busy
  do-not-disturb
  dnd

Signals:
  happy
  bored
  desperate
  warning
  alerting
  ready

Commands:
  status
  off
  help

Press Ctrl+C to stop SignalMe.
```

## Session lock and unlock

SignalMe watches the Windows session it runs in. A lock or an unlock is applied at once and announced on
the console, even while the prompt is waiting — the notification takes lines of its own and the prompt is
printed again after it:

```text
> 
Windows session locked.
Effective status: away
> 
Windows session unlocked.
Effective status: busy
> 
```

With SignalMe off, the device stays off through a lock and the notification says `Effective status: off`.
Only the session SignalMe runs in is watched; see
[Hardware and platform support](hardware.md#session-monitoring).

## Stopping

Ctrl+C is the normal way to stop. SignalMe interrupts any signal, turns the device off, releases it and
exits with `0`:

```text
> ^C
Stopping SignalMe...
SignalMe stopped.
```

The device is turned off on purpose: a lit device after SignalMe has stopped would suggest it is still
watching your presence. Your durable status stays remembered for the next run.

Closing the input (end of file on the console) stops SignalMe the same way. Ctrl+C, or closing the input,
during the device selection prints `SignalMe stopped.` and exits with `0` too.

## Exit codes

| Code | Meaning |
| ---: | --- |
| `0` | SignalMe ran and stopped cleanly: Ctrl+C, or the input was closed |
| `1` | Usage error — unknown mode, unknown option, missing option value, unexpected argument |
| `2` | Device error — no device found, discovery failed, or the device refused a command during the run |
| `3` | Unexpected error |

A device that stops answering while SignalMe runs ends the run: the failure is reported, the device is
turned off as far as it still can be, and SignalMe exits with `2` rather than pretending the last change
was applied.

Scripting against the exit codes:

```shell
signalme || echo "SignalMe did not stop cleanly"
```

## Example session

```text
$ signalme
SignalMe 2.0.0-preview.1
Luxafor device detected.
Mode: manual
Status: busy
Commands: help
Press Ctrl+C to stop.

> available
Status: available

> happy
Playing: happy
Restored: available

> status
Mode: manual
Desired status: available
Effective status: available
Session: active

> 
Windows session locked.
Effective status: away
> 
Windows session unlocked.
Effective status: available
> dnd
Status: do-not-disturb

> off
Status: off

> happy
SignalMe is off: 'happy' is not played.

> ^C
Stopping SignalMe...
SignalMe stopped.
```
