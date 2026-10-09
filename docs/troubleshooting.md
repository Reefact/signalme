_[Version française](troubleshooting-FR.md)_

# Troubleshooting

SignalMe reports every failure on stderr and exits with a
[distinct code](commands.md#exit-codes). While it runs, what it prints on the console tells you what the
device is showing and why; the messages below are reproduced exactly.

## Unknown mode

**Symptom:** `Unknown mode: 'foo'.` followed by `Available modes: manual`, exit code `1`.

**Cause:** the value given to `--mode` is not a mode SignalMe knows. SignalMe 2.0 has one, `manual`,
which is also the default.

**What to do:** run `signalme` with no option, or `signalme --mode manual`.

## Unknown option, unknown command (command line)

**Symptom:** `Unknown option: '--bogus'.` or `Unknown command 'extra'.` (no colon), followed by
`Type 'signalme --help' for usage.`, exit code `1`. A `--mode` with no value gets the same hint, and an
argument placed after `--` reads `Unexpected argument: 'extra'.`.

**Cause:** the command line is not one SignalMe accepts. The only option is `--mode <MODE>`, with `-m` as
its short form; there are no arguments — and no sub-commands either, which is why a stray argument is
reported as an unknown command. This is not the [`Unknown command: 'buzy'.`](#unknown-command) of the
prompt, which has a colon, is followed by `Type 'help' to list available commands.` and leaves SignalMe
running.

**What to do:** `signalme --help` prints the usage. The commands you may be thinking of — `as`, `status`,
`off` — were the SignalMe 1.x command line (`signalme as busy` now prints `Unknown command 'as'.`); in
2.0, statuses and signals are typed once SignalMe runs, see
[Command reference](commands.md#interactive-commands).

## No Luxafor device detected

**Symptom:** `No Luxafor device detected.` and exit code `2`.

**Cause:** the library enumerated the USB HID devices and found no Luxafor among them.

Check that:

1. the device is plugged into a USB port;
2. you are on Windows — SignalMe cannot find a device on any other platform, see
   [Hardware and platform support](hardware.md);
3. the device works elsewhere, for example in Luxafor's own software — then close that software, it
   would hold the device.

## Could not reach a Luxafor device

**Symptom:** `Could not reach a Luxafor device:` followed by an exception type and message, exit code `2`.

**Cause:** something went wrong before enumeration could complete. The message names the cause; the two
common ones are another application currently holding the device, and running outside Windows, where the
HID library cannot load at all.

**What to do:** close the other application that drives the device, then try again. The rest of the
message is the underlying error, worth quoting in an issue if it is neither of those.

## Invalid device number

**Symptom:** `Invalid device number.` and the `Select device:` question again.

**Cause:** several devices were detected and the answer was not one of the numbers in the `#` column —
empty, not a number, `0`, or too large.

**What to do:** type the number of the row. Nothing is lost: SignalMe asks until it gets a valid one.
See [Hardware and platform support](hardware.md#multiple-devices) for the whole dialog.

## Unknown command

**Symptom:** `Unknown command: 'buzy'.` and `Type 'help' to list available commands.`, SignalMe keeps
running.

**Cause:** the line typed at the prompt is none of the statuses, signals or commands of the current mode.
Input is trimmed and lower-cased first, so capitals and spaces are not the problem.

`away` is a frequent one: it was a status in SignalMe 1.x, and is no longer something you type. SignalMe
shows it by itself while your Windows session is locked.

**What to do:** `help` lists what the mode accepts.

## Session locked: the signal is not played

**Symptom:** `Session locked: 'happy' is not played.` — the device stays purple.

**Cause:** SignalMe believes the Windows session it runs in is locked, and `away` has priority over
signals. Expected rather than a failure while the screen is locked. If you are typing at the prompt, the
session you type from is not the one SignalMe is watching, or it did not see the unlock: `status` shows
what it believes.

**What to do:** nothing, once the session is unlocked signals play again. If SignalMe keeps saying
`Session: locked` while you are typing, see
[the lock is not noticed](#signalme-does-not-notice-the-lock-or-the-unlock).

## SignalMe is off: the signal is not played

**Symptom:** `SignalMe is off: 'happy' is not played.` — the device stays dark.

**Cause:** a signal plays over a durable status and puts it back afterwards, and there is none: SignalMe
was started with nothing remembered, or `off` was typed. Off has priority over everything, signals
included. In SignalMe 1.x a signal would play over a dark device; it no longer does.

**What to do:** type a status first — `busy`, for instance — then the signal.

## Luxafor device disconnected

**Symptom:** `Luxafor device disconnected.`, then `Stopping SignalMe...` and `SignalMe stopped.`, exit
code `2`.

**Cause:** SignalMe checks every two seconds that the device it drives is still plugged in, and it no
longer was: unplugged, behind a USB hub or a dock that lost power, or cut off by the laptop's sleep.
SignalMe stops rather than claim a status no device shows, and writes nothing to the device on its way
out, since it is gone.

**What to do:** plug the device back and start SignalMe again. It starts from the last status the device
did show.

## The device refused a command, or could not be reached, during the run

**Symptom:** `The Luxafor device refused to ...` or `The Luxafor device could not be reached: ...`, then
`Stopping SignalMe...` and `SignalMe stopped.`, exit code `2`.

**Cause:** the device was found at startup but a later write failed — typically taken over by another
application, or unplugged too briefly for the check to see it go: the handle SignalMe holds does not
survive even a short absence. SignalMe does not keep running without a device it can drive,
and does not pretend the last change was applied: a refused status is not remembered.

**What to do:** plug the device back, close the application that took it, and start SignalMe again. It
starts from the last status the device did show.

## The device stayed lit after SignalMe stopped

**Symptom:** `The Luxafor device refused to turn its LEDs off.` or
`The Luxafor device could not be turned off: ...` just before `SignalMe stopped.`.

**Cause:** SignalMe turns the device off when it stops, so that a lit device never suggests it is still
watching your presence, and that last command failed — the device was most likely already gone.

**What to do:** unplug the device, or start SignalMe again and stop it. The exit code is the one of
whatever stopped SignalMe; this failure does not change it.

## SignalMe does not notice the lock, or the unlock

**Symptom:** the session is locked but the device keeps your status colour, or `status` reports
`Session: active` after a lock.

**Cause:** SignalMe only sees the lock state of the Windows session it runs in. Started as a service, or
by a scheduled task running in the background, it runs in session 0 and never receives a user's lock. On
a platform other than Windows the session is assumed active and never changes.

**What to do:** run `signalme` from a terminal in your own interactive session.

## `status` does not match the LEDs

**Symptom:** `status` says `Effective status: busy`, the device shows something else.

**Cause:** `status` reports what SignalMe asked for. Luxafor devices cannot be read back, so if another
application drove the device meanwhile, SignalMe has no way to know.

**What to do:** type the status again — `busy` — to bring the device back in line.

## SignalMe starts off although a status was set

**Symptom:** `Status: off` at startup when you expected the previous status.

**Cause:** the durable status is remembered in `%LOCALAPPDATA%\SignalMe\signalme.ini`, and that file is
absent, empty, or holds something SignalMe 2.0 does not recognise — in particular `away`, which SignalMe
1.x could write and which now means "no status". Any of those is read as off rather than treated as an
error. `off` deletes the file on purpose.

**What to do:** type the status again. Deleting the file is safe and simply makes SignalMe forget.

## signalme: an unexpected error

**Symptom:** `signalme: <Type>: <message>` and exit code `3`.

**Cause:** something SignalMe did not plan for, named by the exception type. The device has been turned
off and released all the same.

**What to do:** please [open an issue](https://github.com/Reefact/signalme/issues) with the message.

## `dotnet tool install` is not recognised

**Symptom:** the install command fails before SignalMe is involved.

**Cause:** `dotnet tool install` ships with the .NET SDK, not with the runtime alone.

**What to do:** install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
