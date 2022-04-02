using CODE.Framework.Fundamentals.Utilities;
using CODE.Framework.Services.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

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
        public string Title { get; set; } = "Service Description";
        public string Description { get; set; } = "Open API Service Description";
        public string TermsOfService { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string License { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
    }

    public class OpenApiPathInfo
    {
        private readonly string _path;
        private readonly Dictionary<string, OpenApiVerb> _verbs = new Dictionary<string, OpenApiVerb>();

        public OpenApiPathInfo(string path, string verb = "post", string operationId = "", MethodInfo method = null)
        {
            _path = path;
            verb = verb.Trim().ToLowerInvariant();
            Method = method;
            _verbs.Add(verb, new OpenApiVerb(operationId));
        }

        public Dictionary<string, OpenApiVerb> Verbs => _verbs;

        public override string ToString() => _path;

        public List<OpenApiTag> Tags { get; set; } = new List<OpenApiTag>();

        public Dictionary<string, OpenApiSchemaDefinition> Definitions { get; set; } = new Dictionary<string, OpenApiSchemaDefinition>();

        public List<OpenApiNamedOperationParameter> NamedParameters { get; } = new List<OpenApiNamedOperationParameter>();

        public List<OpenApiPositionalOperationParameter> PositionalParameters { get; } = new List<OpenApiPositionalOperationParameter>();
        
        public OpenApiPayload Payload { get; set; }

        public Type ReturnType { get; set; }

        public MethodInfo Method { get; set; }

        public bool Obsolete { get; set; }
        public string ObsoleteReason { get; set; }
    }

    public class OpenApiSchemaDefinition
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<string, OpenApiPropertyDefinition> Properties { get; } = new Dictionary<string, OpenApiPropertyDefinition>();
        public bool Obsolete { get; set; }
        public string ObsoleteReason { get; set; }
    }

    public class OpenApiPropertyDefinition
    {
        public OpenApiPropertyDefinition(Type type, PropertyInfo info, string description = null, bool obsolete = false, string obsoleteReason = "")
        {
            Type = type;
            PropertyInfo = info;
            Description = description;
            Obsolete = obsolete;
            ObsoleteReason = obsoleteReason;
        }

        public string Description { get; set; }

        public bool Obsolete { get; set; }

        public string ObsoleteReason { get; set; }

        public string Name => Type.Name;

        public Type Type { get; init; } = typeof(string);

        public PropertyInfo PropertyInfo { get; set; }

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

        public string OperationId { get; set; } = string.Empty;
        public string Summary { get; internal set; } = string.Empty;
        public string Description { get; internal set; } = string.Empty;
    }

    public class OpenApiTag
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public OpenApiExternalDocumentation ExternalDocs { get; set; } = null;
    }

    public class OpenApiExternalDocumentation
    {
        public string Description { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public class ComponentsJsonConverter : JsonConverter<Dictionary<string, OpenApiSchemaDefinition>>
    {
        public override Dictionary<string, OpenApiSchemaDefinition> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Not needed
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<string, OpenApiSchemaDefinition> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteStartObject("schemas");
            foreach (var definitionName in value.Keys.OrderBy(k => k))
            {
                writer.WriteStartObject(definitionName);

                writer.WritePropertyName("type");
                writer.WriteStringValue("object");

                if (value[definitionName].Obsolete)
                {
                    writer.WritePropertyName("deprecated");
                    writer.WriteBooleanValue(true);

                    if (!string.IsNullOrEmpty(value[definitionName].ObsoleteReason))
                    {
                        if (string.IsNullOrEmpty(value[definitionName].Description))
                            value[definitionName].Description = value[definitionName].ObsoleteReason;
                        else
                            value[definitionName].Description +=  " Deprecated: " + value[definitionName].ObsoleteReason;
                    }
                }

                if (!string.IsNullOrEmpty(value[definitionName].Description))
                {
                    writer.WritePropertyName("description");
                    writer.WriteStringValue(value[definitionName].Description);
                }

                writer.WriteStartObject("properties");

                foreach (var propertyName in value[definitionName].Properties.Keys)
                {
                    var prop = value[definitionName].Properties[propertyName];
                    writer.WriteStartObject(propertyName);
                    WritePropertyTypeInformation(prop.Type, writer, prop.PropertyInfo, prop.Description, prop.Obsolete, prop.ObsoleteReason);
                    writer.WriteEndObject();
                }

                writer.WriteEndObject(); // properties
                writer.WriteEndObject(); // componens object
            }
            writer.WriteEndObject(); // schemas
            writer.WriteEndObject();
        }

        private void WritePropertyTypeInformation(Type propertyType, Utf8JsonWriter writer, PropertyInfo propertyInfo, string description = null, bool obsolete = false, string obsoleteReason = "")
        {
            var typeString = OpenApiHelper.GetOpenApiType(propertyType);
            if (!string.IsNullOrEmpty(typeString))
            {
                writer.WritePropertyName("type");
                writer.WriteStringValue(typeString);

                var formatString = OpenApiHelper.GetOpenApiTypeFormat(propertyType, propertyInfo);
                if (!string.IsNullOrEmpty(formatString))
                {
                    writer.WritePropertyName("format");
                    writer.WriteStringValue(formatString);
                }

                if (propertyType.IsEnum)
                {
                    OpenApiHelper.WriteEnumDeclaration(writer, propertyType);
                    var enumDescription = OpenApiHelper.GetOpenApiEnumDescription(propertyType);
                    if (string.IsNullOrEmpty(description))
                        description = enumDescription;
                    else
                        description += $" Enum values: {enumDescription}";
                }

                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    writer.WritePropertyName("nullable");
                    writer.WriteBooleanValue(true);
                }

                if (obsolete)
                {
                    writer.WritePropertyName("deprecated");
                    writer.WriteBooleanValue(true);

                    if (!string.IsNullOrEmpty(obsoleteReason))
                    {
                        if (!string.IsNullOrEmpty(description))
                            description += $" Deprecated: {obsoleteReason}";
                        else
                            description = $"Deprecated: {obsoleteReason}";
                    }
                }

                if (!string.IsNullOrEmpty(description))
                {
                    writer.WritePropertyName("description");
                    writer.WriteStringValue(description);
                }

                if (propertyType.Name == "List`1")
                {
                    if (propertyType.GenericTypeArguments.Length > 0)
                    {
                        writer.WriteStartObject("items");
                        WritePropertyTypeInformation(propertyType.GenericTypeArguments[0], writer, propertyInfo);
                        writer.WriteEndObject();
                    }
                }
                else if (propertyType.IsArray && !(typeString == "string" && formatString == "byte"))
                {
                    writer.WriteStartObject("items");
                    var elementType = propertyType.GetElementType();
                    var elementType2 = OpenApiHelper.GetOpenApiType(elementType);
                    if (!string.IsNullOrEmpty(elementType2))
                    {
                        writer.WritePropertyName("type");
                        writer.WriteStringValue(elementType2);
                        var elementTypeFormat = OpenApiHelper.GetOpenApiTypeFormat(elementType, propertyInfo);
                        if (!string.IsNullOrEmpty(elementTypeFormat))
                        {
                            writer.WritePropertyName("format");
                            writer.WriteStringValue(elementTypeFormat);
                        }
                    }
                    else
                        WritePropertyTypeInformation(elementType, writer, propertyInfo);
                    writer.WriteEndObject();
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
            // Not needed
            throw new NotImplementedException();
        }

        private string GetRouteFromOpenApiInfo(string route, List<OpenApiPositionalOperationParameter> positionalParameters, List<OpenApiNamedOperationParameter> namedParameters)
        {
            if (route.IndexOf("::") > -1)
                route = route.Substring(route.IndexOf("::") + 2);

            foreach (var positionalParameter in positionalParameters.OrderBy(p => p.PositionIndex))
                route += $"/{{{positionalParameter.Name}}}";

            if (namedParameters.Count > 0)
            {
                route += "?";
                for (int parameterCounter = 0; parameterCounter < namedParameters.Count; parameterCounter++)
                {
                    var namedParameter = namedParameters[parameterCounter];
                    route += $"{namedParameter.Name}={{{namedParameter.Name}}}";
                    if (parameterCounter < namedParameters.Count - 1)
                        route += "&";
                }
            }

            return route;
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<string, OpenApiPathInfo> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            var pathIdenticalToLast = false;
            var lastRoute = string.Empty;

            var normalizedKeys = value.Keys.OrderBy(k => k.Substring(k.IndexOf("::") + 2)).ToList();
            for (var keyCounter = 0; keyCounter < normalizedKeys.Count; keyCounter++)
            {
                var normalizedKey = normalizedKeys[keyCounter];
                var path = value[normalizedKey];
                var route = GetRouteFromOpenApiInfo(normalizedKey, path.PositionalParameters, path.NamedParameters);

                var nextRoute = string.Empty;
                if (keyCounter < normalizedKeys.Count - 1)
                {
                    var nextNormalizedKey = normalizedKeys[keyCounter + 1];
                    var nextPath = value[nextNormalizedKey];
                    nextRoute = GetRouteFromOpenApiInfo(nextNormalizedKey, nextPath.PositionalParameters, nextPath.NamedParameters);
                }
                var pathIdenticalToNext = nextRoute == route;
                pathIdenticalToLast = route == lastRoute;

                if (!pathIdenticalToLast) // If the last path was the same, then the node already exists, and we just add to it
                    writer.WriteStartObject(route); // "paths"

                foreach (var verb in path.Verbs.Keys)
                {
                    writer.WriteStartObject(verb);

                    var verbObject = path.Verbs[verb];
                    writer.WritePropertyName("operationId");
                    writer.WriteStringValue(verbObject.OperationId);

                    if (path.Obsolete)
                    {
                        writer.WritePropertyName("deprecated");
                        writer.WriteBooleanValue(true);
                    }

                    writer.WritePropertyName("summary");
                    writer.WriteStringValue(verbObject.Summary);

                    writer.WritePropertyName("description");
                    var description = verbObject.Description;
                    if (path.Obsolete && !string.IsNullOrEmpty(path.ObsoleteReason))
                    {
                        if (!string.IsNullOrEmpty(description))
                            description = description.Trim() + " Deprecated: " + path.ObsoleteReason;
                        else
                            description = "Deprecated: " + path.ObsoleteReason;
                    }
                    writer.WriteStringValue(description);

                    if (verb.ToLowerInvariant() != "get")
                    {
                        writer.WriteStartArray("consumes");
                        writer.WriteStringValue("application/json");
                        writer.WriteEndArray();
                    }

                    writer.WriteStartArray("parameters");
                    foreach (var parameter in path.PositionalParameters.OrderBy(p => p.PositionIndex))
                        WritePathParameters(writer, parameter);
                    foreach (var parameter in path.NamedParameters)
                        WritePathParameters(writer, parameter);
                    writer.WriteEndArray();

                    if (path.Payload != null)
                    {
                        writer.WriteStartObject("requestBody");
                        writer.WritePropertyName("required");
                        writer.WriteBooleanValue(true);
                        if (path.Payload != null && !string.IsNullOrEmpty(path.Payload.Description))
                        {
                            writer.WritePropertyName("description");
                            writer.WriteStringValue(path.Payload.Description);
                        }
                        writer.WriteStartObject("content");
                        writer.WriteStartObject("application/json");
                        writer.WritePropertyName("schema");
                        writer.WriteStartObject();
                        writer.WritePropertyName("$ref");
                        writer.WriteStringValue($"#/components/schemas/{path.Payload.Type.FullName}");
                        writer.WriteEndObject();
                        writer.WriteEndObject();
                        writer.WriteEndObject();
                        writer.WriteEndObject();
                    }

                    var responseContentType = "application/json";
                    if (path.ReturnType.GetInterfaces().Contains(typeof(IFileResponse)))
                    {
                        var contentTypeAttribute = path.Method.GetCustomAttributeEx<RestContentTypeAttribute>();
                        if (contentTypeAttribute != null && !string.IsNullOrEmpty(contentTypeAttribute.ContentType))
                            responseContentType = contentTypeAttribute.ContentType.Trim();
                        else
                            responseContentType = "application/x-binary";
                    }

                    writer.WriteStartArray("produces");
                    writer.WriteStringValue(responseContentType);
                    writer.WriteEndArray();

                    writer.WriteStartObject("responses");
                    writer.WriteStartObject("200");
                    writer.WritePropertyName("description");
                    writer.WriteStringValue("Success");

                    writer.WritePropertyName("content");
                    writer.WriteStartObject();
                    writer.WritePropertyName(responseContentType);
                    writer.WriteStartObject();
                    writer.WritePropertyName("schema");
                    writer.WriteStartObject();

                    var openApiType = OpenApiHelper.GetOpenApiType(path.ReturnType);
                    if (string.IsNullOrEmpty(openApiType))
                    {
                        writer.WritePropertyName("$ref");
                        writer.WriteStringValue($"#/components/schemas/{path.ReturnType}");
                    }
                    else
                    {
                        // This should really not happen, since it doesn't follow standard CODE Framework patterns.
                        // But we will still create docs if someone creates something like this.
                        // Note, that most likely, Swagger UI cannot execute a test call against this method anyway.
                        writer.WritePropertyName("type");
                        writer.WriteStringValue(openApiType);
                    }

                    writer.WriteEndObject();
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                    writer.WriteEndObject();

                    writer.WriteStartArray("tags");
                    foreach (var tag in path.Tags)
                        writer.WriteStringValue(tag.Name);
                    writer.WriteEndArray();

                    writer.WriteEndObject(); //verb
                }

                if (!pathIdenticalToNext) // If the next path is identical, then we leave the current node open so the next one can just add to it
                    writer.WriteEndObject(); // "paths"

                lastRoute = route;
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
            if (!string.IsNullOrEmpty(parameter.Description))
            {
                writer.WritePropertyName("description");
                writer.WriteStringValue(parameter.Description.Trim());
            }
            writer.WriteEndObject();
        }
    }

    public static class OpenApiHelper
    {
        public static OpenApiSchemaDefinition GetTypeDefinition(Type type, bool obsolete, string obsoleteReason, Dictionary<Assembly, OpenApiXmlDocumentationFile> xmlDocumentationFiles)
        {
            var schema = new OpenApiSchemaDefinition();

            schema.Name = type.FullName;
            schema.Obsolete = obsolete;
            schema.ObsoleteReason = obsoleteReason;
            schema.Description = GetSummary(type, xmlDocumentationFiles);
            if (string.IsNullOrEmpty(schema.Description))
                schema.Description = GetDescription(type, xmlDocumentationFiles);

            var properties = type.GetProperties();
            foreach (var property in properties)
            {
                var description = string.Empty;

                var descriptionAttribute = property.GetCustomAttributeEx<DescriptionAttribute>();
                if (descriptionAttribute != null && !string.IsNullOrEmpty(descriptionAttribute.Description))
                    description = descriptionAttribute.Description.Trim();
                else
                {
                    var descriptionAttribute2 = property.GetCustomAttributeEx<System.ComponentModel.DescriptionAttribute>();
                    if (descriptionAttribute2 != null && !string.IsNullOrEmpty(descriptionAttribute2.Description))
                        description = descriptionAttribute2.Description.Trim();
                }

                var obsolete2 = false;
                var obsoleteReason2 = string.Empty;
                var obsoleteAttribute = property.GetCustomAttribute<ObsoleteAttribute>();
                if (obsoleteAttribute != null)
                {
                    obsolete2 = true;
                    if (!string.IsNullOrEmpty(obsoleteAttribute.Message))
                        obsoleteReason2 = obsoleteAttribute.Message.Trim();
                }

                schema.Properties.Add(property.Name, new OpenApiPropertyDefinition(property.PropertyType, property, description, obsolete2, obsoleteReason2));
            }

            return schema;
        }

        public static void AddTypeToComponents(OpenApiInformation openApiInfo, Type type, bool obsolete, string obsoleteReason, Dictionary<Assembly, OpenApiXmlDocumentationFile> xmlDocumentationFiles)
        {
            var typeDefinition = GetTypeDefinition(type, obsolete, obsoleteReason, xmlDocumentationFiles);
            if (openApiInfo.Components.ContainsKey(typeDefinition.Name)) return;

            openApiInfo.Components.Add(typeDefinition.Name, typeDefinition);

            foreach (var property in typeDefinition.Properties.Values.Where(p => !p.IsSimpleType))
            {
                if (property.Type.IsArray)
                {
                    if (!(property.Type.FullName.StartsWith("System.") && property.Type.FullName.Split('.').Length == 2))
                        AddTypeToComponents(openApiInfo, property.Type.GetElementType(), property.Obsolete, property.ObsoleteReason, xmlDocumentationFiles);
                }
                else if (property.Type.Name == "List`1")
                {
                    if (property.Type.GenericTypeArguments.Length > 0)
                        if (string.IsNullOrEmpty(GetOpenApiType(property.Type.GenericTypeArguments[0]))) // We don't want simple types like string... so anything that is a known OpenApi type can be ignored here
                            AddTypeToComponents(openApiInfo, property.Type.GenericTypeArguments[0], property.Obsolete, property.ObsoleteReason, xmlDocumentationFiles);
                }
                else if (property.Type.IsGenericType && property.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    // Nullable types are likely just masqueraded simple types, so we can skip this one after all
                    continue;
                else
                    AddTypeToComponents(openApiInfo, property.Type, property.Obsolete, property.ObsoleteReason, xmlDocumentationFiles);
            }
        }

        public static void ExtractOpenApiParameters(MethodInfo methodInfo, OpenApiPathInfo pathInfo, Dictionary<Assembly, OpenApiXmlDocumentationFile> xmlDocumentationFiles)
        {
            var methodParameters = methodInfo.GetParameters();
            if (methodParameters.Length > 0)
            {
                var parameter = methodParameters[0]; // Note that in CODE Framework, there always is just a single in-parameter
                var parameterProperties = parameter.ParameterType.GetProperties();
                foreach (var parameterProperty in parameterProperties)
                {
                    var description = OpenApiHelper.GetDescription(parameterProperty, xmlDocumentationFiles);

                    var restUrParameterAttribute = parameterProperty.GetCustomAttributeEx<RestUrlParameterAttribute>();
                    if (restUrParameterAttribute != null)
                        if (restUrParameterAttribute.Mode == UrlParameterMode.Inline)
                            pathInfo.PositionalParameters.Add(new OpenApiPositionalOperationParameter { Name = parameterProperty.Name, Type = parameterProperty.PropertyType, PositionIndex = restUrParameterAttribute.Sequence, Description = description });
                        else
                        {
                            var isRequired = true;
                            var dataMemberAttribute = parameterProperty.GetCustomAttributeEx<DataMemberAttribute>();
                            if (dataMemberAttribute != null)
                                isRequired = dataMemberAttribute.IsRequired;
                            pathInfo.NamedParameters.Add(new OpenApiNamedOperationParameter { Name = parameterProperty.Name, Type = parameterProperty.PropertyType, Required = isRequired, Description = description });
                        }

                }
            }
        }

        public static OpenApiExternalDocumentation GetExternalDocs(Type implementationType, Type interfaceType)
        {
            var attribute = implementationType.GetCustomAttributeEx<ExternalDocumentationAttribute>();
            if (attribute != null)
                return new OpenApiExternalDocumentation { Description = attribute.Description, Url = attribute.Url };

            var attribute2 = interfaceType.GetCustomAttributeEx<ExternalDocumentationAttribute>();
            if (attribute2 != null)
                return new OpenApiExternalDocumentation { Description = attribute2.Description, Url = attribute2.Url };

            return null;
        }

        public static string GetSummary(Type type, Dictionary<Assembly, OpenApiXmlDocumentationFile> xmlDocumentationFiles)
        {
            var summaryAttribute = type.GetCustomAttributeEx<SummaryAttribute>();
            if (summaryAttribute != null && !string.IsNullOrEmpty(summaryAttribute.Summary))
                return summaryAttribute.Summary.Trim();

            if (xmlDocumentationFiles.ContainsKey(type.Assembly))
            {
                var xmlSummary = xmlDocumentationFiles[type.Assembly].GetSummaryFromXmlDocs(type);
                if (!string.IsNullOrEmpty(xmlSummary))
                    return xmlSummary;
            }

            return string.Empty;
        }

        public static string GetDescription(Type type, Dictionary<Assembly, OpenApiXmlDocumentationFile> xmlDocumentationFiles)
        {
            var descriptionAttribute = type.GetCustomAttributeEx<DescriptionAttribute>();
            if (descriptionAttribute != null && !string.IsNullOrEmpty(descriptionAttribute.Description))
                return descriptionAttribute.Description.Trim();

            var componentModelDescriptionAttribute = type.GetCustomAttributeEx<System.ComponentModel.DescriptionAttribute>();
            if (componentModelDescriptionAttribute != null && !string.IsNullOrEmpty(componentModelDescriptionAttribute.Description))
                return componentModelDescriptionAttribute.Description.Trim();

            if (xmlDocumentationFiles.ContainsKey(type.Assembly))
            {
                var xmlDescription = xmlDocumentationFiles[type.Assembly].GetDescriptionFromXmlDocs(type);
                if (!string.IsNullOrEmpty(xmlDescription))
                    return xmlDescription;
            }

            return string.Empty;
        }

        public static string GetDescription(Type implementationType, Type interfaceType, Dictionary<Assembly, OpenApiXmlDocumentationFile> xmlDocumentationFiles)
        {
            var descriptionAttribute = implementationType.GetCustomAttributeEx<DescriptionAttribute>();
            if (descriptionAttribute != null && !string.IsNullOrEmpty(descriptionAttribute.Description))
                return descriptionAttribute.Description.Trim();

            var descriptionAttribute2 = interfaceType.GetCustomAttributeEx<DescriptionAttribute>();
            if (descriptionAttribute2 != null && !string.IsNullOrEmpty(descriptionAttribute2.Description))
                return descriptionAttribute2.Description.Trim();

            var componentModelDescriptionAttribute = implementationType.GetCustomAttributeEx<System.ComponentModel.DescriptionAttribute>();
            if (componentModelDescriptionAttribute != null && !string.IsNullOrEmpty(componentModelDescriptionAttribute.Description))
                return componentModelDescriptionAttribute.Description.Trim();

            var componentModelDescriptionAttribute2 = interfaceType.GetCustomAttributeEx<System.ComponentModel.DescriptionAttribute>();
            if (componentModelDescriptionAttribute2 != null && !string.IsNullOrEmpty(componentModelDescriptionAttribute2.Description))
                return componentModelDescriptionAttribute2.Description.Trim();

            if (xmlDocumentationFiles.ContainsKey(implementationType.Assembly))
            {
                var xmlDescription = xmlDocumentationFiles[implementationType.Assembly].GetDescriptionFromXmlDocs(implementationType);
                if (!string.IsNullOrEmpty(xmlDescription))
                    return xmlDescription;
            }

            if (xmlDocumentationFiles.ContainsKey(interfaceType.Assembly))
            {
                var xmlDescription = xmlDocumentationFiles[interfaceType.Assembly].GetDescriptionFromXmlDocs(interfaceType);
                if (!string.IsNullOrEmpty(xmlDescription))
                    return xmlDescription;
            }

            return string.Empty;
        }

        public static string GetDescription(PropertyInfo property, Dictionary<Assembly, OpenApiXmlDocumentationFile> xmlDocumentationFiles)
        {
            var descriptionAttribute = property.GetCustomAttributeEx<DescriptionAttribute>();
            if (descriptionAttribute != null && !string.IsNullOrEmpty(descriptionAttribute.Description))
                return descriptionAttribute.Description.Trim();

            var componentModelDescriptionAttribute = property.GetCustomAttributeEx<System.ComponentModel.DescriptionAttribute>();
            if (componentModelDescriptionAttribute != null && !string.IsNullOrEmpty(componentModelDescriptionAttribute.Description))
                return componentModelDescriptionAttribute.Description.Trim();

            if (xmlDocumentationFiles.ContainsKey(property.DeclaringType.Assembly))
            {
                var xmlDescription = xmlDocumentationFiles[property.DeclaringType.Assembly].GetDescriptionFromXmlDocs(property);
                if (!string.IsNullOrEmpty(xmlDescription))
                    return xmlDescription;
            }

            return string.Empty;
        }

        public static string GetDescription(MethodInfo interfaceMethod, Type methodInterface, Dictionary<Assembly, OpenApiXmlDocumentationFile> xmlDocumentationFiles)
        {
            var descriptionAttribute = interfaceMethod.GetCustomAttributeEx<DescriptionAttribute>();
            if (descriptionAttribute != null && !string.IsNullOrEmpty(descriptionAttribute.Description))
                return descriptionAttribute.Description.Trim();

            var componentModelDescriptionAttribute = interfaceMethod.GetCustomAttributeEx<System.ComponentModel.DescriptionAttribute>();
            if (componentModelDescriptionAttribute != null && !string.IsNullOrEmpty(componentModelDescriptionAttribute.Description))
                return componentModelDescriptionAttribute.Description.Trim();

            if (xmlDocumentationFiles.ContainsKey(methodInterface.Assembly))
            {
                var xmlDescription = xmlDocumentationFiles[methodInterface.Assembly].GetDescriptionFromXmlDocs(interfaceMethod);
                if (!string.IsNullOrEmpty(xmlDescription))
                    return xmlDescription;
            }

            return string.Empty;
        }

        public static string GetSummary(MethodInfo interfaceMethod, Type methodInterface, Dictionary<Assembly, OpenApiXmlDocumentationFile> xmlDocumentationFiles)
        {
            var ummaryAttribute = interfaceMethod.GetCustomAttributeEx<SummaryAttribute>();
            if (ummaryAttribute != null && !string.IsNullOrEmpty(ummaryAttribute.Summary))
                return ummaryAttribute.Summary.Trim();

            if (xmlDocumentationFiles.ContainsKey(methodInterface.Assembly))
            {
                var xmlSummary = xmlDocumentationFiles[methodInterface.Assembly].GetSummaryFromXmlDocs(interfaceMethod);
                if (!string.IsNullOrEmpty(xmlSummary))
                    return xmlSummary;
            }

            return string.Empty;
        }

        public static string GetOpenApiType(Type type)
        {
            if (type == typeof(string) || type == typeof(char)) return "string";
            if (type == typeof(Guid)) return "string";
            if (type == typeof(byte[])) return "string";
            if (type == typeof(byte)) return "string";
            if (type == typeof(int) || type == typeof(short) || type == typeof(long)) return "integer";
            if (type == typeof(decimal) || type == typeof(double)) return "number";
            if (type == typeof(DateTime)) return "string";
            if (type == typeof(bool)) return "boolean";
            if (type.IsEnum) return "integer";
            if (type.Name == "List`1") return "array";
            if (type.IsArray) return "array";
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var nulledType = type.GetGenericArguments()[0];
                return GetOpenApiType(nulledType);
            }
            return string.Empty;
        }

        public static string GetOpenApiTypeFormat(Type type, PropertyInfo property = null)
        {
            if (type == typeof(Guid)) return "uuid";
            if (type == typeof(byte[]))
            {
                if (property == null) return "byte";
                if (AttributeHelper.GetCustomAttributeEx<FileContentAttribute>(property) == null) return "byte";
                return "file";
            }
            if (type == typeof(byte)) return "byte";
            if (type == typeof(int) || type == typeof(short) || type == typeof(long)) return "int64";
            if (type == typeof(decimal) || type == typeof(double)) return "double";
            if (type == typeof(DateTime)) return "date-time";
            if (type.IsEnum) return "int64";
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var nulledType = type.GetGenericArguments()[0];
                return GetOpenApiTypeFormat(nulledType, property);
            }
            return string.Empty;
        }

        public static object[] GetOpenApiEnumValues(Type type)
        {
            if (!type.IsEnum) return new object[0];

            var enumValues = Enum.GetValues(type);
            var enumType = type.GetEnumUnderlyingType();
            var returnList = new object[enumValues.Length];
            for (var counter = 0; counter < returnList.Length; counter++)
                returnList[counter] = Convert.ChangeType(enumValues.GetValue(counter), enumType);
            return returnList;
        }

        public static string GetOpenApiEnumDescription(Type type)
        {
            if (!type.IsEnum) return string.Empty;

            var returnList = string.Empty;
            var enumValues = GetOpenApiEnumValues(type);
            var enumNames = type.GetEnumNames();
            var counter = -1;
            foreach (var enumValue in enumValues)
            {
                counter++;
                if (!string.IsNullOrEmpty(returnList))
                    returnList += ", ";
                returnList += $"{enumValue} = {enumNames[counter]}";
            }
            return returnList;
        }

        public static void WriteEnumDeclaration(Utf8JsonWriter writer, Type propertyType)
        {
            writer.WriteStartArray("enum");
            var enumValues = GetOpenApiEnumValues(propertyType);
            foreach (var enumValue in enumValues)
                if (enumValue is int)
                    writer.WriteNumberValue((int)enumValue);
                else if (enumValue is short)
                    writer.WriteNumberValue((short)enumValue);
                else if (enumValue is long)
                    writer.WriteNumberValue((long)enumValue);
                else if (enumValue is uint)
                    writer.WriteNumberValue((uint)enumValue);
                else if (enumValue is ushort)
                    writer.WriteNumberValue((ushort)enumValue);
                else if (enumValue is ulong)
                    writer.WriteNumberValue((ulong)enumValue);
                else if (enumValue is byte)
                    writer.WriteNumberValue((byte)enumValue);
                else if (enumValue is sbyte)
                    writer.WriteNumberValue((sbyte)enumValue);
            writer.WriteEndArray();
        }
    }

    public interface IOpenApiOperationParameter
    {
        string Name { get; set; }
        string Description { get; set; }
        bool Required { get; set; }
        Type Type { get; set; }
    }

    public class OpenApiPayload
    {
        public Type Type { get; set; }
        public string Description { get; set; }
    }

    public class OpenApiNamedOperationParameter : IOpenApiOperationParameter
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Type Type { get; set; }
        public bool Required { get; set; } = true;
    }

    public class OpenApiPositionalOperationParameter : OpenApiNamedOperationParameter
    {
        public int PositionIndex { get; set; }
    }

    public class OpenApiXmlDocumentationFile
    {
        public OpenApiXmlDocumentationFile(Assembly assembly)
        {
            var xmlFileLocation = assembly.Location;
            if (xmlFileLocation.ToLowerInvariant().EndsWith(".dll"))
                xmlFileLocation = xmlFileLocation.Substring(0, xmlFileLocation.Length - 4) + ".xml";

            try
            {
                if (File.Exists(xmlFileLocation))
                {
                    FileExists = true;
                    XmlString = StringHelper.FromFile(xmlFileLocation);
                }
            }
            catch
            {
                // Bummer... but too bad
            }
        }

        public bool FileExists { get; set; }
        public string XmlString { get; set; }

        public string GetSummaryFromXmlDocs(MethodInfo method)
        {
            if (!FileExists) return string.Empty;

            var xml = GetXmlDocument();
            if (xml == null) return string.Empty;

            var node = GetMemberNode(xml, method);
            if (node == null) return string.Empty;
            var summaryNode = node.SelectSingleNode("summary");
            if (summaryNode == null) return string.Empty;
            return summaryNode.InnerText.Trim();
        }

        public string GetSummaryFromXmlDocs(Type type)
        {
            if (!FileExists) return string.Empty;

            var xml = GetXmlDocument();
            if (xml == null) return string.Empty;

            var node = GetMemberNode(xml, type);
            if (node == null) return string.Empty;
            var summaryNode = node.SelectSingleNode("summary");
            if (summaryNode != null && summaryNode.InnerText != null && !string.IsNullOrEmpty(summaryNode.InnerText)) return summaryNode.InnerText.Trim();

            return string.Empty;
        }

        public string GetDescriptionFromXmlDocs(PropertyInfo property)
        {
            if (!FileExists) return string.Empty;

            var xml = GetXmlDocument();
            if (xml == null) return string.Empty;

            var node = GetMemberNode(xml, property);
            if (node == null) return string.Empty;
            var descriptionNode = node.SelectSingleNode("description");
            if (descriptionNode != null && descriptionNode.InnerText != null && !string.IsNullOrEmpty(descriptionNode.InnerText)) return descriptionNode.InnerText.Trim();
            var remarksNode = node.SelectSingleNode("remarks");
            if (remarksNode != null && remarksNode.InnerText != null && !string.IsNullOrEmpty(remarksNode.InnerText)) return remarksNode.InnerText.Trim();

            return string.Empty;
        }

        public string GetDescriptionFromXmlDocs(MethodInfo method)
        {
            if (!FileExists) return string.Empty;

            var xml = GetXmlDocument();
            if (xml == null) return string.Empty;

            var node = GetMemberNode(xml, method);
            if (node == null) return string.Empty;
            var descriptionNode = node.SelectSingleNode("description");
            if (descriptionNode != null && descriptionNode.InnerText != null && !string.IsNullOrEmpty(descriptionNode.InnerText)) return descriptionNode.InnerText.Trim();
            var remarksNode = node.SelectSingleNode("remarks");
            if (remarksNode != null && remarksNode.InnerText != null && !string.IsNullOrEmpty(remarksNode.InnerText)) return remarksNode.InnerText.Trim();

            return string.Empty;
        }

        public string GetDescriptionFromXmlDocs(Type type)
        {
            if (!FileExists) return string.Empty;

            var xml = GetXmlDocument();
            if (xml == null) return string.Empty;

            var node = GetMemberNode(xml, type);
            if (node == null) return string.Empty;
            var descriptionNode = node.SelectSingleNode("description");
            if (descriptionNode != null && descriptionNode.InnerText != null && !string.IsNullOrEmpty(descriptionNode.InnerText)) return descriptionNode.InnerText.Trim();
            var remarksNode = node.SelectSingleNode("remarks");
            if (remarksNode != null && remarksNode.InnerText != null && !string.IsNullOrEmpty(remarksNode.InnerText)) return remarksNode.InnerText.Trim();

            return string.Empty;
        }

        private static XmlNode GetMemberNode(XmlDocument xml, PropertyInfo property)
        {
            var xmlPath = $"{property.DeclaringType.FullName}.{property.Name}";
            var node = xml.SelectSingleNode($"/doc/members/member[@name='P:{xmlPath}']");
            return node;
        }

        private static XmlNode GetMemberNode(XmlDocument xml, MethodInfo method)
        {
            var xmlPath = $"{method.DeclaringType.FullName}.{method.Name}(";

            var firstParameter = true;
            foreach (var parameter in method.GetParameters())
            {
                if (!firstParameter) xmlPath += ",";
                xmlPath += parameter.ParameterType.FullName;
                firstParameter = false;
            }

            xmlPath += ")";

            var node = xml.SelectSingleNode($"/doc/members/member[@name='M:{xmlPath}']");
            return node;
        }

        private static XmlNode GetMemberNode(XmlDocument xml, Type type)
        {
            var xmlPath = $"{type.FullName}";
            var node = xml.SelectSingleNode($"/doc/members/member[@name='T:{xmlPath}']");
            return node;
        }

        private XmlDocument _xml;
        private XmlDocument GetXmlDocument()
        {
            if (string.IsNullOrEmpty(XmlString)) return null;
            try
            {
                if (_xml == null)
                {
                    _xml = new XmlDocument();
                    _xml.LoadXml(XmlString);
                }
            }
            catch
            {
                // Nothing we can do
            }
            return _xml;
        }
    }
}