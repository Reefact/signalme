using SignalMe.Converters;
using SignalMe.Runtime;
using SignalMe.Services;

namespace SignalMe.Tests;

public sealed class ConverterTests {

    /// <summary>
    ///     What the device shows is named like the status it comes from, so "Effective status: busy" and
    ///     "Status: busy" spell it alike; "off" is the one word for a dark device.
    /// </summary>
    [Theory]
    [InlineData(EffectiveStatus.Off, "off")]
    [InlineData(EffectiveStatus.Away, "away")]
    [InlineData(EffectiveStatus.Available, "available")]
    [InlineData(EffectiveStatus.Busy, "busy")]
    [InlineData(EffectiveStatus.DoNotDisturb, "do-not-disturb")]
    public void An_effective_status_is_printed_as_the_status_it_comes_from(EffectiveStatus status, string expected) {
        Assert.Equal(expected, EffectiveStatusConverter.ToCanonicalValue(status));
    }

    [Fact]
    public void Every_effective_status_of_the_enum_has_a_printed_value() {
        Assert.All(Enum.GetValues<EffectiveStatus>(), status => Assert.NotEmpty(EffectiveStatusConverter.ToCanonicalValue(status)));
    }

    [Theory]
    [InlineData("available", UserStatus.Available)]
    [InlineData("free", UserStatus.Available)]
    [InlineData("busy", UserStatus.Busy)]
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
    ///     Away is applied by SignalMe while the session is locked; it is not something the user asks for.
    /// </summary>
    [Fact]
    public void Away_is_neither_parsed_nor_advertised() {
        Assert.False(UserStatusConverter.TryConvert("away", out _));
        Assert.DoesNotContain("away", UserStatusConverter.KnownValues);
    }

    [Fact]
    public void Away_is_still_named_when_printed() {
        Assert.Equal("away", UserStatusConverter.ToCanonicalValue(UserStatus.Away));
    }

    [Theory]
    [InlineData(UserStatus.Available, "available")]
    [InlineData(UserStatus.Busy, "busy")]
    [InlineData(UserStatus.DoNotDisturb, "do-not-disturb")]
    public void A_status_is_printed_as_its_canonical_value_not_an_alias(UserStatus status, string expected) {
        Assert.Equal(expected, UserStatusConverter.ToCanonicalValue(status));
    }

    [Fact]
    public void A_mood_is_printed_as_the_value_the_user_types() {
        foreach (UserMood mood in Enum.GetValues<UserMood>()) {
            Assert.True(UserMoodConverter.TryConvert(UserMoodConverter.ToCanonicalValue(mood), out UserMood? parsed));
            Assert.Equal(mood, parsed);
        }
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
    public void Every_status_of_the_enum_except_away_is_reachable_from_the_console() {
        IEnumerable<UserStatus> reachable = UserStatusConverter.KnownValues.Select(value => {
            UserStatusConverter.TryConvert(value, out UserStatus? status);

            return status!.Value;
        });

        IEnumerable<UserStatus> selectable = Enum.GetValues<UserStatus>().Where(status => status != UserStatus.Away);

        Assert.Equal(selectable.OrderBy(status => status), reachable.Distinct().OrderBy(status => status));
    }

    [Fact]
    public void Every_mood_of_the_enum_is_reachable_from_the_console() {
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
    }

}
