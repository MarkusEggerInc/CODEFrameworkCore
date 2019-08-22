using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace CODE.Framework.Fundamentals.Utilities
{
    public static class ApplicationSettingsJsonHelper
    {
        public static Dictionary<string, string> GetDictionaryFromSection(IConfiguration configuration, string section = "ApplicationSettings")
        {
            var results = new Dictionary<string, string>();

            var applicationSettings = configuration.GetSection(section);
            var firstLevelChildren = applicationSettings.GetChildren();

            foreach (var child in firstLevelChildren)
            {
                var key = child.Key;
                var value = child.Value;

                if (value == null)
                {
                    var subSection = configuration.GetSection(section + ":" + key);
                    if (subSection != null)
                    {
                        var secondLevelChildren = subSection.GetChildren();
                        foreach (var subChild in secondLevelChildren)
                        {
                            var key2 = key + ":" + subChild.Key;
                            var value2 = subChild.Value;
                            if (value2 != null)
                            {
                                if (!results.ContainsKey(key2))
                                    results.Add(key2, value2);
                                else
                                    results[key2] = value2;
                            }
                        }
                    }
                }
                else
                {
                    if (!results.ContainsKey(key))
                        results.Add(key, value);
                    else
                        results[key] = value;
                }
            }

            return results;
        }
    }
}
