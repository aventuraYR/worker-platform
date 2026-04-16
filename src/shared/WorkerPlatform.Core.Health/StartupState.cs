using System.Threading;

namespace WorkerPlatform.Core.Health;

internal sealed class StartupState
{
    private int _isStarted;

    public bool IsStarted => Volatile.Read(ref _isStarted) == 1;

    public void MarkStarted()
    {
        Interlocked.Exchange(ref _isStarted, 1);
    }
}
