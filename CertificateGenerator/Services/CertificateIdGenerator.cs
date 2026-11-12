using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CertificateGenerator.Services
{
    public static class CertificateIdGenerator
    {
        public static string GenerateNextId(
            IEnumerable<string?> existingIds,
            int year)
        {
            string prefix = $"CERT-{year}-";

            int highestNumber = existingIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Where(id => id!.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase))
                .Select(id =>
                {
                    string suffix = id!.Substring(prefix.Length);

                    return int.TryParse(
                        suffix,
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out int number)
                        ? number
                        : 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            return $"{prefix}{highestNumber + 1:D5}";
        }
    }
}
