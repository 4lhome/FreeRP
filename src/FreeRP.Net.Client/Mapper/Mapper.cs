using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Net.Client.Mapper
{
    public static class Mapper
    {
        static readonly ConcurrentDictionary<Type, PropertyInfo> _ids = new();
        static readonly ConcurrentDictionary<Type, GrpcService.Database.QueryType> _queryTypes = new();

        public static PropertyInfo? GetMapId(Type type)
        {
            if (_ids.TryGetValue(type, out PropertyInfo? p))
            {
                return p;
            }

            p = GetIdMember(GetTypeMembers(type));
            if (p != null && p.PropertyType == typeof(string))
            {
                _ids[type] = p;
                return p;
            }

            return null;
        }

        public static bool SetMemberId(Type type, string id)
        {
            if (_ids.TryGetValue(type, out var p))
            {
                p.SetValue(type, id);
                return true;
            }

            return false;
        }

        public static GrpcService.Database.QueryType GetQueryType(Type type)
        {
            if (_queryTypes.TryGetValue(type, out var qt) == false)
            {
                qt = GrpcService.Database.QueryType.ValueObject;

                if (type == typeof(string))
                    qt = GrpcService.Database.QueryType.ValueString;
                else if (IsNumber(type))
                    qt = GrpcService.Database.QueryType.ValueNumber;
                else if (IsNullable(type))
                    qt = GrpcService.Database.QueryType.ValueNull;
                else if (type == typeof(bool))
                    qt = GrpcService.Database.QueryType.ValueBoolean;
                else if (IsEnumerable(type) || IsCollection(type))
                    qt = GrpcService.Database.QueryType.ValueArray;

                _queryTypes[type] = qt;
            }

            return qt;
        }

        /// <summary>
        /// Gets MemberInfo that refers to Id from a document object.
        /// </summary>
        public static PropertyInfo? GetIdMember(IEnumerable<PropertyInfo> members)
        {
            return SelectMember(members,
                x => x.Name.Equals("Id", StringComparison.OrdinalIgnoreCase),
                x => x.Name.Equals(x.DeclaringType?.Name + "Id", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Returns all member that will be have mapper between POCO class to document
        /// </summary>
        public static IEnumerable<PropertyInfo> GetTypeMembers(Type type)
        {
            var members = new List<PropertyInfo>();
            
            var flags = (BindingFlags.Public | BindingFlags.Instance);

            members.AddRange(type.GetProperties(flags)
                .Where(x => x.CanRead && x.CanWrite && x.GetIndexParameters().Length == 0));

            return members;
        }

        public static bool IsNumber(Type type)
        {
            return
                type == typeof(Byte) ||
                type == typeof(Int16) ||
                type == typeof(Int32) ||
                type == typeof(Int64) ||
                type == typeof(SByte) ||
                type == typeof(UInt16) ||
                type == typeof(UInt32) ||
                type == typeof(UInt64) ||
                type == typeof(BigInteger) ||
                type == typeof(Decimal) ||
                type == typeof(Double) ||
                type == typeof(Single);
        }

        /// <summary>
        /// Select member from a list of member using predicate order function to select
        /// </summary>
        public static PropertyInfo? SelectMember(IEnumerable<PropertyInfo> members, params Func<PropertyInfo, bool>[] predicates)
        {
            foreach (var predicate in predicates)
            {
                var member = members.FirstOrDefault(predicate);

                if (member != null)
                {
                    return member;
                }
            }

            return null;
        }

        public static bool IsNullable(Type type)
        {
            if (!type.GetTypeInfo().IsGenericType) return false;
            var g = type.GetGenericTypeDefinition();
            return g.Equals(typeof(Nullable<>));
        }

        /// <summary>
        /// Returns true if Type is any kind of Array/IList/ICollection/....
        /// </summary>
        public static bool IsEnumerable(Type type)
        {
            if (type == typeof(IEnumerable) || type.IsArray) return true;
            if (type == typeof(string)) return false; // do not define "String" as IEnumerable<char>

            foreach (var @interface in type.GetInterfaces())
            {
                if (@interface.GetTypeInfo().IsGenericType)
                {
                    if (@interface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        // if needed, you can also return the type used as generic argument
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if Type implement ICollection (like List, HashSet)
        /// </summary>
        public static bool IsCollection(Type type)
        {
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(ICollection<>)) ||
                type.GetInterfaces().Any(x => x == typeof(ICollection) ||
                (x.GetTypeInfo().IsGenericType ? x.GetGenericTypeDefinition() == typeof(ICollection<>) : false));
        }

        /// <summary>
        /// Get item type from a generic List or Array
        /// </summary>
        public static Type? GetListItemType(Type listType)
        {
            if (listType.IsArray) return listType.GetElementType();

            foreach (var i in listType.GetInterfaces())
            {
                if (i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return i.GetGenericArguments()[0];
                }
                // if interface is IEnumerable (non-generic), let's get from listType and not from interface
                // from #395
                else if (listType.GetTypeInfo().IsGenericType && i == typeof(IEnumerable))
                {
                    return listType.GetGenericArguments()[0];
                }
            }

            return typeof(object);
        }
    }
}
