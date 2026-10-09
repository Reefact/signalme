using Reefact.LuxaforLightingDeviceController;

using SignalMe.Devices;
using SignalMe.Infrastructure;
using SignalMe.Tests.Fakes;

namespace SignalMe.Tests;

/// <summary>
///     The dialog that picks one device among several, and what is left of the devices afterwards: the
///     chosen one off and alive, the others disposed, whichever way the dialog ended.
/// </summary>
public sealed class LuxaforDeviceSelectorTests {

    private const string FirstId  = @"\\?\hid#vid_04d8&pid_f372#7&1";
    private const string SecondId = @"\\?\hid#vid_04d8&pid_f372#7&2";

    /// <summary>Twelve white frames then the LEDs off: what one identification wave leaves in a device's record.</summary>
    private static readonly string[] Wave = [.. Enumerable.Range(0, 12).Select(frame => Frame(frame % 6 + 1)), "TurnOff"];

    public static TheoryData<string> InvalidNumbers => new() { "", "   ", "abc", "0", "-1", "3", "1.5" };

    private static string Frame(int led) {
        return $"Send(Set LED n° {led} color to #FFFFFF)";
    }

    private static (FakeLuxaforDevice First, FakeLuxaforDevice Second) TwoDevices() {
        return (new FakeLuxaforDevice { Path = FirstId }, new FakeLuxaforDevice { Path = SecondId });
    }

    private static Task<ILuxaforDevice> SelectAsync(FakeConsole console, IReadOnlyList<ILuxaforDevice> devices, IDelay? delay = null, CancellationToken cancellationToken = default) {
        return new LuxaforDeviceSelector(console, delay ?? new InstantDelay()).SelectAsync(devices, cancellationToken);
    }

    #region one device

    [Fact]
    public async Task A_single_device_is_taken_without_asking() {
        FakeConsole       console = new();
        FakeLuxaforDevice device  = new();

        ILuxaforDevice selected = await SelectAsync(console, [device]);

        Check.That(selected).IsSameReferenceAs(device);
        Check.That(console.Output).ContainsExactly(["Luxafor device detected."]);
        Check.That(console.Reads).IsEqualTo(0);
        Check.That(device.Commands).IsEmpty();
        Check.That(device.IsDisposed).IsFalse();
    }

    [Fact]
    public void An_empty_discovery_is_not_the_selector_s_to_handle() {
        Check.ThatCode(() => SelectAsync(new FakeConsole(), [])).Throws<ArgumentException>();
    }

    #endregion

    #region the table

    [Fact]
    public async Task Several_devices_are_counted_and_listed_by_their_ids() {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new("2", "y");

        await SelectAsync(console, [first, second]);

        // Under a blank line: the banner is the caller's last word before the dialog starts (spec §43).
        Check.That(console.Output[0]).IsEqualTo("");
        Check.That(console.Output[1]).IsEqualTo("2 Luxafor devices detected.");
        Check.That(console.Output[2]).IsEqualTo("");
        Check.That(console.Output[3]).Contains(FirstId);
        Check.That(console.Output[3]).Contains(SecondId);
    }

    [Fact]
    public async Task The_table_is_drawn_by_hand_and_padded_to_the_longest_id() {
        FakeLuxaforDevice first   = new() { Path = "A" };
        FakeLuxaforDevice second  = new() { Path = "LONGER-ID" };
        FakeConsole       console = new("1", "y");

        await SelectAsync(console, [first, second]);

        string[] expected = [
            "┌───┬───────────┐",
            "│ # │ Id        │",
            "├───┼───────────┤",
            "│ 1 │ A         │",
            "│ 2 │ LONGER-ID │",
            "└───┴───────────┘"
        ];
        Check.That(console.Output[3]).IsEqualTo(string.Join(Environment.NewLine, expected));
    }

    [Fact]
    public async Task The_number_column_widens_past_nine_devices() {
        FakeLuxaforDevice[] devices = Enumerable.Range(1, 10).Select(number => new FakeLuxaforDevice { Path = $"dev{number}" }).ToArray();
        FakeConsole         console = new("10", "y");

        ILuxaforDevice selected = await SelectAsync(console, devices);

        Check.That(selected).IsSameReferenceAs(devices[9]);
        Check.That(console.Output[3]).Contains("│  # │ Id    │");
        Check.That(console.Output[3]).Contains("│  1 │ dev1  │");
        Check.That(console.Output[3]).Contains("│ 10 │ dev10 │");
    }

