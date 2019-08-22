using System;
using System.Collections;
using System.Collections.Generic;

namespace CODE.Framework.Fundamentals.Utilities.Csv
{
    /// <summary>
    /// Supports a simple iteration over the records of a <see cref="T:CsvReader"/>.
    /// </summary>
    public struct RecordEnumerator : IEnumerator<string[]>
    {
        /// <summary>
        /// Contains the enumerated <see cref="T:CsvReader"/>.
        /// </summary>
        private CsvReader _reader;

        /// <summary>
        /// Contains the current record index.
        /// </summary>
        private long _currentRecordIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:RecordEnumerator"/> class.
        /// </summary>
        /// <param name="reader">The <see cref="T:CsvReader"/> to iterate over.</param>
        /// <exception cref="T:ArgumentNullException">
        ///		<paramref name="reader"/> is a <see langword="null"/>.
        /// </exception>
        public RecordEnumerator(CsvReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException("reader");
            Current = null;

            _currentRecordIndex = reader.CurrentRecordIndex;
        }

        /// <summary>
        /// Gets the current record.
        /// </summary>
        public string[] Current { get; private set; }

        /// <summary>
        /// Advances the enumerator to the next record of the CSV.
        /// </summary>
        /// <returns><see langword="true"/> if the enumerator was successfully advanced to the next record, <see langword="false"/> if the enumerator has passed the end of the CSV.</returns>
        public bool MoveNext()
        {
            if (_reader.CurrentRecordIndex != _currentRecordIndex)
                throw new InvalidOperationException("Enumeration version check failed");

            if (_reader.ReadNextRecord())
            {
                Current = new string[_reader.FieldCount];

                _reader.CopyCurrentRecordTo(Current);
                _currentRecordIndex = _reader.CurrentRecordIndex;

                return true;
            }

            Current = null;
            _currentRecordIndex = _reader.CurrentRecordIndex;

            return false;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first record in the CSV.
        /// </summary>
        public void Reset()
        {
            if (_reader.CurrentRecordIndex != _currentRecordIndex)
                throw new InvalidOperationException("Enumeration version check failed");

            _reader.MoveTo(-1);

            Current = null;
            _currentRecordIndex = _reader.CurrentRecordIndex;
        }

        /// <summary>
        /// Gets the current record.
        /// </summary>
        object IEnumerator.Current
        {
            get
            {
                if (_reader.CurrentRecordIndex != _currentRecordIndex)
                    throw new InvalidOperationException("Enumeration version check failed");

                return Current;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _reader = null;
            Current = null;
        }
    }
}