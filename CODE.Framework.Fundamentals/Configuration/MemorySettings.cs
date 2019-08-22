namespace CODE.Framework.Fundamentals.Configuration
{
    internal class MemorySettings : ConfigurationSource
    {
        /// <summary>
        /// Keeps an internal instance of the ConfigurationSourceSettings class.
        /// </summary>
        private ConfigurationSourceSettings settings;

        /// <summary>
        /// Source's Friendly Name.
        /// </summary>
        public override string FriendlyName => "Memory";

        /// <summary>
        /// Determines whether source is secure.
        /// </summary>
        public override bool IsSecure => true;

        /// <summary>
        /// Indicates if this source is read-only.
        /// </summary>
        public override bool IsReadOnly => false;

        /// <summary>
        /// Exposes the Settings member. We're shadowing that member here mostly because
        /// the Memory object is very specialized, designed to override temporarily whatever
        /// other sources might have the same setting. In order to give that special behavior,
        /// a new collection class has been created for it. 
        /// Notice that we still type the member as a ConfigurationSourceSettings class, 
        /// and the only difference is that we instantiate the ConfigurationSourceSettings class instead.
        /// </summary>
        public override ConfigurationSourceSettings Settings => settings ?? (settings = new ConfigurationSourceMemorySettings(this));

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
    }

    /// <summary>
    /// Hashtable that keeps a Name-Value list of settings. This class is mainly used by the MemorySettings class.
    /// </summary>
    public class ConfigurationSourceMemorySettings : ConfigurationSourceSettings
    {
        /// <summary>Constructor</summary>
        /// <param name="parent"></param>
        public ConfigurationSourceMemorySettings(IConfigurationSource parent) : base(parent) { }

        /// <summary>Indexer</summary>
        public override object this[object key]
        {
            get => base[key];
            set
            {
                // If a null value is being assigned to the setting,
                // we remove it (the setting) from the Memory source.
                if (value == null)
                    Remove(key);
                else
                    base[key] = value;
            }
        }
    }
}