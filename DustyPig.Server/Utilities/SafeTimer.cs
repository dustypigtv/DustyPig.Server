using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DustyPig.Server.Utilities;


// <summary>
/// Non-overlapping timer that swallows exceptions in tick callbacks
/// </summary>
internal class SafeTimer : IDisposable
{
    private readonly Timer _timer;
    private readonly Action _action;
    private readonly Action<CancellationToken> _actionWithCancellationToken;
    private readonly Func<CancellationToken, Task> _func;
    private readonly bool _usesCancellationToken;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly bool _async;
    private readonly ILogger _logger;

    private TimeSpan _tickInterval;
    private CancellationTokenSource _cancellationTokenSource = new();
    private bool _disposed;



    public SafeTimer(Action onTick, TimeSpan? tickInterval = null, bool autoStart = false, ILogger logger = null)
    {
        _async = false;
        _usesCancellationToken = false;
        _action = onTick;
        _logger = logger;
        Enabled = autoStart;

        ValidateAndSetTickInterval(tickInterval ?? TimeSpan.FromSeconds(1));
        _timer = new(new(TimerTick), null, _tickInterval, _tickInterval);
    }



    public SafeTimer(Action<CancellationToken> onTick, TimeSpan? tickInterval = null, bool autoStart = false, ILogger logger = null)
    {
        _async = false;
        _usesCancellationToken = true;
        _actionWithCancellationToken = onTick;
        _logger = logger;
        Enabled = autoStart;

        ValidateAndSetTickInterval(tickInterval ?? TimeSpan.FromSeconds(1));
        _timer = new(new(TimerTick), null, _tickInterval, _tickInterval);
    }



    public SafeTimer(Func<CancellationToken, Task> onTick, TimeSpan? tickInterval = null, bool autoStart = false, ILogger logger = null)
    {
        _async = true;
        _usesCancellationToken = true;
        _func = onTick;
        _logger = logger;
        Enabled = autoStart;

        ValidateAndSetTickInterval(tickInterval ?? TimeSpan.FromSeconds(1));
        _timer = new(new(TimerTick), null, _tickInterval, _tickInterval);
    }



    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;

            //Kill the timer first to prevent more ticks after cleaning up other objects
            _timer.Dispose();

            //Kill any running tasks
            CancelTickAndWait();

            //Finish cleanup
            _semaphore.Dispose();
            _cancellationTokenSource.Dispose();

            GC.SuppressFinalize(this);
        }
    }






    public TimeSpan TickInterval
    {
        get => _tickInterval;
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_tickInterval != value)
            {
                ValidateAndSetTickInterval(value);
                _timer.Change(_tickInterval, _tickInterval);
            }
        }
    }



    /// <summary>
    /// Stops ticking, but does not cancel any currently executing actions
    /// </summary>
    public bool Enabled { get; set; }



    /// <summary>
    /// Sets <see cref="Enabled"/> to false, calls the Cancel method on the <see cref="CancellationToken"/> if available, and blocks the current thread until any tick action is complete
    /// </summary>
    public void TryForceStop()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Enabled = false;
        CancelTickAndWait();
        if (_usesCancellationToken)
        {
            //Dispose of the previous cts after going out of scope
            using var oldRef = _cancellationTokenSource;

            //Create a new cts before disposing the old one
            _cancellationTokenSource = new CancellationTokenSource();
        }
    }





    private void ValidateAndSetTickInterval(TimeSpan value)
    {
        if (value < TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(TickInterval)} must be positive or {nameof(Timeout.InfiniteTimeSpan)}");
        _tickInterval = value;
    }



    private void CancelTickAndWait()
    {
        if (_usesCancellationToken)
        {
            _cancellationTokenSource.Cancel();
            _semaphore.Wait();
            _semaphore.Release();
        }
    }



    private async void TimerTick(object state)
    {
        if (_disposed || !Enabled)
            return;

        if (_semaphore.Wait(0))
        {
            try
            {
                if (_async)
                {
                    await _func!(_cancellationTokenSource.Token).ConfigureAwait(false);
                }
                else if (_usesCancellationToken)
                {
                    _actionWithCancellationToken!(_cancellationTokenSource.Token);
                }
                else
                {
                    _action!();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, nameof(TimerTick));
            }

            _semaphore.Release();
        }
    }
}
