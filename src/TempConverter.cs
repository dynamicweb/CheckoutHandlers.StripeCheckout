using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout;

///<summary>
/// Please delete this class when the problem is solved.
/// Right now our converter doesn't support the EnumMember attribute, and without this attribute we can't parse sneak_case enums. 
/// When someone  will add the supporting of EnumMember, this class should be deleted.
/// As temporary solution, I have copied our code from Core converter, and added JsonNamingPolicy.SnakeCaseLower to parse the enums properly.
/// Without this, the provider will not work. But it should be deleted, after the EnumMember related functionality is implemented!
/// </summary>

internal static class TempConverter
{
    public static T Deserialize<T>(string data)
    {
        return SystemTextJsonDeserializer.Default.Deserialize<T>(data);
    }
}

internal sealed class SystemTextJsonDeserializer
{
    /// <summary>
    /// Options used for deserialization.
    /// </summary>
    private JsonSerializerOptions DeserializeOptions { get; }

    private SystemTextJsonDeserializer(JsonSerializerOptions deserializeOptions)
    {
        DeserializeOptions = deserializeOptions;
    }

    private static SystemTextJsonDeserializer Create()
    {
        var deserializeOptions = CreateOptions();
        return new SystemTextJsonDeserializer(deserializeOptions);
    }

    /// <summary>
    /// Creates json serializer options
    /// </summary>
    /// <returns>An instance of <see cref="JsonSerializerOptions"/> which is used for serialization/deserialization.</returns>
    private static JsonSerializerOptions CreateOptions()
    {
        var settings = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            PropertyNameCaseInsensitive = true,
            IncludeFields = true,
            AllowTrailingCommas = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            ReadCommentHandling = JsonCommentHandling.Skip,
            PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
            TypeInfoResolver = new SystemTextJsonDataContractTypeInfoResolver()
        };

        settings.Converters.Add(new SystemTextJsonInferredTypesConverter());
        settings.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, true));

        return settings;
    }

    /// <summary>
    /// Gets json converter with default formatting (indented).
    /// </summary>
    public static SystemTextJsonDeserializer Default { get; } = Create();

    public T Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        return JsonSerializer.Deserialize<T>(json, DeserializeOptions);
    }
}

internal sealed class SystemTextJsonInferredTypesConverter : JsonConverter<object>
{
    public override object Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) => reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number when reader.TryGetInt64(out long l) => l,
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.String when reader.TryGetDateTime(out DateTime datetime) => datetime,
            JsonTokenType.String => reader.GetString()!,
            _ => JsonDocument.ParseValue(ref reader).RootElement.Clone()
        };

    public override void Write(
        Utf8JsonWriter writer,
        object objectToWrite,
        JsonSerializerOptions options)
    {
        var objectToWriteType = objectToWrite.GetType();
        if (objectToWriteType == typeof(object))
            return; // do not serialize an empty object - return to avoid infinite recursion (stack overflow).

        JsonSerializer.Serialize(writer, objectToWrite, objectToWriteType, options);
    }
}

internal sealed class SystemTextJsonDataContractTypeInfoResolver : DefaultJsonTypeInfoResolver
{
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        JsonTypeInfo jsonTypeInfo = base.GetTypeInfo(type, options);

        if (jsonTypeInfo.Kind == JsonTypeInfoKind.Object && type.GetCustomAttribute<DataContractAttribute>() is not null)
        {
            jsonTypeInfo.Properties.Clear();

            foreach (PropertyInfo propInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (propInfo.GetCustomAttribute<IgnoreDataMemberAttribute>() is not null)
                {
                    continue;
                }

                DataMemberAttribute? attr = propInfo.GetCustomAttribute<DataMemberAttribute>();
                JsonPropertyInfo jsonPropertyInfo = jsonTypeInfo.CreateJsonPropertyInfo(propInfo.PropertyType, attr?.Name ?? propInfo.Name);
                jsonPropertyInfo.Order = attr?.Order ?? 0;

                jsonPropertyInfo.Get =
                    propInfo.CanRead
                    ? propInfo.GetValue
                    : null;

                jsonPropertyInfo.Set = propInfo.CanWrite
                    ? propInfo.SetValue
                    : null;

                jsonTypeInfo.Properties.Add(jsonPropertyInfo);
            }
        }

        return jsonTypeInfo;
    }
}
