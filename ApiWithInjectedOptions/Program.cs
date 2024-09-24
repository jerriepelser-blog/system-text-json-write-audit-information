using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpContextAccessor();
builder.Services.ConfigureOptions<ConfigureJsonOptions>();
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/people",
        () => { return new[] { new Person("Jerrie", "Pelser"), new Person("John", "Doe") }; })
    .RequireAuthorization();

app.Run();

[JsonAuditable]
public record Person(string FirstName, string LastName);

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class JsonAuditableAttribute : JsonAttribute
{
}

public class ConfigureJsonOptions(IHttpContextAccessor httpContextAccessor) : IConfigureOptions<JsonOptions>
{
    public void Configure(JsonOptions options)
    {
        options.SerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver()
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
                {
                    return;
                }

                if (!jsonTypeInfo.Type.IsDefined(typeof(JsonAuditableAttribute), inherit: false))
                {
                    return;
                }
                
                var createdBy = jsonTypeInfo.CreateJsonPropertyInfo(typeof(string), ApplyPropertyNamingPolicy("CreatedBy"));
                createdBy.Get = _ => httpContextAccessor.HttpContext?.User.Identity?.Name ?? "Anonymous";
                var createdAt = jsonTypeInfo.CreateJsonPropertyInfo(typeof(DateTimeOffset), ApplyPropertyNamingPolicy("CreatedAt"));
                createdAt.Get = _ => DateTimeOffset.UtcNow;

                jsonTypeInfo.Properties.Add(createdBy);
                jsonTypeInfo.Properties.Add(createdAt);
            });
    }
}