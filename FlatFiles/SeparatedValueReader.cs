﻿using System;
using System.IO;
using System.Threading.Tasks;
using FlatFiles.Properties;

namespace FlatFiles
{
    /// <inheritdoc />
    /// <summary>
    /// Extracts records from a file that has values separated by a separator token.
    /// </summary>
    public sealed class SeparatedValueReader : IReader
    {
        private readonly SeparatedValueRecordParser _parser;
        private readonly SeparatedValueSchemaSelector _schemaSelector;
        private readonly Metadata _metadata;
        private object[] _values;
        private bool _endOfFile;
        private bool _hasError;

        /// <summary>
        /// Initializes a new SeparatedValueReader with no schema.
        /// </summary>
        /// <param name="reader">A reader over the separated value document.</param>
        /// <param name="options">The options controlling how the separated value document is read.</param>
        /// <exception cref="ArgumentNullException">The reader is null.</exception>
        public SeparatedValueReader(TextReader reader, SeparatedValueOptions options = null)
            : this(reader, null, options, false)
        {
        }

        /// <summary>
        /// Initializes a new SeparatedValueReader with the given schema.
        /// </summary>
        /// <param name="reader">A reader over the separated value document.</param>
        /// <param name="schema">The schema of the separated value document.</param>
        /// <param name="options">The options controlling how the separated value document is read.</param>
        /// <exception cref="ArgumentNullException">The reader is null.</exception>
        /// <exception cref="ArgumentNullException">The schema is null.</exception>
        public SeparatedValueReader(TextReader reader, SeparatedValueSchema schema, SeparatedValueOptions options = null)
            : this(reader, schema, options, true)
        {
        }

        /// <summary>
        /// Initializes a new SeparatedValueReader with the given schema.
        /// </summary>
        /// <param name="reader">A reader over the separated value document.</param>
        /// <param name="schemaSelector">The schema selector configured to determine the schema dynamically.</param>
        /// <param name="options">The options controlling how the separated value document is read.</param>
        /// <exception cref="ArgumentNullException">The reader is null.</exception>
        /// <exception cref="ArgumentNullException">The schema selector is null.</exception>
        public SeparatedValueReader(TextReader reader, SeparatedValueSchemaSelector schemaSelector, SeparatedValueOptions options = null)
            : this(reader, null, options, false)
        {
            _schemaSelector = schemaSelector ?? throw new ArgumentNullException(nameof(schemaSelector));
        }

        private SeparatedValueReader(TextReader reader, SeparatedValueSchema schema, SeparatedValueOptions options, bool hasSchema)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            if (hasSchema && schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }
            if (options == null)
            {
                options = new SeparatedValueOptions();
            }
            if (options.RecordSeparator == options.Separator)
            {
                throw new ArgumentException(Resources.SameSeparator, nameof(options));
            }
            RetryReader retryReader = new RetryReader(reader);
            _parser = new SeparatedValueRecordParser(retryReader, options);
            _metadata = new Metadata
            {
                Schema = hasSchema ? schema : null,
                Options = _parser.Options
            };
        }

        /// <summary>
        /// Raised when a record is read but before its columns are parsed.
        /// </summary>
        public event EventHandler<SeparatedValueRecordReadEventArgs> RecordRead;

        /// <summary>
        /// Raised when an error occurs while processing a record.
        /// </summary>
        public event EventHandler<ProcessingErrorEventArgs> Error;

        /// <summary>
        /// Gets the names of the columns found in the file.
        /// </summary>
        /// <returns>The names.</returns>
        public SeparatedValueSchema GetSchema()
        {
            if (_schemaSelector != null)
            {
                return null;
            }
            HandleSchema();
            if (_metadata.Schema == null)
            {
                throw new InvalidOperationException(Resources.SchemaNotDefined);
            }
            return _metadata.Schema;
        }

        ISchema IReader.GetSchema()
        {
            return GetSchema();
        }

        /// <summary>
        /// Gets the schema being used by the parser.
        /// </summary>
        /// <returns>The schema being used by the parser.</returns>
        public async Task<SeparatedValueSchema> GetSchemaAsync()
        {
            if (_schemaSelector != null)
            {
                return null;
            }
            await HandleSchemaAsync().ConfigureAwait(false);
            if (_metadata.Schema == null)
            {
                throw new InvalidOperationException(Resources.SchemaNotDefined);
            }
            return _metadata.Schema;
        }

        async Task<ISchema> IReader.GetSchemaAsync()
        {
            var schema = await GetSchemaAsync().ConfigureAwait(false);
            return schema;
        }

