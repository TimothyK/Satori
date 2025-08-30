using System.Text.Json;
using Flurl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Satori.Kimai.Tests.Globals;
using Satori.Kimai.ViewModels;
using Shouldly;

namespace Satori.Kimai.Tests.AdminTests;

[TestClass]
public class GetCustomerTests
{
    private readonly ServiceProvider _serviceProvider;

    #region Helpers

    #region Arrange
    private readonly ConnectionSettings _connectionSettings;
    private readonly MockHttpMessageHandler _mockHttp;

    public GetCustomerTests()
    {
        var services = new KimaiServiceCollection();
        _serviceProvider = services.BuildServiceProvider();

        _connectionSettings = _serviceProvider.GetRequiredService<ConnectionSettings>();
        _mockHttp = _serviceProvider.GetRequiredService<MockHttpMessageHandler>();
    }

    private Url GetCustomersUrl() =>
        _connectionSettings.Url
            .AppendPathSegment("api/customers")
            .AppendQueryParam("visible", 1);

    private static int _customerId = 10;
    private readonly List<Models.Customer> _customers = [];

    private Models.Customer BuildCustomer()
    {
        var id = Interlocked.Increment(ref _customerId);
        var customer = new Models.Customer
        {
            Id = id,
            Name = $"Customer {id}",
            Visible = true
        };

        _customers.Add(customer);
        
        return customer;
    }

    private Url GetProjectsUrl() =>
        _connectionSettings.Url
            .AppendPathSegment("api/projects")
            .AppendQueryParam("visible", 1);

    private static int _projectId = 20;
    private readonly List<Models.ProjectMaster> _projects = [];

    private Models.ProjectMaster BuildProject()
    {
        var id = Interlocked.Increment(ref _projectId);
        var customer = _customers.FirstOrDefault() ?? BuildCustomer();
        var project = new Models.ProjectMaster
        {
            Id = id,
            Name = $"{100 + id} Project",
            Customer = customer.Id,
            Visible = true,
        };

        _projects.Add(project);

        return project;
    }

    private Url GetActivitiesUrl() =>
        _connectionSettings.Url
            .AppendPathSegment("api/activities")
            .AppendQueryParam("visible", 1);

    private static int _activityId = 30;
    private readonly List<Models.ActivityMaster> _activities = [];

    private Models.ActivityMaster BuildActivity()
    {
        var id = Interlocked.Increment(ref _activityId);
        var project = _projects.FirstOrDefault() ?? BuildProject();
        var activity = new Models.ActivityMaster()
        {
            Id = id,
            Name = $"{id} Meetings",
            Project = project.Id,
            Visible = true,
        };

        _activities.Add(activity);

        return activity;
    }

    private void DefineMock()
    {
        _mockHttp.Clear();

        _mockHttp.When(GetCustomersUrl())
            .Respond("application/json", JsonSerializer.Serialize(_customers));
        _mockHttp.When(GetProjectsUrl())
            .Respond("application/json", JsonSerializer.Serialize(_projects));
        _mockHttp.When(GetActivitiesUrl())
            .Respond("application/json", JsonSerializer.Serialize(_activities));
    }

    #endregion Arrange

    #region Act

