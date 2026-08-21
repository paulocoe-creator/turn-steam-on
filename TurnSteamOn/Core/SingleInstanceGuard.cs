namespace TurnSteamOn.Core;

public sealed class SingleInstanceGuard : IDisposable
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> ActiveNames = [];

    private readonly Mutex _mutex;
    private readonly string _name;
    private bool _ownsMutex;

    private SingleInstanceGuard(Mutex mutex, string name, bool ownsMutex)
    {
        _mutex = mutex;
        _name = name;
        _ownsMutex = ownsMutex;
    }

    public static SingleInstanceGuard? TryAcquire(string name)
    {
        if (!ActiveNames.TryAdd(name, 0))
        {
            return null;
        }

        var mutex = new Mutex(false, name);

        try
        {
            if (mutex.WaitOne(0))
            {
                return new SingleInstanceGuard(mutex, name, ownsMutex: true);
            }
        }
        catch (AbandonedMutexException)
        {
            return new SingleInstanceGuard(mutex, name, ownsMutex: true);
        }

        mutex.Dispose();
        ActiveNames.TryRemove(name, out _);
        return null;
    }

    public void Dispose()
    {
        if (_ownsMutex)
        {
            _mutex.ReleaseMutex();
            _ownsMutex = false;
            ActiveNames.TryRemove(_name, out _);
        }

        _mutex.Dispose();
    }
}