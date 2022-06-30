﻿using System;
using System.Reflection;
using System.Reflection.Emit;

namespace ROStations
{
    public static class ReflectionHelpers
    {
        /// <summary>
        /// Create a getter for an instance or static field. Only works with fields declared in a class (won't work for struct fields).<br/>
        /// </summary>
        /// <typeparam name="T">The field type</typeparam>
        /// <param name="field">The field info</param>
        /// <returns>An func delegate where the argument is the class instance (or null for a static field) and the return value is the field value</returns>
        public static Func<object, T> CreateFieldGetter<T>(FieldInfo field)
        {
            string methodName = field.ReflectedType.FullName + ".get_" + field.Name;
            DynamicMethod setterMethod = new DynamicMethod(methodName, typeof(T), new Type[1] { typeof(object) }, true);
            ILGenerator gen = setterMethod.GetILGenerator();
            if (field.IsStatic)
            {
                gen.Emit(OpCodes.Ldsfld, field);
            }
            else
            {
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldfld, field);
            }
            gen.Emit(OpCodes.Ret);
            return (Func<object, T>)setterMethod.CreateDelegate(typeof(Func<object, T>));
        }

        /// <summary>
        /// Create a setter for an instance or static field. Only works with fields declared in a class (won't work for struct fields).
        /// </summary>
        /// <typeparam name="T">The field type</typeparam>
        /// <param name="field">The field info</param>
        /// <returns>An action delegate where the first argument is the class instance (or null for a static field) and the second argument is the new value</returns>
        public static Action<object, T> CreateFieldSetter<T>(FieldInfo field)
        {
            string methodName = field.ReflectedType.FullName + ".set_" + field.Name;
            DynamicMethod setterMethod = new DynamicMethod(methodName, null, new Type[2] { typeof(object), typeof(T) }, true);
            ILGenerator gen = setterMethod.GetILGenerator();
            if (field.IsStatic)
            {
                gen.Emit(OpCodes.Ldarg_1);
                gen.Emit(OpCodes.Stsfld, field);
            }
            else
            {
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldarg_1);
                gen.Emit(OpCodes.Stfld, field);
            }
            gen.Emit(OpCodes.Ret);
            return (Action<object, T>)setterMethod.CreateDelegate(typeof(Action<object, T>));
        }
    }
}