    #endregion

    #region choosing

    [Fact]
    public async Task The_dialog_of_the_specification_runs_in_that_order() {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new("1", "n", "2", "y");

        ILuxaforDevice selected = await SelectAsync(console, [first, second]);

        Check.That(selected).IsSameReferenceAs(second);
        // Spaced as spec §43 draws it: a blank line under the banner, before the table, each question and
        // each announcement.
        string[] expected = [
            "line: ",
            "line: 2 Luxafor devices detected.",
            "line: ",
            $"line: {console.Output[3]}",
            "line: ",
            "prompt: Select device: ",
            "read: 1",
            "line: ",
            "line: Identifying device #1...",
            "line: ",
            "prompt: Use this device? [Y/N]: ",
            "read: n",
            "line: ",
            "prompt: Select device: ",
            "read: 2",
            "line: ",
            "line: Identifying device #2...",
            "line: ",
            "prompt: Use this device? [Y/N]: ",
            "read: y",
            "line: ",
            $"line: Device selected: {SecondId}"
        ];
        Check.That(console.Transcript).IsEqualTo(expected);
    }

    [Fact]
    public async Task Confirming_selects_the_device_and_disposes_the_others() {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new("2", "y");

        ILuxaforDevice selected = await SelectAsync(console, [first, second]);

        Check.That(selected).IsSameReferenceAs(second);
        Check.That(first.IsDisposed).IsTrue();
        Check.That(second.IsDisposed).IsFalse();
        Check.That(console.Output[^1]).IsEqualTo($"Device selected: {SecondId}");
    }

    [Fact]
    public async Task The_wave_plays_on_the_chosen_device_only_and_hands_it_over_off() {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new("2", "y");

        ILuxaforDevice selected = await SelectAsync(console, [first, second]);

        Check.That(first.Commands).IsEmpty();
        Check.That(second.Commands).IsEqualTo(Wave);
        Check.That(((FakeLuxaforDevice)selected).LastCommand).IsEqualTo("TurnOff");
    }

    [Theory]
    [MemberData(nameof(InvalidNumbers))]
    public async Task An_invalid_number_is_refused_and_asked_again(string input) {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new(input, "2", "y");

        ILuxaforDevice selected = await SelectAsync(console, [first, second]);

        Check.That(selected).IsSameReferenceAs(second);
        Check.That(console.Prompts).ContainsExactly(["Select device: ", "Select device: ", "Use this device? [Y/N]: "]);
        // The refusal follows the answer directly; the question comes back under a blank line (spec §7.2).
        Check.That(console.Transcript.Skip(5).Take(6)).ContainsExactly(["prompt: Select device: ", $"read: {input}", "line: Invalid device number.", "line: ", "prompt: Select device: ", "read: 2"]);
        Check.That(console.Output.Skip(4).Where(block => block.Length > 0)).ContainsExactly(["Invalid device number.", "Identifying device #2...", $"Device selected: {SecondId}"]);
        // Nothing was identified on a refused number: no wave went out.
        Check.That(first.Commands).IsEmpty();
    }

    [Fact]
    public async Task Declining_a_device_asks_for_a_number_again_and_the_next_choice_plays_the_wave_again() {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new("1", "n", "1", "y");

        ILuxaforDevice selected = await SelectAsync(console, [first, second]);

        Check.That(selected).IsSameReferenceAs(first);
        Check.That(console.Prompts).ContainsExactly(["Select device: ", "Use this device? [Y/N]: ", "Select device: ", "Use this device? [Y/N]: "]);
        Check.That(first.Commands).ContainsExactly([.. Wave, .. Wave]);
        Check.That(second.Commands).IsEmpty();
        Check.That(second.IsDisposed).IsTrue();
        Check.That(first.IsDisposed).IsFalse();
    }

    [Theory]
    [InlineData("Y")]
    [InlineData("y")]
    [InlineData(" y ")]
    public async Task The_confirmation_accepts_yes_in_any_case(string answer) {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new("1", answer);

        ILuxaforDevice selected = await SelectAsync(console, [first, second]);

        Check.That(selected).IsSameReferenceAs(first);
    }

