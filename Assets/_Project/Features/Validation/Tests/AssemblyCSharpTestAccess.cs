using System;
using System.Reflection;

namespace Project.Features.Validation.Tests
{
    /// <summary>
    /// Project.Features.Validation.Tests cannot reference Assembly-CSharp via asmdef; use reflection for smoke guards on runtime UI/core/player APIs.
    /// </summary>
    internal static class AssemblyCSharpTestAccess
    {
        private const string AssemblyQualifiedPrefix = ", Assembly-CSharp";

        public static Type GetRuntimeType(string fullTypeName)
        {
            return Type.GetType(fullTypeName + AssemblyQualifiedPrefix);
        }

        public static object InvokeStatic(string fullTypeName, string methodName, BindingFlags flags, object[] args)
        {
            Type type = GetRuntimeType(fullTypeName);
            if (type == null)
                throw new InvalidOperationException("Type not found in Assembly-CSharp: " + fullTypeName);

            MethodInfo method = type.GetMethod(methodName, flags);
            if (method == null)
                throw new InvalidOperationException("Method not found: " + fullTypeName + "." + methodName);

            return method.Invoke(null, args);
        }

        public static bool InvokeStaticBool(string fullTypeName, string methodName, params object[] args)
        {
            object result = InvokeStatic(fullTypeName, methodName, BindingFlags.Public | BindingFlags.Static, args);
            return result is bool b && b;
        }

        public static bool TryInvokeStaticBoolOutString(
            string fullTypeName,
            string methodName,
            out string message)
        {
            message = null;
            Type type = GetRuntimeType(fullTypeName);
            if (type == null)
                return false;

            MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            if (method == null)
                return false;

            object[] args = { null };
            object result = method.Invoke(null, args);
            message = args[0] as string;
            return result is bool b && b;
        }

        public static object InvokeStaticGeneric(
            string fullTypeName,
            string methodName,
            Type genericArg,
            params object[] args)
        {
            Type type = GetRuntimeType(fullTypeName);
            if (type == null)
                throw new InvalidOperationException("Type not found in Assembly-CSharp: " + fullTypeName);

            MethodInfo method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
                throw new InvalidOperationException("Method not found: " + fullTypeName + "." + methodName);

            MethodInfo generic = method.MakeGenericMethod(genericArg);
            return generic.Invoke(null, args);
        }
    }
}
