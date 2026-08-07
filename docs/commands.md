_[Version française](commands-FR.md)_

# Command reference

The commands, values and exit codes on this page are the public contract of SignalMe 1.0. Compatible
additions will come in minor versions; anything incompatible would require a new major version.

```shell
signalme as <status-or-signal>
signalme status
signalme off
signalme --help
```

## `signalme as <status-or-signal>`

Sets a durable status, or plays a temporary signal. The argument is trimmed and lower-cased, so
`signalme as " BUSY "` works.

The value is validated before a device is opened: a typo is reported as a typo, and the accepted values
are listed, rather than a complaint about missing hardware.

### Durable statuses

Stay on until you change them.

| Value | Alias | Colour |
| --- | --- | --- |
| `available` | `free` | green |
| `busy` | | yellow |
| `away` | | purple |
| `do-not-disturb` | `dnd` | red |

### Temporary signals

Play an animation, then restore the durable status.

| Value | Ends on |
| --- | --- |
| `happy` | the previous durable status |
| `bored` | the previous durable status |
| `desperate` | the previous durable status |
| `warning` | the previous durable status |
| `alerting` | the previous durable status |
| `ready` | **`available`** |

See [Statuses and signals](signals.md) for what each one looks like and for the exact restoration rules.

## `signalme status`

Prints the durable status last recorded by SignalMe.

**It does not query the physical Luxafor device.** Luxafor devices cannot report their LED state back, so
this command tells you what SignalMe last requested, not necessarily what the LEDs are displaying. The two
can disagree if another application drove the device afterwards.

It reads a local file only, so it works with no device connected.

```shell
$ signalme status
busy

$ signalme off && signalme status
No durable status is currently set.
```

Aliases are normalised: after `signalme as dnd`, `status` prints `do-not-disturb`.

## `signalme off`

Turns every LED off and forgets the durable status.

Alias: `switch-off`.

## Exit codes

| Code | Meaning |
| ---: | --- |
| `0` | Success |
| `1` | Usage error — unknown status or signal |
| `2` | Device error — none found, or the device refused a command |
| `3` | Unexpected error |
| `4` | Interrupted by the user |

A command never reports success for something the device refused. Pressing Ctrl+C during a temporary
signal stops the animation, restores the previous durable status, and exits with `4`.

## Examples

```shell
# Durable statuses, with their aliases
signalme as available
signalme as free
signalme as busy
signalme as away
signalme as do-not-disturb
signalme as dnd

# Temporary signals
signalme as happy
signalme as bored
signalme as desperate
signalme as warning
signalme as alerting
signalme as ready

# Everything else
signalme status
signalme off
signalme switch-off
```

Scripting against the exit codes:

```shell
signalme as busy || echo "could not reach the device"
```