    [Theory]
    [InlineData("N")]
    [InlineData("n")]
    public async Task The_confirmation_accepts_no_in_any_case(string answer) {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new("1", answer, "2", "y");

        ILuxaforDevice selected = await SelectAsync(console, [first, second]);

        Check.That(selected).IsSameReferenceAs(second);
    }

    [Fact]
    public async Task Any_other_answer_to_the_confirmation_asks_again() {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new("1", "maybe", "", "yes", "y");

        ILuxaforDevice selected = await SelectAsync(console, [first, second]);

        Check.That(selected).IsSameReferenceAs(first);
        Check.That(console.Prompts).ContainsExactly(["Select device: ", "Use this device? [Y/N]: ", "Use this device? [Y/N]: ", "Use this device? [Y/N]: ", "Use this device? [Y/N]: "]);
        // Asked again, not identified again: one wave only.
        Check.That(first.Commands).IsEqualTo(Wave);
    }

    #endregion

    #region device failures

    [Fact]
    public async Task A_device_failing_during_its_wave_is_reported_and_a_number_is_asked_again() {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        first.ThrowOnCall = 3;
        FakeConsole console = new("1", "2", "y");

        ILuxaforDevice selected = await SelectAsync(console, [first, second]);

        Check.That(selected).IsSameReferenceAs(second);
        // Two frames got through, then the clean-up; the LEDs were off before the question came back.
        Check.That(first.Commands).ContainsExactly([Frame(1), Frame(2), "TurnOff"]);
        Check.That(console.Prompts).ContainsExactly(["Select device: ", "Select device: ", "Use this device? [Y/N]: "]);
        Check.That(console.Error).HasSize(1);
        string error = console.Error[0];
        Check.That(error).Contains("USB write failed");
        Check.That(console.Transcript.Skip(8).Take(4)).ContainsExactly(["line: Identifying device #1...", $"error: {error}", "line: ", "prompt: Select device: "]);
        Check.That(first.IsDisposed).IsTrue();
    }

    [Fact]
    public async Task A_device_refusing_its_wave_is_reported_and_a_number_is_asked_again() {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        first.RefuseFromCall = 1;
        FakeConsole console = new("1", "2", "y");

        ILuxaforDevice selected = await SelectAsync(console, [first, second]);

        Check.That(selected).IsSameReferenceAs(second);
        Check.That(console.Error).ContainsExactly(["The Luxafor device refused to run the command \"Set LED n° 1 color to #FFFFFF\"."]);
        Check.That(console.Prompts).ContainsExactly(["Select device: ", "Select device: ", "Use this device? [Y/N]: "]);
    }

    /// <summary>
    ///     The one exit where the device may still be lit: its wave ended on a refused turn-off and the
    ///     dialog was then abandoned. The selector tries once more and says so, rather than releasing a
    ///     device it knows nothing about.
    /// </summary>
    [Fact]
    public void Cancelling_after_a_device_refused_to_switch_off_retries_the_turn_off_on_the_way_out_and_reports_it() {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        first.RefuseFromCall = Wave.Length; // the wave's final turn-off, and everything after it
        FakeConsole console = new("1");

        Check.ThatCode(() => SelectAsync(console, [first, second])).Throws<OperationCanceledException>();

        Check.That(console.Prompts).ContainsExactly(["Select device: ", "Select device: "]);
        // The wave's own refusal first, then the retry's: two attempts, both reported.
        Check.That(console.Error).ContainsExactly(["The Luxafor device refused to turn its LEDs off.", "The Luxafor device refused to turn its LEDs off."]);
        // Every frame got through; no turn-off ever did.
        Check.That(first.Commands).IsEqualTo(Wave[..^1]);
        Check.That(second.Commands).IsEmpty();
        Check.That(first.IsDisposed).IsTrue();
        Check.That(second.IsDisposed).IsTrue();
    }

    #endregion

    #region cancellation

