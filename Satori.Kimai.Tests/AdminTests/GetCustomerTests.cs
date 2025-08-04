using System.Text.Json;
using Autofac;
using Flurl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RichardSzalay.MockHttp;
using Satori.Kimai.Tests.Globals;
using Satori.Kimai.ViewModels;
using Shouldly;

namespace Satori.Kimai.Tests.AdminTests;

[TestClass]
public class GetCustomerTests
{
    #region Helpers

    #region Arrange
    private readonly ConnectionSettings _connectionSettings = Services.Scope.Resolve<ConnectionSettings>();

    private readonly MockHttpMessageHandler _mockHttp = Services.Scope.Resolve<MockHttpMessageHandler>();

    private Url GetCustomersUrl() =>
        _connectionSettings.Url
            .AppendPathSegment("api/customers")
            .AppendQueryParam("visible", 1);

    private static int _customerId;
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

    private static int _projectId;
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
    
    private void DefineMock()
    {
        _mockHttp.Clear();

        _mockHttp.When(GetCustomersUrl())
            .Respond("application/json", JsonSerializer.Serialize(_customers));
        _mockHttp.When(GetProjectsUrl())
            .Respond("application/json", JsonSerializer.Serialize(_projects));
    }

    #endregion Arrange

    #region Act

    private async Task<Customer[]> GetCustomersAsync()
    {
        //Arrange
        DefineMock();

        //Act
        var srv = Services.Scope.Resolve<IKimaiServer>();
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
        customers.Length.ShouldBe(1);
        var actual = customers[0];
        actual.Id.ShouldBe(customer.Id);
        actual.Name.ShouldBe(customer.Name);
    }
    
    [TestMethod]
    public async Task Customer_NoLogo_ReturnsNull()
    {
        //Arrange
        var customer = BuildCustomer();
        customer.Comment = null;

        //Act
        var customers = await GetCustomersAsync();

        //Assert
        var actual = customers.Single();
        actual.Logo.ShouldBeNull();
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
    public async Task Customer_HasBadUriLogo_ReturnsNull()
    {
        //Arrange
        var customer = BuildCustomer();
        const string expectedUri = "http:/images.com/MyLogo.png";  // Missing slash after http means the URL is invalid
        customer.Comment = "[Logo](" + expectedUri + ")";

        //Act
        var customers = await GetCustomersAsync();

        //Assert
        var actual = customers.Single();
        actual.Logo.ShouldBeNull();
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

    #endregion Project Tests
}