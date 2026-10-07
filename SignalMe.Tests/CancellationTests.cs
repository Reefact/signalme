using SignalMe.MoodPatterns;
using SignalMe.Services;
using SignalMe.Tests.Fakes;

namespace SignalMe.Tests;

/// <summary>
///     An animation must stop at the first wait after it is cancelled, and send nothing more: the
///     coordinator writes the next status itself, right after it.
/// </summary>
public sealed class CancellationTests {

    /// <summary>Derived from the enum, so adding a mood cannot silently leave it untested.</summary>
    public static TheoryData<UserMood> AllMoods => new(Enum.GetValues<UserMood>());

    [Theory]
    [MemberData(nameof(AllMoods))]
    public async Task An_interrupted_animation_stops_early(UserMood mood) {
        using CancellationTokenSource cancellation = new();
        FakeLuxaforDevice             device       = new();
        // Stands in for a lock or a new intent arriving a few frames in.
        InstantDelay delay = new() { OnWait = wait => { if (wait == 3) { cancellation.Cancel(); } } };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => MoodPatternFactory.Create(mood, device, delay).PlayAsync(UserStatus.Busy, cancellation.Token));

        // It gave up at the third wait rather than running the sequence to the end.
        Assert.Equal(3, delay.Waits);
    }

    [Theory]
    [MemberData(nameof(AllMoods))]
    public async Task An_interrupted_animation_sends_no_frame_after_the_interruption(UserMood mood) {
        using CancellationTokenSource cancellation = new();
        FakeLuxaforDevice             device       = new();
        int                           framesSent   = -1;
        InstantDelay delay = new() {
            OnWait = wait => {
                if (wait != 3) { return; }

                framesSent = device.Commands.Count;
                cancellation.Cancel();
            }
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => MoodPatternFactory.Create(mood, device, delay).PlayAsync(UserStatus.Busy, cancellation.Token));

        Assert.Equal(framesSent, device.Commands.Count);
    }

    /// <summary>
    ///     Patterns check the token only at their waits, so the frames before the first wait go out; what
    ///     matters is that the animation goes no further.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllMoods))]
    public async Task An_animation_cancelled_before_it_starts_stops_at_its_first_wait(UserMood mood) {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        FakeLuxaforDevice device = new();
        InstantDelay      delay  = new();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => MoodPatternFactory.Create(mood, device, delay).PlayAsync(UserStatus.Busy, cancellation.Token));

        Assert.Equal(1, delay.Waits);
    }

}
