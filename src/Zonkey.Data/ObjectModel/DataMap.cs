using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;

namespace Zonkey.ObjectModel
{
    /// <summary>
    /// Provides methods and properties for mapping a property in a <see cref="Zonkey.ObjectModel.DataClass"/> to a field in a database table.
    /// </summary>
    public class DataMap
    {
        private static readonly Dictionary<string, DataMap> _mapCache = new Dictionary<string, DataMap>();

        private readonly Dictionary<int, IDataMapField> _propsToFieldsToken;
        private readonly Dictionary<string, IDataMapField> _propsToFieldsName;

        private readonly Dictionary<string, IDataMapField> _dataFieldsDict;
        private readonly Dictionary<string, IDataMapField> _readableFieldsDict;
        private readonly List<IDataMapField> _keyFields, _allKeys;

        private readonly TypeInfo _typeInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="Zonkey.ObjectModel.DataMap"/> class.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        public DataMap(Type objectType)
        {
            ObjectType = objectType;
            _typeInfo = objectType.GetTypeInfo();

            var keyComparer = StringComparer.CurrentCultureIgnoreCase;
            _readableFieldsDict = new Dictionary<string, IDataMapField>(keyComparer);
            _dataFieldsDict = new Dictionary<string, IDataMapField>(keyComparer);

            _propsToFieldsToken = new Dictionary<int, IDataMapField>();
            _propsToFieldsName = new Dictionary<string, IDataMapField>();

            _keyFields = new List<IDataMapField>();
            _allKeys = new List<IDataMapField>();
        }

        /// <summary>
        /// Gets or sets the type of the object.
        /// </summary>
        /// <value>The type of the object.</value>
        public Type ObjectType { get; }

        /// <summary>
        /// Gets the IDataMapItem for this instance
        /// </summary>
        public IDataMapItem DataItem { get; set; }

        /// <summary>
        /// Gets an IList of all fields
        /// </summary>
        public ICollection<IDataMapField> DataFields => _dataFieldsDict.Values;

        /// <summary>
        /// Gets an IList of all fields
        /// </summary>
        public ICollection<IDataMapField> ReadableFields => _readableFieldsDict.Values;

        /// <summary>
        /// Gets an IList of the key fields.
        /// </summary>
        /// <value>The key fields.</value>
        public IList<IDataMapField> KeyFields => _keyFields;

        /// <summary>
        /// Gets an IList of the key fields and partition keys.
        /// </summary>
        /// <value>The key fields and partition keys.</value>
        public IList<IDataMapField> AllKeys => _allKeys;

        public JoinDefinition JoinDefinition { get; set; }

        /// <summary>
        /// Gets the data fields for the given field name
        /// </summary>
        /// <param name="fieldName">the field name</param>
        /// <returns>A <see cref="Zonkey.ObjectModel.IDataMapField"/> object.</returns>
        public IDataMapField this[string fieldName] => (_dataFieldsDict.ContainsKey(fieldName)) ? _dataFieldsDict[fieldName] : null;

        /// <summary>
        /// Gets the data field for a given property
        /// </summary>
        /// <param name="pi"></param>
        /// <returns>A <see cref="Zonkey.ObjectModel.IDataMapField"/> object.</returns>
        public IDataMapField GetFieldForProperty(PropertyInfo pi)
        {
            if (_propsToFieldsToken.TryGetValue(pi.MetadataToken, out IDataMapField field))
                return field;

            if (_propsToFieldsName.TryGetValue(pi.Name, out field))
                return field;

            return null;
        }

        /// <summary>
        /// Gets the readable data field for a supplied name
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns>A <see cref="System.Reflection.PropertyInfo"/> object.</returns>
        public IDataMapField GetReadableField(string fieldName)
        {
            return (_readableFieldsDict.ContainsKey(fieldName)) ? _readableFieldsDict[fieldName] : null;
        }


