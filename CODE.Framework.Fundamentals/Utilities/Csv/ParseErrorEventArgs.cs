using System;

namespace CODE.Framework.Fundamentals.Utilities.Csv
{
    /// <summary>
    /// Provides data for the <see cref="M:CsvReader.ParseError"/> event.
    /// </summary>
    public class ParseErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the ParseErrorEventArgs class.
        /// </summary>
        /// <param name="error">The error that occured.</param>
        /// <param name="defaultAction">The default action to take.</param>
        public ParseErrorEventArgs(MalformedCsvException error, ParseErrorAction defaultAction)
        {
            Error = error;
            Action = defaultAction;
        }

        /// <summary>
        /// Gets the error that occured.
        /// </summary>
        /// <value>The error that occured.</value>
        public MalformedCsvException Error { get; }

        /// <summary>
        /// Gets or sets the action to take.
        /// </summary>
        /// <value>The action to take.</value>
        public ParseErrorAction Action { get; set; }
    }
}