#region Usings declarations

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Infrastructure;

#endregion

namespace SignalMe.Devices;

/// <summary>
///     Picks the device signalme will drive: the only one when there is one, otherwise the one the user
///     chooses after watching it play the identification wave.
/// </summary>
/// <remarks>
///     <para>
///         Owns every discovered device until it hands one back: the others are disposed on the way out,
///         and when the dialog ends early, on Ctrl+C or because the input closed, every device is disposed,
///         after the one last identified has been turned off. Nothing is read from the console before the
///         dialog needs an answer, so a single device never touches the input at all: the CI agent has none.
///     </para>
///     <para>
///         The table is drawn by hand rather than through Spectre.Console, whose rendering bends to the
///         terminal width it detects and collapses cells to "…" once the output is redirected. The id
///         column must show the whole path: that is what the user matches against the device they
///         recognised.
///     </para>
/// </remarks>
public sealed class LuxaforDeviceSelector {

    #region Statics members declarations

    private static string RenderTable(IReadOnlyList<ILuxaforDevice> devices) {
        int numberWidth = Math.Max(1, devices.Count.ToString(CultureInfo.InvariantCulture).Length);
        int idWidth     = Math.Max("Id".Length, devices.Max(device => device.Path.Length));

        string Border(char left, char middle, char right) {
            return $"{left}{new string('─', numberWidth + 2)}{middle}{new string('─', idWidth + 2)}{right}";
        }

        string Row(string number, string id) {
            return $"│ {number.PadLeft(numberWidth)} │ {id.PadRight(idWidth)} │";
        }

        List<string> lines = new(devices.Count + 4) {
            Border('┌', '┬', '┐'),
            Row("#", "Id"),
            Border('├', '┼', '┤')
        };
        for (int index = 0; index < devices.Count; index++) {
            lines.Add(Row((index + 1).ToString(CultureInfo.InvariantCulture), devices[index].Path));
        }
        lines.Add(Border('└', '┴', '┘'));

        return string.Join(Environment.NewLine, lines);
    }

    private static void DisposeAll(IReadOnlyList<ILuxaforDevice> devices, ILuxaforDevice? except = null) {
        foreach (ILuxaforDevice device in devices) {
            if (!ReferenceEquals(device, except)) { device.Dispose(); }
        }
    }

    #endregion

    #region Fields declarations

    private readonly IConsole _console;
    private readonly IDelay   _delay;

    #endregion

    #region Constructors declarations

    public LuxaforDeviceSelector(IConsole console, IDelay delay) {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(delay);

        _console = console;
        _delay   = delay;
    }

    #endregion

    /// <summary>
    ///     Returns the device to use; every other device has been disposed. On cancellation or end of
    ///     input, turns the device last identified off (best effort), disposes every device and throws
    ///     <see cref="OperationCanceledException" />. The token is checked before anything else.
    /// </summary>
    /// <param name="devices">The discovered devices, at least one: an empty discovery is the caller's to report.</param>
    public async Task<ILuxaforDevice> SelectAsync(IReadOnlyList<ILuxaforDevice> devices, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(devices);
        if (devices.Count == 0) { throw new ArgumentException("No device to choose from: an empty discovery is reported by the caller, before any dialog.", nameof(devices)); }

        // A Ctrl+C that landed during the discovery stops signalme before the dialog starts; the devices
        // are let go here, since nobody else holds them yet.
        if (cancellationToken.IsCancellationRequested) {
            DisposeAll(devices);

            throw new OperationCanceledException(cancellationToken);
        }

        if (devices.Count == 1) {
            _console.WriteLine("Luxafor device detected.");

            return devices[0];
        }

        // The dialog is spaced as the spec draws it (§43): a blank line under the banner, before the table,
        // before each question, before each announcement. Written as lines of their own rather than folded
        // into the blocks, so that a test reads the transcript as the user sees the screen.
        ILuxaforDevice? identified = null;
        try {
            _console.WriteLine(string.Empty);
            _console.WriteLine($"{devices.Count} Luxafor devices detected.");
            _console.WriteLine(string.Empty);
            _console.WriteLine(RenderTable(devices));

            while (true) {
                int            number    = await AskForNumberAsync(devices.Count, cancellationToken).ConfigureAwait(false);
                ILuxaforDevice candidate = devices[number - 1];

                identified = candidate;
                _console.WriteLine(string.Empty);
                _console.WriteLine($"Identifying device #{number}...");
                try {
                    await DeviceIdentificationWave.PlayAsync(candidate, _delay, cancellationToken).ConfigureAwait(false);
                } catch (DeviceCommandFailedException exception) {
                    // The wave has turned its LEDs off already, as far as the device let it. Failing its own
                    // identification does not disqualify a device: the user may pick another, or try again.
                    _console.WriteError(exception.Message);

                    continue;
                }

                // The wave ran to its end and the device obeyed its final turn-off: whatever the answer to
                // the question, and however the dialog ends from here, its LEDs are known to be off.
                identified = null;

                if (await ConfirmAsync(cancellationToken).ConfigureAwait(false)) {
                    _console.WriteLine(string.Empty);
                    _console.WriteLine($"Device selected: {candidate.Path}");
                    DisposeAll(devices, candidate);

                    return candidate;
                }
            }
        } catch {
            // A device still held here is one whose wave was cut short or failed. The wave turns its LEDs
            // off by itself on that path but ignores whether the device obeyed; this last attempt is the
            // one that reports, before the device is released for good.
            identified?.TurnOffQuietly(_console);
            DisposeAll(devices);

            throw;
        }
    }

    private async Task<int> AskForNumberAsync(int count, CancellationToken cancellationToken) {
        while (true) {
            string answer = await ReadAnswerAsync("Select device: ", cancellationToken).ConfigureAwait(false);
            if (int.TryParse(answer, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number) && number >= 1 && number <= count) { return number; }

            _console.WriteLine("Invalid device number.");
        }
    }

    private async Task<bool> ConfirmAsync(CancellationToken cancellationToken) {
        while (true) {
            string answer = await ReadAnswerAsync("Use this device? [Y/N]: ", cancellationToken).ConfigureAwait(false);
            if (string.Equals(answer.Trim(), "y", StringComparison.OrdinalIgnoreCase)) { return true; }
            if (string.Equals(answer.Trim(), "n", StringComparison.OrdinalIgnoreCase)) { return false; }

            // Anything else is simply asked again: the prompt already says what is expected.
        }
    }

    /// <summary>
    ///     An input that ends is a user who is gone, Ctrl+C or not: the dialog cannot be completed without
    ///     an answer, so the end of input is treated exactly like a cancellation.
    /// </summary>
    private async Task<string> ReadAnswerAsync(string prompt, CancellationToken cancellationToken) {
        // Every question sits under a blank line, whatever preceded it: the table, a refused answer, the
        // report of a failed wave, or the question's own previous ask.
        _console.WriteLine(string.Empty);
        _console.Write(prompt);
        string? answer = await _console.ReadLineAsync(cancellationToken).ConfigureAwait(false);

        return answer ?? throw new OperationCanceledException("The console input ended before a device was selected.");
    }

}
