using System;
using System.Reflection;

namespace CODE.Framework.Services.Server.AspNetCore
{
    public static class AttributeHelper
    {
        public static T GetCustomAttributeEx<T>(this Type type) where T : Attribute
        {
            var attribute = type.GetCustomAttribute<T>(true);
            if (attribute != null) return attribute;

            var interfaces = type.GetInterfaces();
            foreach (var inter in interfaces)
            {
                attribute = inter.GetCustomAttribute<T>(true);
                if (attribute != null) return attribute;
            }

            return null;
        }

        public static T GetCustomAttributeEx<T>(this MethodInfo method) where T : Attribute
        {
            var attribute = method.GetCustomAttribute<T>(true);
            if (attribute != null) return attribute;

            var interfaces = method.DeclaringType.GetInterfaces();
            foreach (var inter in interfaces)
            {
                var method2 = inter.GetMethod(method.Name);
                if (method2 != null)
                {
                    attribute = method2.GetCustomAttribute<T>(true);
                    if (attribute != null) return attribute;
                }
            }

            return null;
        }

        public static T GetCustomAttributeEx<T>(this PropertyInfo property) where T : Attribute
        {
            var attribute = property.GetCustomAttribute<T>(true);
            if (attribute != null) return attribute;

            var interfaces = property.DeclaringType.GetInterfaces();
            foreach (var inter in interfaces)
            {
                var property2 = inter.GetProperty(property.Name);
                if (property2 != null)
                {
                    attribute = property2.GetCustomAttribute<T>(true);
                    if (attribute != null) return attribute;
                }
            }

            return null;
        }
    }
}
