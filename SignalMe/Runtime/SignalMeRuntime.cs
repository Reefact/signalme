#region Usings declarations

using System;
using System.Threading;
using System.Threading.Tasks;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Devices;
using SignalMe.Infrastructure;
using SignalMe.Modes;
using SignalMe.Sessions;

#endregion

namespace SignalMe.Runtime;

/// <summary>
///     The lifecycle of one run: starts the session and device monitors, renders the initial status, runs
///     the coordinator and the mode side by side, and whatever happens turns the device off and disposes it
///     before returning.
/// </summary>
public sealed class SignalMeRuntime {

    #region Statics members declarations

    /// <summary>
    ///     Waits for a task that was asked to stop and keeps the error it ended with, if any. A
    ///     cancellation is the stop itself, not an error.
    /// </summary>
    private static async Task<Exception?> AwaitStoppedAsync(Task task) {
        try {
            await task.ConfigureAwait(false);

            return null;
        } catch (OperationCanceledException) {
            return null;
        } catch (Exception exception) {
            return exception;
        }
    }

    /// <summary>
    ///     Starts the mode as a task even when its implementation throws before its first await, so that
    ///     the coordinator is always stopped and awaited before the device is turned off.
    /// </summary>
    private static Task StartMode(ISignalMeMode mode, ISignalMeContext context, CancellationToken cancellationToken) {
        try {
            return mode.RunAsync(context, cancellationToken);
        } catch (Exception exception) {
            return Task.FromException(exception);
        }
    }

    private static RuntimeOutcome Classify(Exception failure) {
        return failure is DeviceCommandFailedException or DeviceDisconnectedException ? RuntimeOutcome.DeviceFailed : RuntimeOutcome.Faulted;
    }

    #endregion

    #region Fields declarations

    private readonly string                   _modeName;
    private readonly ISignalMeMode            _mode;
    private readonly ILuxaforDevice           _device;
    private readonly IDeviceConnectionMonitor _deviceMonitor;
    private readonly ISessionMonitor          _sessionMonitor;
    private readonly UserCurrentStatus        _store;
    private readonly IConsole                 _console;
    private readonly IDelay                   _delay;

    #endregion

    #region Constructors declarations

    public SignalMeRuntime(string modeName, ISignalMeMode mode, ILuxaforDevice device, IDeviceConnectionMonitor deviceMonitor, ISessionMonitor sessionMonitor, UserCurrentStatus store, IConsole console, IDelay delay) {
        ArgumentNullException.ThrowIfNull(modeName);
        ArgumentNullException.ThrowIfNull(mode);
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(deviceMonitor);
        ArgumentNullException.ThrowIfNull(sessionMonitor);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(delay);

        _modeName       = modeName;
        _mode           = mode;
        _device         = device;
        _deviceMonitor  = deviceMonitor;
        _sessionMonitor = sessionMonitor;
        _store          = store;
        _console        = console;
        _delay          = delay;
    }

    #endregion

    /// <summary>
    ///     Runs until the token is cancelled, the mode returns, or something fails. Owns the device from
    ///     here on: it is turned off and disposed before this returns, whatever happened.
    /// </summary>
    public async Task<RuntimeOutcome> RunAsync(CancellationToken cancellationToken) {
        StatusCoordinator? coordinator        = null;
        Exception?         coordinatorFailure = null;
        Exception?         modeFailure        = null;

        void OnSessionChanged(object? sender, SessionState state) {
            coordinator?.OnSessionChanged(state);
        }

        void OnDeviceDisconnected(object? sender, EventArgs e) {
            coordinator?.OnDeviceDisconnected();
        }

        try {
            coordinator = new StatusCoordinator(_modeName, _device, _store, _console, _delay, _store.Get());

            // Subscribed before Start(): an event raised from inside Start() lands in the channel, where the
            // loop drops it if it duplicates the state Initialize() renders from.
            _sessionMonitor.StateChanged += OnSessionChanged;
            _sessionMonitor.Start();

            // Queued like a lock: a device found missing before the loop runs is the first thing it handles.
            _deviceMonitor.Disconnected += OnDeviceDisconnected;
            _deviceMonitor.Start();

            // Read after Start(), so the first render reflects the session as the OS reports it now.
            coordinator.Initialize(_sessionMonitor.Current);

            using CancellationTokenSource stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            Task coordinatorTask = coordinator.RunAsync(stop.Token);
            Task modeTask        = StartMode(_mode, coordinator, stop.Token);

            await Task.WhenAny(coordinatorTask, modeTask).ConfigureAwait(false);

            // The mode is awaited before anything is printed: a mode waiting for input has a prompt on
            // screen, which the console ends once that read is cancelled. Printed earlier, "Stopping
            // SignalMe..." could land while the prompt is still pending and get it repeated after it.
            await stop.CancelAsync().ConfigureAwait(false);
            modeFailure = await AwaitStoppedAsync(modeTask).ConfigureAwait(false);

            _console.WriteLine("Stopping SignalMe...");
            coordinatorFailure = await AwaitStoppedAsync(coordinatorTask).ConfigureAwait(false);
        } catch (Exception exception) {
            // Only the start-up steps reach here: the store, a monitor's Start() or the first render.
            // Nothing has printed the error yet, and nothing else has started.
            coordinatorFailure = exception;
            _console.WriteError(ErrorReporting.Describe(exception));
        } finally {
            _deviceMonitor.Disconnected -= OnDeviceDisconnected;
            _deviceMonitor.Dispose();
            _sessionMonitor.StateChanged -= OnSessionChanged;
            _sessionMonitor.Dispose();

            // Reached only once the loop has completed, or when it never ran: the device has one writer.
            // A device found unplugged is not written to again: the turn-off could only fail, and its error
            // would come after the one that says why SignalMe stopped.
            if (coordinator is null) {
                _device.TurnOffQuietly(_console);
            } else {
                if (coordinatorFailure is not DeviceDisconnectedException) { coordinator.TurnOffQuietly(); }
                coordinator.Dispose();
            }
            _device.Dispose();

            _console.WriteLine("SignalMe stopped.");
        }

        // The coordinator's failure wins and was reported already, by the loop or just above; the mode's
        // is only reported here, once, when nothing else failed.
        if (coordinatorFailure is not null) { return Classify(coordinatorFailure); }
        if (modeFailure is null) { return RuntimeOutcome.Stopped; }

        _console.WriteError(ErrorReporting.Describe(modeFailure));

        return Classify(modeFailure);
    }

}
