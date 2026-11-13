using System.Text.Json;

namespace Voidwell.Common.Configuration.Tests;

public class ApplicationConfigurationJsonTests
{
    [Fact]
    public void Flatten_Scalars_UseConfigurationText()
    {
        Assert.Equal("true", Single("Key", JsonSerializer.SerializeToElement(true)));
        Assert.Equal("1.5", Single("Key", JsonSerializer.SerializeToElement(1.5)));
        Assert.Equal("text", Single("Key", JsonSerializer.SerializeToElement("text")));
        Assert.Null(Single("Key", JsonSerializer.SerializeToElement<string?>(null)));
    }

    [Fact]
    public void Flatten_ObjectsAndArrays_BecomeChildKeys()
    {
        var value = JsonSerializer.SerializeToElement(new { Name = "a", Items = new[] { 1, 2 } });

        var data = ApplicationConfigurationJson.Flatten("Root", value).ToDictionary();

        Assert.Equal("a", data["Root:Name"]);
        Assert.Equal("1", data["Root:Items:0"]);
        Assert.Equal("2", data["Root:Items:1"]);
        Assert.Equal(3, data.Count);
    }

    [Fact]
    public void Parse_InvalidJson_IsTreatedAsString()
    {
        var value = ApplicationConfigurationJson.Parse("not json");

        Assert.Equal(JsonValueKind.String, value.ValueKind);
        Assert.Equal("not json", value.GetString());
    }

    private static string? Single(string key, JsonElement value)
    {
        return Assert.Single(ApplicationConfigurationJson.Flatten(key, value)).Value;
    }
}
