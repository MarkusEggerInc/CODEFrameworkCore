using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;

namespace CODE.Framework.Fundamentals.Utilities.Csv
{
    public partial class CachedCsvReader : CsvReader
    {
        /// <summary>
        /// Represents a binding list wrapper for a CSV reader.
        /// </summary>
        private class CsvBindingList : IBindingList, ITypedList, IList<string[]>
        {
            /// <summary>
            /// Contains the linked CSV reader.
            /// </summary>
            private readonly CachedCsvReader _csv;

            /// <summary>
            /// Contains the cached record count.
            /// </summary>
            private int _count;

            /// <summary>
            /// Contains the cached property descriptors.
            /// </summary>
            private PropertyDescriptorCollection _properties;

            /// <summary>
            /// Contains the current sort property.
            /// </summary>
            private CsvPropertyDescriptor _sort;

            /// <summary>
            /// Initializes a new instance of the CsvBindingList class.
            /// </summary>
            /// <param name="csv"></param>
            public CsvBindingList(CachedCsvReader csv)
            {
                _csv = csv;
                _count = -1;
                SortDirection = ListSortDirection.Ascending;
            }

            public void AddIndex(PropertyDescriptor property) { }

            public bool AllowNew => false;

            public void ApplySort(PropertyDescriptor property, ListSortDirection direction)
            {
                _sort = (CsvPropertyDescriptor) property;
                SortDirection = direction;

                _csv.ReadToEnd();

                _csv._records.Sort(new CsvRecordComparer(_sort.Index, SortDirection));
            }

            public PropertyDescriptor SortProperty => _sort;

            public int Find(PropertyDescriptor property, object key)
            {
                var fieldIndex = ((CsvPropertyDescriptor) property).Index;
                var value = (string) key;

                var recordIndex = 0;
                var count = Count;

                while (recordIndex < count && _csv[recordIndex, fieldIndex] != value)
                    recordIndex++;

                if (recordIndex == count)
                    return -1;
                return recordIndex;
            }

            public bool SupportsSorting => true;

            public bool IsSorted => _sort != null;

            public bool AllowRemove => false;

            public bool SupportsSearching => true;

            /// <summary>
            /// Contains the current sort direction.
            /// </summary>
            public ListSortDirection SortDirection { get; private set; }

            public event ListChangedEventHandler ListChanged
            {
                add { }
                remove { }
            }

            public bool SupportsChangeNotification => false;

            public void RemoveSort()
            {
                _sort = null;
                SortDirection = ListSortDirection.Ascending;
            }

            public object AddNew() => throw new NotSupportedException();

            public bool AllowEdit => false;

            public void RemoveIndex(PropertyDescriptor property) { }

            public PropertyDescriptorCollection GetItemProperties(PropertyDescriptor[] listAccessors)
            {
                if (_properties == null)
                {
                    var properties = new PropertyDescriptor[_csv.FieldCount];

                    for (var i = 0; i < properties.Length; i++)
                        properties[i] = new CsvPropertyDescriptor(((IDataReader) _csv).GetName(i), i);

                    _properties = new PropertyDescriptorCollection(properties);
                }

                return _properties;
            }

            public string GetListName(PropertyDescriptor[] listAccessors) => string.Empty;

            public int IndexOf(string[] item) => throw new NotSupportedException();

            public void Insert(int index, string[] item) => throw new NotSupportedException();

            public void RemoveAt(int index) => throw new NotSupportedException();

            public string[] this[int index]
            {
                get
                {
                    _csv.MoveTo(index);
                    return _csv._records[index];
                }
                set => throw new NotSupportedException();
            }

            public void Add(string[] item) => throw new NotSupportedException();

            public void Clear() => throw new NotSupportedException();

            public bool Contains(string[] item) => throw new NotSupportedException();

            public void CopyTo(string[][] array, int arrayIndex)
            {
                _csv.MoveToStart();

                while (_csv.ReadNextRecord())
                    _csv.CopyCurrentRecordTo(array[arrayIndex++]);
            }

            public int Count
            {
                get
                {
                    if (_count < 0)
                    {
                        _csv.ReadToEnd();
                        _count = (int) _csv.CurrentRecordIndex + 1;
                    }

                    return _count;
                }
            }

            public bool IsReadOnly => true;

            public bool Remove(string[] item) => throw new NotSupportedException();

            public IEnumerator<string[]> GetEnumerator() => _csv.GetEnumerator();

            public int Add(object value) => throw new NotSupportedException();

            public bool Contains(object value) => throw new NotSupportedException();

            public int IndexOf(object value) => throw new NotSupportedException();

            public void Insert(int index, object value) => throw new NotSupportedException();

            public bool IsFixedSize => true;

            public void Remove(object value) => throw new NotSupportedException();

            object IList.this[int index]
            {
                get => this[index];
                set => throw new NotSupportedException();
            }

            public void CopyTo(Array array, int index)
            {
                _csv.MoveToStart();

                while (_csv.ReadNextRecord())
                    _csv.CopyCurrentRecordTo((string[]) array.GetValue(index++));
            }

            public bool IsSynchronized => false;

            public object SyncRoot => null;

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}