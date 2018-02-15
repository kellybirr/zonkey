using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Zonkey.ObjectModel
{
    public abstract class DataClass<TKey> : DataClass, IKeyed, IKeyed<TKey>
        where TKey : struct
    {
        private static readonly ConcurrentDictionary<int, Func<TKey>> _keyGetters = new ConcurrentDictionary<int, Func<TKey>>();

        protected DataClass(bool addingNew) : base(addingNew)
        { }

        public virtual TKey GetKey()
        {
            Func<TKey> getter = _keyGetters.GetOrAdd(
                GetType().MetadataToken, 
                t => BuildKeyGetter<TKey>( GetType() )
                );

            return getter();
        }

        object IKeyed.GetKey() => GetKey();        

        private static Func<T> BuildKeyGetter<T>(Type type)
        {
            // get keys
            TypeInfo typeInfo = type.GetTypeInfo();
            PropertyInfo[] keys = typeInfo.GetProperties().Where(
                p => DataFieldAttribute.GetFromProperty(p)?.IsKeyField ?? false
            ).ToArray();

            // require exactly one key
            if (keys.Length == 0)
                throw new InvalidOperationException("No KeyFields defined, override GetKey() in your class");

            if (keys.Length > 1)
                throw new NotSupportedException("Composite keys not supported via base GetKey(), override GetKey() in your class");

            // emit dynamic method
            var method = new DynamicMethod("EmitedKeyGetter", type, Type.EmptyTypes, type, true);
            var generator = method.GetILGenerator();
            generator.Emit(OpCodes.Callvirt, keys[0].GetGetMethod(true));
            generator.Emit(OpCodes.Ret);

            // all good
            return (Func<T>)method.CreateDelegate(typeof(Func<T>));
        }
    }
}
