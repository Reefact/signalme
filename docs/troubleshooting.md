# Troubleshooting

SignalMe reports every failure on stderr and exits with a
[distinct code](commands.md#exit-codes). If a command said nothing and exited `0`, it worked.

## No Luxafor device detected

**Symptom:** `No Luxafor device detected.` and exit code `2`.

**Cause:** the library enumerated the USB HID devices and found no Luxafor among them.

Check that:

1. the device is plugged into a USB port;
2. you are on Windows — SignalMe cannot find a device on any other platform, see
   [Hardware and platform support](hardware.md);
3. the device works elsewhere, for example in Luxafor's own software.

## Could not reach a Luxafor device

**Symptom:** `Could not reach a Luxafor device:` followed by an exception type and message, exit code `2`.

**Cause:** something went wrong before enumeration could complete. The message names the cause; the two
common ones are another application currently holding the device, and running outside Windows, where the
HID library cannot load at all.

**What to do:** close the other application that drives the device, then try again. The rest of the
message is the underlying error, worth quoting in an issue if it is neither of those.

## The device refused a command

**Symptom:** `The Luxafor device refused to ...` and exit code `2`.

**Cause:** the device was found but rejected a write — typically unplugged mid-command, or taken over by
another application while SignalMe was running.

**What to do:** run the command again. If a temporary signal was interrupted this way, your durable status
was restored first, so nothing is left half applied.

## `signalme status` does not match the LEDs

**Symptom:** `status` says `busy`, the device shows something else.

**Cause:** expected. `status` reports what SignalMe last requested. Luxafor devices cannot be read back, so
if another application drove the device afterwards, SignalMe has no way to know.

**What to do:** run the status again — `signalme as busy` — to bring the device back in line with what
SignalMe remembers.

## A signal was interrupted

**Symptom:** `Interrupted. The previous status was restored.` and exit code `4`.

**Cause:** you pressed Ctrl+C during a temporary signal.

**What to do:** nothing. The animation stopped and your durable status was put back.

## The durable status file is missing or unreadable

**Symptom:** `signalme status` prints `No durable status is currently set.` when you expected a status.

**Cause:** the file is absent, empty, or holds something SignalMe does not recognise. Any of those is read
as "no status" rather than treated as an error.

**What to do:** set the status again. The file lives at `%LOCALAPPDATA%\SignalMe\signalme.ini`; deleting
it is safe and simply makes SignalMe forget.

## `dotnet tool install` is not recognised

**Symptom:** the install command fails before SignalMe is involved.

**Cause:** `dotnet tool install` ships with the .NET SDK, not with the runtime alone.

**What to do:** install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
