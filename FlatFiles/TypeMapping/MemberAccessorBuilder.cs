﻿using System;
using System.Linq.Expressions;
using System.Reflection;
using FlatFiles.Properties;

namespace FlatFiles.TypeMapping
{
    internal static class MemberAccessorBuilder
    {
        public static IMemberAccessor GetMember<TEntity, TProp>(string memberName)
        {
            return GetMember<TEntity>(typeof(TProp), memberName);
        }

        public static IMemberAccessor GetMember<TEntity>(Type propertyType, string memberName)
        {
            string[] memberNames = memberName.Split('.');
            var member = GetMember(typeof(TEntity), memberNames, 0, null);
            if (propertyType != null && member.Type != propertyType && member.Type != Nullable.GetUnderlyingType(propertyType))
            {
                throw new ArgumentException(Resources.WrongPropertyType);
            }
            return member;
        }

        public static IMemberAccessor GetMember(Type entityType, string[] memberNames, int nameIndex, IMemberAccessor parent)
        {
            if (nameIndex == memberNames.Length)
            {
                return parent;
            }
            string memberName = memberNames[nameIndex];
            var propertyInfo = GetProperty(entityType, memberName);
            if (propertyInfo != null)
            {
                var accessor = new PropertyAccessor(propertyInfo, parent);
                return GetMember(propertyInfo.PropertyType, memberNames, nameIndex + 1, accessor);
            }
            var fieldInfo = GetField(entityType, memberName);
            if (fieldInfo != null)
            {
                var accessor = new FieldAccessor(fieldInfo, parent);
                return GetMember(fieldInfo.FieldType, memberNames, nameIndex + 1, accessor);
            }
            throw new ArgumentException(Resources.BadPropertySelector, nameof(memberName));
        }

        private static PropertyInfo GetProperty(Type type, string propertyName)
        {
            var bindingFlags = BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            return type.GetTypeInfo().GetProperty(propertyName, bindingFlags);
        }

        private static FieldInfo GetField(Type type, string fieldName)
        {
            var bindingFlags = BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            return type.GetTypeInfo().GetField(fieldName, bindingFlags);
        }

        public static IMemberAccessor GetMember<TEntity, TProp>(Expression<Func<TEntity, TProp>> accessor)
        {
            if (accessor == null)
            {
                throw new ArgumentNullException(nameof(accessor));
            }
            return GetMember<TEntity>(accessor.Body);
        }

        private static IMemberAccessor GetMember<TEntity>(Expression expression)
        {
            if (!(expression is MemberExpression member))
            {
                throw new ArgumentException(Resources.BadPropertySelector, nameof(expression));
            }
            if (member.Member is PropertyInfo propertyInfo)
            {
                if (propertyInfo.DeclaringType.GetTypeInfo().IsAssignableFrom(typeof(TEntity)))
                {
                    return new PropertyAccessor(propertyInfo, null);
                }

                IMemberAccessor parentAccessor = GetMember<TEntity>(member.Expression);
                return new PropertyAccessor(propertyInfo, parentAccessor);
            }

            if (member.Member is FieldInfo fieldInfo)
            {
                if (fieldInfo.DeclaringType.GetTypeInfo().IsAssignableFrom(typeof(TEntity)))
                {
                    return new FieldAccessor(fieldInfo, null);
                }

                IMemberAccessor parentAccessor = GetMember<TEntity>(member.Expression);
                return new FieldAccessor(fieldInfo, parentAccessor);
            }

            throw new ArgumentException(Resources.BadPropertySelector, nameof(expression));
        }
    }
}
