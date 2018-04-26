using Dbarone.Core;
using Dbarone.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Repository
{
    /// <summary>
    /// Xml-based repository implementation. Implements basic referential cascading of deletes. However, NOT ACID compliand.
    /// </summary>
    public class XmlRepository : IRepository
    {
        private class Relation
        {
            public PropertyInfo Child { get; set; }
            public PropertyInfo Parent { get; set; }
        }

        private static object lockObject = new Object();
        private static Dictionary<Type, List<object>> database = new Dictionary<Type, List<object>>();
        private IEnumerable<Relation> relations = new List<Relation>();
        private string basePath = null;

        public XmlRepository(string basePath)
        {
            this.basePath = basePath;
        }

        private string GetPath(Type type)
        {
            var path = Path.Combine(this.basePath, type.Name);
            // while we're here, check that path exists. If not, create
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        private void Initialise(Type type)
        {
            var t = type;
            lock (lockObject)
            {
                if (!database.ContainsKey(t))
                {
                    var items = new List<object>();

                    var path = GetPath(type);

                    // initialise
                    var files = Directory.GetFiles(path, "*.xml");
                    foreach (var file in files)
                    {
                        var xml = File.ReadAllText(Path.Combine(path, file));
                        items.Add(xml.XmlStringToObject(t));
                    }
                    database.Add(t, items);
                }
                relations = GetRelationships();
            }
        }

        private List<Relation> GetRelationships()
        {
            var relations = new List<Relation>();

            foreach (var type in database.Keys)
            {
                foreach (var childProperty in type.GetPropertiesDecoratedBy<ReferencesAttribute>(true))
                {
                    var attr = childProperty.GetCustomAttribute<ReferencesAttribute>(true);
                    var parentType = attr.ReferencedType;
                    // Get the 1st key field on parent. Does NOT support complex keys.
                    var parentProperties = parentType.GetPropertiesDecoratedBy<KeyAttribute>();
                    if (parentProperties.Count() != 1)
                        throw new Exception("XmlRepository only supports single column FK-PK relationships.");

                    var parentProperty = parentProperties.OrderBy(p => p.GetCustomAttribute<KeyAttribute>().Order).First();

                    relations.Add(new Relation{ Child = childProperty, Parent = parentProperty });
                }
            }
            return relations;
        }

        public IEnumerable<object> Read(Type type)
        {
            lock (lockObject)
            {
                Initialise(type);
                foreach (var item in database[type])
                    yield return item;
            }
        }

        public IEnumerable<T> Read<T>()
        {
            lock (lockObject)
            {
                Initialise(typeof(T));
                foreach (var item in Read(typeof(T)))
                    yield return (T)item;
            }
        }

        public void Upsert<T>(T item)
        {
            lock (lockObject)
            {
                Initialise(typeof(T));
                if (Read<T>().Contains(item))
                    Update<T>(item);
                else
                    Create<T>(item);
            }
        }

        /// <summary>
        /// Validates an object, checking that all properties marked with [References]
        /// attribute contain a value which corresponds to a valid parent object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private IDictionary<string, object> GetParents<T>(object obj)
        {
            Dictionary<string, object> results = new Dictionary<string, object>();
            foreach (var relation in relations.Where(r => r.Child.DeclaringType == typeof(T))){
                var value = relation.Child.GetValue(obj);
                var parent = this.Find(relation.Parent.DeclaringType, value);
                results.Add(relation.Child.Name, parent);
            }
            return results;
        }

        /// <summary>
        /// Returns true if object is pointed to by any child object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private void AssertDeleteReferentialIntegrity<T>(object obj)
        {
            foreach (var relation in relations.Where(r => r.Parent.DeclaringType == typeof(T)))
            {
                var value = relation.Parent.GetValue(obj);
                // for string, do case insensitive
                if (value.GetType() == typeof(string))
                {
                    if (this.Read(relation.Child.DeclaringType).Any(c => ((string)relation.Child.GetValue(c)).Equals((string)value, StringComparison.OrdinalIgnoreCase)))
                        throw new Exception("Cannot delete object as it has child objects.");
                }
                else
                {
                    if (this.Read(relation.Child.DeclaringType).Any(c => relation.Child.GetValue(c).Equals(value)))
                        throw new Exception("Cannot delete object as it has child objects.");
                }
            }
        }

        private void AssertForeignKeys<T>(object item)
        {
            // Invalid foreign keys
            var invalidFK = GetParents<T>(item).Where(r => r.Value == null);
            if (invalidFK.Any())
                throw new Exception(string.Format("Column [{0}] contains a value which violates referential integrity.", invalidFK.First().Key));
        }

        public void Create<T>(T item)
        {
            lock (lockObject)
            {
                Initialise(typeof(T));
                foreach (var i in database[typeof(T)])
                    if (i.Equals(item))
                        throw new Exception("Cannot add duplicate entity.");

                AssertForeignKeys<T>(item);

                // save file
                var path = GetPath(typeof(T));
                var xml = item.ObjectToXmlString();
                string fileName = string.Format("{0}.xml", item.ToString());
                string file = Path.Combine(path, fileName);
                if (File.Exists(file))
                    throw new Exception("Cannot create entity as file already exists.");

                File.WriteAllText(file, xml);
                database[typeof(T)].Add(item);
            }
        }

        public void Delete<T>(T item)
        {
            lock (lockObject)
            {
                Initialise(typeof(T));
                if (item == null)
                    throw new Exception("Cannot delete null object.");

                AssertDeleteReferentialIntegrity<T>(item);

                // delete object / file
                var path = GetPath(typeof(T));
                string fileName = string.Format("{0}.xml", item.ToString());
                string file = Path.Combine(path, fileName);
                if (!File.Exists(file))
                    throw new Exception("Cannot delete object as file storage does not exist.");

                File.Delete(file);
                database[typeof(T)].Remove(item);
            }
        }

        public void Update<T>(T item)
        {
            lock (lockObject)
            {
                Initialise(typeof(T));

                AssertForeignKeys<T>(item);

                // save file
                var path = GetPath(typeof(T));
                var xml = item.ObjectToXmlString();
                string fileName = string.Format("{0}.xml", item.ToString());
                string file = Path.Combine(path, fileName);
                File.WriteAllText(file, xml);
                var oldItem = database[typeof(T)].First(i => i.Equals(item));
                database[typeof(T)].Remove(oldItem);
                database[typeof(T)].Add(item);
            }
        }

        public object Find(Type type, params object[] values)
        {
            lock (lockObject)
            {
                Type t = type;
                Initialise(t);
                List<PropertyInfo> keys = new List<PropertyInfo>();
                var properties = t
                    .GetPropertiesDecoratedBy<KeyAttribute>()
                    .OrderBy(p => ((KeyAttribute)p.GetCustomAttribute(typeof(KeyAttribute), true)).Order)
                    .ToList();

                if (values.Length != properties.Count())
                    throw new Exception("Incorrect number of values supplied to Find() method.");

                foreach (var item in database[t])
                {
                    bool match = true;
                    for (int i = 0; i < values.Length; i++)
                    {
                        // When finding record, if property is string, do case insensitive compare
                        var propertyType = properties[i].PropertyType;
                        var value = properties[i].GetValue(item);
                        if (propertyType == typeof(string) && !((string)value).Equals((string)values[i], StringComparison.InvariantCultureIgnoreCase))
                        {
                            match = false;
                            break;
                        }
                        else if (propertyType != typeof(string) && !value.Equals(values[i]))
                        {
                            match = false;
                            break;
                        }
                    }
                    // if got here, then the item matches all values provided. Return this value.
                    if (match) return item;
                }
                return null;
            }
        }

        public T Find<T>(params object[] values)
        {
            var obj = Find(typeof(T), values);
            if (obj != null)
                return (T)obj;
            else
                return default(T);
        }

        public DateTime Created<T>(T item)
        {
            lock (lockObject)
            {
                Initialise(typeof(T));
                // get created date on file
                var path = GetPath(typeof(T));
                string fileName = string.Format("{0}.xml", item.ToString());
                string file = Path.Combine(path, fileName);
                return File.GetCreationTime(file);
            }
        }

        public DateTime Updated<T>(T item)
        {
            lock (lockObject)
            {
                Initialise(typeof(T));
                // get created date on file
                var path = GetPath(typeof(T));
                string fileName = string.Format("{0}.xml", item.ToString());
                string file = Path.Combine(path, fileName);
                return File.GetLastWriteTime(file);
            }
        }

        public void Critical(Action<IRepository> action)
        {
            lock (lockObject)
            {
                action(this);
            }
        }
    }
}
