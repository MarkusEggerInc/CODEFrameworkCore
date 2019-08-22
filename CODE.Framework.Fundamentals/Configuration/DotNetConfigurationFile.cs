using System.Configuration;

namespace CODE.Framework.Fundamentals.Configuration
{
    /// <summary>
    /// This class wraps up the functionality available natively in .NET for reading 
    /// the default settings (AppSettings) available in the config files.
    /// </summary>
    public class DotNetConfigurationFile : ConfigurationSource
    {
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public DotNetConfigurationFile() => Read();

        /// <summary>
        /// Indicates source's Friendly Name.
        /// </summary>
        public override string FriendlyName => "DotNetConfigurationFile";

        /// <summary>
        /// Indicates whether the source is read-only. .NET's native AppSettings is read-only,
        /// therefore we mark this class as read-only too.
        /// </summary>
        public override bool IsReadOnly => true;

        /// <summary>
        /// Determines whether the source is secure or not.
        /// </summary>
        public override bool IsSecure => false;

        /// <summary>
        /// Read settings from native .NET object and feed settings into our own object.
        /// </summary>
        public override void Read()
        {
            // TODO: Implement this after adding the NuGet package
            // We read the native AppSettings and feed our own class.
            //foreach (var setting in ConfigurationManager.AppSettings.AllKeys)
            //    Settings.Add(setting, ConfigurationManager.AppSettings[setting]);
        }

        /// <summary>
        /// Checks whether a given source type is supported, according to Enum ConfigurationSourceTypes.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <returns>True/False for supported or not.</returns>
        public override bool SupportsType(ConfigurationSourceTypes sourceType) => false;

        /// <summary>
        /// Persists settings from memory into storage.
        /// </summary>
        public override void Write() { }
    }
}