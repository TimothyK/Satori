using Flurl;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Models;
using Satori.AzureDevOps.Tests.Globals;
using Shouldly;

namespace Satori.AzureDevOps.Tests.Users;

[TestClass]
public class GetIdentityTests
{
    private readonly ServiceProvider _serviceProvider;

    public GetIdentityTests()
    {
        var services = new AzureDevOpsServiceCollection();
        _serviceProvider = services.BuildServiceProvider();

        _connectionSettings = _serviceProvider.GetRequiredService<ConnectionSettings>();
        _mockHttp = _serviceProvider.GetRequiredService<MockHttpMessageHandler>();
    }

    #region Helpers

    #region Arrange

    private readonly ConnectionSettings _connectionSettings;


    private Url GetUrl(Guid id) =>
        _connectionSettings.Url
            .AppendPathSegment("_apis/Identities")
            .AppendPathSegment(id)
            .AppendQueryParam("api-version", "6.0-preview.1");

    private readonly MockHttpMessageHandler _mockHttp;

    private void SetResponse(Guid id) => SetResponse(GetUrl(id), GetPayload(id));
    private void SetResponse(Url url, byte[] response)
    {
        _mockHttp.When(url).Respond("application/json", System.Text.Encoding.Default.GetString(response));
    }

    private static byte[] GetPayload(Guid id)
    {
        if (id == TestUser)
        {
            return SampleFiles.SampleResponses.Identity;
        }

        throw new ArgumentOutOfRangeException($"Unknown test identity ID: {id}");
    }

    private static readonly Guid TestUser = new("c00ef764-dc77-4b32-9a19-590db59f039b");
    
    #endregion Arrange

    #region Act

    private Identity GetIdentity(Guid id)
    {
        //Arrange
        SetResponse(id);

        //Act
        var srv = _serviceProvider.GetRequiredService<IAzureDevOpsServer>();
        return srv.GetIdentityAsync(id).Result;
    }

    #endregion Act

    #endregion Helpers

    [TestMethod] public void ASmokeTest() => GetIdentity(TestUser).Id.ShouldBe(TestUser);
    [TestMethod] public void DisplayName() => GetIdentity(TestUser).ProviderDisplayName.ShouldBe("Timothy Klenke");
    [TestMethod] public void IsActive() => GetIdentity(TestUser).IsActive.ShouldBeTrue();
    [TestMethod] public void JobTitle() => GetIdentity(TestUser).Properties.Description.ShouldHaveValue().ShouldBe("Code Monkey");
    [TestMethod] public void Domain() => GetIdentity(TestUser).Properties.Domain.ShouldHaveValue().ShouldBe("Domain");
    [TestMethod] public void Account() => GetIdentity(TestUser).Properties.Account.ShouldHaveValue().ShouldBe("TimothyK");
    [TestMethod] public void Mail() => GetIdentity(TestUser).Properties.Mail.ShouldHaveValue().ShouldBe("timothy@klenkeverse.com");

    [TestMethod]
    public void ComplianceValidated() => 
        GetIdentity(TestUser).Properties.ComplianceValidated
            .ShouldHaveValue()
            .ShouldBe(new DateTimeOffset(2024, 5, 14, 0, 0, 0, TimeSpan.Zero));
}

internal static class IdentityPropertyValueExtensions
{
    public static T ShouldHaveValue<T>(this IdentityPropertyValue<T>? property)
    {
        property.ShouldNotBeNull();
        return property.Value;
    }
}