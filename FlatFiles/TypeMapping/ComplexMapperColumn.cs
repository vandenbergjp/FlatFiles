﻿using System;

namespace FlatFiles.TypeMapping
{
    internal class ComplexMapperColumn<TEntity> : IColumnDefinition
    {
        private readonly IColumnDefinition column;
        private readonly Func<IRecordContext, object[], TEntity> reader;
        private readonly Action<IRecordContext, TEntity, object[]> writer;
        private readonly int logicalCount;

        public ComplexMapperColumn(IColumnDefinition column, IMapper<TEntity> mapper)
        {
            this.column = column;
            reader = mapper.GetReader();
            writer = mapper.GetWriter();
            logicalCount = mapper.LogicalCount;
        }

        public string ColumnName => column.ColumnName;

        public Type ColumnType => typeof(TEntity);

        public bool IsIgnored => column.IsIgnored;

        public bool IsNullable => true;

        public IDefaultValue DefaultValue
        {
            get => column.DefaultValue;
            set => column.DefaultValue = value;
        }

        public INullFormatter NullFormatter
        {
            get => column.NullFormatter;
            set => column.NullFormatter = value;
        }

        [Obsolete]
        public Func<string, string> Preprocessor
        {
            get => column.Preprocessor;
            set => column.Preprocessor = value;
        }

        public Func<IColumnContext, string, string> OnParsing
        {
            get => column.OnParsing;
            set => column.OnParsing = value;
        }

        public Func<IColumnContext, object, object> OnParsed
        {
            get => column.OnParsed;
            set => column.OnParsed = value;
        }

        public Func<IColumnContext, object, object> OnFormatting
        {
            get => column.OnFormatting;
            set => column.OnFormatting = value;
        }

        public Func<IColumnContext, string, string> OnFormatted
        {
            get => column.OnFormatted;
            set => column.OnFormatted = value;
        }

        public object Parse(IColumnContext context, string value)
        {
            object parsed = column.Parse(context, value);
            object[] values = parsed as object[];
            TEntity result = reader(null, values);
            return result;
        }

        public string Format(IColumnContext context, object value)
        {
            object[] values = new object[logicalCount];
            writer(null, (TEntity)value, values);
            string formatted = column.Format(context, values);
            return formatted;
        }
    }
}
