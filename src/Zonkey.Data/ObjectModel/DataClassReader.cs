using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Zonkey.ObjectModel.Projection;

namespace Zonkey.ObjectModel
{
    /// <summary>
    /// A class that reads DCs from a DataReader
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataClassReader<T> : IEnumerable<T>, IDisposable where T : class
    {
        private readonly IProjectionMapBuilder _projectionBuilder = new ProjectionMapBuilder();
        private readonly ProjectionMap _projectionMap;
        private readonly DbDataReader _reader;
        private QuickFillInfo[] _fillInfo;
        //private Func<IDataRecord, T> _builder;
        private bool _disposed;

        private readonly Type _objectType;
        private readonly TypeInfo _typeInfo;

        private bool _isCustomFill;
        private bool _isSavable;

#if (false)
        public bool UseFastBuilder { get; set; };
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="DataClassReader&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        public DataClassReader(DbDataReader reader)
            : this(reader, DataMap.GenerateCached(typeof(T)), true)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataClassReader&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="disposeReader">if set to <c>true</c> [dispose reader].</param>
        public DataClassReader(DbDataReader reader, bool disposeReader)
            : this(reader, DataMap.GenerateCached(typeof(T)), disposeReader)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataClassReader&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="map">The map.</param>
        public DataClassReader(DbDataReader reader, DataMap map)
            : this(reader, map, true)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataClassReader&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="map">The map.</param>
        /// <param name="disposeReader">if set to <c>true</c> [dispose reader].</param>
        public DataClassReader(DbDataReader reader, DataMap map, bool disposeReader)
        {
            _objectType = typeof(T);
            _typeInfo = _objectType.GetTypeInfo();

            _projectionMap = _projectionBuilder.FromDataMap(map);
            _reader = reader;

            DisposeBaseReader = disposeReader;
            TestInterfaces();
        }

        /// <summary>
        /// Gets or sets a value indicating whether [dispose base reader].
        /// </summary>
        /// <value><c>true</c> if [dispose base reader]; otherwise, <c>false</c>.</value>
        public bool DisposeBaseReader { get; set; }

        /// <summary>
        /// Keep the reader open at the end of the cursor
        /// </summary>
        public bool KeepOpen { get; set; }

        /// <summary>
        /// Gets the base reader.
        /// </summary>
        /// <value>The base reader.</value>
        public DbDataReader BaseReader
        {
            get { return _reader; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            T item;
            while ((item = Read()) != null)
                yield return item;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            if (_disposed) return;

            if (DisposeBaseReader) 
                _reader.Dispose();

            GC.SuppressFinalize(this);
            _disposed = true;
        }

        ~DataClassReader()
        {
            Dispose();
        }

        /// <summary>
        /// Reads the next record class from the reader.
        /// </summary>
        /// <returns></returns>
        public T Read()
        {
            if (_reader.Read())
                return ReadObjectInternal();

            if (!KeepOpen) Dispose();
            return default(T);
        }

        /// <summary>
        /// Reads the next record class from the reader async.
        /// </summary>
        /// <returns></returns>
        public async Task<T> ReadAsync()
        {
            if (await _reader.ReadAsync().ConfigureAwait(false))
                return ReadObjectInternal();

            if (!KeepOpen) Dispose();
            return default(T);
        }

        private T ReadObjectInternal()
        {
            T item;
            if (_isCustomFill)
            {
                item = CreateNewT();
                ((ICustomFill)item).FillObject(_reader);
            }
            else
            {
#if (false)
                if (UseFastBuilder)
                {
                    if (_builder == null)
                        _builder = CreateBuilder(_reader);

                    item = _builder(_reader);
                }
                else
                {
                    item = BuildObject(_reader);
                }
#else
                item = BuildObject(_reader);
#endif
                if (_isSavable)
                    ((ISavable)item).CommitValues();
            }

            return item;
        }

        private T BuildObject(IDataRecord record)
        {
            var obj = CreateNewT();
            for (int i = 0; i < _fillInfo.Length; i++)
            {
                QuickFillInfo info = _fillInfo[i];
                if (info == null) continue;

                if (record.IsDBNull(i)) continue;
                object oValue = record.GetValue(i);				

                try
                {                    
                    if (!info.IsAssignable)
                    {
                        if ((info.PropertyType == typeof(Guid)) && (oValue is string))
                            info.PropertyInfo.SetValue(obj, new Guid(oValue.ToString()), null);
                        else if (info.FieldType.Name.EndsWith("SqlHierarchyId")) // if the column is a HierarchyID type, then just treat it as a string (SQL server can implicitly convert between the two)
                            info.PropertyInfo.SetValue(obj, oValue.ToString(), null);
                        else
                            info.PropertyInfo.SetValue(obj, Convert.ChangeType(oValue, info.PropertyType), null);
                    }
                    else
                        info.PropertyInfo.SetValue(obj, oValue, null);
                }
                catch (Exception ex)
                {	
                    throw new PropertyReadException(info.PropertyInfo, oValue, ex);
                }
            }

            return obj;
        }

        protected virtual T CreateNewT()
        {
            return ObjectFactory();
        }

        /// <summary>
        /// get or sets the Object factory used for creating new objects
        /// </summary>
        public Func<T> ObjectFactory
        {
            get
            {
                if (_objectFactory == null)
                {
                    lock (this)
                    {
                        if (_objectFactory != null)
                            return _objectFactory;

                        _objectFactory = ClassFactory.GetFactory<T>();
                    }
                }

                return _objectFactory;
            }
            set { _objectFactory = value; }
        }
        private Func<T> _objectFactory;

        private void BuildQuickFillArray(DbDataReader reader)
        {
            // init quick fill array
            var outArray = new QuickFillInfo[reader.VisibleFieldCount];

            // put field name/ordinal pairs in dictionary for exception free lookup
            var keyComparer = StringComparer.CurrentCultureIgnoreCase;
            var readerFields = new Dictionary<string, int>(keyComparer);
            for (int i = 0; i < reader.VisibleFieldCount; i++)
                readerFields.Add(reader.GetName(i), i);

            foreach (KeyValuePair<PropertyInfo, ProjectionField> entry in _projectionMap.Map)
            {
                if (!readerFields.TryGetValue(entry.Value.ExpressionField, out int ordinal))
                    continue;

                Type propType = entry.Key.PropertyType;
                TypeInfo propInfo = propType.GetTypeInfo();
                if (propInfo.IsEnum)
                {
                    propType = Enum.GetUnderlyingType(propType);
                    propInfo = propType.GetTypeInfo();
                }

                var qfi = new QuickFillInfo
                            {
                                MapField = entry.Value,
                                PropertyInfo = entry.Key, 
                                PropertyType = propType, 
                                FieldType = reader.GetFieldType(ordinal),								
                            };
                
                // determine quickly if is assignable
                qfi.IsAssignable = (propInfo.IsAssignableFrom(qfi.FieldType));

                outArray[ordinal] = qfi;
            }

            _fillInfo = outArray;
        }

        /// <summary>
        /// Gets a value indicating whether this instance has rows.
        /// </summary>
        /// <value><c>true</c> if this instance has rows; otherwise, <c>false</c>.</value>
        public bool HasRows
        {
            get { return BaseReader.HasRows; }
        }

        /// <summary>
        /// Fills an ICollection of T from the reader, async
        /// </summary>
        /// <param name="collection">The collection to fill</param>
        /// <returns></returns>
        public int Fill(ICollection<T> collection)
        {
            int nRecordCount = 0;

            lock (collection)
            {
                while (_reader.Read())
                {
                    T item = ReadObjectInternal();
                    nRecordCount++;

                    collection.Add(item);
                }
            }

            if (!KeepOpen) Dispose();

            return nRecordCount;
        }

        /// <summary>
        /// Fills an ICollection of T from the reader, async
        /// </summary>
        /// <param name="collection">The collection to fill</param>
        /// <returns></returns>
        public async Task<int> FillAsync(ICollection<T> collection)
        {
            int nRecordCount = 0;

            // TODO: Optimize This
            while (await _reader.ReadAsync().ConfigureAwait(false))
            {
                T item = ReadObjectInternal();
                nRecordCount++;

                lock (collection)
                    collection.Add(item);
            }

            if (! KeepOpen) Dispose();

            return nRecordCount;
        }


        /// <summary>
        /// Get a list containing the records from the reader
        /// </summary>
        /// <returns></returns>
        public List<T> ToList()
        {
            var list = new List<T>();

            
            while (_reader.Read())
            {
                T item = ReadObjectInternal();
                list.Add(item);
            }

            if (!KeepOpen) Dispose();

            return list;
        }

        /// <summary>
        /// Get a list containing the records from the reader
        /// </summary>
        /// <returns></returns>
        public async Task<List<T>> ToListAsync()
        {            
            var list = new List<T>();

            // TODO: Optimize This
            while (await _reader.ReadAsync().ConfigureAwait(false))
            {
                T item = ReadObjectInternal();
                list.Add(item);
            }                

            return list;
        }

        /// <summary>
        /// Gets an array with items from the reader
        /// </summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            return ToList().ToArray();
        }

        /// <summary>
        /// Gets an array with items from the reader
        /// </summary>
        /// <returns></returns>
        public async Task<T[]> ToArrayAsync()
        {
            return (await ToListAsync().ConfigureAwait(false)).ToArray();
        }
 
        private void TestInterfaces()
        {
            _isSavable = (_typeInfo.GetInterface("ISavable", false) != null);
            _isCustomFill = (_typeInfo.GetInterface("ICustomFill", false) != null);

            if (!_isCustomFill)
            {
                BuildQuickFillArray(_reader);
            }
        }

#region Nested type: QuickFillInfo

        private class QuickFillInfo
        {
            public ProjectionField MapField;
            public Type FieldType;
            public bool IsAssignable;
            public PropertyInfo PropertyInfo;
            public Type PropertyType;
        }

#endregion

#if (false)
        private Func<IDataRecord, T> CreateBuilder(DbDataReader reader)
        {
            // put field name/ordinal pairs in dictionary for exception free lookup
            var keyComparer = StringComparer.CurrentCultureIgnoreCase;
            var readerFields = new Dictionary<string, int>(keyComparer);
            for (int i = 0; i < reader.VisibleFieldCount; i++)
                readerFields.Add(reader.GetName(i), i);

            // start generator
            var method = new DynamicMethod("DynamicCreate", _objectType, new[] { typeof(IDataRecord) }, _objectType, true);
            var generator = method.GetILGenerator();

            var result = generator.DeclareLocal(_objectType);
            generator.Emit(OpCodes.Newobj, _typeInfo.GetConstructor(Type.EmptyTypes));
            generator.Emit(OpCodes.Stloc, result);

            foreach (IDataMapField field in _dataMap.ReadableFields)
            {
                int ordinal;
                if (! readerFields.TryGetValue(field.FieldName, out ordinal)) continue;
                if (field.Property.GetSetMethod(true) == null) continue;

                Type propType = field.Property.PropertyType;
                TypeInfo propInfo = propType.GetTypeInfo();
                if (propInfo.IsEnum)
                {
                    propType = Enum.GetUnderlyingType(propType);
                    propInfo = propType.GetTypeInfo();
                }

                Type dbFieldType = reader.GetFieldType(ordinal);
                if (dbFieldType == null) continue;

                var endIfLabel = generator.DefineLabel();			

                // gen code to check if field is null
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldc_I4, ordinal);
                generator.Emit(OpCodes.Callvirt, RefBits.IDataRecord_IsDBNull);
                generator.Emit(OpCodes.Brtrue, endIfLabel);

                // get value from record onto stack
                generator.Emit(OpCodes.Ldloc, result);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldc_I4, ordinal);
                generator.Emit(OpCodes.Callvirt, RefBits.IDataRecord_GetValue);

                if (propInfo.IsAssignableFrom(dbFieldType))
                {
                    // special date/time handling for UTC and Local times
                    if (dbFieldType == typeof(DateTime) && (field.DateTimeKind != DateTimeKind.Unspecified))
                    {
                        generator.Emit(OpCodes.Unbox, typeof(DateTime));
                        generator.Emit(OpCodes.Call, RefBits.DateTime_Ticks);
                        generator.Emit(OpCodes.Ldc_I4, (int) field.DateTimeKind);
                        generator.Emit(OpCodes.Newobj, RefBits.DateTime_Info.GetConstructor(new[] { typeof(long), typeof(DateTimeKind) }));
                    }
                    else
                    {
                        // direct unbox/assign
                        generator.Emit(OpCodes.Unbox_Any, dbFieldType);
                    }
                }
                else if ((propType == typeof(Guid)) && (dbFieldType == typeof(string)))
                {
                    // deal with string->guid	
                    generator.Emit(OpCodes.Castclass, typeof(string));
                    generator.Emit(OpCodes.Newobj, RefBits.Guid_Info.GetConstructor(new[] { typeof(string) }) );
                }
                else if (dbFieldType.Name.EndsWith("SqlHierarchyId"))
                {   // if the column is a SqlHierarchyId, then just treat it as a string
                    generator.Emit(OpCodes.Callvirt, RefBits.Object_ToString);
                }
                else
                {
                    // deal with other converts
                    generator.Emit(OpCodes.Ldtoken, dbFieldType);
                    generator.Emit(OpCodes.Call, RefBits.getTypeHandleMethod);
                    generator.Emit(OpCodes.Call, RefBits.Convert_ChangeType);
                    generator.Emit(OpCodes.Unbox_Any, dbFieldType);
                }

                // load into property
                generator.Emit(OpCodes.Callvirt, field.Property.GetSetMethod(true));

                // end if
                generator.MarkLabel(endIfLabel);
            }

