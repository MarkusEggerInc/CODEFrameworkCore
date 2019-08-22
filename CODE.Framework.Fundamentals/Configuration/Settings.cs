using System;
using System.Runtime.Serialization;

namespace CODE.Framework.Fundamentals.Configuration
{
    /// <summary>
    /// Exposes settings inside the ConfigurationSettings class (which is the main class that uses 
    /// the Settings class). The Settings class doesn't actually store settings. Instead, it just
    /// exposes an interface for getting to settings in sources that were added to the 
    /// ConfigurationSettings class.
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Indexer that allows a setting to be accessed by its name. 
        /// </summary>
        public string this[string setting]
        {
            set
            {
                // Indicates if setting is supported.
                var supported = false;

                // Look for the setting through all sources.
                foreach (var source in ConfigurationSettings.Sources)
                    if (source.IsActive)
                        // Check if setting is supported.
                        if (source.IsSettingSupported(setting))
                        {
                            // Check if source is readonly.
                            if (source.IsReadOnly)
                                throw new SettingReadOnlyException();

                            // If setting is supported and not readonly, write new value to it.
                            source.Settings[setting] = value;

                            // We've found what we want, so just stop iterating through sources.
                            // By the specs, we go for the order sources were added.
                            supported = true;
                            break;
                        }

                if (!supported)
                    throw new SettingNotSupportedException("Setting '@setting' is not supported.");
            }

            get
            {
                // Indicates if setting is supported.
                var supported = false;

                // Keeps return value.
                string ret = null;

                // Look for the setting through all sources.
                foreach (var source in ConfigurationSettings.Sources)
                    if (source.IsActive)
                        if (source.IsSettingSupported(setting))
                        {
                            ret = source.Settings[setting].ToString();

                            // We've found what we want, so just stop iterating through sources.
                            // By the specs, we go for the order sources were added.
                            supported = true;
                            break;
                        }

                if (!supported)
                    throw new SettingNotSupportedException("Setting '@setting' is not supported.");

                return ret;
            }
        }

        /// <summary>
        /// Checks whether a given setting is supported by any source inside the ConfigurationSettings object.
        /// </summary>
        /// <param name="setting">Name of the setting.</param>
        /// <returns>True/False, indicating whether the setting is supported or not.</returns>
        public bool IsSettingSupported(string setting)
        {
            // Indicates if setting is supported.
            var supported = false;

            // Look for the setting through all sources.
            foreach (var source in ConfigurationSettings.Sources)
                if (source.IsActive)
                    // Check if setting is supported.
                    if (source.IsSettingSupported(setting))
                    {
                        // We've found the setting at least in one source, so it is supported.
                        supported = true;
                        break;
                    }

            return supported;
        }
    }

    /// <summary>
    /// Enum with possible Configuration Source Types.
    /// </summary>
    public enum ConfigurationSourceTypes
    {
        /// <summary>
        /// User.
        /// </summary>
        User,

        /// <summary>
        /// Machine.
        /// </summary>
        Machine,

        /// <summary>
        /// System.
        /// </summary>
        System,

        /// <summary>
        /// Network.
        /// </summary>
        Network,

        /// <summary>
        /// Security.
        /// </summary>
        Security,

        /// <summary>
        /// Other.
        /// </summary>
        Other
    }

    /// <summary>
    /// Enum with possible Security Types.
    /// </summary>
    public enum SecurityType
    {
        /// <summary>
        /// Secure.
        /// </summary>
        Secure,

        /// <summary>
        /// Non-Secure.
        /// </summary>
        NonSecure
    }

    // Class Name: SettingNotSupportedException
    // Package Name: EPS.Configuration
    // Class Language: C#
    // Class Filename: SettingNotSupportedException.cs

    /// <summary>
    /// Exception thrown when some code is trying to write to a read-only setting.
    /// </summary>
    [Serializable]
    public class SettingNotSupportedException : Exception
    {
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public SettingNotSupportedException() { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Exception message</param>
        public SettingNotSupportedException(string message) : base(message) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public SettingNotSupportedException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        protected SettingNotSupportedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Exception thrown when some code is trying to write to a read-only setting.
    /// </summary>
    [Serializable]
    public class SettingReadOnlyException : Exception
    {
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public SettingReadOnlyException() { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Exception message</param>
        public SettingReadOnlyException(string message) : base(message) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public SettingReadOnlyException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        protected SettingReadOnlyException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}