        /// <summary>
        /// Gets the readable object property for a supplied name
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns>A <see cref="System.Reflection.PropertyInfo"/> object.</returns>
        public PropertyInfo GetReadableProperty(string fieldName)
        {
            return (_readableFieldsDict.ContainsKey(fieldName)) ? _readableFieldsDict[fieldName].Property : null;
        }

        #region AddField Methods

        /// <overloads>Adds a field to the Data Map</overloads>
        /// <summary>
        /// Adds the field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="dbType">Type of the db.</param>
        /// <returns>A <see cref="Zonkey.ObjectModel.IDataMapField"/> object.</returns>
        public IDataMapField AddField(string fieldName, DbType dbType)
        {
            return AddField(fieldName, fieldName, dbType);
        }

        /// <summary>
        /// Adds the field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="dbType">Type of the db.</param>
        /// <param name="isNullable">if set to <c>true</c>, then the database field is nullable.</param>
        /// <returns>A <see cref="Zonkey.ObjectModel.IDataMapField"/> object.</returns>
        public IDataMapField AddField(string fieldName, DbType dbType, bool isNullable)
        {
            return AddField(fieldName, fieldName, dbType, isNullable);
        }

        /// <summary>
        /// Adds the field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="dbType">Type of the db.</param>
        /// <param name="isNullable">if set to <c>true</c>, then the database field is nullable.</param>
        /// <param name="length">The length of the field.</param>
        /// <returns>A <see cref="Zonkey.ObjectModel.IDataMapField"/> object.</returns>
        public IDataMapField AddField(string fieldName, DbType dbType, bool isNullable, int length)
        {
            return AddField(fieldName, fieldName, dbType, isNullable, length);
        }

        /// <summary>
        /// Adds the field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="dbType">Type of the db.</param>
        /// <param name="isNullable">if set to <c>true</c>, then the database field is nullable.</param>
        /// <param name="length">The length of the field.</param>
        /// <param name="accessType">Type of the access.</param>
        /// <returns>A <see cref="Zonkey.ObjectModel.IDataMapField"/> object.</returns>
        public IDataMapField AddField(string fieldName, DbType dbType, bool isNullable, int length, AccessType accessType)
        {
            return AddField(fieldName, fieldName, dbType, isNullable, length, accessType);
        }

        /// <summary>
        /// Adds the field.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="dbType">Type of the db.</param>
        /// <returns>A <see cref="Zonkey.ObjectModel.IDataMapField"/> object.</returns>
        public IDataMapField AddField(string propertyName, string fieldName, DbType dbType)
        {
            if (string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(propertyName));
            if (string.IsNullOrEmpty(fieldName)) throw new ArgumentNullException(nameof(fieldName));

            PropertyInfo pi = _typeInfo.GetProperty(propertyName);
            if (pi == null) throw new ArgumentException("invalid property name");

            var field = new DataMapField(pi, fieldName, dbType, true);

            AppendField(field);
            return field;
        }

        /// <summary>
        /// Adds the field.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="dbType">Type of the db.</param>
        /// <param name="isNullable">if set to <c>true</c>, then the database field is nullable.</param>
        /// <returns>A <see cref="Zonkey.ObjectModel.IDataMapField"/> object.</returns>
        public IDataMapField AddField(string propertyName, string fieldName, DbType dbType, bool isNullable)
        {
            if (string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(propertyName));
            if (string.IsNullOrEmpty(fieldName)) throw new ArgumentNullException(nameof(fieldName));

            PropertyInfo pi = _typeInfo.GetProperty(propertyName);
            if (pi == null) throw new ArgumentException("invalid property name");

            var field = new DataMapField(pi, fieldName, dbType, isNullable);

            AppendField(field);
            return field;
        }

