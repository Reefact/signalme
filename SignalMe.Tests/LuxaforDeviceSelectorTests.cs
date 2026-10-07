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

        Assert.Same(device, selected);
        Assert.Equal(["Luxafor device detected."], console.Output);
        Assert.Equal(0, console.Reads);
        Assert.Empty(device.Commands);
        Assert.False(device.IsDisposed);
    }

    [Fact]
    public async Task An_empty_discovery_is_not_the_selector_s_to_handle() {
        await Assert.ThrowsAsync<ArgumentException>(() => SelectAsync(new FakeConsole(), []));
    }

    #endregion

    #region the table

    [Fact]
    public async Task Several_devices_are_counted_and_listed_by_their_ids() {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new("2", "y");

        await SelectAsync(console, [first, second]);

        Assert.Equal("2 Luxafor devices detected.", console.Output[0]);
        Assert.Contains(FirstId, console.Output[1]);
        Assert.Contains(SecondId, console.Output[1]);
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
        Assert.Equal(string.Join(Environment.NewLine, expected), console.Output[1]);
    }

    [Fact]
    public async Task The_number_column_widens_past_nine_devices() {
        FakeLuxaforDevice[] devices = Enumerable.Range(1, 10).Select(number => new FakeLuxaforDevice { Path = $"dev{number}" }).ToArray();
        FakeConsole         console = new("10", "y");

        ILuxaforDevice selected = await SelectAsync(console, devices);

        Assert.Same(devices[9], selected);
        Assert.Contains("│  # │ Id    │", console.Output[1]);
        Assert.Contains("│  1 │ dev1  │", console.Output[1]);
        Assert.Contains("│ 10 │ dev10 │", console.Output[1]);
    }

    #endregion

    #region choosing

    [Fact]
    public async Task The_dialog_of_the_specification_runs_in_that_order() {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new("1", "n", "2", "y");

        ILuxaforDevice selected = await SelectAsync(console, [first, second]);

        Assert.Same(second, selected);
        string[] expected = [
            "line: 2 Luxafor devices detected.",
            $"line: {console.Output[1]}",
            "prompt: Select device: ",
            "read: 1",
            "line: Identifying device #1...",
            "prompt: Use this device? [Y/N]: ",
            "read: n",
            "prompt: Select device: ",
            "read: 2",
            "line: Identifying device #2...",
            "prompt: Use this device? [Y/N]: ",
            "read: y",
            $"line: Device selected: {SecondId}"
        ];
        Assert.Equal(expected, console.Transcript);
    }

    [Fact]
    public async Task Confirming_selects_the_device_and_disposes_the_others() {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new("2", "y");

        ILuxaforDevice selected = await SelectAsync(console, [first, second]);

        Assert.Same(second, selected);
        Assert.True(first.IsDisposed);
        Assert.False(second.IsDisposed);
        Assert.Equal($"Device selected: {SecondId}", console.Output[^1]);
    }

    [Fact]
    public async Task The_wave_plays_on_the_chosen_device_only_and_hands_it_over_off() {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new("2", "y");

        ILuxaforDevice selected = await SelectAsync(console, [first, second]);

        Assert.Empty(first.Commands);
        Assert.Equal(Wave, second.Commands);
        Assert.Equal("TurnOff", ((FakeLuxaforDevice)selected).LastCommand);
    }

    [Theory]
    [MemberData(nameof(InvalidNumbers))]
    public async Task An_invalid_number_is_refused_and_asked_again(string input) {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new(input, "2", "y");

        ILuxaforDevice selected = await SelectAsync(console, [first, second]);

        Assert.Same(second, selected);
        Assert.Equal(["Select device: ", "Select device: ", "Use this device? [Y/N]: "], console.Prompts);
        Assert.Equal(["Invalid device number.", "Identifying device #2...", $"Device selected: {SecondId}"], console.Output.Skip(2));
        // Nothing was identified on a refused number: no wave went out.
        Assert.Empty(first.Commands);
    }

    [Fact]
    public async Task Declining_a_device_asks_for_a_number_again_and_the_next_choice_plays_the_wave_again() {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new("1", "n", "1", "y");

        ILuxaforDevice selected = await SelectAsync(console, [first, second]);

        Assert.Same(first, selected);
        Assert.Equal(["Select device: ", "Use this device? [Y/N]: ", "Select device: ", "Use this device? [Y/N]: "], console.Prompts);
        Assert.Equal([.. Wave, .. Wave], first.Commands);
        Assert.Empty(second.Commands);
        Assert.True(second.IsDisposed);
        Assert.False(first.IsDisposed);
    }

    [Theory]
    [InlineData("Y")]
    [InlineData("y")]
    [InlineData(" y ")]
    public async Task The_confirmation_accepts_yes_in_any_case(string answer) {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new("1", answer);

        ILuxaforDevice selected = await SelectAsync(console, [first, second]);

        Assert.Same(first, selected);
    }

    [Theory]
    [InlineData("N")]
    [InlineData("n")]
    public async Task The_confirmation_accepts_no_in_any_case(string answer) {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new("1", answer, "2", "y");

        ILuxaforDevice selected = await SelectAsync(console, [first, second]);

        Assert.Same(second, selected);
    }

    [Fact]
    public async Task Any_other_answer_to_the_confirmation_asks_again() {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new("1", "maybe", "", "yes", "y");

        ILuxaforDevice selected = await SelectAsync(console, [first, second]);

        Assert.Same(first, selected);
        Assert.Equal(["Select device: ", "Use this device? [Y/N]: ", "Use this device? [Y/N]: ", "Use this device? [Y/N]: ", "Use this device? [Y/N]: "], console.Prompts);
        // Asked again, not identified again: one wave only.
        Assert.Equal(Wave, first.Commands);
    }

    #endregion

    #region device failures

    [Fact]
    public async Task A_device_failing_during_its_wave_is_reported_and_a_number_is_asked_again() {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        first.ThrowOnCall = 3;
        FakeConsole console = new("1", "2", "y");

        ILuxaforDevice selected = await SelectAsync(console, [first, second]);

        Assert.Same(second, selected);
        // Two frames got through, then the clean-up; the LEDs were off before the question came back.
        Assert.Equal([Frame(1), Frame(2), "TurnOff"], first.Commands);
        Assert.Equal(["Select device: ", "Select device: ", "Use this device? [Y/N]: "], console.Prompts);
        string error = Assert.Single(console.Error);
        Assert.Contains("USB write failed", error);
        Assert.Equal(["line: Identifying device #1...", $"error: {error}", "prompt: Select device: "], console.Transcript.Skip(4).Take(3));
        Assert.True(first.IsDisposed);
    }

    [Fact]
    public async Task A_device_refusing_its_wave_is_reported_and_a_number_is_asked_again() {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        first.RefuseFromCall = 1;
        FakeConsole console = new("1", "2", "y");

        ILuxaforDevice selected = await SelectAsync(console, [first, second]);

        Assert.Same(second, selected);
        Assert.Equal(["The Luxafor device refused to run the command \"Set LED n° 1 color to #FFFFFF\"."], console.Error);
        Assert.Equal(["Select device: ", "Select device: ", "Use this device? [Y/N]: "], console.Prompts);
    }

    /// <summary>
    ///     The one exit where the device may still be lit: its wave ended on a refused turn-off and the
    ///     dialog was then abandoned. The selector tries once more and says so, rather than releasing a
    ///     device it knows nothing about.
    /// </summary>
    [Fact]
    public async Task Cancelling_after_a_device_refused_to_switch_off_retries_the_turn_off_on_the_way_out_and_reports_it() {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        first.RefuseFromCall = Wave.Length; // the wave's final turn-off, and everything after it
        FakeConsole console = new("1");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SelectAsync(console, [first, second]));

        Assert.Equal(["Select device: ", "Select device: "], console.Prompts);
        // The wave's own refusal first, then the retry's: two attempts, both reported.
        Assert.Equal(["The Luxafor device refused to turn its LEDs off.", "The Luxafor device refused to turn its LEDs off."], console.Error);
        // Every frame got through; no turn-off ever did.
        Assert.Equal(Wave[..^1], first.Commands);
        Assert.Empty(second.Commands);
        Assert.True(first.IsDisposed);
        Assert.True(second.IsDisposed);
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

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => selecting);
        Assert.Equal(["Select device: ", "Use this device? [Y/N]: ", "Select device: "], console.Prompts);
        Assert.Equal("TurnOff", first.LastCommand);
        Assert.Empty(second.Commands);
        Assert.True(first.IsDisposed);
        Assert.True(second.IsDisposed);
    }

    [Fact]
    public async Task Cancelling_while_a_confirmation_is_awaited_leaves_the_identified_device_off_and_disposes_every_device() {
        using CancellationTokenSource cancellation = new();
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new("1") { EndOfInput = false };

        Task<ILuxaforDevice> selecting = SelectAsync(console, [first, second], cancellationToken: cancellation.Token);
        await console.InputAwaited;
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => selecting);
        Assert.Equal(["Select device: ", "Use this device? [Y/N]: "], console.Prompts);
        // The wave's own turn-off is the only one: a device known to be off is not told again.
        Assert.Equal(Wave, first.Commands);
        Assert.True(first.IsDisposed);
        Assert.True(second.IsDisposed);
    }

    [Fact]
    public async Task Cancelling_during_the_wave_leaves_the_device_off_and_disposes_every_device() {
        using CancellationTokenSource cancellation = new();
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        InstantDelay delay   = new() { OnWait = wait => { if (wait == 3) { cancellation.Cancel(); } } };
        FakeConsole  console = new("1", "y");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SelectAsync(console, [first, second], delay, cancellation.Token));

        // Three frames went out before the cut; never asked whether to keep a device it could not show.
        Assert.Equal(3, first.Commands.Count(command => command.StartsWith("Send(", StringComparison.Ordinal)));
        Assert.Equal("TurnOff", first.LastCommand);
        Assert.Equal(["Select device: "], console.Prompts);
        Assert.True(first.IsDisposed);
        Assert.True(second.IsDisposed);
    }

    [Fact]
    public async Task An_input_that_ends_while_a_number_is_awaited_is_a_cancellation() {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SelectAsync(console, [first, second]));

        Assert.Equal(["Select device: "], console.Prompts);
        Assert.Empty(first.Commands);
        Assert.True(first.IsDisposed);
        Assert.True(second.IsDisposed);
    }

    [Fact]
    public async Task An_input_that_ends_while_a_confirmation_is_awaited_is_a_cancellation() {
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new("2");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SelectAsync(console, [first, second]));

        Assert.Equal(["Select device: ", "Use this device? [Y/N]: "], console.Prompts);
        Assert.Equal(Wave, second.Commands);
        Assert.True(first.IsDisposed);
        Assert.True(second.IsDisposed);
    }

    [Fact]
    public async Task A_token_already_cancelled_releases_the_devices_before_anything_is_printed_or_read() {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        (FakeLuxaforDevice first, FakeLuxaforDevice second) = TwoDevices();
        FakeConsole console = new("1", "y");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SelectAsync(console, [first, second], cancellationToken: cancellation.Token));

        Assert.Empty(console.Transcript);
        Assert.Empty(first.Commands);
        Assert.True(first.IsDisposed);
        Assert.True(second.IsDisposed);
    }

    [Fact]
    public async Task A_token_already_cancelled_releases_a_single_device_too() {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        FakeLuxaforDevice device  = new();
        FakeConsole       console = new();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SelectAsync(console, [device], cancellationToken: cancellation.Token));

        Assert.Empty(console.Transcript);
        Assert.True(device.IsDisposed);
    }

    #endregion

}
