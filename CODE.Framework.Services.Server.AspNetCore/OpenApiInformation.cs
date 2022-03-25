using CODE.Framework.Services.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CODE.Framework.Services.Server.AspNetCore
{
    public class OpenApiInformation
    {
        public string Openapi => "3.0.3";
        public OpenApiInfo Info { get; set; } = new OpenApiInfo();

        [JsonConverter(typeof(PathJsonConverter))]
        public Dictionary<string, OpenApiPathInfo> Paths { get; set; } = new Dictionary<string, OpenApiPathInfo>();

        public string BasePath { get; set; } = "/";
        public string Host { get; set; } = "localhost";
        public List<OpenApiTag> Tags { get; set; } = new List<OpenApiTag>();

        [JsonConverter(typeof(ComponentsJsonConverter))]
        public Dictionary<string, OpenApiSchemaDefinition> Components { get; set; } = new Dictionary<string, OpenApiSchemaDefinition>();
    }

    public class OpenApiInfo
    {
        public string Description { get; set; } = string.Empty;
    }

    public class OpenApiPathInfo
    {
        private readonly string _path;
        private readonly Dictionary<string, OpenApiVerb> _verbs = new Dictionary<string, OpenApiVerb>();

        public OpenApiPathInfo(string path, string verb = "post", string operationId = "")
        {
            _path = path;
            verb = verb.Trim().ToLowerInvariant();
            _verbs.Add(verb, new OpenApiVerb(operationId));
        }

        public Dictionary<string, OpenApiVerb> Verbs => _verbs;

        public override string ToString() => _path;

        public List<OpenApiTag> Tags { get; set; } = new List<OpenApiTag>();

        public Dictionary<string, OpenApiSchemaDefinition> Definitions { get; set; } = new Dictionary<string, OpenApiSchemaDefinition>();

        public List<OpenApiNamedOperationParameter> NamedParameters { get; } = new List<OpenApiNamedOperationParameter>();

        public List<OpenApiPositionalOperationParameter> PositionalParameters { get; } = new List<OpenApiPositionalOperationParameter>();
        
        public OpenApiPayload Payload { get; set; }

        public string ReturnTypeName { get; set; }
    }

    public class OpenApiSchemaDefinition
    {
        public string Name { get; set; }
        public Dictionary<string, OpenApiPropertyDefinition> Properties { get; } = new Dictionary<string, OpenApiPropertyDefinition>();
    }

    public class OpenApiPropertyDefinition
    {
        public OpenApiPropertyDefinition(Type type) => Type = type;

        public string Name => Type.Name;
        public Type Type { get; init; } = typeof(string);
        
        public bool IsSimpleType
        {
            get
            {
                var propertyType = Type;
                if (propertyType.IsArray) return false;
                if (propertyType.Name == "List`1") return false;

                if (propertyType == typeof(string) ||
                    propertyType == typeof(byte[]) ||
                    propertyType == typeof(int) ||
                    propertyType == typeof(decimal) ||
                    propertyType == typeof(double) ||
                    propertyType == typeof(DateTime) ||
                    propertyType == typeof(bool) ||
                    propertyType == typeof(Guid) ||
                    propertyType == typeof(char))
                    return true;

                return false;
            }
        }
    }

    public class OpenApiVerb
    {
        public OpenApiVerb(string operationId) => OperationId = operationId;

        public string OperationId { get; set; } = " ";

        public string Summary { get; internal set; } = " ";
    }

    public class OpenApiTag
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class ComponentsJsonConverter : JsonConverter<Dictionary<string, OpenApiSchemaDefinition>>
    {
        public override Dictionary<string, OpenApiSchemaDefinition> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<string, OpenApiSchemaDefinition> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteStartObject("schemas");
            foreach (var definitionName in value.Keys.OrderBy(k => k))
            {
                writer.WriteStartObject(definitionName); // "paths"

                writer.WritePropertyName("type");
                writer.WriteStringValue("object");

                writer.WriteStartObject("properties");

                foreach (var propertyName in value[definitionName].Properties.Keys)
                {
                    writer.WriteStartObject(propertyName);
                    WritePropertyTypeInformation(value[definitionName].Properties[propertyName].Type, writer);
                    writer.WriteEndObject();
                }

                writer.WriteEndObject(); // properties
                writer.WriteEndObject(); // componens object
            }
            writer.WriteEndObject(); // schemas
            writer.WriteEndObject();
        }

        private void WritePropertyTypeInformation(Type propertyType, Utf8JsonWriter writer)
        {
            var typeString = OpenApiHelper.GetOpenApiType(propertyType);
            if (!string.IsNullOrEmpty(typeString))
            {
                writer.WritePropertyName("type");
                writer.WriteStringValue(typeString);

                var formatString = OpenApiHelper.GetOpenApiTypeFormat(propertyType);
                if (!string.IsNullOrEmpty(formatString))
                {
                    writer.WritePropertyName("format");
                    writer.WriteStringValue(formatString);
                }

                if (propertyType.Name == "List`1")
                {
                    if (propertyType.GenericTypeArguments.Length > 0)
                    {
                        writer.WriteStartObject("items");
                        WritePropertyTypeInformation(propertyType.GenericTypeArguments[0], writer);
                        writer.WriteEndObject();
                    }
                }
                else if (propertyType.IsArray)
                {
                    // TODO: Define member types
                }
            }
            else
            {
                writer.WritePropertyName("$ref");
                writer.WriteStringValue($"#/components/schemas/{propertyType.FullName}");
            }
        }
    }

    public class PathJsonConverter : JsonConverter<Dictionary<string, OpenApiPathInfo>>
    {
        public override Dictionary<string, OpenApiPathInfo> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<string, OpenApiPathInfo> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var path in value.Keys.OrderBy(k => k))
            {
                var path2 = value[path];
                var route = path;

                foreach (var positionalParameter in path2.PositionalParameters.OrderBy(p => p.PositionIndex))
                    route += $"/{{{positionalParameter.Name}}}";

                if (path2.NamedParameters.Count > 0)
                {
                    route += "?";
                    for (int parameterCounter = 0; parameterCounter < path2.NamedParameters.Count; parameterCounter++)
                    {
                        var namedParameter = path2.NamedParameters[parameterCounter];
                        route += $"{namedParameter.Name}={{{namedParameter.Name}}}";
                        if (parameterCounter < path2.NamedParameters.Count - 1)
                            route += "&";
                    }
                }

                writer.WriteStartObject(route); // "paths"

                foreach (var verb in path2.Verbs.Keys)
                {
                    writer.WriteStartObject(verb);

                    var verbObject = path2.Verbs[verb];
                    writer.WritePropertyName("operationId");
                    writer.WriteStringValue(verbObject.OperationId);

                    writer.WritePropertyName("summary");
                    writer.WriteStringValue(""); // TODO: fill this in

                    writer.WritePropertyName("description");
                    writer.WriteStringValue(""); // TODO: fill this in

                    if (verb.ToLowerInvariant() != "get")
                    {
                        writer.WriteStartArray("consumes");
                        writer.WriteStringValue("application/json");
                        writer.WriteEndArray();
                    }

                    writer.WriteStartArray("parameters");
                    foreach (var parameter in path2.PositionalParameters.OrderBy(p => p.PositionIndex))
                        WritePathParameters(writer, parameter);
                    foreach (var parameter in path2.NamedParameters)
                        WritePathParameters(writer, parameter);

                    if (path2.Payload != null)
                    {
                        writer.WriteStartObject();
                        writer.WritePropertyName("name");
                        writer.WriteStringValue("body");
                        writer.WritePropertyName("in");
                        writer.WriteStringValue("query");
                        writer.WritePropertyName("required");
                        writer.WriteBooleanValue(true);
                        //writer.WriteStartObject();
                        writer.WritePropertyName("schema");
                        writer.WriteStartObject();
                        writer.WritePropertyName("$ref");
                        writer.WriteStringValue($"#/components/schemas/{path2.Payload.Type.FullName}");
                        writer.WriteEndObject();
                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();

                    writer.WriteStartArray("produces");
                    writer.WriteStringValue("application/json");
                    writer.WriteEndArray();

                    writer.WriteStartObject("responses");
                    writer.WriteStartObject("200");
                    writer.WritePropertyName("description");
                    writer.WriteStringValue("Success");

                    writer.WritePropertyName("content");
                    writer.WriteStartObject();
                    writer.WritePropertyName("application/json");
                    writer.WriteStartObject();
                    writer.WritePropertyName("schema");
                    writer.WriteStartObject();
                    writer.WritePropertyName("$ref");
                    writer.WriteStringValue($"#/components/schemas/{path2.ReturnTypeName}");
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                    writer.WriteEndObject();

                    writer.WriteStartArray("tags");
                    foreach (var tag in path2.Tags)
                        writer.WriteStringValue(tag.Name);
                    writer.WriteEndArray();

                    writer.WriteEndObject(); //verb
                }

                writer.WriteEndObject(); // "paths"
            }
            writer.WriteEndObject();
        }

        private static void WritePathParameters(Utf8JsonWriter writer, OpenApiNamedOperationParameter parameter)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("name");
            writer.WriteStringValue(parameter.Name);
            writer.WritePropertyName("in");
            writer.WriteStringValue("path");
            writer.WritePropertyName("required");
            writer.WriteBooleanValue(parameter.Required);
            var parameterOpenApiType = OpenApiHelper.GetOpenApiType(parameter.Type);
            if (!string.IsNullOrEmpty(parameterOpenApiType))
            {
                writer.WritePropertyName("type");
                writer.WriteStringValue(parameterOpenApiType);
                var parameterOpenApiTypeFormat = OpenApiHelper.GetOpenApiTypeFormat(parameter.Type);
                if (!string.IsNullOrEmpty(parameterOpenApiTypeFormat))
                {
                    writer.WritePropertyName("format");
                    writer.WriteStringValue(parameterOpenApiTypeFormat);
                }
            }
            writer.WriteEndObject();
        }
    }

    public static class OpenApiHelper
    {
        public static OpenApiSchemaDefinition GetTypeDefinition(Type type)
        {
            var schema = new OpenApiSchemaDefinition();

            schema.Name = type.FullName;

            var properties = type.GetProperties();
            foreach (var property in properties)
                schema.Properties.Add(property.Name, new OpenApiPropertyDefinition(property.PropertyType));

            return schema;
        }

        public static void AddTypeToComponents(OpenApiInformation openApiInfo, Type type)
        {
            var typeDefinition = GetTypeDefinition(type);
            if (openApiInfo.Components.ContainsKey(typeDefinition.Name)) return;

            openApiInfo.Components.Add(typeDefinition.Name, typeDefinition);

            foreach (var property in typeDefinition.Properties.Values.Where(p => !p.IsSimpleType))
            {
                if (property.Type.IsArray) continue; // TODO: Handle this
                if (property.Type.Name == "List`1")
                {
                    if (property.Type.GenericTypeArguments.Length > 0)
                        AddTypeToComponents(openApiInfo, property.Type.GenericTypeArguments[0]);
                }
                else
                    AddTypeToComponents(openApiInfo, property.Type);
            }
        }

        public static void ExtractOpenApiParameters(MethodInfo methodInfo, OpenApiPathInfo pathInfo)
        {
            var methodParameters = methodInfo.GetParameters();
            if (methodParameters.Length > 0)
            {
                var parameter = methodParameters[0]; // Note that in CODE Framework, there always is just a single in-parameter
                var parameterProperties = parameter.ParameterType.GetProperties();
                foreach (var parameterProperty in parameterProperties)
                {
                    var restUrParameterAttribute = parameterProperty.GetCustomAttribute<RestUrlParameterAttribute>();
                    if (restUrParameterAttribute != null)
                        if (restUrParameterAttribute.Mode == UrlParameterMode.Inline)
                            pathInfo.PositionalParameters.Add(new OpenApiPositionalOperationParameter { Name = parameterProperty.Name, Type = parameterProperty.PropertyType, PositionIndex = restUrParameterAttribute.Sequence });
                        else
                        {
                            var isRequired = true;
                            var dataMemberAttribute = parameterProperty.GetCustomAttribute<DataMemberAttribute>();
                            if (dataMemberAttribute != null)
                                isRequired = dataMemberAttribute.IsRequired;
                            pathInfo.NamedParameters.Add(new OpenApiNamedOperationParameter { Name = parameterProperty.Name, Type = parameterProperty.PropertyType, Required = isRequired });
                        }
                }
            }
        }

        public static string GetOpenApiType(Type type)
        {
            if (type == typeof(string) || type == typeof(char)) return "string";
            if (type == typeof(Guid)) return "string";
            if (type == typeof(byte[])) return "string";
            if (type == typeof(int)) return "integer";
            if (type == typeof(decimal) || type == typeof(double)) return "number";
            if (type == typeof(DateTime)) return "string";
            if (type == typeof(bool)) return "boolean";
            if (type.Name == "List`1") return "array";
            if (type.IsArray) return "array";
            return string.Empty;
        }

        public static string GetOpenApiTypeFormat(Type type)
        {
            if (type == typeof(Guid)) return "uuid";
            if (type == typeof(byte[])) return "byte";
            if (type == typeof(int)) return "int64";
            if (type == typeof(decimal) || type == typeof(double)) return "double";
            if (type == typeof(DateTime)) return "date-time";
            return string.Empty;
        }
    }

    public interface IOpenApiOperationParameter
    {
        string Name { get; set; }
        bool Required { get; set; }
        Type Type { get; set; }
    }

    public class OpenApiPayload
    {
        public Type Type { get; set; }
    }

    public class OpenApiNamedOperationParameter : IOpenApiOperationParameter
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public bool Required { get; set; } = true;
    }

    public class OpenApiPositionalOperationParameter : OpenApiNamedOperationParameter
    {
        public int PositionIndex { get; set; }
    }
}