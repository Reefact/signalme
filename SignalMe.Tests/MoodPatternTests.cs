using System.ComponentModel;

using SignalMe.Infrastructure;
using SignalMe.MoodPatterns;
using SignalMe.Services;
using SignalMe.Tests.Fakes;

namespace SignalMe.Tests;

/// <summary>
///     Checks the contract every animation owes, not the frames it draws: it says which status to leave
///     behind, a refused command stops it, and a broken device surfaces as a device failure.
/// </summary>
public sealed class MoodPatternTests {

    /// <summary>Every mood except "ready", which deliberately ends on another status.</summary>
    public static TheoryData<UserMood> RestoringMoods => new(Enum.GetValues<UserMood>().Where(mood => mood != UserMood.Ready));

    /// <summary>Derived from the enum, so adding a mood cannot silently leave it untested.</summary>
    public static TheoryData<UserMood> AllMoods => new(Enum.GetValues<UserMood>());

    private static Task<UserStatus?> PlayAsync(UserMood mood, FakeLuxaforDevice device, UserStatus? baseStatus = UserStatus.Busy, IDelay? delay = null, CancellationToken cancellationToken = default) {
        return MoodPatternFactory.Create(mood, device, delay ?? new InstantDelay()).PlayAsync(baseStatus, cancellationToken);
    }

    [Theory]
    [MemberData(nameof(RestoringMoods))]
    public async Task An_animation_hands_back_the_status_it_started_from(UserMood mood) {
        FakeLuxaforDevice device = new();

        UserStatus? left = await PlayAsync(mood, device, UserStatus.DoNotDisturb);

        Check.That(left).IsEqualTo(UserStatus.DoNotDisturb);
    }

    [Fact]
    public async Task Ready_ends_on_available_whatever_it_started_from() {
        FakeLuxaforDevice device = new();

        UserStatus? left = await PlayAsync(UserMood.Ready, device, UserStatus.DoNotDisturb);

        Check.That(left).IsEqualTo(UserStatus.Available);
    }

    /// <summary>
    ///     Restoring the status is the coordinator's job: the animation only draws. The last command is a
    ///     frame of the animation, never a status render.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllMoods))]
    public async Task An_animation_does_not_render_the_status_it_hands_back(UserMood mood) {
        FakeLuxaforDevice device = new();

        await PlayAsync(mood, device, UserStatus.DoNotDisturb);

        Check.That(device.LastCommand).IsNotEqualTo("SetColor(#FF0000)");
    }

    [Fact]
    public async Task Bored_fades_back_to_the_colour_of_the_base_status() {
        FakeLuxaforDevice device = new();

        await PlayAsync(UserMood.Bored, device, UserStatus.Busy);

        // The fade is part of the animation and lands every LED on the base colour.
        Check.That(device.LastCommand).Contains("#FFFF00");
    }

    [Theory]
    [MemberData(nameof(AllMoods))]
    public void A_refused_command_stops_the_animation(UserMood mood) {
        FakeLuxaforDevice device = new() { RefuseFromCall = 1 };

        Check.ThatCode(() => PlayAsync(mood, device)).Throws<DeviceCommandFailedException>();

        // The animation gave up straight away rather than sending its whole sequence into the void.
        Check.That(device.Commands).IsEmpty();
    }

    [Theory]
    [MemberData(nameof(AllMoods))]
    public void A_device_exception_surfaces_as_a_device_failure_with_the_cause_inside(UserMood mood) {
        FakeLuxaforDevice device = new() { ThrowOnCall = 3 };

        DeviceCommandFailedException thrown = Check.ThatCode(() => PlayAsync(mood, device)).Throws<DeviceCommandFailedException>().Value;

        Check.That(thrown.Message).Contains("could not be reached");
        Check.That(thrown.Message).Contains("USB write failed");
        Check.That(thrown.InnerException).IsInstanceOf<InvalidOperationException>();
        // It stopped at the broken call: two commands got through, nothing after.
        Check.That(device.Commands.Count).IsEqualTo(2);
    }

    [Fact]
    public async Task The_whole_suite_of_animations_runs_without_waiting_for_real() {
        InstantDelay delay = new();

        foreach (UserMood mood in Enum.GetValues<UserMood>()) {
            await PlayAsync(mood, new FakeLuxaforDevice(), delay: delay);
        }

        // Proof the animations really are asking to wait, and that the tests are simply not sitting there.
        Check.WithCustomMessage($"expected the animations to request many waits, got {delay.Waits}.").That(delay.Waits > 100).IsTrue();
    }

    [Fact]
    public void The_factory_refuses_a_mood_it_does_not_know() {
        Check.ThatCode(() => MoodPatternFactory.Create((UserMood)99, new FakeLuxaforDevice(), new InstantDelay())).Throws<InvalidEnumArgumentException>();
    }

}