            generator.Emit(OpCodes.Ldloc, result);
            generator.Emit(OpCodes.Ret);

            return (Func<IDataRecord, T>)method.CreateDelegate(typeof(Func<IDataRecord, T>));
        }
#endif
    }

#if (false)
    internal static class RefBits
    {
        internal static readonly TypeInfo Guid_Info = typeof(Guid).GetTypeInfo();
        internal static readonly TypeInfo DateTime_Info = typeof(DateTime).GetTypeInfo();

        internal static readonly MethodInfo IDataRecord_GetValue = typeof(IDataRecord).GetTypeInfo().GetMethod("get_Item", new[] { typeof(int) });
        internal static readonly MethodInfo IDataRecord_IsDBNull = typeof(IDataRecord).GetTypeInfo().GetMethod("IsDBNull", new[] { typeof(int) });
        internal static readonly MethodInfo getTypeHandleMethod = typeof(Type).GetTypeInfo().GetMethod("GetTypeFromHandle", new[] { typeof(RuntimeTypeHandle) });
        internal static readonly MethodInfo Convert_ChangeType = typeof(Convert).GetTypeInfo().GetMethod("ChangeType", new[] { typeof(object), typeof(Type) });
        internal static readonly MethodInfo Object_ToString = typeof(object).GetTypeInfo().GetMethod("ToString");
        internal static readonly MethodInfo DateTime_Ticks = typeof(DateTime).GetTypeInfo().GetProperty("Ticks").GetGetMethod();
    }
#endif
}
