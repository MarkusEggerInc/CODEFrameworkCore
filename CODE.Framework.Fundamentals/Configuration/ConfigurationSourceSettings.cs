using System.Collections;

namespace CODE.Framework.Fundamentals.Configuration
{
    /// <summary>
    /// Hashtable that keeps a Name-Value list of settings. This class is mainly used by the ConfigurationSource class.
    /// </summary>
    public class ConfigurationSourceSettings : Hashtable
    {
        /// <summary>
        /// Keeps a reference to the config source that hosts the settings collection.
        /// </summary>
        private readonly IConfigurationSource parentConfigSource;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent">The parent configuration source that hosts this settings collection.</param>
        public ConfigurationSourceSettings(IConfigurationSource parent) => parentConfigSource = parent;

        /// <summary>
        /// Indexer.
        /// </summary>
        /// <remarks>
        /// The main reason this indexer is being overridden is so that we can
        /// flag as "dirty" the config source that hosts the settings.
        /// </remarks>
        public override object this[object key]
        {
            get => base[key];
            set
            {
                base[key] = value;
                parentConfigSource?.MarkDirty();
            }
        }
    }
}