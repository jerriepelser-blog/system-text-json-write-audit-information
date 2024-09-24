using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

var jsonSerializerOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
    TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        .WithAddedModifier(jsonTypeInfo =>
        {
            string ApplyPropertyNamingPolicy(string propertyName)
            {
                if (jsonTypeInfo.Options.PropertyNamingPolicy == null)
                {
                    return propertyName;
                }

                return jsonTypeInfo.Options.PropertyNamingPolicy.ConvertName(propertyName);
            }

            if (jsonTypeInfo.Kind != JsonTypeInfoKind.Object)
                return;

            var createdBy = jsonTypeInfo.CreateJsonPropertyInfo(typeof(string), ApplyPropertyNamingPolicy("CreatedBy"));
            createdBy.Get = _ => "Jerrie Pelser";
            var createdAt = jsonTypeInfo.CreateJsonPropertyInfo(typeof(DateTimeOffset), ApplyPropertyNamingPolicy("CreatedAt"));
            createdAt.Get = _ => DateTimeOffset.UtcNow;

            jsonTypeInfo.Properties.Add(createdBy);
            jsonTypeInfo.Properties.Add(createdAt);
        })
};

Console.WriteLine(JsonSerializer.Serialize(new Person("Jerrie", "Pelser"), jsonSerializerOptions));

public record Person( string FirstName, string LastName);