    [Fact]
    public async Task Cancelling_while_a_number_is_awaited_leaves_the_identified_device_off_and_disposes_every_device() {
        using CancellationTokenSource cancellation = new();
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new("1", "n") { EndOfInput = false };

        Task<ILuxaforDevice> selecting = SelectAsync(console, [first, second], cancellationToken: cancellation.Token);
        await console.InputAwaited;
        await cancellation.CancelAsync();

        Check.ThatCode(() => selecting).Throws<OperationCanceledException>();
        Check.That(console.Prompts).ContainsExactly(["Select device: ", "Use this device? [Y/N]: ", "Select device: "]);
        Check.That(first.LastCommand).IsEqualTo("TurnOff");
        Check.That(second.Commands).IsEmpty();
        Check.That(first.IsDisposed).IsTrue();
        Check.That(second.IsDisposed).IsTrue();
    }

    [Fact]
    public async Task Cancelling_while_a_confirmation_is_awaited_leaves_the_identified_device_off_and_disposes_every_device() {
        using CancellationTokenSource cancellation = new();
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new("1") { EndOfInput = false };

        Task<ILuxaforDevice> selecting = SelectAsync(console, [first, second], cancellationToken: cancellation.Token);
        await console.InputAwaited;
        await cancellation.CancelAsync();

        Check.ThatCode(() => selecting).Throws<OperationCanceledException>();
        Check.That(console.Prompts).ContainsExactly(["Select device: ", "Use this device? [Y/N]: "]);
        // The wave's own turn-off is the only one: a device known to be off is not told again.
        Check.That(first.Commands).IsEqualTo(Wave);
        Check.That(first.IsDisposed).IsTrue();
        Check.That(second.IsDisposed).IsTrue();
    }

    [Fact]
    public void Cancelling_during_the_wave_leaves_the_device_off_and_disposes_every_device() {
        using CancellationTokenSource cancellation = new();
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        InstantDelay delay   = new() { OnWait = wait => { if (wait == 3) { cancellation.Cancel(); } } };
        FakeConsole  console = new("1", "y");

        Check.ThatCode(() => SelectAsync(console, [first, second], delay, cancellation.Token)).Throws<OperationCanceledException>();

        // Three frames went out before the cut; never asked whether to keep a device it could not show.
        Check.That(first.Commands.Count(command => command.StartsWith("Send(", StringComparison.Ordinal))).IsEqualTo(3);
        Check.That(first.LastCommand).IsEqualTo("TurnOff");
        Check.That(console.Prompts).ContainsExactly(["Select device: "]);
        Check.That(first.IsDisposed).IsTrue();
        Check.That(second.IsDisposed).IsTrue();
    }

    [Fact]
    public void An_input_that_ends_while_a_number_is_awaited_is_a_cancellation() {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new();

        Check.ThatCode(() => SelectAsync(console, [first, second])).Throws<OperationCanceledException>();

        Check.That(console.Prompts).ContainsExactly(["Select device: "]);
        Check.That(first.Commands).IsEmpty();
        Check.That(first.IsDisposed).IsTrue();
        Check.That(second.IsDisposed).IsTrue();
    }

    [Fact]
    public void An_input_that_ends_while_a_confirmation_is_awaited_is_a_cancellation() {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new("2");

        Check.ThatCode(() => SelectAsync(console, [first, second])).Throws<OperationCanceledException>();

        Check.That(console.Prompts).ContainsExactly(["Select device: ", "Use this device? [Y/N]: "]);
        Check.That(second.Commands).IsEqualTo(Wave);
        Check.That(first.IsDisposed).IsTrue();
        Check.That(second.IsDisposed).IsTrue();
    }

    [Fact]
    public async Task A_token_already_cancelled_releases_the_devices_before_anything_is_printed_or_read() {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new("1", "y");

        Check.ThatCode(() => SelectAsync(console, [first, second], cancellationToken: cancellation.Token)).Throws<OperationCanceledException>();

        Check.That(console.Transcript).IsEmpty();
        Check.That(first.Commands).IsEmpty();
        Check.That(first.IsDisposed).IsTrue();
        Check.That(second.IsDisposed).IsTrue();
    }

    [Fact]
    public async Task A_token_already_cancelled_releases_a_single_device_too() {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        FakeLuxaforDevice device  = new();
        FakeConsole       console = new();

        Check.ThatCode(() => SelectAsync(console, [device], cancellationToken: cancellation.Token)).Throws<OperationCanceledException>();

        Check.That(console.Transcript).IsEmpty();
        Check.That(device.IsDisposed).IsTrue();
    }

    #endregion

}
