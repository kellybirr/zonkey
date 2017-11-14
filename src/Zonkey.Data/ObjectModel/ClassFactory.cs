using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Zonkey.ObjectModel
{
    /// <summary>
    /// Creator and global registry of Data Classes
    /// </summary>
    public static class ClassFactory
    {
        private static readonly Dictionary<Type, Func<object>> _typeRegistry = new Dictionary<Type, Func<object>>();

        /// <summary>
        /// Registers the type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="valueFactory">The value factory.</param>
        public static void RegisterType(Type type, Func<object> valueFactory)
        {
            lock (_typeRegistry)
                _typeRegistry[type] = valueFactory;
        }

        /// <summary>
        /// Registers the type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void RegisterType<T>() where T : class, new()
        {
            lock (_typeRegistry)
                _typeRegistry[typeof(T)] = () => new T();
        }

        /// <summary>
        /// Registers the type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="valuefactory">The value factory.</param>
        public static void RegisterType<T>(Func<T> valuefactory) where T : class
        {
            lock (_typeRegistry)
                _typeRegistry[typeof(T)] = valuefactory;
        }

        /// <summary>
        /// Registers the interface with the type.
        /// </summary>
        /// <typeparam name="Tinterface">The Interface To Register</typeparam>
        /// <typeparam name="Tconcrete"></typeparam>
        /// <param name="valuefactory">The value factory.</param>
        public static void RegisterInterface<Tinterface, Tconcrete>(Func<Tconcrete> valuefactory) where Tconcrete : class 
        {
            lock (_typeRegistry)
                _typeRegistry[typeof(Tinterface)] = valuefactory;	
        }

        /// <summary>
        /// Gets the factory.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static Func<object> GetFactory(Type type)
        {
            Func<object> f;
            lock (_typeRegistry)
            {
                if (!_typeRegistry.TryGetValue(type, out f))
                {
                    f = EmitDefaultFactory<object>(type);
                    _typeRegistry[type] = f;
                }
            }

            return f;
        }

        /// <summary>
        /// Gets the factory.
        /// </summary>
        /// <returns></returns>
        public static Func<T> GetFactory<T>() where T: class
        {
            Func<object> f;
            lock (_typeRegistry)
            {
                if (!_typeRegistry.TryGetValue(typeof (T), out f))
                {
                    f = EmitDefaultFactory<T>(typeof (T));
                    _typeRegistry[typeof (T)] = f;
                }
            }

            return (Func<T>)f;				
        }

        /// <summary>
        /// Compiles a default constructor factory using a compiled lambda
        /// </summary>
        /// <typeparam name="T">The Type that the factory is to construct</typeparam>
        /// <returns>A factory function</returns>
        public static Func<T> CompileDefaultFactory<T>()
        {
            return CompileDefaultFactory<T>(typeof(T));
        }

        /// <summary>
        /// Compiles a default constructor factory using a compiled lambda
        /// </summary>
        /// <typeparam name="T">The return-type of the factory function</typeparam>
        /// <param name="type">The Type that the factory is to construct</param>
        /// <returns>A factory function</returns>
        public static Func<T> CompileDefaultFactory<T>(Type type)
        {
            ConstructorInfo ctor = type.GetTypeInfo().GetConstructor(Type.EmptyTypes);
            if (ctor == null)
                throw new InvalidOperationException($"Type '{type.FullName}' does not have a default constructor");

            // compile lambda expression
            var lambda = Expression.Lambda<Func<T>>(Expression.New(ctor));
            return lambda.Compile();	        
        }

        /// <summary>
        /// Emit a default constructor factory using a IL generation
        /// </summary>
        /// <typeparam name="T">The Type that the factory is to construct</typeparam>
        /// <returns>A factory function</returns>
        public static Func<T> EmitDefaultFactory<T>()
        {
            return EmitDefaultFactory<T>(typeof(T));
        }

        /// <summary>
        /// Emit a default constructor factory using a IL generation
        /// </summary>
        /// <typeparam name="T">The return-type of the factory function</typeparam>
        /// <param name="type">The Type that the factory is to construct</param>
        /// <returns>A factory function</returns>
        public static Func<T> EmitDefaultFactory<T>(Type type)
        {
            ConstructorInfo ctor = type.GetTypeInfo().GetConstructor(Type.EmptyTypes);
            if (ctor == null)
                throw new InvalidOperationException($"Type '{type.FullName}' does not have a default constructor");

            // emit dynamic method
            var method = new DynamicMethod("EmitedDefaultFactory", type, Type.EmptyTypes, type, true);
            var generator = method.GetILGenerator();
            generator.Emit(OpCodes.Newobj, ctor);
            generator.Emit(OpCodes.Ret);

            return (Func<T>)method.CreateDelegate(typeof(Func<T>));
        }
    }
}