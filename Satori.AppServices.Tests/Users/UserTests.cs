using Flurl;
using Moq;
using Satori.AppServices.Services;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.Tests.TestDoubles.AlertServices;
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
    private readonly Mock<IAzureDevOpsServer> _azureDevOpsMock;
    private readonly Mock<IKimaiServer> _kimaiMock;
    private readonly TestAlertService _alertService = new();

    #region Helpers

    #region Arrange

    private class TestData
    {
        public TestData()
        {
            Person.Me = null;  //Clear cache

            TestUserAzureDevOpsId = Guid.NewGuid();
            ConnectionData = new ConnectionData
            {
                AuthenticatedUser = new ConnectionUser { Id = TestUserAzureDevOpsId },
                DeploymentType = "onPremises",
            };
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

        public bool AzureDevOpsEnabled { get; set; } = true;
        public bool KimaiEnabled { get; set; } = true;

        private const string AzureDevOpsRootUrl = "http://devops.test/Org";

        public ConnectionSettings AzureDevOpsConnectionSettings { get; } =
            new()
            {
                Url = new Uri(AzureDevOpsRootUrl),
                PersonalAccessToken = "token"
            };

        public Guid TestUserAzureDevOpsId { get; } 

        public ConnectionData ConnectionData { get; set; }

        public Identity Identity { get; } 
        
        public User KimaiUser { get; } = new()
        {
            Id = Sequence.KimaiUserId.Next(),
            UserName = "kimai login",
            Alias = "Test User (Kimai)",
            Avatar = null,
            Language = "en_CA",
        };

    }

    public UserTests()
    {
        _azureDevOpsMock = BuildAzureDevOpsMock();
        _kimaiMock = BuildKimaiMock();
    }

    private UserService CreateUserService()
    {
        var srv = new UserService(_azureDevOpsMock.Object, _kimaiMock.Object, _alertService);
        return srv;
    }

    private Mock<IAzureDevOpsServer> BuildAzureDevOpsMock()
    {
        var mock = new Mock<IAzureDevOpsServer>(MockBehavior.Strict);

        mock.Setup(srv => srv.Enabled)
            .Returns(() => _testData.AzureDevOpsEnabled);

        mock.Setup(srv => srv.GetCurrentUserAsync())
            .ReturnsAsync(() => _testData.ConnectionData);

        mock.Setup(srv => srv.GetIdentityAsync(_testData.ConnectionData))
            .ReturnsAsync(() => _testData.Identity);

        mock.Setup(srv => srv.ConnectionSettings)
            .Returns(() => _testData.AzureDevOpsConnectionSettings);

        return mock;
    }


    private Mock<IKimaiServer> BuildKimaiMock()
    {
        var kimaiMock = new Mock<IKimaiServer>(MockBehavior.Strict);

        kimaiMock.Setup(srv => srv.Enabled)
            .Returns(() => _testData.KimaiEnabled);

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

    #region Assert

    [TestCleanup]
    public void TearDown()
    {
        _alertService.VerifyNoMessagesWereBroadcast();
    }

    #endregion Assert

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
    public void CacheStale()
    {
        //Arrange
        var azureDevOpsUser = new AzureDevOps.Models.User
        {
            Id = _testData.TestUserAzureDevOpsId,
            DisplayName = "New User",
            ImageUrl = "http://newuser.com/avatar",
            UniqueName = "New User",
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
    
    [TestMethod]
    public void AzureDevOpsAndKimaiDisabled_ReturnsEmpty()
    {
        //Arrange
        _testData.AzureDevOpsEnabled = false;
        _testData.KimaiEnabled = false;

        //Act
        var user = GetCurrentUser();

        //Assert
        user.ShouldBeSameAs(Person.Empty);
    }

    [TestMethod]
    public void AzureDevOpsDisabled_ReturnsKimaiData()
    {
        //Arrange
        _testData.AzureDevOpsEnabled = false;
        _testData.KimaiUser.Avatar = new Uri("http://gravatar.com/me");
    
        //Act
        var user = GetCurrentUser();

        //Assert
        user.KimaiId.ShouldBe(_testData.KimaiUser.Id);
        user.DisplayName.ShouldBe(_testData.KimaiUser.Alias);
        user.AvatarUrl.ShouldBe(_testData.KimaiUser.Avatar);
        user.AzureDevOpsId.ShouldBe(Guid.Empty);
        user.DomainLogin.ShouldBeNull();
    }
    
    [TestMethod]
    public void AzureDevOpsDisabledAndNoKimaiAvatar_ReturnsDefaultAvatar()
    {
        //Arrange
        _testData.AzureDevOpsEnabled = false;
        _testData.KimaiUser.Avatar = null;

        //Act
        var user = GetCurrentUser();

        //Assert
        user.AvatarUrl.ShouldBe(new Url("/images/DefaultAvatar.png").ToUri());
    }

    [TestMethod]
    public void KimaiDisabled_ReturnsNoKimaiData()
    {
        //Arrange
        _testData.KimaiEnabled = false;
    
        //Act
        var user = GetCurrentUser();

        //Assert
        user.KimaiId.ShouldBeNull();
        user.Language.ShouldBe("en");
        user.FirstDayOfWeek.ShouldBe(DayOfWeek.Monday);
    }

    #region Connection Errors

    [TestMethod] public void KimaiConnectionError()
    {
        //Arrange
        _kimaiMock.Setup(srv => srv.GetMyUserAsync()).Throws<ApplicationException>();

        //Act
        var user = GetCurrentUser();
        
        //Asset
        user.AzureDevOpsId.ShouldBe(_testData.Identity.Id);
        _alertService.LastException.ShouldNotBeNull();
        _alertService.LastException.ShouldBeOfType<ApplicationException>();
        _alertService.DisableVerifications();
    }
    
    [TestMethod] public void AzureDevOpsConnectionError()
    {
        //Arrange
        _azureDevOpsMock.Setup(srv => srv.GetCurrentUserAsync()).Throws<ApplicationException>();

        //Act
        var user = GetCurrentUser();
        
        //Asset
        user.KimaiId.ShouldBe(_testData.KimaiUser.Id);
        _alertService.LastException.ShouldNotBeNull();
        _alertService.LastException.ShouldBeOfType<ApplicationException>();
        _alertService.DisableVerifications();
    }
    
    [TestMethod] public void NoConnections_ReturnEmptyPerson()
    {
        //Arrange
        _kimaiMock.Setup(srv => srv.GetMyUserAsync()).Throws<ApplicationException>();
        _azureDevOpsMock.Setup(srv => srv.GetCurrentUserAsync()).Throws<ApplicationException>();

        //Act
        var user = GetCurrentUser();
        
        //Asset
        user.ShouldBe(Person.Empty);
        _alertService.DisableVerifications();
    }

    #endregion Connection Errors
}