using TurnSteamOn.Core;

namespace TurnSteamOn.Tests;

public sealed class SingleInstanceGuardTests
{
    [Fact]
    public void OnlyOneGuardCanAcquireTheSameName()
    {
        var name = $"TurnSteamOn.Tests.{Guid.NewGuid():N}";

        using var first = SingleInstanceGuard.TryAcquire(name);
        using var second = SingleInstanceGuard.TryAcquire(name);

        Assert.NotNull(first);
        Assert.Null(second);
    }
}