﻿using System;
using System.Globalization;

namespace FlatFiles
{
    /// <summary>
    /// Represents a column of signed byte values.
    /// </summary>
    public class SByteColumn : ColumnDefinition
    {
        /// <summary>
        /// Initializes a new instance of a SByteColumn.
        /// </summary>
        /// <param name="columnName">The name of the column.</param>
        public SByteColumn(string columnName)
            : base(columnName)
        {
        }

        /// <summary>
        /// Gets the type of the values in the column.
        /// </summary>
        public override Type ColumnType
        {
            get { return typeof(SByte); }
        }

        /// <summary>
        /// Gets or sets the format provider to use to parse the value.
        /// </summary>
        public IFormatProvider FormatProvider { get; set; }

        /// <summary>
        /// Gets or sets the number styles to use when parsing the value.
        /// </summary>
        public NumberStyles NumberStyles { get; set; } = NumberStyles.Integer;

        /// <summary>
        /// Gets or sets the formatting to use when converting the value to a string.
        /// </summary>
        public string OutputFormat { get; set; }

        /// <summary>
        /// Parses the given value into a signed byte.
        /// </summary>
        /// <param name="value">The value to parse.</param>
        /// <returns>The parsed signed byte value.</returns>
        public override object Parse(string value)
        {
            if (Preprocessor != null)
            {
                value = Preprocessor(value);
            }
            if (NullHandler.IsNullRepresentation(value))
            {
                return null;
            }
            IFormatProvider provider = FormatProvider ?? CultureInfo.CurrentCulture;
            return SByte.Parse(value, NumberStyles, provider);
        }

        /// <summary>
        /// Formats the given object.
        /// </summary>
        /// <param name="value">The object to format.</param>
        /// <returns>The formatted value.</returns>
        public override string Format(object value)
        {
            if (value == null)
            {
                return NullHandler.GetNullRepresentation();
            }
            sbyte actual = (sbyte)value;
            if (OutputFormat == null)
            {
                return actual.ToString(FormatProvider ?? CultureInfo.CurrentCulture);
            }
            else
            {
                return actual.ToString(OutputFormat, FormatProvider ?? CultureInfo.CurrentCulture);
            }
        }
    }
}
