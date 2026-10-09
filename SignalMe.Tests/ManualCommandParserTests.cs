using SignalMe.Converters;
using SignalMe.Modes.Manual;
using SignalMe.Services;

namespace SignalMe.Tests;

/// <summary>
///     The parser of the manual mode: a pure function from a typed line to a command, tested without a console.
/// </summary>
public sealed class ManualCommandParserTests {

    /// <summary>Every status the user may ask for: all of them but away, which the session lock applies.</summary>
    public static TheoryData<UserStatus> SelectableStatuses => new(Enum.GetValues<UserStatus>().Where(status => status != UserStatus.Away));

    /// <summary>Derived from the enum, so adding a mood cannot silently leave it untested.</summary>
    public static TheoryData<UserMood> AllMoods => new(Enum.GetValues<UserMood>());

    #region statuses and signals

    [Theory]
    [InlineData("available", UserStatus.Available)]
    [InlineData("free", UserStatus.Available)]
    [InlineData("busy", UserStatus.Busy)]
    [InlineData("do-not-disturb", UserStatus.DoNotDisturb)]
    [InlineData("dnd", UserStatus.DoNotDisturb)]
    public void A_status_or_one_of_its_aliases_sets_the_desired_status(string line, UserStatus expected) {
        Check.That(ManualCommandParser.Parse(line)).IsEqualTo(new ManualCommand.SetStatus(expected));
    }

    [Theory]
    [InlineData("happy", UserMood.Happy)]
    [InlineData("bored", UserMood.Bored)]
    [InlineData("desperate", UserMood.Desperate)]
    [InlineData("warning", UserMood.Warning)]
    [InlineData("alerting", UserMood.Alerting)]
    [InlineData("ready", UserMood.Ready)]
    public void A_mood_plays_a_signal(string line, UserMood expected) {
        Check.That(ManualCommandParser.Parse(line)).IsEqualTo(new ManualCommand.PlaySignal(expected));
    }

    [Theory]
    [MemberData(nameof(SelectableStatuses))]
    public void Every_status_except_away_is_reachable_from_the_prompt(UserStatus status) {
        Check.That(ManualCommandParser.Parse(UserStatusConverter.ToCanonicalValue(status))).IsEqualTo(new ManualCommand.SetStatus(status));
    }

    [Theory]
    [MemberData(nameof(AllMoods))]
    public void Every_mood_is_reachable_from_the_prompt(UserMood mood) {
        Check.That(ManualCommandParser.Parse(UserMoodConverter.ToCanonicalValue(mood))).IsEqualTo(new ManualCommand.PlaySignal(mood));
    }

    /// <summary>
    ///     The parser accepts whatever the converters advertise, so an alias added there is a command here
    ///     without anything to update.
    /// </summary>
    [Fact]
    public void Every_advertised_status_and_mood_is_a_command() {
        foreach (string value in UserStatusConverter.KnownValues) {
            Check.That(ManualCommandParser.Parse(value)).IsInstanceOf<ManualCommand.SetStatus>();
        }
        foreach (string value in UserMoodConverter.KnownValues) {
            Check.That(ManualCommandParser.Parse(value)).IsInstanceOf<ManualCommand.PlaySignal>();
        }
    }

    /// <summary>
    ///     Away is applied by SignalMe while the session is locked; it is not something the user asks for.
    /// </summary>
    [Theory]
    [InlineData("away")]
    [InlineData(" AWAY ")]
    public void Away_is_not_a_command(string line) {
        Check.That(ManualCommandParser.Parse(line)).IsEqualTo(new ManualCommand.Unknown(line.Trim()));
    }

    #endregion

    #region keywords

    [Fact]
    public void Status_asks_what_is_shown() {
        Check.That(ManualCommandParser.Parse("status")).IsEqualTo(new ManualCommand.ShowStatus());
    }

    [Fact]
    public void Off_turns_off() {
        Check.That(ManualCommandParser.Parse("off")).IsEqualTo(new ManualCommand.TurnOff());
    }

    [Fact]
    public void Help_lists_the_commands() {
        Check.That(ManualCommandParser.Parse("help")).IsEqualTo(new ManualCommand.Help());
    }

    #endregion

    #region spaces, case, blank and unknown

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void A_blank_line_is_empty(string line) {
        Check.That(ManualCommandParser.Parse(line)).IsEqualTo(new ManualCommand.Empty());
    }

    [Theory]
    [InlineData(" BUSY", UserStatus.Busy)]
    [InlineData("Busy ", UserStatus.Busy)]
    [InlineData("\tDo-Not-Disturb\t", UserStatus.DoNotDisturb)]
    [InlineData("  FREE  ", UserStatus.Available)]
    public void Surrounding_spaces_and_the_case_are_forgiven_for_a_status(string line, UserStatus expected) {
        Check.That(ManualCommandParser.Parse(line)).IsEqualTo(new ManualCommand.SetStatus(expected));
    }

    [Theory]
    [InlineData("  Happy  ", UserMood.Happy)]
    [InlineData("ALERTING", UserMood.Alerting)]
    public void Surrounding_spaces_and_the_case_are_forgiven_for_a_signal(string line, UserMood expected) {
        Check.That(ManualCommandParser.Parse(line)).IsEqualTo(new ManualCommand.PlaySignal(expected));
    }

    [Fact]
    public void Surrounding_spaces_and_the_case_are_forgiven_for_a_keyword() {
        Check.That(ManualCommandParser.Parse(" STATUS ")).IsEqualTo(new ManualCommand.ShowStatus());
        Check.That(ManualCommandParser.Parse("Off")).IsEqualTo(new ManualCommand.TurnOff());
        Check.That(ManualCommandParser.Parse("\tHELP")).IsEqualTo(new ManualCommand.Help());
    }

    [Theory]
    [InlineData("buzy")]
    [InlineData("busy now")]
    [InlineData("--mode")]
    [InlineData("help me")]
    public void Anything_else_is_unknown(string line) {
        Check.That(ManualCommandParser.Parse(line)).IsEqualTo(new ManualCommand.Unknown(line));
    }

    /// <summary>
    ///     The message quotes the command, so it carries what the user wrote: trimmed, since the spaces
    ///     are not what went wrong, but in its own case.
    /// </summary>
    [Fact]
    public void An_unknown_command_carries_what_was_typed_without_its_surrounding_spaces() {
        Check.That(ManualCommandParser.Parse("  Buzy  ")).IsEqualTo(new ManualCommand.Unknown("Buzy"));
    }

    [Fact]
    public void A_null_line_is_refused() {
        Check.ThatCode(() => ManualCommandParser.Parse(null!)).Throws<ArgumentNullException>();
    }

    #endregion

}
