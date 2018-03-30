using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Dbarone.Data
{
    /// <summary>
    /// Gets metadata (attribute information) for all entities.
    /// </summary>
    public static class MetaDataStore
    {
        private static readonly Dictionary<Type, TableInfo> typeToTableInfo = new Dictionary<Type, TableInfo>();

        public static TableInfo GetTableInfoFor<TEntity>()
        {
            return GetTableInfoFor(typeof(TEntity));
        }

        /// <summary>
        /// Returns metadata for an entity. If the entity does not exist
        /// an error is thrown.
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public static TableInfo GetTableInfoFor(Type entityType)
        {
            if (!typeToTableInfo.ContainsKey(entityType))
            {
                throw new Exception(string.Format("TypeInfo for {0} does not exist. Check you have built metadata for appropriate assemblies by calling the BuildMetaDataFor() method", entityType.Name));
            }

            return typeToTableInfo[entityType];
        }

        public static bool HasTableInfoFor(Type entityType)
        {
            return typeToTableInfo.ContainsKey(entityType);
        }

        public static void BuildMetaDataFor(Assembly assembly)
        {
            BuildMapOfEntityTypesWithTheirTableInfo(assembly);

            foreach (KeyValuePair<Type, TableInfo> pair in typeToTableInfo.Where(t=>t.Value.Columns==null))
            {
                // columns
                LoopThroughPropertiesWith<ColumnAttribute>(pair.Key, pair.Value, AddColumnInfo);

                // If no columns at this point, the entity may be simple POCO.
                // in this case, just loop through all public properties without
                // checking for special attributes.
                if (pair.Value.Columns == null)
                {
                    LoopThroughProperties(pair.Key, pair.Value, (table, prop) =>
                        table.AddColumn(new ColumnInfo
                        {
                            Name = prop.Name,
                            DotNetType = prop.PropertyType,
                            Key = false,
                            Order = 0,
                            PropertyInfo = prop,
                            DatabaseGenerated = (prop.GetCustomAttribute<ReadOnlyAttribute>() != null) ? true : false,
                        }));
                }

                // keys
                LoopThroughPropertiesWith<KeyAttribute>(pair.Key, pair.Value, SetPrimaryKeyInfo);

            }
        }

        private static void BuildMapOfEntityTypesWithTheirTableInfo(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                // Entity types must implement IEntity
                if (typeof(IEntity).IsAssignableFrom(type) && type!=typeof(IEntity))
                {

                    // type MAY have table attribute or may not.
                    // default name to name of entity
                    var tableInfo = new TableInfo
                    {
                        TableName = {
                            { CrudOperation.CREATE, type.Name },
                            { CrudOperation.READ, type.Name},
                            { CrudOperation.UPDATE, type.Name },
                            { CrudOperation.DELETE, type.Name }
                        },
                        EntityType = type
                    };

                    var typeAttributes = (TableAttribute[])Attribute.GetCustomAttributes(type, typeof(TableAttribute));
                    if (typeAttributes.Length > 0)
                    {
                        foreach (var attribute in typeAttributes)
                        {
                            if ((attribute.Operation & CrudOperation.CREATE) == CrudOperation.CREATE)
                                tableInfo.TableName[CrudOperation.CREATE] = attribute.Name;

                            if ((attribute.Operation & CrudOperation.READ) == CrudOperation.READ)
                                tableInfo.TableName[CrudOperation.READ] = attribute.Name;

                            if ((attribute.Operation & CrudOperation.UPDATE) == CrudOperation.UPDATE)
                                tableInfo.TableName[CrudOperation.UPDATE] = attribute.Name;

                            if ((attribute.Operation & CrudOperation.DELETE) == CrudOperation.DELETE)
                                tableInfo.TableName[CrudOperation.DELETE] = attribute.Name;
                        }
//                        var tableAttribute = (TableAttribute)typeAttributes[0];
//                        tableInfo.Name = tableAttribute.Name;
                    }
                    if (!typeToTableInfo.Keys.Contains(type))
                    {
                        typeToTableInfo.Add(type, tableInfo);
                    }
                }
            }
        }

        private static void LoopThroughEachTableUsing<TAttribute>(Type entityType, TableInfo tableInfo,
            Action<TableInfo, TAttribute> andExecuteFollowingCode) where TAttribute : Attribute
        {
            var attributes = Attribute.GetCustomAttributes(entityType, typeof(TAttribute));
            foreach (var attribute in attributes)
                andExecuteFollowingCode(tableInfo, (TAttribute)attribute);
        }

        private static void LoopThroughProperties(Type entityType, TableInfo tableInfo,
            Action<TableInfo, PropertyInfo> andExecuteFollowingCode)
        {
            foreach (var propertyInfo in entityType.GetProperties())
            {
                andExecuteFollowingCode(tableInfo, propertyInfo);
            }
        }

        private static void LoopThroughPropertiesWith<TAttribute>(Type entityType, TableInfo tableInfo,
            Action<TableInfo, PropertyInfo, TAttribute> andExecuteFollowingCode)
            where TAttribute : Attribute
        {
            foreach (var propertyInfo in entityType.GetProperties())
            {
                var attribute = GetAttribute<TAttribute>(propertyInfo);

                // Check for NotMapped attribute too
                var notMapped = GetAttribute<NotMappedAttribute>(propertyInfo);

                if (attribute != null && notMapped==null)
                {
                    andExecuteFollowingCode(tableInfo, propertyInfo, attribute);
                }
            }
        }

        private static void SetPrimaryKeyInfo(TableInfo tableInfo, PropertyInfo propertyInfo, KeyAttribute keyAttribute)
        {
            // Set key property on appropriate ColumnInfo
            var c = tableInfo.GetColumn(propertyInfo);
            c.Key = true;
        }

        private static void AddColumnInfo(TableInfo tableInfo, PropertyInfo propertyInfo, ColumnAttribute columnAttribute)
        {
            tableInfo.AddColumn(new ColumnInfo
            {
                Name = columnAttribute.Name,
                DotNetType = propertyInfo.PropertyType,
                Key = false,
                Order = columnAttribute.Order,
                PropertyInfo = propertyInfo,
                DatabaseGenerated = (propertyInfo.GetCustomAttribute<ReadOnlyAttribute>() != null) ? true : false,
                EnumColumnBehaviour = columnAttribute.EnumColumnBehaviour
            });
        }

        private static TAttribute GetAttribute<TAttribute>(PropertyInfo propertyInfo) where TAttribute : Attribute
        {
            return GetAttributes<TAttribute>(propertyInfo).FirstOrDefault();
        }

        private static IEnumerable<TAttribute> GetAttributes<TAttribute>(PropertyInfo propertyInfo) where TAttribute : Attribute
        {
            var attributes = Attribute.GetCustomAttributes(propertyInfo, typeof(TAttribute));
            return attributes.Select(a => (TAttribute)a);
        }
    }
}
