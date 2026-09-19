using Mirror.Tracking;
using Xunit;

namespace Mirror.Tracking.Tests;

public class ApplicationIdentityResolverTests
{
    [Theory]
    [InlineData("code.exe", "dev.vscode", "Visual Studio Code", "Development")]
    [InlineData("devenv.exe", "dev.vs", "Visual Studio", "Development")]
    [InlineData("msedge.exe", "browser.edge", "Microsoft Edge", "Browser")]
    [InlineData("chrome.exe", "browser.chrome", "Google Chrome", "Browser")]
    [InlineData("slack.exe", "comm.slack", "Slack", "Communication")]
    [InlineData("teams.exe", "comm.teams", "Microsoft Teams", "Communication")]
    [InlineData("spotify.exe", "media.spotify", "Spotify", "Media")]
    public void ResolveIdentity_KnownApplications_MatchesCanonicalMapping(string process, string expectedKey, string expectedName, string expectedCategory)
    {
        var resolver = new ApplicationIdentityResolver();
        var identity = resolver.ResolveIdentity(process);

        Assert.Equal(expectedKey, identity.AppKey);
        Assert.Equal(expectedName, identity.DisplayName);
        Assert.Equal(expectedCategory, identity.Category);
        Assert.False(identity.IsExcluded);
    }

    [Fact]
    public void SetCategoryOverride_OverridesDefaultCategory()
    {
        var resolver = new ApplicationIdentityResolver();
        resolver.SetCategoryOverride("media.spotify", "AudioWork");

        var identity = resolver.ResolveIdentity("spotify.exe");
        Assert.Equal("AudioWork", identity.Category);
    }

    [Fact]
    public void SetExcluded_MarksApplicationAsExcluded()
    {
        var resolver = new ApplicationIdentityResolver();
        resolver.SetExcluded("keepass", true);

        var identity = resolver.ResolveIdentity("keepass.exe");
        Assert.True(identity.IsExcluded);
    }

    [Fact]
    public void ResolveIdentity_UnknownApplication_DefaultsGracefully()
    {
        var resolver = new ApplicationIdentityResolver();
        var identity = resolver.ResolveIdentity("custom_tool_42.exe");

        Assert.Equal("app.custom_tool_42.exe", identity.AppKey);
        Assert.Equal("Custom_tool_42", identity.DisplayName);
        Assert.Equal("Other", identity.Category);
        Assert.False(identity.IsExcluded);
    }
}