        /// <summary>
        /// Adds the field.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="dbType">Type of the db.</param>
        /// <param name="isNullable">if set to <c>true</c>, then the database field is nullable.</param>
        /// <param name="length">The length of the field.</param>
        /// <returns>A <see cref="Zonkey.ObjectModel.IDataMapField"/> object.</returns>
        public IDataMapField AddField(string propertyName, string fieldName, DbType dbType, bool isNullable, int length)
        {
            if (string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(propertyName));
            if (string.IsNullOrEmpty(fieldName)) throw new ArgumentNullException(nameof(fieldName));

            PropertyInfo pi = _typeInfo.GetProperty(propertyName);
            if (pi == null) throw new ArgumentException("invalid property name");

            var field = new DataMapField(pi, fieldName, dbType, isNullable)
                            {
                                Length = length
                            };

            AppendField(field);
            return field;
        }

        /// <summary>
        /// Adds the field.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="dbType">Type of the db.</param>
        /// <param name="isNullable">if set to <c>true</c>, then the database field is nullable.</param>
        /// <param name="length">The length of the field.</param>
        /// <param name="accessType">Type of the access.</param>
        /// <returns>A <see cref="Zonkey.ObjectModel.IDataMapField"/> object.</returns>
        public IDataMapField AddField(string propertyName, string fieldName, DbType dbType, bool isNullable, int length, AccessType accessType)
        {
            if (string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(propertyName));
            if (string.IsNullOrEmpty(fieldName)) throw new ArgumentNullException(nameof(fieldName));

            PropertyInfo pi = _typeInfo.GetProperty(propertyName);
            if (pi == null) throw new ArgumentException("invalid property name");

            var field = new DataMapField(pi, fieldName, dbType, isNullable)
                            {
                                AccessType = accessType, 
                                Length = length
                            };

            AppendField(field);
            return field;
        }

        /// <summary>
        /// Adds the field.
        /// </summary>
        /// <param name="field">The field.</param>
        public void AddField(IDataMapField field)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            if (field.Property == null) throw new ArgumentException("field.Property cannot be null");

            AppendField(field);
        }

        #endregion

        #region AddKeyField Methods

        /// <summary>
        /// Adds the key field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="dbType">Type of the db.</param>
        /// <returns>A <see cref="Zonkey.ObjectModel.IDataMapField"/> object.</returns>
        public IDataMapField AddKeyField(string fieldName, DbType dbType)
        {
            return AddKeyField(fieldName, fieldName, dbType);
        }

        /// <summary>
        /// Adds the key field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="dbType">Type of the db.</param>
        /// <param name="length">The lengthof the field.</param>
        /// <returns>A <see cref="Zonkey.ObjectModel.IDataMapField"/> object.</returns>
        public IDataMapField AddKeyField(string fieldName, DbType dbType, int length)
        {
            return AddKeyField(fieldName, fieldName, dbType, length);
        }

        /// <summary>
        /// Adds the key field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="dbType">Type of the db.</param>
        /// <param name="isAutoIncrement">if set to <c>true</c>, then the key field is an autoincrement field.</param>
        /// <returns>A <see cref="Zonkey.ObjectModel.IDataMapField"/> object.</returns>
        public IDataMapField AddKeyField(string fieldName, DbType dbType, bool isAutoIncrement)
        {
            return AddKeyField(fieldName, fieldName, dbType, isAutoIncrement);
        }

        /// <summary>
        /// Adds the key field.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="dbType">Type of the db.</param>
        /// <returns>A <see cref="Zonkey.ObjectModel.IDataMapField"/> object.</returns>
        public IDataMapField AddKeyField(string propertyName, string fieldName, DbType dbType)
        {
            if (string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(propertyName));
            if (string.IsNullOrEmpty(fieldName)) throw new ArgumentNullException(nameof(fieldName));

            PropertyInfo pi = _typeInfo.GetProperty(propertyName);
            if (pi == null) throw new ArgumentException("invalid property name");

            var field = new DataMapField(pi, fieldName, dbType, false)
                            {
                                IsKeyField = true
                            };

            AppendField(field);
            return field;
        }

