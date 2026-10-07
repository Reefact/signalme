using System.ComponentModel;

using SignalMe.Runtime;
using SignalMe.Services;

namespace SignalMe.Tests;

public sealed class StatusColorsTests {

    [Theory]
    [InlineData(UserStatus.Available, "#00FF00")]
    [InlineData(UserStatus.Busy, "#FFFF00")]
    [InlineData(UserStatus.DoNotDisturb, "#FF0000")]
    [InlineData(UserStatus.Away, "#9932CC")]
    public void Each_status_has_its_colour(UserStatus status, string expected) {
        Assert.Equal(expected, StatusColors.For(status).ToString());
    }

    [Fact]
    public void No_status_is_black_that_is_off() {
        Assert.Equal("#000000", StatusColors.For((UserStatus?)null).ToString());
    }

    [Theory]
    [InlineData(EffectiveStatus.Off, "#000000")]
    [InlineData(EffectiveStatus.Away, "#9932CC")]
    [InlineData(EffectiveStatus.Available, "#00FF00")]
    [InlineData(EffectiveStatus.Busy, "#FFFF00")]
    [InlineData(EffectiveStatus.DoNotDisturb, "#FF0000")]
    public void Each_effective_status_has_its_colour(EffectiveStatus status, string expected) {
        Assert.Equal(expected, StatusColors.For(status).ToString());
    }

    /// <summary>
    ///     An animation fades from the colour of the desired status and the coordinator renders the
    ///     effective one; the two tables must agree on every status they share.
    /// </summary>
    [Theory]
    [InlineData(UserStatus.Available, EffectiveStatus.Available)]
    [InlineData(UserStatus.Busy, EffectiveStatus.Busy)]
    [InlineData(UserStatus.DoNotDisturb, EffectiveStatus.DoNotDisturb)]
    [InlineData(UserStatus.Away, EffectiveStatus.Away)]
    public void The_two_overloads_agree(UserStatus status, EffectiveStatus effective) {
        Assert.Equal(StatusColors.For(status), StatusColors.For(effective));
    }

    [Fact]
    public void An_undefined_status_is_refused() {
        Assert.Throws<InvalidEnumArgumentException>(() => StatusColors.For((UserStatus)99));
        Assert.Throws<InvalidEnumArgumentException>(() => StatusColors.For((EffectiveStatus)99));
    }

}
