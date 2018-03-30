using Dbarone.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Dbarone.Core
{
    public static class ExtensionMethods
    {
        #region Xml

        public static string ObjectToXmlString(this object obj)
        {
            XmlSerializer xs = new XmlSerializer(obj.GetType());
            System.IO.StringWriter sw = new System.IO.StringWriter();
            XmlWriter writer = XmlWriter.Create(sw);
            xs.Serialize(writer, obj);
            var xml = sw.ToString();
            return xml;
        }

        public static T XmlStringToObject<T>(this string str)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            StreamReader reader = new StreamReader(str.ToStream());
            var obj = serializer.Deserialize(reader);
            reader.Close();
            return (T)obj;
        }

        #endregion


        #region Arrays

        public class PrettyPrintColumn
        {
            public string PropertyName { get; set; }
            public string Title { get; set; }
            public int Width { get; set; }
            public Justification Justification { get; set; }
        }

        /// <summary>
        /// Pretty prints a single object or Enumeration of objects by reflecting through
        /// the properties. The pretty print based on 132 column display.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public static string PrettyPrint(this object source, IList<PrettyPrintColumn> columns = null)
        {
            Type t = source.GetType();
            StringBuilder sb = new StringBuilder();
            if (columns != null && columns.Count()>0)
            {
                sb.AppendLine(string.Join(" ", columns.Select(c => c.Title.Justify(c.Width, c.Justification))));
                sb.AppendLine(string.Join(" ", columns.Select(c => "".PadRight(c.Width, '-'))));
            }

            var sourceAsEnumerable = source as IEnumerable;
            if (sourceAsEnumerable != null)
            {
                // IEnumerable - render table
                // Each row allows for text wrapping
                foreach (var item in sourceAsEnumerable)
                {
                    var elementType = item.GetType();
                    List<List<string>> rowValues = new List<List<string>>();
                    foreach (var column in columns)
                    {
                        List<string> cell = new List<string>();
                        rowValues.Add(cell);
                        // First split cells by new line
                        var value = (elementType.GetProperty(column.PropertyName).GetValue(item) ?? string.Empty).ToString();
                        var valueRows = value.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                        foreach (var row in valueRows)
                        {
                            foreach (var wrappedLine in row.WordWrap(column.Width))
                            {
                                cell.Add(wrappedLine);
                            }
                        }
                    }

                    // At this point check the max height of the rowValues array.
                    int maxHeight = rowValues.Max(r => r.Count());
                    foreach (var column in columns)
                    {
                        var i = columns.IndexOf(column);
                        var k = rowValues[i].Count();
                        for (int j = k; j < maxHeight; j++)
                            rowValues[i].Add(string.Empty);
                    }

                    // At this point, all rowValues have same dimensionality
                    // Just pad values to cell widths
                    foreach (var column in columns)
                    {
                        var i = columns.IndexOf(column);
                        for (int j = 0; j < maxHeight; j++)
                            rowValues[i][j] = rowValues[i][j].Justify(column.Width, column.Justification);
                    }

                    for (int i = 0; i < maxHeight; i++)
                    {
                        sb.AppendLine(string.Join(" ", columns.Select(c => rowValues[columns.IndexOf(c)][i])));
                    }
                }
            }
            else
            {
                const int keyWidth = 30;
                const int valueWidth = 132 - 30 - 1;
                sb.AppendLine(string.Format("{0} {1}", "Name".Justify(keyWidth, Justification.LEFT), "Description".Justify(keyWidth, Justification.LEFT)));
                sb.AppendLine(string.Format("{0} {1}", "".PadRight(keyWidth, '-'), "".PadRight(valueWidth, '-')));

                // Single object - iterate properties
                Hashtable ht = new Hashtable();
                foreach (var property in t.GetProperties())
                    ht[property.Name] = (property.GetValue(source) ?? string.Empty).ToString().WordWrap(valueWidth).ToList();

                foreach (var key in ht.Keys)
                {
                    List<string> k = new List<string>();
                    k.Add(key.ToString());
                    var lines = (IList<string>)ht[key];
                    for (int i = 1; i <= ((IList<string>)ht[key]).Count(); i++)
                        k.Add(string.Empty);
                    
                    for (int i=0; i< ((IList<string>)ht[key]).Count(); i++)
                        sb.AppendLine(string.Format("{0} {1}", k[i].ToString().Justify(keyWidth, Justification.LEFT), ((IList<string>)ht[key])[i].Justify(valueWidth, Justification.LEFT)));
                }
            }
            // remove trailing newline
            var ret = sb.ToString();
            if (ret.Length > 0)
                ret = ret.Substring(0, ret.Length - Environment.NewLine.Length);
            return ret;
        }

        /// <summary>
        /// Splices an array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="start"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        public static T[] Splice<T>(this T[] source, long start, long? number = null)
        {
            var len = source.Length;
            var c = len - start;
            if (number.HasValue)
                c = number.Value;
            T[] result = new T[c];

            while (c > 0)
            {
                result[c - 1] = source[start + c - 1];
                c--;
            }
            return result;
        }

        #endregion

        #region Types

        /// <summary>
        /// Determines whether an object is a certain type.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public static bool CanConvertTo(this object input, Type targetType)
        {
            try
            {
                TypeDescriptor.GetConverter(targetType).ConvertFrom(input);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsNumeric(this object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        #endregion

        #region String

        /// <summary>
        /// Returns time part of Guid which should be relatively
        /// unique locally. Used to enable users to specify a
        /// shorter string to select a unique record.
        /// </summary>
        /// <param name="g"></param>
        /// <returns></returns>
        public static string TimeLow(this Guid g)
        {
            return g.ToString("N").Substring(0,8);
        }

        /// <summary>
        /// Allows a short (TimeLow) guid or full guid to be converted to Guid
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static Guid ToGuid(this string s)
        {
            if (s.Length == 8)
                return new Guid(string.Format("{0}-0000-0000-0000-000000000000", s));
            else
                return new Guid(s);
        }

        public enum Justification
        {
            LEFT,
            CENTRE,
            RIGHT
        }

        public static string Justify(this string s, int length, Justification justification)
        {
            if (s.Length > length)
                s = s.Substring(0, length);

            if (justification == Justification.LEFT)
                return s.PadRight(length);
            else if (justification == Justification.CENTRE)
                return (" " + s.PadRight(length / 2).PadLeft(length / 2)).PadRight(length);
            else
                return s.PadLeft(length);
        }

        /// <summary>
        /// Parses a string for arguments. Arguments can be
        /// separated by whitespace. Single or double quotes
        /// can be used to delimit fields that contain space
        /// characters.
        /// </summary>
        public static string[] ParseArgs(this string s)
        {
            List<string> args = new List<string>();
            string currentArg = "";
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(s ?? "")))
            {
                using (var sr = new StreamReader(ms))
                {
                    bool inWhiteSpace = false;
                    char inQuoteChar = '\0';
                    char nextChar;
                    while (!sr.EndOfStream)
                    {
                        nextChar = (char)sr.Read();
                        if (inQuoteChar == '\0' && (nextChar == '\'' || nextChar == '"'))
                        {
                            // Start of quoted field
                            inQuoteChar = nextChar;
                            currentArg = "";
                        }
                        else if (nextChar == inQuoteChar && nextChar != '\0')
                        {
                            // End of quoted field
                            // The end of quoted field MUST be followed by whitespace.
                            args.Add(currentArg);
                            inQuoteChar = '\0';
                        }
                        else if (!inWhiteSpace && inQuoteChar == '\0' && string.IsNullOrWhiteSpace(nextChar.ToString()))
                        {
                            // Start of whitespace, not in quoted field
                            args.Add(currentArg);
                            inWhiteSpace = true;
                        }
                        else if (inWhiteSpace && inQuoteChar == '\0' && !string.IsNullOrWhiteSpace(nextChar.ToString()))
                        {
                            // Start of new argument
                            currentArg = nextChar.ToString();
                            inWhiteSpace = false;
                        }
                        else
                        {
                            currentArg += nextChar.ToString();
                        }
                    }
                    if (!string.IsNullOrEmpty(currentArg))
                        args.Add(currentArg);
                }
            }
            return args.ToArray();
        }

        public static bool IsNullOrWhiteSpace(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        public static string RemoveRight(this string str, int length)
        {
            return str.Remove(str.Length - length);
        }

        public static string RemoveLeft(this string str, int length)
        {
            return str.Remove(0, length);
        }

        public static Stream ToStream(this string str)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(str);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// Splits a string into chunks of [length] characters. Word breaks
        /// are avoided.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static IEnumerable<String> WordWrap(this string s, int length)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (length <= 0)
                throw new ArgumentException("Part length has to be positive.", "partLength");

            var i = 0;
            while (i < s.Length)
            {
                // remove white space at start of line
                while (i < s.Length && char.IsWhiteSpace(s[i]))
                    i++;

                var j = length;   // add extra character to check white space just after line.

                while (j >= 0)
                {
                    if (i + j < s.Length && char.IsWhiteSpace(s[i + j]))
                        break;
                    else if (i + j == s.Length)
                        break;
                    j--;
                }
                if (j <= 0 || j > length)
                    j = length;

                if (i + j >= s.Length)
                    j = s.Length - i;

                var result = s.Substring(i, j);
                i += j;
                yield return result;
            }
        }

        #endregion

        #region StringBuilder

        public static StringBuilder RemoveRight(this StringBuilder sb, int length)
        {
            return sb.Remove(sb.Length - length, length);
        }

        public static StringBuilder RemoveLeft(this StringBuilder sb, int length)
        {
            return sb.Remove(0, length);
        }

        #endregion

        #region Reflection

        /// <summary>
        /// Returns a list of properties decorated by the specified attribute.
        /// </summary>
        /// <typeparam name="T">The specified attribute.</typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static PropertyInfo[] GetPropertiesDecoratedBy<T>(this object obj, bool inherit = false) where T : Attribute
        {
            return obj.GetType()
                .GetProperties()
                .Where(pi => Attribute.IsDefined(pi, typeof(T), inherit))
                .ToArray();
        }

        public static PropertyInfo[] GetPropertiesDecoratedBy<T>(this Type t, bool inherit = false)
        {
            return t
                .GetProperties()
                .Where(pi => Attribute.IsDefined(pi, typeof(T), inherit))
                .ToArray();
        }

        public static MemberInfo[] GetMembersDecoratedBy<T>(this object obj) where T : Attribute
        {
            return obj.GetType()
                .GetMembers()
                .Where(pi => Attribute.IsDefined(pi, typeof(T), false))
                .ToArray();
        }

        /// <summary>
        /// Returns a list of methods decorated by the specified attribute.
        /// </summary>
        /// <typeparam name="T">The specified attribute.</typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static MethodInfo[] GetMethodsDecoratedBy<T>(this object obj) where T : Attribute
        {
            return obj.GetType()
                .GetMethods()
                .Where(pi => Attribute.IsDefined(pi, typeof(T), false))
                .ToArray();
        }

        /// <summary>
        /// Gets the value of a property using reflection.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public static object Value(this object obj, string propertyName)
        {
            Type t = obj.GetType();
            return t.GetProperty(propertyName).GetValue(obj, null);
        }

        /// <summary>
        /// Returns a dictionary of concrete types that implement a base type within
        /// an app domain. Typically used as AppDomain.CurrentDomain.GetTypesImplementing().
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="domain"></param>
        /// <returns></returns>
        public static IDictionary<string, Type> GetTypesImplementing(this AppDomain domain, Type baseType)
        {
            Dictionary<string, Type> types = new Dictionary<string, Type>();
            // Get all the command types:
            foreach (var assembly in domain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes().Where(t => baseType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract))
                    {
                        // Remove word 'Adapter' from class name if present
                        var name = Regex.Replace(type.Name, "command", "", RegexOptions.IgnoreCase);
                        if (!types.ContainsKey(name))
                            types.Add(name.ToLower(), type);
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                }
            }
            return types;
        }


        #endregion

        #region DataTypes

        /// <summary>
        /// Supports a parse method for nullable types.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        /// <param name="parseMethod"></param>
        /// <returns></returns>
        public static Nullable<T> NullableParse<T>(string expression, ParseDelegate<T> parseMethod) where T : struct
        {
            if (string.IsNullOrEmpty(expression))
                return null;
            else
            {
                try
                {
                    MethodInfo mi = typeof(T).GetMethod("Parse");
                    if (mi != null)
                    {
                        var del = (ParseDelegate<T>)Delegate.CreateDelegate(typeof(ParseDelegate<T>), mi);
                        return del(expression);
                    }
                    else
                        throw new ApplicationException("Type does not support the Parse method.");
                }
                catch { return null; }
            }
        }

        /// <summary>
        /// Accesses a 'parse' function for nullable types
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public delegate T ParseDelegate<T>(string expression);

        #endregion

        #region General

        public static object Default(this Type t)
        {
            if (t.IsValueType)
            {
                return Activator.CreateInstance(t);
            }
            else
            {
                return null;
            }
        }

        public static object Extend(this object obj1, params object[] obj2)
        {
            IDictionary<string, object> obj_dict1 = new Dictionary<string, object>();
            var tableInfo1 = MetaDataStore.GetTableInfoFor(obj1.GetType());

            if (typeof(System.Dynamic.IDynamicMetaObjectProvider).IsAssignableFrom(obj1.GetType()))
            {
                // dynamic type
                if (obj1.GetType() == typeof(System.Dynamic.ExpandoObject))
                {
                    obj_dict1 = (IDictionary<string, object>)obj1;
                }
            }
            else if (typeof(IDictionary).IsAssignableFrom(obj1.GetType()))
            {
                // dictionary
                var p = obj1 as IDictionary;
                foreach (var item in p.Keys)
                {
                    obj_dict1.Add(item.ToString(), p[item]);
                }
            }
            else
            {
                foreach (PropertyInfo pi in obj1.GetType().GetProperties())
                {
                    // Check whether the property has any information
                    var key = pi.Name;
                    if (tableInfo1 != null)
                    {
                        var ci = tableInfo1.GetColumn(pi);
                        if (ci != null)
                            key = ci.Name;
                    }
                    obj_dict1.Add(key, pi.GetValue(obj1, null) ?? DBNull.Value);
                }
            }

            foreach (var obj in obj2)
            {
                IDictionary<string, object> obj_dict2 = new Dictionary<string, object>();
                var tableInfo2 = MetaDataStore.GetTableInfoFor(obj.GetType());
                if (typeof(System.Dynamic.IDynamicMetaObjectProvider).IsAssignableFrom(obj.GetType()))
                {
                    // dynamic type
                    if (obj.GetType() == typeof(System.Dynamic.ExpandoObject))
                    {
                        obj_dict2 = (IDictionary<string, object>)obj;
                    }
                }
                else if (typeof(IDictionary).IsAssignableFrom(obj.GetType()))
                {
                    // dictionary
                    var p = obj as IDictionary;
                    foreach (var item in p.Keys)
                    {
                        obj_dict2.Add(item.ToString(), p[item]);
                    }
                }
                else
                {
                    foreach (PropertyInfo pi in obj.GetType().GetProperties())
                    {
                        // Check whether the property has any information
                        var key = pi.Name;
                        if (tableInfo2 != null)
                        {
                            var ci = tableInfo2.GetColumn(pi);
                            if (ci != null)
                                key = ci.Name;
                        }
                        obj_dict2.Add(key, pi.GetValue(obj, null) ?? DBNull.Value);
                    }
                }
                // merge
                foreach (var key in obj_dict2.Keys)
                {
                    obj_dict1.Add(key, obj_dict2[key]);
                }
            }
            return obj_dict1;
        }

        #endregion
    }
}
