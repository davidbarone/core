using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Dbarone.Data
{
    public class TableInfo
    {
        public Dictionary<CrudOperation, string> TableName = new Dictionary<CrudOperation, string>();
        //public string Name { get; set; }
        public Type EntityType { get; set; }

        private Dictionary<string, ColumnInfo> columns = null;

        public IEnumerable<ColumnInfo> Columns
        {
            get
            {
                return columns!=null?columns.Values:null;
            }
        }

        public void AddColumn(ColumnInfo column)
        {
            if (columns == null)
            {
                columns = new Dictionary<string, ColumnInfo>();
            }
            if (columns.ContainsKey(column.Name))
            {
                throw new InvalidOperationException(string.Format("An item with key {0} has already been added", column.Name));
            }

            columns.Add(column.Name, column);
        }

        public ColumnInfo GetColumn(PropertyInfo propertyInfo)
        {
            return columns.Values.FirstOrDefault(c => c.PropertyInfo == propertyInfo);
        }

        public ColumnInfo GetColumn(string columnName)
        {
            if (!columns.ContainsKey(columnName))
            {
                throw new InvalidOperationException(string.Format("The entity type {0} does not have a {1} column", EntityType, columnName));
            }

            return columns[columnName];
        }
    }
}
