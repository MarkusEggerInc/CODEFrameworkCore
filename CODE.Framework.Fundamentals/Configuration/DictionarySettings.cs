using System.Collections.Generic;

namespace CODE.Framework.Fundamentals.Configuration
{
    public class DictionarySettings : ConfigurationSource
    {
        public DictionarySettings(Dictionary<string, string> sourceSettings)
        {
            var settings = new ConfigurationSourceSettings(this);

            foreach (var key in sourceSettings.Keys) 
                settings.Add(key, sourceSettings[key]);

            Settings = settings;
        }

        /// <summary>
        /// Source's Friendly Name.
        /// </summary>
        public override string FriendlyName => "Dictionary";

        /// <summary>
        /// Determines whether source is secure.
        /// </summary>
        public override bool IsSecure => false;

        /// <summary>
        /// Indicates if this source is read-only.
        /// </summary>
        public override bool IsReadOnly => true;

        /// <summary>
        /// Exposes the Settings member. We're shadowing that member here mostly because
        /// the Memory object is very specialized, designed to override temporarily whatever
        /// other sources might have the same setting. In order to give that special behavior,
        /// a new collection class has been created for it. 
        /// Notice that we still type the member as a ConfigurationSourceSettings class, 
        /// and the only difference is that we instantiate the ConfigurationSourceSettings class instead.
        /// </summary>
        public override ConfigurationSourceSettings Settings { get; }

        /// <summary>
        /// Read settings from file.
        /// </summary>
        public override void Read() { }

        /// <summary>
        /// Write settings to file.
        /// </summary>
        public override void Write() { }

        /// <summary>
        /// Checks whether a given type is supported.
        /// </summary>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        public override bool SupportsType(ConfigurationSourceTypes sourceType) => false;

        public override bool IsSettingSupported(string settingName) => Settings.ContainsKey(settingName);
    }
}
