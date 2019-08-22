using System;
using System.Collections.Generic;
using System.Linq;

namespace CODE.Framework.Fundamentals.Configuration
{
    /// <summary>
    /// The ConfigurationSettings class is the main point of access to an application settings
    /// </summary>
    public static class ConfigurationSettings
    {
        private static Settings _internalSettings;

        private static readonly ConfigurationSettingsSourcesCollection InternalConfigurationSources = new ConfigurationSettingsSourcesCollection();

        /// <summary>
        /// Exposes access to the Settings.
        /// </summary>
        public static Settings Settings
        {
            get => _internalSettings ?? (_internalSettings = new Settings());
            set => _internalSettings = value;
        }

        /// <summary>
        /// Exposes access to the ConfigurationSettingsSourcesCollection.
        /// </summary>
        public static ConfigurationSettingsSourcesCollection Sources
        {
            get
            {
                if (InternalConfigurationSources.Count == 0)
                {
                    // The MemorySettings object is designed to be the first one in the sequence of sources to allow overriding other settings
                    InternalConfigurationSources.Add(new MemorySettings());
                    InternalConfigurationSources.Add(new SecureConfigurationFile());
                    InternalConfigurationSources.Add(new DotNetConfigurationFile());
                }

                return InternalConfigurationSources;
            }
        }

        public static IConfigurationSource GetConfigurationSourceByFriendlyName(string friendlyName)
        {
            foreach (var source in Sources)
                if (string.Compare(friendlyName, source.FriendlyName, StringComparison.OrdinalIgnoreCase) == 0)
                    return source;
            return null;
        }
    }

    public class ConfigurationSettingsSourcesCollection : List<IConfigurationSource>
    {
        /// <summary>
        /// Indexer that allows a source to be accessed by its name. 
        /// </summary>
        public IConfigurationSource this[string sourceName] => this.FirstOrDefault(s => string.Compare(s.FriendlyName, sourceName, StringComparison.OrdinalIgnoreCase) == 0);
    }
}