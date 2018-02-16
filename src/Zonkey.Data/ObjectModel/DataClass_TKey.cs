using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Zonkey.ObjectModel
{
    public abstract class DataClass<TKey> : DataClass, IKeyed, IKeyed<TKey>
        where TKey : struct
    {
        private static readonly ConcurrentDictionary<int, Func<object, TKey>> _keyGetters;

        static DataClass()
        {
            _keyGetters = new ConcurrentDictionary<int, Func<object, TKey>>();
        }

        protected DataClass(bool addingNew) : base(addingNew)
        { }

        public virtual TKey GetKey()
        {
            Func<DataClass<TKey>, TKey> getter = _keyGetters.GetOrAdd(
                GetType().MetadataToken, 
                t => BuildKeyGetter<TKey>( GetType() )
                );

            return getter(this);
        }

        object IKeyed.GetKey() => GetKey();        

        private static Func<object, T> BuildKeyGetter<T>(Type type) where T : struct 
        {
            // get keys
            TypeInfo typeInfo = type.GetTypeInfo();
            PropertyInfo[] keys = typeInfo.GetProperties().Where(
                p => DataFieldAttribute.GetFromProperty(p)?.IsKeyField ?? false
            ).ToArray();

            if (keys.Length == 0)   // if no keys
                throw new InvalidOperationException("No KeyFields defined, override GetKey() in your class");

            // start dynamic method
            var method = new DynamicMethod("EmitedKeyGetter", typeof(T), new[] { typeof(object) }, type, true);
            var generator = method.GetILGenerator();

            // if exactly one key
            if (keys.Length == 1)
            {
                // single key method
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Callvirt, keys[0].GetGetMethod(true));
            }
            else
            {
                // emit to create out Tuple
                TypeInfo outInfo = typeof(T).GetTypeInfo();
                FieldInfo[] fields = outInfo.GetFields();

                var ctorTypes = new List<Type>();
                for (int i = 0; i < fields.Length; i++)
                {
                    ctorTypes.Add(fields[i].FieldType);

                    // get property value onto stack
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Callvirt, keys[i].GetGetMethod(true));
                }

                // create Tuple with input params
                generator.Emit(OpCodes.Newobj, outInfo.GetConstructor(ctorTypes.ToArray()));
            }

            // return result
            generator.Emit(OpCodes.Ret);

            // build delegate & return
            Delegate myFunc = method.CreateDelegate(typeof(Func<object, T>));
            return (Func<object, T>) myFunc;
        }
    }
}
