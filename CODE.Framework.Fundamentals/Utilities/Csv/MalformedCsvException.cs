using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace CODE.Framework.Fundamentals.Utilities.Csv
{
    /// <summary>
    /// Represents the exception that is thrown when a CSV file is malformed.
    /// </summary>
    [Serializable()]
    public class MalformedCsvException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the MalformedCsvException class.
        /// </summary>
        public MalformedCsvException() : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MalformedCsvException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public MalformedCsvException(string message)
            : this(message, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MalformedCsvException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public MalformedCsvException(string message, Exception innerException) : base(string.Empty, innerException)
        {
            Message = message ?? string.Empty;

            RawData = string.Empty;
            CurrentPosition = -1;
            CurrentRecordIndex = -1;
            CurrentFieldIndex = -1;
        }

        /// <summary>
        /// Initializes a new instance of the MalformedCsvException class.
        /// </summary>
        /// <param name="rawData">The raw data when the error occured.</param>
        /// <param name="currentPosition">The current position in the raw data.</param>
        /// <param name="currentRecordIndex">The current record index.</param>
        /// <param name="currentFieldIndex">The current field index.</param>
        public MalformedCsvException(string rawData, int currentPosition, long currentRecordIndex, int currentFieldIndex)
            : this(rawData, currentPosition, currentRecordIndex, currentFieldIndex, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MalformedCsvException class.
        /// </summary>
        /// <param name="rawData">The raw data when the error occured.</param>
        /// <param name="currentPosition">The current position in the raw data.</param>
        /// <param name="currentRecordIndex">The current record index.</param>
        /// <param name="currentFieldIndex">The current field index.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public MalformedCsvException(string rawData, int currentPosition, long currentRecordIndex, int currentFieldIndex, Exception innerException) : base(string.Empty, innerException)
        {
            RawData = rawData ?? string.Empty;
            CurrentPosition = currentPosition;
            CurrentRecordIndex = currentRecordIndex;
            CurrentFieldIndex = currentFieldIndex;

            Message = string.Format(CultureInfo.InvariantCulture, "Malformed CSV Exception", CurrentRecordIndex, CurrentFieldIndex, CurrentPosition, RawData);
        }

        /// <summary>
        /// Initializes a new instance of the MalformedCsvException class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="T:SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected MalformedCsvException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Message = info.GetString("MyMessage");

            RawData = info.GetString("RawData");
            CurrentPosition = info.GetInt32("CurrentPosition");
            CurrentRecordIndex = info.GetInt64("CurrentRecordIndex");
            CurrentFieldIndex = info.GetInt32("CurrentFieldIndex");
        }

        /// <summary>
        /// Gets the raw data when the error occured.
        /// </summary>
        /// <value>The raw data when the error occured.</value>
        public string RawData { get; }

        /// <summary>
        /// Gets the current position in the raw data.
        /// </summary>
        /// <value>The current position in the raw data.</value>
        public int CurrentPosition { get; }

        /// <summary>
        /// Gets the current record index.
        /// </summary>
        /// <value>The current record index.</value>
        public long CurrentRecordIndex { get; }

        /// <summary>
        /// Gets the current field index.
        /// </summary>
        /// <value>The current record index.</value>
        public int CurrentFieldIndex { get; }

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        /// <value>A message that describes the current exception.</value>
        public override string Message { get; }

        /// <summary>
        /// When overridden in a derived class, sets the <see cref="T:SerializationInfo"/> with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="T:SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:StreamingContext"/> that contains contextual information about the source or destination.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("MyMessage", Message);

            info.AddValue("RawData", RawData);
            info.AddValue("CurrentPosition", CurrentPosition);
            info.AddValue("CurrentRecordIndex", CurrentRecordIndex);
            info.AddValue("CurrentFieldIndex", CurrentFieldIndex);
        }
    }
}