        /// <summary>
        /// Adds the key field.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="dbType">Type of the db.</param>
        /// <param name="length">The length of the field.</param>
        /// <returns>A <see cref="Zonkey.ObjectModel.IDataMapField"/> object.</returns>
        public IDataMapField AddKeyField(string propertyName, string fieldName, DbType dbType, int length)
        {
            if (string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(propertyName));
            if (string.IsNullOrEmpty(fieldName)) throw new ArgumentNullException(nameof(fieldName));

            PropertyInfo pi = _typeInfo.GetProperty(propertyName);
            if (pi == null) throw new ArgumentException("invalid property name");

            var field = new DataMapField(pi, fieldName, dbType, false)
                            {
                                IsKeyField = true, 
                                Length = length
                            };

            AppendField(field);
            return field;
        }

        /// <summary>
        /// Adds the key field.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="dbType">Type of the db.</param>
        /// <param name="isAutoIncrement">if set to <c>true</c>, then the key field is an autoincrement field.</param>
        /// <returns>A <see cref="Zonkey.ObjectModel.IDataMapField"/> object.</returns>
        public IDataMapField AddKeyField(string propertyName, string fieldName, DbType dbType, bool isAutoIncrement)
        {
            if (string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(propertyName));
            if (string.IsNullOrEmpty(fieldName)) throw new ArgumentNullException(nameof(fieldName));

            PropertyInfo pi = _typeInfo.GetProperty(propertyName);
            if (pi == null) throw new ArgumentException("invalid property name");

            var field = new DataMapField(pi, fieldName, dbType, false)
                            {
                                IsAutoIncrement = isAutoIncrement, 
                                IsKeyField = true
                            };

            AppendField(field);
            return field;
        }

        #endregion

        #region AddRowVersionField Methods

        /// <summary>
        /// Adds the row version field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="dbType">Type of the db.</param>
        /// <param name="length">The length of the field.</param>
        /// <returns>A <see cref="Zonkey.ObjectModel.IDataMapField"/> object.</returns>
        public IDataMapField AddRowVersionField(string fieldName, DbType dbType, int length)
        {
            return AddRowVersionField(fieldName, fieldName, dbType, length);
        }

        /// <summary>
        /// Adds the row version field.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="dbType">Type of the db.</param>
        /// <param name="length">The length of the field.</param>
        /// <returns>A <see cref="Zonkey.ObjectModel.IDataMapField"/> object.</returns>
        public IDataMapField AddRowVersionField(string propertyName, string fieldName, DbType dbType, int length)
        {
            if (string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(propertyName));
            if (string.IsNullOrEmpty(fieldName)) throw new ArgumentNullException(nameof(fieldName));

            PropertyInfo pi = _typeInfo.GetProperty(propertyName);
            if (pi == null) throw new ArgumentException("invalid property name");

            var field = new DataMapField(pi, fieldName, dbType, false)
                            {
                                IsRowVersion = true, 
                                Length = length
                            };

            AppendField(field);
            return field;
        }

        /// <summary>
        /// Adds the row version field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A <see cref="Zonkey.ObjectModel.IDataMapField"/> object.</returns>
        public IDataMapField AddRowVersionField(string fieldName)
        {
            return AddRowVersionField(fieldName, fieldName);
        }

        /// <summary>
        /// Adds the row version field.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A <see cref="Zonkey.ObjectModel.IDataMapField"/> object.</returns>
        public IDataMapField AddRowVersionField(string propertyName, string fieldName)
        {
            if (string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(propertyName));
            if (string.IsNullOrEmpty(fieldName)) throw new ArgumentNullException(nameof(fieldName));

            PropertyInfo pi = _typeInfo.GetProperty(propertyName);
            if (pi == null) throw new ArgumentException("invalid property name");

            var field = new DataMapField(pi, fieldName, DbType.Binary, false)
                            {
                                IsRowVersion = true, 
                                Length = 8
                            };

            AppendField(field);
            return field;
        }

