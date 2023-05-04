using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Linq;

namespace NPokerEngine.Utils
{
    public static class ObjectUtils
    {
        public static object DeepCopyByReflection(object obj)
        {
            var type = obj.GetType();
            var properties = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            var constructorParametersCount = type.GetConstructors().Select(t => t.GetParameters().Length).Max();
            var clonedObj = Activator.CreateInstance(type, Enumerable.Range(0, constructorParametersCount).Select(t => (object)null).ToArray());
            foreach (var property in properties)
            {
                object value = property.GetValue(obj);
                if (value != null && value.GetType().IsClass && !value.GetType().FullName.StartsWith("System."))
                {
                    property.SetValue(clonedObj, DeepCopyByReflection(value));
                }
                else
                {
                    property.SetValue(clonedObj, value);
                }
            }
            return clonedObj;
        }
    }
}
