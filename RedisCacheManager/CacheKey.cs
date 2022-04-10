using System;
using System.Linq;
using System.Text;

namespace RedisCacheManager
{
    public class CacheKey<TItem>
        where TItem : class
    {
        public string Key { get; }

        public bool IsValid { get; }

        public CacheKey(string key)
        {
            IsValid = !string.IsNullOrWhiteSpace(key);
            Key = IsValid ? $"{ExtractTypeName(typeof(TItem))}-{key}" : key;
        }

        private static string ExtractTypeName(Type type)
        {
            if (!type.IsGenericType)
            {
                return type.Name;
            }

            var types = type.GenericTypeArguments;

            var stringBuilder = new StringBuilder();
            stringBuilder.Append(type.Name);
            stringBuilder.Append('<');
            stringBuilder.Append(string.Join("|", types.Select(ExtractTypeName)));
            stringBuilder.Append('>');
            return stringBuilder.ToString();
        }
    }
}
