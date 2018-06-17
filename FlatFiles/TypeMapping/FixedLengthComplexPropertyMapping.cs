﻿using System;
using FlatFiles.Properties;

namespace FlatFiles.TypeMapping
{
    /// <summary>
    /// Represents the mapping from a type property to an object.
    /// </summary>
    public interface IFixedLengthComplexPropertyMapping
    {
        /// <summary>
        /// Sets the name of the column in the input or output file.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The property mapping for further configuration.</returns>
        IFixedLengthComplexPropertyMapping ColumnName(string name);

        /// <summary>
        /// Sets the options to use when reading/writing the complex type.
        /// </summary>
        /// <param name="options">The options to use.</param>
        /// <returns>The property mapping for further configuration.</returns>
        IFixedLengthComplexPropertyMapping WithOptions(FixedLengthOptions options);

        /// <summary>
        /// Sets the value to treat as null.
        /// </summary>
        /// <param name="value">The value to treat as null.</param>
        /// <returns>The property mapping for further configuration.</returns>
        IFixedLengthComplexPropertyMapping NullValue(string value);

        /// <summary>
        /// Sets a custom handler for nulls.
        /// </summary>
        /// <param name="handler">The handler to use to recognize nulls.</param>
        /// <returns>The property mapping for further configuration.</returns>
        /// <remarks>Setting the handler to null with use the default handler.</remarks>
        IFixedLengthComplexPropertyMapping NullHandler(INullHandler handler);

        /// <summary>
        /// Sets a function to preprocess in the input before parsing it.
        /// </summary>
        /// <param name="preprocessor">A preprocessor function.</param>
        /// <returns>The property mapping for further configuration.</returns>
        IFixedLengthComplexPropertyMapping Preprocessor(Func<string, string> preprocessor);
    }

    internal sealed class FixedLengthComplexPropertyMapping<TEntity> : IFixedLengthComplexPropertyMapping, IMemberMapping
    {
        private readonly IFixedLengthTypeMapper<TEntity> _mapper;
        private string _columnName;
        private FixedLengthOptions _options;
        private INullHandler _nullHandler;
        private Func<string, string> _preprocessor;

        public FixedLengthComplexPropertyMapping(IFixedLengthTypeMapper<TEntity> mapper, IMemberAccessor member, int fileIndex, int workIndex)
        {
            _mapper = mapper;
            Member = member;
            _columnName = member.Name;
            FileIndex = fileIndex;
            WorkIndex = workIndex;
        }

        public IColumnDefinition ColumnDefinition
        {
            get
            {
                FixedLengthSchema schema = _mapper.GetSchema();
                FixedLengthComplexColumn column = new FixedLengthComplexColumn(_columnName, schema)
                {
                    Options = _options,
                    NullHandler = _nullHandler,
                    Preprocessor = _preprocessor
                };
                var mapperSource = (IMapperSource<TEntity>)_mapper;
                var recordMapper = mapperSource.GetMapper();
                return new ComplexMapperColumn<TEntity>(column, recordMapper);
            }
        }

        public IMemberAccessor Member { get; }

        public int FileIndex { get; }

        public int WorkIndex { get; }

        public IFixedLengthComplexPropertyMapping ColumnName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(Resources.BlankColumnName);
            }
            _columnName = name;
            return this;
        }

        public IFixedLengthComplexPropertyMapping WithOptions(FixedLengthOptions options)
        {
            _options = options;
            return this;
        }

        public IFixedLengthComplexPropertyMapping NullHandler(INullHandler handler)
        {
            _nullHandler = handler;
            return this;
        }

        public IFixedLengthComplexPropertyMapping NullValue(string value)
        {
            _nullHandler = new ConstantNullHandler(value);
            return this;
        }

        public IFixedLengthComplexPropertyMapping Preprocessor(Func<string, string> preprocessor)
        {
            _preprocessor = preprocessor;
            return this;
        }
    }
}
