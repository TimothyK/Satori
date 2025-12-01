using System.Text.Json;
using System.Text.Json.Serialization;
using Shouldly;

namespace Satori.Converters.Tests;

[TestClass]
public class EncryptedStringTests
{
    #region Act

    private static string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions() {WriteIndented = true});
    }

    #endregion Act

    [TestMethod]
    public void Write()
    {
        //Arrange
        var credentials = new Credentials
        {
            UserName = "TimothyK",
            Password = "secret"
        };

        //Act
        var json = Serialize(credentials);

        //Assert
        const string expected = """
                                {
                                  "UserName": "TimothyK",
                                  "Password": "c2VjcmV0"
                                }
                                """;
        json.ShouldBe(expected);
    }
    
    [TestMethod]
    public void Read()
    {
        //Arrange
        const string payload = 
            """
            {
              "UserName": "TimothyK",
              "Password": "c2VjcmV0"
            }
            """;

        //Act
        var credentials = JsonSerializer.Deserialize<Credentials>(payload);

        //Assert
        credentials.ShouldNotBeNull();
        credentials.UserName.ShouldBe("TimothyK");
        credentials.Password.ShouldBe("secret");
    }
    
    /// <summary>
    /// This is for backward compatibility so that we can add encryption to a field that didn't have it before and the value will still deserialize.
    /// The value will need to be serialized again to add the encryption.
    /// </summary>
    [TestMethod]
    public void DecryptError_ReturnsCorruptValue()
    {
        //Arrange
        const string payload = 
            """
            {
              "UserName": "TimothyK",
              "Password": "secret"
            }
            """;

        //Act
        var credentials = JsonSerializer.Deserialize<Credentials>(payload);

        //Assert
        credentials.ShouldNotBeNull();
        credentials.UserName.ShouldBe("TimothyK");
        credentials.Password.ShouldBe("secret");
    }
}

internal class Credentials
{
    public required string UserName { get; set; }
    [JsonConverter(typeof(EncryptedStringConverter))]
    public required string Password { get; set; }
}