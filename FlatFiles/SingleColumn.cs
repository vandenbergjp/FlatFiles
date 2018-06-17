﻿using System;
using System.Globalization;

namespace FlatFiles
{
    /// <summary>
    /// Represents a column containing singles.
    /// </summary>
    public class SingleColumn : ColumnDefinition
    {
        /// <summary>
        /// Initializes a new instance of an SingleColumn.
        /// </summary>
        /// <param name="columnName">The name of the column.</param>
        public SingleColumn(string columnName)
            : base(columnName)
        {
        }

        /// <summary>
        /// Gets the type of the values in the column.
        /// </summary>
        public override Type ColumnType => typeof(float);

        /// <summary>
        /// Gets or sets the format provider to use when parsing.
        /// </summary>
        public IFormatProvider FormatProvider { get; set; }

        /// <summary>
        /// Gets or sets the number styles to use when parsing.
        /// </summary>
        public NumberStyles NumberStyles { get; set; } = NumberStyles.Float;

        /// <summary>
        /// Gets or sets the format string to use when converting the value to a string.
        /// </summary>
        public string OutputFormat { get; set; }

        /// <summary>
        /// Parses the given value, returning a Single.
        /// </summary>
        /// <param name="value">The value to parse.</param>
        /// <returns>The parsed Single.</returns>
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
            value = TrimValue(value);
            return float.Parse(value, NumberStyles, provider);
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

            float actual = (float)value;
            return OutputFormat == null ? actual.ToString(FormatProvider ?? CultureInfo.CurrentCulture) : actual.ToString(OutputFormat, FormatProvider ?? CultureInfo.CurrentCulture);
        }
    }
}