        #endregion

        /// <summary>
        /// Appends the field to the data map
        /// </summary>
        /// <param name="field"></param>
        private void AppendField(IDataMapField field)
        {
            if (_dataFieldsDict.ContainsKey(field.FieldName))
                throw new Exception($"Field '{field.FieldName}' already exists in _dataFieldsDict");                        
            _dataFieldsDict.Add(field.FieldName, field);

            if (_propsToFieldsToken.ContainsKey(field.Property.MetadataToken))
                throw new Exception($"Property '{field.Property}' already exists in _propsToFieldsToken");                        
            _propsToFieldsToken.Add(field.Property.MetadataToken, field);

            if (_propsToFieldsName.ContainsKey(field.Property.Name))
                throw new Exception($"Property '{field.Property}' already exists in _propsToFieldsName");
            _propsToFieldsName.Add(field.Property.Name, field);

            if (field.IsKeyField)
            {
                if (_keyFields.Contains(field))
                    throw new Exception($"Field '{field}' already exists in KeyFields");                        

                _keyFields.Add(field);
                _allKeys.Add(field);
            }
            else if (field.IsPartitionKey)
            {
                if (_allKeys.Contains(field))
                    throw new Exception($"Field '{field}' already exists in AllFields");

                _allKeys.Add(field);
            }

            if (field.AccessType != AccessType.WriteOnly)
            {
                if (_readableFieldsDict.ContainsKey(field.FieldName))
                    throw new Exception($"Field '{field.FieldName}' already exists in _readableFieldsDict");                        

                _readableFieldsDict.Add(field.FieldName, field);
            }
        }

        /// <summary>
        /// Removes the field from the data map.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        public void RemoveField(string fieldName)
        {
            IDataMapField field = this[fieldName];
            if (field == null) return;

            _dataFieldsDict.Remove(field.FieldName);
            _propsToFieldsToken.Remove(field.Property.MetadataToken);
            _propsToFieldsName.Remove(field.Property.Name);

            if (field.IsKeyField) _keyFields.Remove(field);
            if (field.IsKeyField || field.IsPartitionKey) _allKeys.Remove(field);

            if (field.AccessType != AccessType.WriteOnly)
                _readableFieldsDict.Remove(field.FieldName);
        }

        /// <summary>
        /// Determines whether the DataMap contains a field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        ///   <c>true</c> if the specified field name contains field; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsField(string fieldName)
        {
            return _dataFieldsDict.ContainsKey(fieldName);
        }

        /// <summary>
        /// Generates the new DataMap.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static DataMap GenerateNew(Type type)
        {
            return GenerateNew(type, null, null, 0);
        }

        /// <summary>
        /// Generates the new DataMap.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="schemaVersion">The schema version</param>
        /// <returns></returns>
        public static DataMap GenerateNew(Type type, int schemaVersion)
        {
            return GenerateNew(type, null, null, schemaVersion);
        }

        /// <summary>
        /// Generates the new DataMap.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="keyFields">The key fields.</param>
        /// <returns></returns>
        public static DataMap GenerateNew(Type type, string tableName, string[] keyFields)
        {
            return GenerateNew(type, tableName, keyFields, 0);
        }

        /// <summary>
        /// Generates the new DataMap.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="keyFields">The key fields.</param>
        /// <param name="schemaVersion">The schema version.</param>
        /// <returns></returns>
        public static DataMap GenerateNew(Type type, string tableName, string[] keyFields, int schemaVersion)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (keyFields == null)
                keyFields = new[] { "-" };

