using CertificateGenerator.Models;
using System;
using System.IO;
using System.Text.Json;

namespace CertificateGenerator.Services
{
    public static class OrganizationSettingsStorage
    {
        private static readonly string FolderPath =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "CertificateGenerator");

        private static readonly string FilePath =
            Path.Combine(
                FolderPath,
                "organization-settings.json");

        private static readonly JsonSerializerOptions JsonOptions =
            new JsonSerializerOptions
            {
                WriteIndented = true
            };

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
            catch (JsonException)
            {
                return new OrganizationSettings();
            }
            catch (IOException)
            {
                return new OrganizationSettings();
            }
            catch (UnauthorizedAccessException)
            {
                return new OrganizationSettings();
            }
        }

        public static void Save(OrganizationSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            Directory.CreateDirectory(FolderPath);

            string json = JsonSerializer.Serialize(
                settings,
                JsonOptions);

            File.WriteAllText(FilePath, json);
        }
    }
}