        /// <summary>
        /// Attempts to read the next record from the stream.
        /// </summary>
        /// <returns>True if the next record was read or false if all records have been read.</returns>
        public bool Read()
        {
            if (_hasError)
            {
                throw new InvalidOperationException(Resources.ReadingWithErrors);
            }
            HandleSchema();
            try
            {
                _values = ParsePartitions();
                if (_values == null)
                {
                    return false;
                }

                ++_metadata.LogicalRecordCount;
                return true;
            }
            catch (FlatFileException)
            {
                _hasError = true;
                throw;
            }
        }

        private void HandleSchema()
        {
            if (_metadata.RecordCount != 0)
            {
                return;
            }
            if (!_parser.Options.IsFirstRecordSchema)
            {
                return;
            }
            if (_schemaSelector != null || _metadata.Schema != null)
            {
                skip();
                return;
            }
            string[] columnNames = ReadNextRecord();
            _metadata.Schema = new SeparatedValueSchema();
            foreach (string columnName in columnNames)
            {
                StringColumn column = new StringColumn(columnName);
                _metadata.Schema.AddColumn(column);
            }
        }

        private object[] ParsePartitions()
        {
            string[] rawValues = ReadWithFilter();
            while (rawValues != null)
            {
                var schema = GetSchema(rawValues);
                if (schema != null && hasWrongNumberOfColumns(schema, rawValues))
                {
                    ProcessError(new RecordProcessingException(_metadata.RecordCount, Resources.SeparatedValueRecordWrongNumberOfColumns));
                }
                else
                {
                    object[] values = ParseValues(schema, rawValues);
                    if (values != null)
                    {
                        return values;
                    }
                }
                rawValues = ReadWithFilter();
            }
            return null;
        }

        private string[] ReadWithFilter()
        {
            string[] rawValues = ReadNextRecord();
            while (rawValues != null && IsSkipped(rawValues))
            {
                rawValues = ReadNextRecord();
            }
            return rawValues;
        }

        /// <inheritdoc />
        /// <summary>
        /// Attempts to read the next record from the stream.
        /// </summary>
        /// <returns>True if the next record was read or false if all records have been read.</returns>
        public async ValueTask<bool> ReadAsync()
        {
            if (_hasError)
            {
                throw new InvalidOperationException(Resources.ReadingWithErrors);
            }
            await HandleSchemaAsync().ConfigureAwait(false);
            try
            {
                _values = await ParsePartitionsAsync().ConfigureAwait(false);
                if (_values == null)
                {
                    return false;
                }

                ++_metadata.LogicalRecordCount;
                return true;
            }
            catch (FlatFileException)
            {
                _hasError = true;
                throw;
            }
        }

        private async Task HandleSchemaAsync()
        {
            if (_metadata.RecordCount != 0)
            {
                return;
            }
            if (!_parser.Options.IsFirstRecordSchema)
            {
                return;
            }
            if (_metadata.Schema != null)
            {
                await skipAsync().ConfigureAwait(false);
                return;
            }
            string[] columnNames = await ReadNextRecordAsync().ConfigureAwait(false);
            _metadata.Schema = new SeparatedValueSchema();
            foreach (string columnName in columnNames)
            {
                StringColumn column = new StringColumn(columnName);
                _metadata.Schema.AddColumn(column);
            }
        }

        private async Task<object[]> ParsePartitionsAsync()
        {
            string[] rawValues = await ReadWithFilterAsync().ConfigureAwait(false);
            while (rawValues != null)
            {
                var schema = GetSchema(rawValues);
                if (schema != null && hasWrongNumberOfColumns(schema, rawValues))
                {
                    ProcessError(new RecordProcessingException(_metadata.RecordCount, Resources.SeparatedValueRecordWrongNumberOfColumns));
                }
                else
                {
                    object[] values = ParseValues(schema, rawValues);
                    if (values != null)
                    {
                        return values;
                    }
                }
                rawValues = await ReadWithFilterAsync().ConfigureAwait(false);
            }
            return null;
        }

        private SeparatedValueSchema GetSchema(string[] rawValues)
        {
            return _schemaSelector == null ? _metadata.Schema : _schemaSelector.GetSchema(rawValues);
        }

        private bool hasWrongNumberOfColumns(SeparatedValueSchema schema, string[] values)
        {
            return values.Length + schema.ColumnDefinitions.MetadataCount < schema.ColumnDefinitions.PhysicalCount;
        }

        private async Task<string[]> ReadWithFilterAsync()
        {
            string[] rawValues = await ReadNextRecordAsync().ConfigureAwait(false);
            while (rawValues != null && IsSkipped(rawValues))
            {
                rawValues = await ReadNextRecordAsync().ConfigureAwait(false);
            }
            return rawValues;
        }

