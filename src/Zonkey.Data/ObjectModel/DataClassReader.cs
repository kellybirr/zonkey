using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
#pragma warning disable 8424
#pragma warning disable CS1066

namespace Zonkey.ObjectModel
{
    /// <summary>
    /// A class that reads DCs from a DataReader
    /// </summary>
    /// <typeparam name="T"></typeparam>
#if NET6_0_OR_GREATER
    public class DataClassReader<T> : IEnumerable<T>, IAsyncEnumerable<T>, IDisposable, IAsyncDisposable where T : class
#else
    public class DataClassReader<T> : IEnumerable<T>, IDisposable where T : class
#endif
    {
        private readonly DataMap _dataMap;
        private readonly DbDataReader _reader;
        private QuickFillInfo[] _fillInfo;
        private BuilderDelegate _builder;
        private readonly int[] _builderTracker = new int[1];
        private bool _disposed;

        private readonly Type _objectType;
        private readonly TypeInfo _typeInfo;

        private bool _isCustomFill;
        private bool _isSavable;

        private delegate T BuilderDelegate(IDataRecord record, int[] tracker, Func<T> factory);

        /// <summary>
        /// The process-wide default for <see cref="UseFastBuilder"/>. Defaults to true.
        /// </summary>
        public static bool DefaultUseFastBuilder { get; set; } = true;

        /// <summary>
        /// When true (the default), rows are populated by an IL-emitted builder compiled
        /// once per (type, result-set shape) instead of per-field reflection. Conversion
        /// failures throw <see cref="PropertyReadException"/> identifying the property,
        /// exactly like the reflection path.
        /// </summary>
        public bool UseFastBuilder { get; set; } = DefaultUseFastBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataClassReader&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        public DataClassReader(DbDataReader reader)
        {
            _objectType = typeof(T);
            _typeInfo = _objectType.GetTypeInfo();

            _dataMap = DataMap.GenerateCached(_objectType);
            _reader = reader;

            DisposeBaseReader = true;
            TestInterfaces();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataClassReader&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="disposeReader">if set to <c>true</c> [dispose reader].</param>
        public DataClassReader(DbDataReader reader, bool disposeReader)
        {
            _objectType = typeof(T);
            _typeInfo = _objectType.GetTypeInfo();

            _dataMap = DataMap.GenerateCached(_objectType);
            _reader = reader;

            DisposeBaseReader = disposeReader;
            TestInterfaces();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataClassReader&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="map">The map.</param>
        public DataClassReader(DbDataReader reader, DataMap map)
        {
            _objectType = typeof(T);
            _typeInfo = _objectType.GetTypeInfo();

            _dataMap = map;
            _reader = reader;

            DisposeBaseReader = true;
            TestInterfaces();
        }

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

            _dataMap = map;
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

#if NET6_0_OR_GREATER
        async IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator([EnumeratorCancellation] System.Threading.CancellationToken cancellationToken = default)
        {
            T item;
            while (((item = (await ReadAsync())) != null) && (!cancellationToken.IsCancellationRequested))
                yield return item;
        }
#endif

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (DisposeBaseReader && !_disposed) 
                _reader.Dispose();

            _disposed = true;
        }

        ~DataClassReader() => Dispose(false);

#if NET6_0_OR_GREATER
        public async ValueTask DisposeAsync()
        {
            if (DisposeBaseReader && !_disposed)
                await _reader.DisposeAsync();

            _disposed = true;
        }
#endif

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
                if (UseFastBuilder)
                {
                    _builder ??= CreateBuilder();

                    try
                    {
                        item = _builder(_reader, _builderTracker, ObjectFactory);
                    }
                    catch (Exception ex) when (ex is not PropertyReadException)
                    {
                        throw WrapBuilderException(ex);
                    }
                }
                else
                {
                    item = BuildObject(_reader);
                }

                if (_isSavable)
                    ((ISavable)item).CommitValues();
            }

            return item;
        }

        private Exception WrapBuilderException(Exception ex)
        {
            int ordinal = _builderTracker[0];
            QuickFillInfo info = ((ordinal >= 0) && (ordinal < _fillInfo.Length)) ? _fillInfo[ordinal] : null;
            if (info == null) return ex; // failed outside a field set (e.g. object construction)

            object value = null;
            try { value = _reader.GetValue(ordinal); }
            catch { /* value stays null for the exception report */ }

            return new PropertyReadException(info.PropertyInfo, value, ex);
        }

