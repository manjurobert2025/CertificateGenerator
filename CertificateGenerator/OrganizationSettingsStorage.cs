
using System;
using System.IO;
using System.Text.Json;

namespace CertificateGenerator
{
    public static class OrganizationSettingsStorage
    {
        private static readonly string FolderPath =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "CertificateGenerator");

        private static readonly string FilePath =
            Path.Combine(FolderPath, "organization-settings.json");

        public static OrganizationSettings Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return new OrganizationSettings();

                string json = File.ReadAllText(FilePath);

                return JsonSerializer.Deserialize<OrganizationSettings>(
                    json) ?? new OrganizationSettings();
            }
            catch
            {
                return new OrganizationSettings();
            }
        }

        public static void Save(OrganizationSettings settings)
        {
            Directory.CreateDirectory(FolderPath);

            string json = JsonSerializer.Serialize(
                settings,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(FilePath, json);
        }
    }
}
