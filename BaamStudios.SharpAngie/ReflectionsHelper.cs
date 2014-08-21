using System;
using System.Collections;
using System.Data.Odbc;
using System.Diagnostics;
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
        public static void GetDeepProperty(object rootObject, string propertyPath, out object targetPropertyOwner, out PropertyInfo targetProperty, out object targetPropertyIndex)
        {
            targetPropertyOwner = null;
            targetProperty = null;
            targetPropertyIndex = null;

            object currentObject = rootObject;

            // the javascript side may separate the index of array properties with property.1234 instead of property[1234] for simplicity of the javascript code.
            // replacing the [] with . is not really necessary when calling only from javascript but it makes this method more compatible with calls from c#.
            var propertyNames = propertyPath.Replace('[', '.').Replace(']', '.').Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            if (propertyNames.Length == 0)
                return;

            for (int i = 0; i < propertyNames.Length; i++)
            {
                if (i < propertyNames.Length - 1)
                {
                    var propertyName = propertyNames[i];

                    var currentProperty = GetProperty(currentObject, propertyName);
                    if (currentProperty == null)
                        return;

                    var value = currentProperty.GetValue(currentObject);
                    if (value == null)
                        return;

                    if (value is IEnumerable)
                    {
                        var possiblePropertyIndex = propertyNames[i + 1];
                        if (value is IDictionary)
                        {
                            var key = ((IDictionary) value).Keys.Cast<object>()
                                .FirstOrDefault(x => x.ToString() == possiblePropertyIndex);

                            if (i + 1 == propertyNames.Length - 1)
                            {
                                targetPropertyOwner = currentObject;
                                targetProperty = currentProperty;
                                targetPropertyIndex = key;
                                return;
                            }

                            currentObject = ((IDictionary)value)[key];
                            i++;
                            continue;
                        }

                        if (NumberRegex.IsMatch(possiblePropertyIndex))
                        {
                            var key = Int32.Parse(possiblePropertyIndex);

                            if (i + 1 == propertyNames.Length - 1)
                            {
                                targetPropertyOwner = currentObject;
                                targetProperty = currentProperty;
                                targetPropertyIndex = key;
                                return;
                            }

                            currentObject = ((IEnumerable)value).Cast<object>().ElementAt(key);
                            i++;
                            continue;
                        }

                        return;
                    }
                    else
                    {
                        currentObject = value;
                    }
                }
                else
                {
                    targetPropertyOwner = currentObject;
                    targetProperty = GetProperty(targetPropertyOwner, propertyNames.Last());
                    targetPropertyIndex = null;
                }
            }
        }

        public static PropertyInfo GetProperty(object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName);
        }

        public static void SetDeepProperty(object rootObject, string propertyPath, object value, Action beforeSet = null, Action afterSet = null)
        {
            object targetPropertyOwner;
            PropertyInfo targetProperty;
            object targetPropertyIndex;
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

        private static object GetPropertyValue(object propertyOwner, PropertyInfo property, object propertyIndex)
        {
            var value = property.GetValue(propertyOwner);
            if (propertyIndex == null)
                return value;

            var dictionary = value as IDictionary;
            if (dictionary != null)
            {
                return dictionary[propertyIndex];
            }

            if (propertyIndex is int)
            {
                var enumerable = value as IEnumerable;
                if (enumerable == null)
                    return null;

                return enumerable.Cast<object>().ElementAt((int) propertyIndex);
            }

            return null;
        }

        private static void SetPropertyValue(object propertyOwner, PropertyInfo property, object propertyIndex, object value)
        {
            if (propertyIndex == null)
            {
                property.SetValue(propertyOwner, Convert.ChangeType(value, property.PropertyType));
                return;
            }

            var enumerable = property.GetValue(propertyOwner);

            var dictionary = enumerable as IDictionary;
            if (dictionary != null)
            {
                dictionary[propertyIndex] = Convert.ChangeType(value, dictionary.GetType().GenericTypeArguments[1]);
            }

            if (propertyIndex is int)
            {
                var list = enumerable as IList;
                if (list == null)
                    return;

                list[(int) propertyIndex] = Convert.ChangeType(value, list.GetType().GenericTypeArguments[0]);
            }
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
                object targetPropertyIndex;
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