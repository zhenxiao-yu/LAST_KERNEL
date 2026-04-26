using System.Text;
using UnityEngine;

namespace Markyu.LastKernel
{
    public static class LocalizationKeyBuilder
    {
        public static string ForAsset(Object asset, string category, string field)
        {
            if (asset == null || string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(field))
                return string.Empty;

            return $"{category}.{ToKeySegment(asset.name)}.{field}";
        }

        public static string ToKeySegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "unnamed";

            var builder = new StringBuilder(value.Length);
            bool previousWasSeparator = false;

            foreach (char character in value.Trim())
            {
                char lower = char.ToLowerInvariant(character);
                bool isAllowed = char.IsLetterOrDigit(lower);

                if (isAllowed)
                {
                    builder.Append(lower);
                    previousWasSeparator = false;
                }
                else if (!previousWasSeparator)
                {
                    builder.Append('_');
                    previousWasSeparator = true;
                }
            }

            while (builder.Length > 0 && builder[builder.Length - 1] == '_')
            {
                builder.Length--;
            }

            return builder.Length > 0 ? builder.ToString() : "unnamed";
        }
    }
}