        private bool IsSkipped(string[] values)
        {
            if (RecordRead == null)
            {
                return false;
            }
            var e = new SeparatedValueRecordReadEventArgs(values);
            RecordRead(this, e);
            return e.IsSkipped;
        }

        private object[] ParseValues(SeparatedValueSchema schema, string[] rawValues)
        {
            if (schema == null)
            {
                return rawValues;
            }
            try
            {
                var metadata = _schemaSelector == null ? _metadata : new Metadata
                {
                    Schema = schema,
                    Options = _metadata.Options,
                    RecordCount = _metadata.RecordCount,
                    LogicalRecordCount = _metadata.LogicalRecordCount
                };
                return schema.ParseValues(metadata, rawValues);
            }
            catch (FlatFileException exception)
            {
                ProcessError(new RecordProcessingException(_metadata.RecordCount, Resources.InvalidRecordConversion, exception));
                return null;
            }
        }

        /// <summary>
        /// Attempts to skip the next record from the stream.
        /// </summary>
        /// <returns>True if the next record was skipped or false if all records have been read.</returns>
        /// <remarks>The previously parsed values remain available.</remarks>
        public bool Skip()
        {
            if (_hasError)
            {
                throw new InvalidOperationException(Resources.ReadingWithErrors);
            }
            HandleSchema();
            bool result = skip();
            return result;
        }

        private bool skip()
        {
            string[] rawValues = ReadNextRecord();
            return rawValues != null;
        }

        /// <inheritdoc />
        /// <summary>
        /// Attempts to skip the next record from the stream.
        /// </summary>
        /// <returns>True if the next record was skipped or false if all records have been read.</returns>
        /// <remarks>The previously parsed values remain available.</remarks>
        public async ValueTask<bool> SkipAsync()
        {
            if (_hasError)
            {
                throw new InvalidOperationException(Resources.ReadingWithErrors);
            }
            await HandleSchemaAsync().ConfigureAwait(false);
            bool result = await skipAsync().ConfigureAwait(false);
            return result;
        }

        private async ValueTask<bool> skipAsync()
        {
            string[] rawValues = await ReadNextRecordAsync().ConfigureAwait(false);
            return rawValues != null;
        }

        private void ProcessError(RecordProcessingException exception)
        {
            if (Error != null)
            {
                var args = new ProcessingErrorEventArgs(exception);
                Error(this, args);
                if (args.IsHandled)
                {
                    return;
                }
            }
            throw exception;
        }

        private string[] ReadNextRecord()
        {
            if (_parser.IsEndOfStream())
            {
                _endOfFile = true;
                _values = null;
                return null;
            }
            try
            {
                string[] results = _parser.ReadRecord();
                ++_metadata.RecordCount;
                return results;
            }
            catch (SeparatedValueSyntaxException exception)
            {
                throw new RecordProcessingException(_metadata.RecordCount, Resources.InvalidRecordFormatNumber, exception);
            }
        }

        private async Task<string[]> ReadNextRecordAsync()
        {
            if (await _parser.IsEndOfStreamAsync().ConfigureAwait(false))
            {
                _endOfFile = true;
                _values = null;
                return null;
            }
            try
            {
                string[] results = await _parser.ReadRecordAsync().ConfigureAwait(false);
                ++_metadata.RecordCount;
                return results;
            }
            catch (SeparatedValueSyntaxException exception)
            {
                throw new RecordProcessingException(_metadata.RecordCount, Resources.InvalidRecordFormatNumber, exception);
            }
        }

        /// <summary>
        /// Gets the values for the current record.
        /// </summary>
        /// <returns>The values of the current record.</returns>
        public object[] GetValues()
        {
            if (_hasError)
            {
                throw new InvalidOperationException(Resources.ReadingWithErrors);
            }
            if (_metadata.RecordCount == 0)
            {
                throw new InvalidOperationException(Resources.ReadNotCalled);
            }
            if (_endOfFile)
            {
                throw new InvalidOperationException(Resources.NoMoreRecords);
            }
            object[] copy = new object[_values.Length];
            Array.Copy(_values, copy, _values.Length);
            return copy;
        }

        private class Metadata : IProcessMetadata
        {
            public SeparatedValueSchema Schema { get; set; }

            ISchema IProcessMetadata.Schema => Schema;

            public SeparatedValueOptions Options { get; set; }

            IOptions IProcessMetadata.Options => Options;

            public int RecordCount { get; set; }

            public int LogicalRecordCount { get; set; }
        }
    }
}
