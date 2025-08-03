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

    private void DefineMock()
    {
        _mockHttp.Clear();

        _mockHttp.When(GetCustomersUrl())
            .Respond("application/json", JsonSerializer.Serialize(_customers));
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
}