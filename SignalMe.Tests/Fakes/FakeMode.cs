using SignalMe.Modes;
using SignalMe.Runtime;

namespace SignalMe.Tests.Fakes;

/// <summary>
///     A mode that does whatever the test hands it, with the usual behaviours ready-made: wait to be
///     cancelled, return at once, send a script of intents, or fail.
/// </summary>
public sealed class FakeMode : ISignalMeMode {

    private readonly Func<ISignalMeContext, CancellationToken, Task> _run;

    public FakeMode(Func<ISignalMeContext, CancellationToken, Task> run) {
        _run = run;
    }

    /// <summary>What the coordinator answered to each intent of a <see cref="Sending" /> mode, in order.</summary>
    public List<IntentOutcome> Outcomes { get; private init; } = new();

    public bool Started { get; private set; }

    /// <summary>Runs until the token is cancelled, like a mode waiting for input that never comes.</summary>
    public static FakeMode WaitingForCancellation() {
        return new FakeMode((_, cancellationToken) => Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken));
    }

    /// <summary>Returns at once, like a mode whose input closed immediately.</summary>
    public static FakeMode Returning() {
        return new FakeMode((_, _) => Task.CompletedTask);
    }

    /// <summary>Sends the intents one after the other, awaiting each, then returns.</summary>
    public static FakeMode Sending(params SignalMeIntent[] intents) {
        List<IntentOutcome> outcomes = new();

        return new FakeMode(async (context, cancellationToken) => {
            foreach (SignalMeIntent intent in intents) {
                outcomes.Add(await context.ExecuteAsync(intent, cancellationToken));
            }
        }) { Outcomes = outcomes };
    }

    /// <summary>Fails after its first await, the way a bug in a real mode would.</summary>
    public static FakeMode Throwing(Exception exception) {
        return new FakeMode(async (_, _) => {
            await Task.Yield();

            throw exception;
        });
    }

    /// <inheritdoc />
    public Task RunAsync(ISignalMeContext context, CancellationToken cancellationToken) {
        Started = true;

        return _run(context, cancellationToken);
    }

}
