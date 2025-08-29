using Flurl;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RichardSzalay.MockHttp;
using Satori.Kimai.Models;
using Satori.Kimai.Tests.Globals;
using Satori.Kimai.Tests.UserTests.SampleFiles;
using Shouldly;

namespace Satori.Kimai.Tests.UserTests;

[TestClass]
public class UserTests
{
    private readonly ServiceProvider _serviceProvider;

    public UserTests()
    {
        var services = new KimaiServiceCollection();
        _serviceProvider = services.BuildServiceProvider();

        _connectionSettings = _serviceProvider.GetRequiredService<ConnectionSettings>();
        _mockHttp = _serviceProvider.GetRequiredService<MockHttpMessageHandler>();
    }

    #region Helpers

    #region Arrange

    private readonly ConnectionSettings _connectionSettings;


    private Url GetUrl() =>
        _connectionSettings.Url
            .AppendPathSegment("api/users/me");

    private readonly MockHttpMessageHandler _mockHttp;

    private void SetResponse(Url url, byte[] response)
    {
        _mockHttp.When(url).Respond("application/json", System.Text.Encoding.Default.GetString(response));
    }

    #endregion Arrange

    #region Act

    private User GetMyUser()
    {
        //Arrange
        SetResponse(GetUrl(), SampleUsers.SampleUser);

        //Act
        var srv = _serviceProvider.GetRequiredService<IKimaiServer>();
        return srv.GetMyUserAsync().Result;
    }

    #endregion Act

    #endregion Helpers

    [TestMethod] public void ASmokeTest() => GetMyUser().UserName.ShouldBe("TimothyK");
    [TestMethod] public void Id() => GetMyUser().Id.ShouldBe(42);
    [TestMethod] public void TimeZone() => GetMyUser().TimeZone.ShouldBe("America/Edmonton");
    [TestMethod] public void Language() => GetMyUser().Language.ShouldBe("en_CA");
    [TestMethod] public void Alias() => GetMyUser().Alias.ShouldBe("Timothy Klenke");
    [TestMethod] public void AccountNumber() => GetMyUser().AccountNumber.ShouldBe(@"Domain\TimothyK");
    [TestMethod] public void Avatar() => GetMyUser().Avatar.ShouldBe(new Uri("https://codemonkeyprojectiles.com/img/TimothyKlenke-2022.avatar.jpg"));
    [TestMethod] public void Enabled() => GetMyUser().Enabled.ShouldBeTrue();

    [TestMethod] public void PreferenceFirstWeekday()
    {
        var user = GetMyUser();
        user.Preferences.ShouldNotBeNull();
        user.Preferences.Single(p => p.Name == "first_weekday").Value.ShouldBe("monday");
    }
}