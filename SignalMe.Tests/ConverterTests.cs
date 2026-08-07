using SignalMe.Converters;
using SignalMe.Services;

namespace SignalMe.Tests;

public sealed class ConverterTests {

    [Theory]
    [InlineData("available", UserStatus.Available)]
    [InlineData("free", UserStatus.Available)]
    [InlineData("busy", UserStatus.Busy)]
    [InlineData("away", UserStatus.Away)]
    [InlineData("do-not-disturb", UserStatus.DoNotDisturb)]
    [InlineData("dnd", UserStatus.DoNotDisturb)]
    public void Statuses_and_their_aliases_are_parsed(string input, UserStatus expected) {
        Assert.True(UserStatusConverter.TryConvert(input, out UserStatus? status));
        Assert.Equal(expected, status);
    }

    [Theory]
    [InlineData("happy", UserMood.Happy)]
    [InlineData("bored", UserMood.Bored)]
    [InlineData("desperate", UserMood.Desperate)]
    [InlineData("ready", UserMood.Ready)]
    [InlineData("warning", UserMood.Warning)]
    [InlineData("alerting", UserMood.Alerting)]
    public void Moods_are_parsed(string input, UserMood expected) {
        Assert.True(UserMoodConverter.TryConvert(input, out UserMood? mood));
        Assert.Equal(expected, mood);
    }

    /// <summary>
    ///     The values signalme advertises and the values it accepts come from one table, so this can only
    ///     fail if that stops being true.
    /// </summary>
    [Fact]
    public void Every_advertised_status_is_accepted() {
        Assert.All(UserStatusConverter.KnownValues, value => Assert.True(UserStatusConverter.TryConvert(value, out _), $"'{value}' is advertised but rejected."));
    }

    [Fact]
    public void Every_advertised_mood_is_accepted() {
        Assert.All(UserMoodConverter.KnownValues, value => Assert.True(UserMoodConverter.TryConvert(value, out _), $"'{value}' is advertised but rejected."));
    }

    [Fact]
    public void Every_status_of_the_enum_is_reachable_from_the_command_line() {
        IEnumerable<UserStatus> reachable = UserStatusConverter.KnownValues.Select(value => {
            UserStatusConverter.TryConvert(value, out UserStatus? status);

            return status!.Value;
        });

        Assert.Equal(Enum.GetValues<UserStatus>().OrderBy(status => status), reachable.Distinct().OrderBy(status => status));
    }

    [Fact]
    public void Every_mood_of_the_enum_is_reachable_from_the_command_line() {
        IEnumerable<UserMood> reachable = UserMoodConverter.KnownValues.Select(value => {
            UserMoodConverter.TryConvert(value, out UserMood? mood);

            return mood!.Value;
        });

        Assert.Equal(Enum.GetValues<UserMood>().OrderBy(mood => mood), reachable.Distinct().OrderBy(mood => mood));
    }

    [Theory]
    [InlineData("")]
    [InlineData("nope")]
    [InlineData("BUSY")]
    [InlineData(" busy")]
    public void Anything_else_is_rejected(string input) {
        Assert.False(UserStatusConverter.TryConvert(input, out _));
        Assert.False(UserMoodConverter.TryConvert(input, out _));
        Assert.False(SignalMeService.IsKnown(input));
    }

    [Fact]
    public void IsKnown_covers_statuses_and_moods_alike() {
        Assert.All(UserStatusConverter.KnownValues, value => Assert.True(SignalMeService.IsKnown(value)));
        Assert.All(UserMoodConverter.KnownValues, value => Assert.True(SignalMeService.IsKnown(value)));
    }

}
