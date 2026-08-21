using TurnSteamOn.Core;

namespace TurnSteamOn.Tests;

public sealed class SteamStartupCoordinatorTests
{
    [Fact]
    public async Task DoesNotLaunchSteamWhenItIsAlreadyRunning()
    {
        var steam = new FakeSteamProcess(isRunning: true);
        var coordinator = new SteamStartupCoordinator(steam);

        var launched = await coordinator.HandleDualSenseConnectedAsync();

        Assert.False(launched);
        Assert.Equal(0, steam.LaunchCount);
    }

    [Fact]
    public async Task LaunchesSteamWhenItIsNotRunning()
    {
        var steam = new FakeSteamProcess(isRunning: false);
        var coordinator = new SteamStartupCoordinator(steam);

        var launched = await coordinator.HandleDualSenseConnectedAsync();

        Assert.True(launched);
        Assert.Equal(1, steam.LaunchCount);
    }

    [Fact]
    public async Task LaunchesSteamOnlyOnceForConcurrentConnectionEvents()
    {
        var steam = new BlockingFakeSteamProcess();
        var coordinator = new SteamStartupCoordinator(steam);

        var firstLaunch = coordinator.HandleDualSenseConnectedAsync();
        await steam.LaunchStarted.Task;
        var secondLaunch = coordinator.HandleDualSenseConnectedAsync();

        steam.AllowLaunches.SetResult();

        var results = await Task.WhenAll(firstLaunch, secondLaunch);

        Assert.Equal(1, results.Count(result => result));
        Assert.Equal(1, steam.LaunchCount);
    }

    private sealed class FakeSteamProcess : ISteamProcess
    {
        private readonly bool _isRunning;

        public FakeSteamProcess(bool isRunning)
        {
            _isRunning = isRunning;
        }

        public int LaunchCount { get; private set; }

        public bool IsRunning() => _isRunning;

        public Task LaunchAsync(CancellationToken cancellationToken)
        {
            LaunchCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class BlockingFakeSteamProcess : ISteamProcess
    {
        public TaskCompletionSource LaunchStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource AllowLaunches { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int LaunchCount { get; private set; }

        public bool IsRunning() => LaunchCount > 0;

        public async Task LaunchAsync(CancellationToken cancellationToken)
        {
            LaunchCount++;
            LaunchStarted.TrySetResult();
            await AllowLaunches.Task.WaitAsync(cancellationToken);
        }
    }
}