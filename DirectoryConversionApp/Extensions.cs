using System;
using System.ComponentModel;
using System.Linq;

namespace DirectoryConversionApp
{
    public static class Extensions
    {
        public static string GetDescription(this Enum obj)
        {
            return obj.GetType().GetField(obj.ToString())
                .GetCustomAttributes(typeof(DescriptionAttribute), false)
                .Select(a => ((DescriptionAttribute)a).Description)
                .DefaultIfEmpty(obj.ToString())
                .First();
        }
    }
}
