using System;
using System.Collections;
using System.Data.Odbc;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace BaamStudios.SharpAngie
{
    static internal class ReflectionsHelper
    {
        private static readonly Regex NumberRegex = new Regex(@"^\d+$");

        /// <summary>
        /// Gets the object and property from the view model that are described by the path.
        /// </summary>
        /// <param name="rootObject"></param>
        /// <param name="propertyPath">
        /// Supported value patterns: <br/>
        /// - myprop <br/>
        /// - myobj.myprop <br/>
        /// - myobj.myarrayprop[123] <br/>
        /// - myobj.myarrayprop.123 <br/>
        /// - myobj.myarrayprop[123].prop1 <br/>
        /// - myobj.myarrayprop.123.prop1 <br/>
        /// </param>
        public static void GetDeepProperty(object rootObject, string propertyPath, out object targetPropertyOwner, out PropertyInfo targetProperty, out int? targetPropertyIndex)
        {
            targetPropertyOwner = null;
            targetProperty = null;
            targetPropertyIndex = null;

            object currentObject = rootObject;

            // the javascript side may separate the index of array properties with property.1234 instead of property[1234] for simplicity of the javascript code.
            // replacing the [] with . is not really necessary when calling only from javascript but it makes this method more compatible with calls from c#.
            var propertyNames = propertyPath.Replace('[', '.').Replace(']', '.').Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            var finalPropertyIsList = NumberRegex.IsMatch(propertyNames.Last());
            var lastPropertyIndex = finalPropertyIsList
                ? propertyNames.Length - 3
                : propertyNames.Length - 2;

            for (int i = 0; i <= lastPropertyIndex; i++)
            {
                var propertyName = propertyNames[i];

                var property = GetProperty(currentObject, propertyName);
                if (property == null)
                    return;

                currentObject = property.GetValue(currentObject);
                if (currentObject == null)
                    return;

                var possiblePropertyIndex = propertyNames[i + 1];
                if (NumberRegex.IsMatch(possiblePropertyIndex))
                {
                    var propertyIndex = Int32.Parse(possiblePropertyIndex);
                    currentObject = ((IEnumerable)currentObject).Cast<object>().ElementAt(propertyIndex);
                    i++;
                }
            }

            var finalProperty = GetProperty(currentObject, propertyNames[lastPropertyIndex + 1]);
            if (finalProperty == null) return;

            targetPropertyOwner = currentObject;
            targetProperty = finalProperty;
            targetPropertyIndex = finalPropertyIsList ? Int32.Parse(propertyNames[lastPropertyIndex + 2]) : (int?)null;
        }

        public static PropertyInfo GetProperty(object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName);
        }

        public static void SetDeepProperty(object rootObject, string propertyPath, object value, Action beforeSet = null, Action afterSet = null)
        {
            object targetPropertyOwner;
            PropertyInfo targetProperty;
            int? targetPropertyIndex;
            GetDeepProperty(rootObject, propertyPath, out targetPropertyOwner, out targetProperty,
                out targetPropertyIndex);
            if (targetProperty == null) return;

            var oldValue = GetPropertyValue(targetPropertyOwner, targetProperty, targetPropertyIndex);
            if (!Equals(oldValue, value))
            {
                if (beforeSet != null) beforeSet();
                try
                {
                    SetPropertyValue(targetPropertyOwner, targetProperty, targetPropertyIndex, value);
                }
                finally
                {
                    if (afterSet != null) afterSet();
                }
            }
        }

        private static object GetPropertyValue(object propertyOwner, PropertyInfo property, int? propertyIndex)
        {
            var value = property.GetValue(propertyOwner);
            if (propertyIndex == null)
                return value;

            var enumerable = value as IEnumerable;
            if (enumerable == null)
                return null;

            return enumerable.Cast<object>().ElementAt(propertyIndex.Value);
        }

        private static void SetPropertyValue(object propertyOwner, PropertyInfo property, int? propertyIndex, object value)
        {
            if (propertyIndex == null)
            {
                property.SetValue(propertyOwner, value);
                return;
            }

            var list = property.GetValue(propertyOwner) as IList;
            if (list == null)
                return;

            list[propertyIndex.Value] = value;
        }

        private static void GetDeepMethod(object rootObject, string methodPath, out object methodOwner, out string methodName)
        {
            methodOwner = null;
            methodName = null;

            var lastDot = methodPath.LastIndexOf('.');
            if (lastDot >= 0)
            {
                var propertyPath = methodPath.Substring(0, lastDot);

                object targetPropertyOwner;
                PropertyInfo targetProperty;
                int? targetPropertyIndex;
                GetDeepProperty(rootObject, propertyPath, out targetPropertyOwner, out targetProperty,
                    out targetPropertyIndex);
                if (targetProperty == null) return;

                methodOwner = GetPropertyValue(targetPropertyOwner, targetProperty, targetPropertyIndex);
                methodName = methodPath.Substring(lastDot + 1);
            }
            else
            {
                methodOwner = rootObject;
                methodName = methodPath;
            }
        }

        private static MethodInfo GetMethod(object methodOwner, string methodName, object[] args)
        {
            return methodOwner.GetType().GetMethods()
                .FirstOrDefault(x => x.Name == methodName && x.GetParameters().Length == args.Length);
        }

        public static void InvokeDeepMethod(object rootObject, string methodPath, object[] args)
        {
            object methodOwner;
            string methodName;
            GetDeepMethod(rootObject, methodPath, out methodOwner, out methodName);
            if (methodOwner == null)
                return;

            var method = GetMethod(methodOwner, methodName, args);
            if (method == null)
                return;

            InvokeMethod(methodOwner, method, args);
        }

        private static void InvokeMethod(object methodOwner, MethodInfo method, object[] args)
        {
            var parameterInfos = method.GetParameters();
            var parameters = new object[parameterInfos.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                var arg = args[i];
                var parameterType = parameterInfos[i].ParameterType;

                if (parameterType == typeof (string))
                    parameters[i] = arg != null ? arg.ToString() : null;
                else if (parameterType == typeof (Guid))
                    parameters[i] = arg != null ? Guid.Parse(arg.ToString()) : Guid.Empty;
                else
                    parameters[i] = Convert.ChangeType(arg, parameterType);
            }

            method.Invoke(methodOwner, parameters);
        }
    }
}