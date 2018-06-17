﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlatFiles.Properties;

namespace FlatFiles
{
    internal sealed class FixedLengthRecordWriter
    {
        private readonly TextWriter _writer;
        private readonly FixedLengthSchemaInjector _injector;

        public FixedLengthRecordWriter(TextWriter writer, FixedLengthSchema schema, FixedLengthOptions options)
        {
            _writer = writer;
            Metadata = new FixedLengthWriterMetadata
            {
                Schema = schema,
                Options = options.Clone()
            };
        }

        public FixedLengthRecordWriter(TextWriter writer, FixedLengthSchemaInjector injector, FixedLengthOptions options)
            : this(writer, (FixedLengthSchema)null, options)
        {
            _injector = injector;
        }

        public FixedLengthWriterMetadata Metadata { get; }

        public void WriteRecord(object[] values)
        {
            var schema = GetSchema(values);
            if (values.Length != schema.ColumnDefinitions.PhysicalCount)
            {
                throw new ArgumentException(Resources.WrongNumberOfValues, nameof(values));
            }
            string[] formattedColumns = FormatValues(values, schema);
            FitWindows(schema, formattedColumns);
            foreach (string column in formattedColumns)
            {
                _writer.Write(column);
            }
        }

        public async Task WriteRecordAsync(object[] values)
        {
            var schema = GetSchema(values);
            if (values.Length != schema.ColumnDefinitions.PhysicalCount)
            {
                throw new ArgumentException(Resources.WrongNumberOfValues, nameof(values));
            }
            var formattedColumns = FormatValues(values, schema);
            FitWindows(schema, formattedColumns);
            foreach (string column in formattedColumns)
            {
                await _writer.WriteAsync(column).ConfigureAwait(false);
            }
        }

        private FixedLengthSchema GetSchema(object[] values)
        {
            return _injector == null ? Metadata.Schema : _injector.GetSchema(values);
        }

        private string[] FormatValues(object[] values, FixedLengthSchema schema)
        {
            var metadata = _injector == null ? Metadata : new FixedLengthWriterMetadata
            {
                Schema = schema,
                Options = Metadata.Options,
                RecordCount = Metadata.RecordCount,
                LogicalRecordCount = Metadata.LogicalRecordCount
            };
            return schema.FormatValues(metadata, values);
        }

        private void FitWindows(FixedLengthSchema schema, string[] values)
        {
            for (int index = 0; index != values.Length; ++index)
            {
                var window = schema.Windows[index];
                values[index] = FitWidth(window, values[index]);
            }
        }

        public void WriteSchema()
        {
            if (_injector != null)
            {
                return;
            }
            var names = Metadata.Schema.ColumnDefinitions.Select(c => c.ColumnName);
            var fitted = names.Select((v, i) => FitWidth(Metadata.Schema.Windows[i], v));
            foreach (string column in fitted)
            {
                _writer.Write(column);
            }
        }

        public async Task WriteSchemaAsync()
        {
            if (_injector != null)
            {
                return;
            }
            var names = Metadata.Schema.ColumnDefinitions.Select(c => c.ColumnName);
            var fitted = names.Select((v, i) => FitWidth(Metadata.Schema.Windows[i], v));
            foreach (string column in fitted)
            {
                await _writer.WriteAsync(column).ConfigureAwait(false);
            }
        }

        private string FitWidth(Window window, string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }
            if (value.Length > window.Width)
            {
                return GetTruncatedValue(value, window);
            }

            if (value.Length < window.Width)
            {
                return GetPaddedValue(value, window);
            }

            return value;
        }

        private string GetTruncatedValue(string value, Window window)
        {
            OverflowTruncationPolicy policy = window.TruncationPolicy ?? Metadata.Options.TruncationPolicy;
            if (policy == OverflowTruncationPolicy.TruncateLeading)
            {
                int start = value.Length - window.Width;  // take characters on the end
                return value.Substring(start, window.Width);
            }

            return value.Substring(0, window.Width);
        }

        private string GetPaddedValue(string value, Window window)
        {
            var alignment = window.Alignment ?? Metadata.Options.Alignment;
            if (alignment == FixedAlignment.LeftAligned)
            {
                return value.PadRight(window.Width, window.FillCharacter ?? Metadata.Options.FillCharacter);
            }

            return value.PadLeft(window.Width, window.FillCharacter ?? Metadata.Options.FillCharacter);
        }

        public void WriteRecordSeparator()
        {
            if (Metadata.Options.HasRecordSeparator)
            {
                _writer.Write(Metadata.Options.RecordSeparator ?? Environment.NewLine);
            }
        }

        public async Task WriteRecordSeparatorAsync()
        {
            if (Metadata.Options.HasRecordSeparator)
            {
                await _writer.WriteAsync(Metadata.Options.RecordSeparator ?? Environment.NewLine).ConfigureAwait(false);
            }
        }

        internal class FixedLengthWriterMetadata : IProcessMetadata
        {
            public FixedLengthSchema Schema { get; internal set; }

            ISchema IProcessMetadata.Schema => Schema;

            public FixedLengthOptions Options { get; internal set; }

            IOptions IProcessMetadata.Options => Options;

            public int RecordCount { get; internal set; }

            public int LogicalRecordCount { get; internal set; }
        }
    }
}