            return GenerateMapInternal(type, tableName, keyFields, schemaVersion);
        }

        /// <summary>
        /// Generates the cached DataMap.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static DataMap GenerateCached(Type type)
        {
            return GenerateCached(type, null, null, 0);
        }

        /// <summary>
        /// Generates the cached DataMap.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="schemaVersion">The schema version.</param>
        /// <returns></returns>
        public static DataMap GenerateCached(Type type, int schemaVersion)
        {
            return GenerateCached(type, null, null, schemaVersion);
        }

        /// <summary>
        /// Generates the cached DataMap.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="keyFields">The key fields.</param>
        /// <returns></returns>
        public static DataMap GenerateCached(Type type, string tableName, string[] keyFields)
        {
            return GenerateCached(type, tableName, keyFields, 0);
        }

        /// <summary>
        /// Generates the cached DataMap.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="keyFields">The key fields.</param>
        /// <param name="schemaVersion">The schema version.</param>
        /// <returns></returns>
        public static DataMap GenerateCached(Type type, string tableName, string[] keyFields, int schemaVersion)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (keyFields == null)
                keyFields = new[] { "-" };

            string sMapKey = String.Concat(type.FullName, "|", (tableName ?? "-"), "|", String.Join(",", keyFields), "|", schemaVersion);
            if (_mapCache.TryGetValue(sMapKey, out DataMap map))
                return map;

            lock (_mapCache)
            {   // this code should only execute once per process

                // try again now that we have the lock, make it thread-safe
                if (_mapCache.TryGetValue(sMapKey, out map))
                    return map;

                map = GenerateMapInternal(type, tableName, keyFields, schemaVersion);

                _mapCache.Add(sMapKey, map);
                return map;
            }
        }

        private static DataMap GenerateMapInternal(Type type, string tableName, IEnumerable<string> keyFields, int schemaVersion)
        {
            if (schemaVersion == 0)
                schemaVersion = int.MaxValue;

            if (keyFields == null)
                keyFields = new string[0];

            var map = new DataMap(type);

            IDataMapItem theDataItem;

            // check if Join
            var theDataJoin = DataJoinAttribute.GetFromType(type);
            if (theDataJoin != null)
            {
                // get the join definition
                var joinDef = (JoinDefinition)Activator.CreateInstance(theDataJoin.JoinDefinition);
                map.JoinDefinition = joinDef;

                // get or create data item from join table 0
                theDataItem = DataItemAttribute.GetFromType(joinDef.JoinTypes[0]);
            }
            else
            {
                // get or create data item
                theDataItem = DataItemAttribute.GetFromType(type);
            }

            if (theDataItem != null)
            {
                if (! string.IsNullOrEmpty(tableName))
                    theDataItem.TableName = tableName;
            }
            else
                theDataItem = new DataMapItem(tableName, true);

            // assign to data map
            map.DataItem = theDataItem;

            // get property filter
            BindingFlags flags = theDataItem.IncludePrivateProperties
                ? BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                : BindingFlags.Instance | BindingFlags.Public;

            // create fields from properties
            foreach (var pi in type.GetTypeInfo().GetProperties(flags))
            {
                IDataMapField field = DataFieldAttribute.GetFromProperty(pi);

                if ( (field == null) && theDataItem.ImplicitFieldDefinition)
                {
                    field = new DataMapField(pi, pi.Name, DataManager.GetDbType(pi.PropertyType), true);
                        
                    if (keyFields.Contains(field.FieldName, StringComparer.OrdinalIgnoreCase)) 
                        field.IsKeyField = true;

                    // special handling of rowVersion field
                    if ( (pi.PropertyType == typeof(byte[])) && field.FieldName.Equals("rowVersion", StringComparison.OrdinalIgnoreCase) )
                        field.IsRowVersion = true;

                    // special handling of UTC fields (not in 4.0)
                    if (((pi.PropertyType == typeof(DateTime)) || (pi.PropertyType == typeof(DateTime?))) && field.FieldName.EndsWith("UTC", StringComparison.OrdinalIgnoreCase))
                        field.DateTimeKind = DateTimeKind.Utc;
                }

                if ( (field != null) && (field.SchemaVersion <= schemaVersion) )
                    map.AppendField(field);
            }

            return map;
        }
    }
}