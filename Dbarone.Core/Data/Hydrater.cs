using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Data
{
    public class Hydrater
    {
        public Hashtable GetHashTable(IDataReader dataReader)
        {
            var values = new Hashtable(StringComparer.InvariantCultureIgnoreCase);

            for (int i = 0; i < dataReader.FieldCount; i++)
                values.Add(dataReader.GetName(i), dataReader.GetValue(i));

            return values;
        }

        public T GetEntity<T>(IDataReader reader) where T : IEntity
        {
            return CreateEntityFromValues<T>(GetHashTable(reader));
        }

        private T CreateEntityFromValues<T>(Hashtable values) where T : IEntity
        {
            var entity = Activator.CreateInstance<T>();
            Hydrate(entity, values);
            return entity;
        }

        private void Hydrate<T>(T entity, Hashtable values) where T : IEntity
        {
            var tableInfo = MetaDataStore.GetTableInfoFor<T>();

            var properties = typeof(T).GetProperties();
            foreach (var propertyInfo in properties)
            {
                if (propertyInfo.CanWrite)
                {
                    var columnInfo = tableInfo.GetColumn(propertyInfo);
                    if (columnInfo != null)
                    {
                        object value = values[columnInfo.Name];
                        if (value is DBNull) value = null;

                        // If enum column, convert database value to enum
                        if (propertyInfo.PropertyType.IsEnum)
                        {
                            value = Enum.Parse(propertyInfo.PropertyType, value.ToString());
                        }

                        propertyInfo.SetValue(entity, value, null);
                    }
                }
            }
        }
    }
}
