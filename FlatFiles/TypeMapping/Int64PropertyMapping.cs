﻿using System;
using System.Globalization;

namespace FlatFiles.TypeMapping
{
    /// <summary>
    /// Represents the mapping from a type property to a long column.
    /// </summary>
    public interface IInt64PropertyMapping
    {
        /// <summary>
        /// Sets the name of the column in the input or output file.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The property mapping for further configuration.</returns>
        IInt64PropertyMapping ColumnName(string name);

        /// <summary>
        /// Sets the format provider to use.
        /// </summary>
        /// <param name="provider">The provider to use.</param>
        /// <returns>The property mapping for further configuration.</returns>
        IInt64PropertyMapping FormatProvider(IFormatProvider provider);

        /// <summary>
        /// Sets the number styles to expect when parsing the input.
        /// </summary>
        /// <param name="styles">The number styles used in the input.</param>
        /// <returns>The property mappig for further configuration.</returns>
        IInt64PropertyMapping NumberStyles(NumberStyles styles);

        /// <summary>
        /// Sets the output format to use.
        /// </summary>
        /// <param name="format">The format to use.</param>
        /// <returns>The property mapping for further configuration.</returns>
        IInt64PropertyMapping OutputFormat(string format);

        /// <summary>
        /// Sets the value to treat as null.
        /// </summary>
        /// <param name="value">The value to treat as null.</param>
        /// <returns>The property mapping for further configuration.</returns>
        IInt64PropertyMapping NullValue(string value);

        /// <summary>
        /// Sets a custom handler for nulls.
        /// </summary>
        /// <param name="handler">The handler to use to recognize nulls.</param>
        /// <returns>The property mapping for further configuration.</returns>
        /// <remarks>Setting the handler to null with use the default handler.</remarks>
        IInt64PropertyMapping NullHandler(INullHandler handler);

        /// <summary>
        /// Sets a function to preprocess in the input before parsing it.
        /// </summary>
        /// <param name="preprocessor">A preprocessor function.</param>
        /// <returns>The property mapping for further configuration.</returns>
        IInt64PropertyMapping Preprocessor(Func<string, string> preprocessor);
    }

    internal sealed class Int64PropertyMapping : IInt64PropertyMapping, IMemberMapping
    {
        private readonly Int64Column _column;

        public Int64PropertyMapping(Int64Column column, IMemberAccessor member, int fileIndex, int workIndex)
        {
            _column = column;
            Member = member;
            FileIndex = fileIndex;
            WorkIndex = workIndex;
        }

        public IInt64PropertyMapping ColumnName(string name)
        {
            _column.ColumnName = name;
            return this;
        }

        public IInt64PropertyMapping FormatProvider(IFormatProvider provider)
        {
            _column.FormatProvider = provider;
            return this;
        }

        public IInt64PropertyMapping NumberStyles(NumberStyles styles)
        {
            _column.NumberStyles = styles;
            return this;
        }

        public IInt64PropertyMapping OutputFormat(string format)
        {
            _column.OutputFormat = format;
            return this;
        }

        public IInt64PropertyMapping NullValue(string value)
        {
            _column.NullHandler = new ConstantNullHandler(value);
            return this;
        }

        public IInt64PropertyMapping NullHandler(INullHandler handler)
        {
            _column.NullHandler = handler;
            return this;
        }

        public IInt64PropertyMapping Preprocessor(Func<string, string> preprocessor)
        {
            _column.Preprocessor = preprocessor;
            return this;
        }

        public IMemberAccessor Member { get; }

        public IColumnDefinition ColumnDefinition => _column;

        public int FileIndex { get; }

        public int WorkIndex { get; }
    }
}
