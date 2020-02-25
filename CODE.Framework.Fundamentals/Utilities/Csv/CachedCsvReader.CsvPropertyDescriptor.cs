using System;
using System.ComponentModel;

namespace CODE.Framework.Fundamentals.Utilities.Csv
{
    public partial class CachedCsvReader
    {
        /// <summary>
        /// Represents a CSV field property descriptor.
        /// </summary>
        private class CsvPropertyDescriptor : PropertyDescriptor
        {
            /// <summary>
            /// Initializes a new instance of the CsvPropertyDescriptor class.
            /// </summary>
            /// <param name="fieldName">The field name.</param>
            /// <param name="index">The field index.</param>
            public CsvPropertyDescriptor(string fieldName, int index) : base(fieldName, null) => Index = index;

            /// <summary>
            /// Gets the field index.
            /// </summary>
            /// <value>The field index.</value>
            public int Index { get; }

            public override bool CanResetValue(object component) => false;

            public override object GetValue(object component) => ((string[]) component)[Index];

            public override void ResetValue(object component) { }

            public override void SetValue(object component, object value) { }

            public override bool ShouldSerializeValue(object component) => false;

            public override Type ComponentType => typeof(CachedCsvReader);

            public override bool IsReadOnly => true;

            public override Type PropertyType => typeof(string);
        }
    }
}