        private T BuildObject(IDataRecord record)
        {
            var obj = CreateNewT();
            for (int i = 0; i < _fillInfo.Length; i++)
            {
                QuickFillInfo info = _fillInfo[i];
                if (info == null || record.IsDBNull(i)) continue;

                FieldHandler.SetValue(obj, record.GetValue(i), info.MapField, info.FieldType, info.PropertyInfo, info.PropertyType, info.IsAssignable);
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

            foreach (IDataMapField field in _dataMap.ReadableFields)
            {
                if (!readerFields.TryGetValue(field.FieldName, out int ordinal))
                    continue;

                Type propType = field.Property.PropertyType;
                TypeInfo propInfo = propType.GetTypeInfo();
                if (propInfo.IsEnum)
                {
                    propType = Enum.GetUnderlyingType(propType);
                    propInfo = propType.GetTypeInfo();
                }

                var qfi = new QuickFillInfo
                            {
                                MapField = field,
                                PropertyInfo = field.Property, 
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
            public IDataMapField MapField;
            public Type FieldType;
            public bool IsAssignable;
            public PropertyInfo PropertyInfo;
            public Type PropertyType;
        }

#endregion

        /// <summary>
        /// Emits an IL method that populates one T from the current row of a reader with
        /// this reader's exact column layout. Conversions are resolved at emit time from
        /// the known reader field types, so the generated code is branch-free straight-line
        /// IL: null-check, convert, set -- per mapped column. Before each field-set the
        /// method writes the ordinal into the tracker array, which lets the single
        /// try/catch in ReadObjectInternal report the failing property without any
        /// exception handling inside the generated code.
        /// </summary>
        private BuilderDelegate CreateBuilder()
        {
            var method = new DynamicMethod(
                "ZonkeyBuild_" + _objectType.Name, _objectType,
                new[] { typeof(IDataRecord), typeof(int[]), typeof(Func<T>) },
                typeof(DataClassReader<T>).Module, skipVisibility: true);

            ILGenerator il = method.GetILGenerator();
            LocalBuilder result = il.DeclareLocal(_objectType);

            // tracker[0] = -1: failures before any field-set (e.g. construction) stay unattributed
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldc_I4_M1);
            il.Emit(OpCodes.Stelem_I4);

            // result = factory();
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Callvirt, FastBuilderRefs.FuncOfT_Invoke(typeof(T)));
            il.Emit(OpCodes.Stloc, result);

            for (int ordinal = 0; ordinal < _fillInfo.Length; ordinal++)
            {
                QuickFillInfo info = _fillInfo[ordinal];
                if (info == null) continue;

                PropertyInfo pi = info.PropertyInfo;
                MethodInfo setter = pi.GetSetMethod(true);
                if (setter == null) continue;

                Type dbType = info.FieldType;
                if (dbType == null) continue;

                Type propType = pi.PropertyType;
                Type nullableOf = Nullable.GetUnderlyingType(propType);
                Type coreType = nullableOf ?? propType;                                     // e.g. decimal, Habitat, DateTime, Guid, string
                Type enumType = coreType.IsEnum ? coreType : null;
                Type basicType = (enumType != null) ? Enum.GetUnderlyingType(enumType) : coreType; // Convert.ChangeType target

                Label endIfLabel = il.DefineLabel();

                // tracker[0] = ordinal;
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ldc_I4, ordinal);
                il.Emit(OpCodes.Stelem_I4);

                // if (record.IsDBNull(ordinal)) goto endIf;
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, ordinal);
                il.Emit(OpCodes.Callvirt, FastBuilderRefs.IDataRecord_IsDBNull);
                il.Emit(OpCodes.Brtrue, endIfLabel);

                // stack: [result], [boxed value]
                il.Emit(OpCodes.Ldloc, result);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, ordinal);
                il.Emit(OpCodes.Callvirt, FastBuilderRefs.IDataRecord_GetValue);

                // convert the boxed value to an unboxed value of stack-type coreType
                if ((coreType == typeof(Guid)) && (dbType == typeof(string)))
                {
                    il.Emit(OpCodes.Castclass, typeof(string));
                    il.Emit(OpCodes.Newobj, FastBuilderRefs.Guid_CtorString);
                }
                else if (dbType.Name.EndsWith("SqlHierarchyId") && (coreType == typeof(string)))
                {
                    il.Emit(OpCodes.Callvirt, FastBuilderRefs.Object_ToString);
                }
#if NET6_0_OR_GREATER
                else if (coreType == typeof(DateOnly))
                {
                    if (dbType == typeof(DateTime))
                    {
                        il.Emit(OpCodes.Unbox_Any, typeof(DateTime));
                        il.Emit(OpCodes.Call, FastBuilderRefs.DateOnly_FromDateTime);
                    }
                    else
                    {
                        il.Emit(OpCodes.Callvirt, FastBuilderRefs.Object_ToString);
                        il.Emit(OpCodes.Call, FastBuilderRefs.DateOnly_Parse);
                    }
                }
                else if (coreType == typeof(TimeOnly))
                {
                    if (dbType == typeof(TimeSpan))
                    {
                        il.Emit(OpCodes.Unbox_Any, typeof(TimeSpan));
                        il.Emit(OpCodes.Call, FastBuilderRefs.TimeOnly_FromTimeSpan);
                    }
                    else if (dbType == typeof(DateTime))
                    {
                        il.Emit(OpCodes.Unbox_Any, typeof(DateTime));
                        il.Emit(OpCodes.Call, FastBuilderRefs.TimeOnly_FromDateTime);
                    }
                    else
                    {
                        il.Emit(OpCodes.Callvirt, FastBuilderRefs.Object_ToString);
                        il.Emit(OpCodes.Call, FastBuilderRefs.TimeOnly_Parse);
                    }
                }
#endif
                else if (enumType != null)
                {
                    // enums are their underlying type at IL level; widen via ChangeType when needed
                    if (dbType != basicType)
                        EmitChangeType(il, basicType);

                    il.Emit(OpCodes.Unbox_Any, enumType);
                }
                else if (coreType.IsAssignableFrom(dbType))
                {
                    // exact match for value types; reference conversion for string/byte[]/object
                    il.Emit(OpCodes.Unbox_Any, coreType);
                }
                else
                {
                    EmitChangeType(il, basicType);
                    il.Emit(OpCodes.Unbox_Any, coreType);
                }

