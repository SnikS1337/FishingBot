using FishingBot.Core;
using Xunit;

namespace FishingBot.Tests;

public class BootstrapSmokeTests
{
    [Fact]
    public void CoreAssembly_IsReferenced()
    {
        Assert.Equal("0.1.0-bootstrap", BootstrapMarker.Version);
    }
}
