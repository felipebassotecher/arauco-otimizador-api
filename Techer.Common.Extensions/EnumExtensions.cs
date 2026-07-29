using System.ComponentModel;
using System.Runtime.Serialization;

namespace Techer.Common.Extensions
{
    public static class EnumExtensions
    {
        public static string GetEnumMemberValue(this Enum enumValue)
        {
            var type = enumValue.GetType();
            var info = type.GetField(enumValue.ToString());
            var da = (EnumMemberAttribute[])info.GetCustomAttributes(typeof(EnumMemberAttribute), false);

            if (da.Length > 0)
                return da[0].Value;
            else
                return string.Empty;
        }

        public static string GetEnumDescriptionValue(this Enum enumValue)
        {
            var type = enumValue.GetType();
            var info = type.GetField(enumValue.ToString());
            var da = (DescriptionAttribute[])info.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (da.Length > 0)
                return da[0].Description;
            else
                return string.Empty;
        }

        public static T ToEnum<T>(this string str)
        {
            var enumType = typeof(T);

            foreach (var name in Enum.GetNames(enumType))
            {
                var enumMemberAttribute = ((EnumMemberAttribute[])enumType.GetField(name).GetCustomAttributes(typeof(EnumMemberAttribute), true)).First();

                if (enumMemberAttribute.Value == str)
                    return (T)Enum.Parse(enumType, name);
            }

            //throw exception or whatever handling you want or
            return default;
        }
    }
}
