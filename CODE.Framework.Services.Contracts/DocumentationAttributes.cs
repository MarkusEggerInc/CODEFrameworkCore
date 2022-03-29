using System;

namespace CODE.Framework.Services.Contracts
{
    /// <summary>
    /// Generic description attribute usable in all service elements
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class DescriptionAttribute : Attribute
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="description">Description text</param>
        public DescriptionAttribute(string description)
        {
            Description = description;
        }

        /// <summary>
        /// Description text (depending on usage, this may or may not support markdown)
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Generic summary attribute usable in all service elements
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class SummaryAttribute : Attribute
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="summary">Summary text</param>
        public SummaryAttribute(string summary)
        {
            Summary = summary;
        }

        /// <summary>
        /// Summary text (depending on usage, this may or may not support markdown)
        /// </summary>
        public string Summary { get; set; } = string.Empty;
    }

    /// <summary>
    /// Generic external documentation attribute usable in all service elements
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class ExternalDocumentationAttribute : Attribute
    {
        public ExternalDocumentationAttribute(string description, string url)
        {
            Description = description;
            Url = url;
        }

        /// <summary>
        /// Description text (depending on usage, this may or may not support markdown)
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Full URL for the external description
        /// </summary>
        public string Url { get; set; } = string.Empty;
    }
}