                // apply the mapped DateTimeKind to every DateTime, whichever conversion produced it
                if ((coreType == typeof(DateTime)) && (info.MapField.DateTimeKind != DateTimeKind.Unspecified))
                {
                    il.Emit(OpCodes.Ldc_I4, (int)info.MapField.DateTimeKind);
                    il.Emit(OpCodes.Call, FastBuilderRefs.DateTime_SpecifyKind);
                }

                // wrap in Nullable<coreType> when the property is nullable
                if (nullableOf != null)
                    il.Emit(OpCodes.Newobj, FastBuilderRefs.NullableCtor(coreType));

                il.Emit(OpCodes.Callvirt, setter);
                il.MarkLabel(endIfLabel);
            }

            il.Emit(OpCodes.Ldloc, result);
            il.Emit(OpCodes.Ret);

            return (BuilderDelegate)method.CreateDelegate(typeof(BuilderDelegate));
        }

        private static void EmitChangeType(ILGenerator il, Type targetType)
        {
            il.Emit(OpCodes.Ldtoken, targetType);
            il.Emit(OpCodes.Call, FastBuilderRefs.Type_GetTypeFromHandle);
            il.Emit(OpCodes.Call, FastBuilderRefs.Convert_ChangeType);
        }
    }

    internal static class FastBuilderRefs
    {
        internal static readonly MethodInfo IDataRecord_GetValue = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue), new[] { typeof(int) });
        internal static readonly MethodInfo IDataRecord_IsDBNull = typeof(IDataRecord).GetMethod(nameof(IDataRecord.IsDBNull), new[] { typeof(int) });
        internal static readonly MethodInfo Type_GetTypeFromHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), new[] { typeof(RuntimeTypeHandle) });
        internal static readonly MethodInfo Convert_ChangeType = typeof(Convert).GetMethod(nameof(Convert.ChangeType), new[] { typeof(object), typeof(Type) });
        internal static readonly MethodInfo Object_ToString = typeof(object).GetMethod(nameof(ToString));
        internal static readonly MethodInfo DateTime_SpecifyKind = typeof(DateTime).GetMethod(nameof(DateTime.SpecifyKind), new[] { typeof(DateTime), typeof(DateTimeKind) });
        internal static readonly ConstructorInfo Guid_CtorString = typeof(Guid).GetConstructor(new[] { typeof(string) });

#if NET6_0_OR_GREATER
        internal static readonly MethodInfo DateOnly_FromDateTime = typeof(DateOnly).GetMethod(nameof(DateOnly.FromDateTime), new[] { typeof(DateTime) });
        internal static readonly MethodInfo DateOnly_Parse = typeof(DateOnly).GetMethod(nameof(DateOnly.Parse), new[] { typeof(string) });
        internal static readonly MethodInfo TimeOnly_FromTimeSpan = typeof(TimeOnly).GetMethod(nameof(TimeOnly.FromTimeSpan), new[] { typeof(TimeSpan) });
        internal static readonly MethodInfo TimeOnly_FromDateTime = typeof(TimeOnly).GetMethod(nameof(TimeOnly.FromDateTime), new[] { typeof(DateTime) });
        internal static readonly MethodInfo TimeOnly_Parse = typeof(TimeOnly).GetMethod(nameof(TimeOnly.Parse), new[] { typeof(string) });
#endif

        internal static MethodInfo FuncOfT_Invoke(Type itemType)
            => typeof(Func<>).MakeGenericType(itemType).GetMethod("Invoke");

        internal static ConstructorInfo NullableCtor(Type coreType)
            => typeof(Nullable<>).MakeGenericType(coreType).GetConstructor(new[] { coreType });
    }
}