    private async Task<Customers> GetCustomersAsync()
    {
        //Arrange
        DefineMock();
        var srv = _serviceProvider.GetRequiredService<IKimaiServer>();

        //Act
        return await srv.GetCustomersAsync();
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest_EmptyDataSet()
    {
        //Act
        var customers = await GetCustomersAsync();

        //Assert
        customers.ShouldBeEmpty();
    }

    #region Customer Tests

    [TestMethod]
    public async Task ASmokeTest_SingleCustomer()
    {
        //Arrange
        var customer = BuildCustomer();

        //Act
        var customers = await GetCustomersAsync();

        //Assert
        customers.ShouldNotBeEmpty();
        customers.Count().ShouldBe(1);
        var actual = customers.Single();
        actual.Id.ShouldBe(customer.Id);
        actual.Name.ShouldBe(customer.Name);
    }
    
    [TestMethod]
    public async Task Customer_NoLogo_ReturnsDefault()
    {
        //Arrange
        var customer = BuildCustomer();
        customer.Comment = null;

        //Act
        var customers = await GetCustomersAsync();

        //Assert
        var actual = customers.Single();
        actual.Logo.ShouldBeSameAs(Customer.DefaultLogo);
    }
    
    [TestMethod]
    public async Task Customer_HasLogo_ReturnsUri()
    {
        //Arrange
        var customer = BuildCustomer();
        const string expectedUri = "http://images.com/MyLogo.png";
        customer.Comment = "[Logo](" + expectedUri + ")";

        //Act
        var customers = await GetCustomersAsync();

        //Assert
        var actual = customers.Single();
        actual.Logo.ShouldNotBeNull();
        actual.Logo.ToString().ShouldBe(expectedUri);
    }
    
    [TestMethod]
    public async Task Customer_HasBadUriLogo_ReturnsDefault()
    {
        //Arrange
        var customer = BuildCustomer();
        const string expectedUri = "http:/images.com/MyLogo.png";  // Missing slash after http means the URL is invalid
        customer.Comment = "[Logo](" + expectedUri + ")";

        //Act
        var customers = await GetCustomersAsync();

        //Assert
        var actual = customers.Single();
        actual.Logo.ShouldBeSameAs(Customer.DefaultLogo);
    }

    #endregion Customer Tests

    #region Project Tests

    [TestMethod]
    public async Task NoProjects()
    {
        //Arrange
        BuildCustomer();

        //Act
        var customers = await GetCustomersAsync();

        //Assert
        var actual = customers.Single();
        actual.Projects.ShouldBeEmpty();
    }
    
    [TestMethod]
    public async Task SingleProject()
    {
        //Arrange
        var project = BuildProject();

        //Act
        var customers = await GetCustomersAsync();

        //Assert
        var customer = customers.Single();
        customer.Projects.ShouldNotBeEmpty();
        customer.Projects.Count.ShouldBe(1);
        var actual = customer.Projects[0];
        actual.Id.ShouldBe(project.Id);
        actual.Name.ShouldBe(project.Name);
    }
    
    [TestMethod]
    public async Task ProjectBackNavigation()
    {
        //Arrange
        BuildProject();

        //Act
        var customers = await GetCustomersAsync();

        //Assert
        var customer = customers.Single();
        var actual = customer.Projects.Single();
        actual.Customer.ShouldBeSameAs(customer);
    }
    
    [TestMethod]
    public async Task ProjectCode()
    {
        //Arrange
        var project = BuildProject();
        project.Name = "123 Ninja Project";

        //Act
        var customers = await GetCustomersAsync();

        //Assert
        var actual = customers.Single().Projects.Single();
        actual.ProjectCode.ShouldBe("123");
    }
    
    [TestMethod]
    public async Task ProjectCode_StripLeadingZeros()
    {
        //Arrange
        var project = BuildProject();
        project.Name = "0123 Ninja Project";

        //Act
        var customers = await GetCustomersAsync();

        //Assert
        var actual = customers.Single().Projects.Single();
        actual.ProjectCode.ShouldBe("123");
    }
    
    [TestMethod]
    public async Task ProjectCode_LeadingZerosWithNoProjectName_StripLeadingZeros()
    {
        //Arrange
        var project = BuildProject();
        project.Name = "0123";

        //Act
        var customers = await GetCustomersAsync();

        //Assert
        var actual = customers.Single().Projects.Single();
        actual.ProjectCode.ShouldBe("123");
    }

    [TestMethod]
    public async Task ProjectCode_PeriodSeparator()
    {
        //Arrange
        var project = BuildProject();
        project.Name = "123. Ninja Project";

        //Act
        var customers = await GetCustomersAsync();

        //Assert
        var actual = customers.Single().Projects.Single();
        actual.ProjectCode.ShouldBe("123");
    }
    
    [TestMethod]
    public async Task ProjectCode_HyphenSeparator()
    {
        //Arrange
        var project = BuildProject();
        project.Name = "123-Ninja Project";

        //Act
        var customers = await GetCustomersAsync();

        //Assert
        var actual = customers.Single().Projects.Single();
        actual.ProjectCode.ShouldBe("123");
    }
    
    [TestMethod]
    public async Task ProjectCode_NoNumberPrefix_UseWholeProjectName()
    {
        //Arrange
        var project = BuildProject();
        project.Name = "Ninja Project";

        //Act
        var customers = await GetCustomersAsync();

        //Assert
        var actual = customers.Single().Projects.Single();
        actual.ProjectCode.ShouldBe("Ninja Project");
    }

    #endregion Project Tests

    #region Activity Tests

    [TestMethod]
    public async Task Activity()
    {
        //Arrange
        var activity = BuildActivity();

        //Act
        var customers = await GetCustomersAsync();

        //Assert
        var project = customers.Single().Projects.Single();
        project.Activities.ShouldNotBeEmpty();
        project.Activities.Count.ShouldBe(1);
        var actual = project.Activities.Single();
        actual.Id.ShouldBe(activity.Id);
        actual.Name.ShouldBe(activity.Name);
    }
    
    [TestMethod]
    public async Task ActivityBackNavigation()
    {
        //Arrange
        BuildActivity();

        //Act
        var customers = await GetCustomersAsync();

        //Assert
        var project = customers.Single().Projects.Single();
        var actual = project.Activities.Single();
        actual.Project.ShouldBeSameAs(project);
    }

    [TestMethod]
    public async Task ActivityCode_SmokeTest()
    {
        //Arrange
        var activity = BuildActivity();
        activity.Name = "1.2.3 Meetings";

        //Act
        var customers = await GetCustomersAsync();

        //Assert
        var project = customers.Single().Projects.Single();
        var actual = project.Activities.Single();
        actual.ActivityCode.ShouldBe("1.2.3");
    }
    
    [TestMethod]
    public async Task ActivityCode_StripLeadingZeros()
    {
        //Arrange
        var activity = BuildActivity();
        activity.Name = "01.02.03 Meetings";

        //Act
        var customers = await GetCustomersAsync();

        //Assert
        var project = customers.Single().Projects.Single();
        var actual = project.Activities.Single();
        actual.ActivityCode.ShouldBe("1.2.3");
    }
    
    [TestMethod]
    public async Task ActivityCode_PeriodSeparator()
    {
        //Arrange
        var activity = BuildActivity();
        activity.Name = "1.2.3. Meetings";

        //Act
        var customers = await GetCustomersAsync();

        //Assert
        var project = customers.Single().Projects.Single();
        var actual = project.Activities.Single();
        actual.ActivityCode.ShouldBe("1.2.3");
    }
    
    [TestMethod]
    public async Task ActivityCode_HyphenSeparator()
    {
        //Arrange
        var activity = BuildActivity();
        activity.Name = "1.2.3-Meetings";

        //Act
        var customers = await GetCustomersAsync();

        //Assert
        var project = customers.Single().Projects.Single();
        var actual = project.Activities.Single();
        actual.ActivityCode.ShouldBe("1.2.3");
    }
    
    [TestMethod]
    public async Task ActivityCode_CodeHasZeroSegments()
    {
        //Arrange
        var activity = BuildActivity();
        activity.Name = "1.0.3 Meetings";

        //Act
        var customers = await GetCustomersAsync();

        //Assert
        var project = customers.Single().Projects.Single();
        var actual = project.Activities.Single();
        actual.ActivityCode.ShouldBe("1.0.3");
    }
    
    [TestMethod]
    public async Task ActivityCode_NoNumericPrefix_UseWholeActivityName()
    {
        //Arrange
        var activity = BuildActivity();
        activity.Name = "Meetings";

        //Act
        var customers = await GetCustomersAsync();

        //Assert
        var project = customers.Single().Projects.Single();
        var actual = project.Activities.Single();
        actual.ActivityCode.ShouldBe("Meetings");
    }
    #endregion Activity Tests
}