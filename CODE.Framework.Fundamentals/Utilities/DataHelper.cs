using System;
using System.Data;
using System.IO;
using System.Text;
using CODE.Framework.Fundamentals.Utilities.Csv;

namespace CODE.Framework.Fundamentals.Utilities
{
    /// <summary>
    /// This class provides a number of methods that help with a number of standard data tasks.
    /// </summary>
    public static class DataHelper
    {
        /// <summary>
        /// This method takes a csv string (comma separated) and turns into a DataTable.
        /// </summary>
        /// <param name="csvString">The CSV string (comma separated).</param>
        /// <param name="tableName">Name of the table.</param>
        /// <returns></returns>
        public static DataTable CsvToTable(string csvString, string tableName)
        {
            var dtResult = new DataTable {TableName = tableName};

            using (var csv = new CachedCsvReader(new StringReader(csvString), true))
            {
                var headers = csv.GetFieldHeaders();

                foreach (var header in headers) dtResult.Columns.Add(header, typeof(string));

                while (csv.ReadNextRecord())
                {
                    var newRow = dtResult.NewRow();
                    for (var columnNumber = 0; columnNumber < headers.GetLongLength(0); columnNumber++) newRow[columnNumber] = csv[columnNumber];
                    dtResult.Rows.Add(newRow);
                }
            }

            return dtResult;
        }

        /// <summary>
        /// This method takes a data table and turns all its contents into a CSV formatted string.
        /// </summary>
        /// <param name="table">Data Table</param>
        /// <returns>CSV String</returns>
        public static string TableToCSV(DataTable table)
        {
            var sb = new StringBuilder();

            // We create the header record
            for (var counter = 0; counter < table.Columns.Count; counter++)
            {
                // We separate all but the first field by a comma
                if (counter > 0) sb.Append(",");
                // We add the field name
                sb.Append(table.Columns[counter].ColumnName.Trim());
            }

            sb.Append("\r\n");

            // We iterate over all rows, and all fields/columns
            for (var counter = 0; counter < table.Rows.Count; counter++)
            {
                DataRow row = table.Rows[counter];
                for (var counter2 = 0; counter2 < table.Columns.Count; counter2++)
                {
                    var field = row[counter2].ToString().Trim().Replace("\"", "\"\"");
                    if (field.IndexOf(",", StringComparison.Ordinal) > 0 || field.IndexOf("\"", StringComparison.Ordinal) > 0 || field.IndexOf("\r", StringComparison.Ordinal) > 0 || field.IndexOf("\n", StringComparison.Ordinal) > 0) field = "\"" + field + "\"";

                    // For all fields but the first, we need to add a comma-separator
                    if (counter2 > 0) sb.Append(",");

                    // We add the field value to the output stream.
                    sb.Append(field);
                }

                // End of record. We add a line feed
                sb.Append("\r\n");
            }

            return sb.ToString();
        }

        /// <summary>
        /// This method takes a data view and turns all its contents into a CSV formatted string.
        /// </summary>
        /// <param name="view">Data View</param>
        /// <returns>CSV String</returns>
        public static string TableToCSV(DataView view) => DataHelper.TableToCSV(view.Table);

        /// <summary>
        /// Safely converts a value into a Guid or returns Guid.Empty if the value is invalid.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Guid</returns>
        /// <remarks>
        /// This method is an extension method
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// 
        /// // more code here
        /// 
        /// Guid myGuid = dataSet.Tables[0].Rows[0]["id"].ToGuidSave();
        /// </example>
        public static Guid ToGuidSafe(this object value)
        {
            if (value == null) return Guid.Empty;

            try
            {
                return value != DBNull.Value ? (Guid) value : Guid.Empty;
            }
            catch
            {
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Safely converts a value into a string or returns string.Empty if the value is invalid.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <remarks>
        /// This method is an extension method
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// 
        /// // more code here
        /// 
        /// string myString = dataSet.Tables[0].Rows[0]["name"].ToStringSave();
        /// </example>
        public static string ToStringSafe(this object value)
        {
            if (value == null) return string.Empty;

            try
            {
                return value != DBNull.Value ? value.ToString() : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Safely converts a value into a boolean or returns false if the value is invalid.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <remarks>
        /// This method is an extension method
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// 
        /// // more code here
        /// 
        /// bool myBool = dataSet.Tables[0].Rows[0]["active"].ToBooleanSave();
        /// </example>
        public static bool ToBooleanSafe(this object value)
        {
            if (value == null) return false;

            try
            {
                return value != DBNull.Value && (bool) value;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Safely converts a value into a DateTime or returns DateTime.MinValue if the value is invalid.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <remarks>
        /// This method is an extension method
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// 
        /// // more code here
        /// 
        /// DateTime myDate = dataSet.Tables[0].Rows[0]["timeStamp"].ToDateTimeSave();
        /// </example>
        public static DateTime ToDateTimeSafe(this object value)
        {
            if (value == null) return DateTime.MinValue;

            try
            {
                return value != DBNull.Value ? (DateTime) value : DateTime.MinValue;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Safely converts a value into an integer or returns 0 if the value is invalid.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <remarks>
        /// This method is an extension method
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// 
        /// // more code here
        /// 
        /// int myInt = dataSet.Tables[0].Rows[0]["number"].ToIntegerSave();
        /// </example>
        public static int ToIntegerSafe(this object value)
        {
            if (value == null) return 0;

            try
            {
                return value != DBNull.Value ? (int) value : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Safely converts a value into a double or returns 0.0 if the value is invalid.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <remarks>
        /// This method is an extension method
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// 
        /// // more code here
        /// 
        /// int myDouble = dataSet.Tables[0].Rows[0]["number"].ToDoubleSave();
        /// </example>
        public static double ToDoubleSafe(this object value)
        {
            if (value == null) return 0.0;

            try
            {
                return value != DBNull.Value ? (double) value : 0.0;
            }
            catch
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Safely converts a value into a decimal or returns 0.0 if the value is invalid.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <remarks>
        /// This method is an extension method
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// 
        /// // more code here
        /// 
        /// decimal myDec = dataSet.Tables[0].Rows[0]["price"].ToDecimalSave();
        /// </example>
        public static decimal ToDecimalSafe(this object value)
        {
            if (value == null) return 0m;

            try
            {
                return value != DBNull.Value ? (decimal) value : 0m;
            }
            catch
            {
                return 0m;
            }
        }

        /// <summary>
        /// Safely converts a value into a char or returns ' ' if the value is invalid.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <remarks>
        /// This method is an extension method
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// 
        /// // more code here
        /// 
        /// char myChar = dataSet.Tables[0].Rows[0]["character"].ToCharSave();
        /// </example>
        public static char ToCharSafe(this object value)
        {
            if (value == null) return ' ';

            try
            {
                return value != DBNull.Value ? (char) value : ' ';
            }
            catch
            {
                return ' ';
            }
        }

        /// <summary>
        /// Safely converts a value into a byte array or returns an empty byte array if the value is invalid.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <remarks>
        /// This method is an extension method
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// 
        /// // more code here
        /// 
        /// byte[] myBytes = dataSet.Tables[0].Rows[0]["image"].ToByteArraySave();
        /// </example>
        public static byte[] ToByteArraySafe(this object value)
        {
            if (value == null) return Array.Empty<byte>();

            try
            {
                return value != DBNull.Value ? (byte[]) value : Array.Empty<byte>();
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }
    }
}