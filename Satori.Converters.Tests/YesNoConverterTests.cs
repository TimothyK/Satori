using System.Text.Json;
using System.Text.Json.Serialization;
using Shouldly;
using Snapshooter.MSTest;

namespace Satori.Converters.Tests;

[TestClass]
public class YesNoConverterTests
{
    #region Act

    private static string Serialize(Survey survey)
    {
        return JsonSerializer.Serialize(survey, new JsonSerializerOptions() {WriteIndented = true});
    }

    #endregion Act

    [TestMethod]
    public void Write()
    {
        //Arrange
        var survey = new Survey
        {
            Question1Response = true,
            Question2Response = false,
            Question3Response = true,
        };

        //Act
        var json = Serialize(survey);

        //Assert
        Snapshot.Match(json);
    }
    
    [TestMethod]
    public void Read()
    {
        //Arrange
        const string payload = 
            """
            {
                "Question1Response": "Yes",
                "Question2Response": "No"
            }
            """;

        //Act
        var survey = JsonSerializer.Deserialize<Survey>(payload);

        //Assert
        survey.ShouldNotBeNull();
        survey.Question1Response.ShouldBeTrue();
        survey.Question2Response.ShouldBeFalse();
    }
}

internal class Survey
{
    [JsonConverter(typeof(YesNoConverter))]
    public bool Question1Response { get; set; }
    [JsonConverter(typeof(YesNoConverter))]
    public bool Question2Response { get; set; }
    public bool Question3Response { get; set; }
}