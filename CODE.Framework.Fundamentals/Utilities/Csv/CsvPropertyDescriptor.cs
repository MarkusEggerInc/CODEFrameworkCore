using System;
using System.ComponentModel;

namespace CODE.Framework.Fundamentals.Utilities.Csv
{
    /// <summary>
    /// Represents a CSV field property descriptor.
    /// </summary>
    public class CsvPropertyDescriptor : PropertyDescriptor
    {
        /// <summary>
        /// Contains the field index.
        /// </summary>
        private readonly int _index;

        /// <summary>
        /// Initializes a new instance of the CsvPropertyDescriptor class.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="index">The field index.</param>
        public CsvPropertyDescriptor(string fieldName, int index) : base(fieldName, null) => _index = index;

        /// <summary>
        /// Gets the field index.
        /// </summary>
        /// <value>The field index.</value>
        public int Index => _index;

        public override bool CanResetValue(object component) => false;

        public override object GetValue(object component) => ((string[])component)[_index];

        public override void ResetValue(object component)
        {
        }

        public override void SetValue(object component, object value)
        {
        }

        public override bool ShouldSerializeValue(object component) => false;

        public override Type ComponentType => typeof (CachedCsvReader);

        public override bool IsReadOnly => true;

        public override Type PropertyType => typeof (string);
    }
}