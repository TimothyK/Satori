using Moq;
using Satori.AppServices.Services;
using Satori.AppServices.ViewModels;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;
using Satori.Kimai;
using Satori.Kimai.Models;
using Shouldly;
using ConnectionSettings = Satori.AzureDevOps.ConnectionSettings;
using User = Satori.Kimai.Models.User;

namespace Satori.AppServices.Tests.Users;

[TestClass]
public class UserTests
{
    private readonly TestData _testData = new();

    #region Helpers

    #region Arrange

    private class TestData
    {
        public TestData()
        {
            Person.Me = null;  //Clear cache

            TestUserAzureDevOpsId = Guid.NewGuid();
            Identity = new Identity
            {
                Id = TestUserAzureDevOpsId,
                ProviderDisplayName = "Test User (AzDO)",
                Properties = new IdentityProperties()
                {
                    Description = new IdentityPropertyValue<string>() { Value = "Code Monkey" },
                    Domain = new IdentityPropertyValue<string>() { Value = "DomainName" },
                    Account = new IdentityPropertyValue<string>() { Value = "TimothyK" },
                    Mail = new IdentityPropertyValue<string>() { Value = "timothy@klenkeverse.com" },
                }
            };
        }

        private const string AzureDevOpsRootUrl = "http://devops.test/Org";

        public ConnectionSettings AzureDevOpsConnectionSettings { get; set; } =
            new()
            {
                Url = new Uri(AzureDevOpsRootUrl),
                PersonalAccessToken = "token"
            };

        public Guid TestUserAzureDevOpsId { get; } 

        public Identity Identity { get; set; } 
        
        public User KimaiUser { get; set; } = new()
        {
            Id = 1,
            UserName = "kimai login",
            Alias = "Test User (Kimai)",
            Avatar = null,
            Language = "en_CA",
        };

    }



    private UserService CreateUserService()
    {
        var azureDevOpsMock = BuildAzureDevOpsMock();
        var kimaiMock = BuildKimaiMock();

        var srv = new UserService(azureDevOpsMock.Object, kimaiMock.Object);
        return srv;
    }


    private Mock<IAzureDevOpsServer> BuildAzureDevOpsMock()
    {
        var mock = new Mock<IAzureDevOpsServer>(MockBehavior.Strict);

        mock.Setup(srv => srv.GetCurrentUserIdAsync())
            .ReturnsAsync(() => _testData.TestUserAzureDevOpsId);

        mock.Setup(srv => srv.GetIdentityAsync(_testData.TestUserAzureDevOpsId))
            .ReturnsAsync(() => _testData.Identity);

        mock.Setup(srv => srv.ConnectionSettings)
            .Returns(() => _testData.AzureDevOpsConnectionSettings);

        return mock;
    }


    private Mock<IKimaiServer> BuildKimaiMock()
    {
        var kimaiMock = new Mock<IKimaiServer>(MockBehavior.Strict);

        kimaiMock.Setup(srv => srv.GetMyUserAsync())
            .ReturnsAsync(() => _testData.KimaiUser);

        return kimaiMock;
    }

    #endregion Arrange

    #region Act

    private Person GetCurrentUser()
    {
        //Arrange
        var srv = CreateUserService();

        //Act
        var user = srv.GetCurrentUserAsync().Result;
        return user;
    }

    #endregion Act

    #endregion Helpers

    [TestMethod] public void ASmokeTest() => GetCurrentUser().AzureDevOpsId.ShouldBe(_testData.Identity.Id);

    [TestMethod] public void DisplayName() => GetCurrentUser().DisplayName.ShouldBe(_testData.Identity.ProviderDisplayName);
    
    [TestMethod] public void AvatarUrl() => 
        GetCurrentUser().AvatarUrl
            .ShouldBe(new Uri($"{_testData.AzureDevOpsConnectionSettings.Url}/_api/_common/identityImage?id={_testData.Identity.Id}"));
    
    [TestMethod] public void AvatarUrl_OverrideInKimai()
    {
        //Arrange
        _testData.KimaiUser.Avatar = new Uri("http://gravatar.com/me");

        //Act
        var user = GetCurrentUser();

        //Assert
        user.AvatarUrl.ShouldBe(_testData.KimaiUser.Avatar);
    }

    [TestMethod] public void Email() => GetCurrentUser().EmailAddress.ShouldBe(_testData.Identity.Properties.Mail?.Value);
    [TestMethod] public void KimaiId() => GetCurrentUser().KimaiId.ShouldBe(_testData.KimaiUser.Id);
    [TestMethod] public void Language() => GetCurrentUser().Language.ShouldBe("en-CA");
    [TestMethod] public void DomainUser() => 
        GetCurrentUser().DomainLogin
            .ShouldBe($@"{_testData.Identity.Properties.Domain?.Value}\{_testData.Identity.Properties.Account?.Value}");

    [TestMethod]
    public void CacheBusted()
    {
        //Arrange
        var azureDevOpsUser = new AzureDevOps.Models.User
        {
            Id = _testData.TestUserAzureDevOpsId,
            DisplayName = "New User",
            ImageUrl = "http://newuser.com/avatar",
            UniqueName = "newuser",
            Url = "http://newuser.com/profile",
        };
        Person person = azureDevOpsUser;
        person.KimaiId.ShouldBeNull();

        //Act
        var user = GetCurrentUser();

        //Assert
        user.ShouldNotBeSameAs(person);
        user.KimaiId.ShouldBe(_testData.KimaiUser.Id);
    }

    [TestMethod]
    public void FirstDayOfWeek_Undefined_DefaultsToMonday()
    {
        //Arrange
        _testData.KimaiUser.Preferences = [];

        //Act
        var user = GetCurrentUser();

        //Assert
        user.FirstDayOfWeek.ShouldBe(DayOfWeek.Monday);
    }

    [TestMethod]
    public void FirstDayOfWeek_Monday()
    {
        //Arrange
        _testData.KimaiUser.Preferences =
        [
            new Preference() { Name = "firstDayOfWeek", Value = "monday" }
        ];

        //Act
        var user = GetCurrentUser();

        //Assert
        user.FirstDayOfWeek.ShouldBe(DayOfWeek.Monday);
    }

    [TestMethod]
    public void FirstDayOfWeek_Sunday()
    {
        //Arrange
        _testData.KimaiUser.Preferences =
        [
            new Preference() { Name = "firstDayOfWeek", Value = "sunday" }
        ];

        //Act
        var user = GetCurrentUser();

        //Assert
        user.FirstDayOfWeek.ShouldBe(DayOfWeek.Sunday);
    }
}