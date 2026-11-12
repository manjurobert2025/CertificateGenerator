using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace CertificateGenerator.Services
{
    public static class TemplateStorage
    {
        public static string TemplatesFolder
        {
            get
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "CertificateGenerator",
                    "Templates");

                Directory.CreateDirectory(folder);

                return folder;
            }
        }

        public static List<SavedTemplate> GetTemplates()
        {
            var templates = new List<SavedTemplate>();

            string[] files = Directory.GetFiles(
                TemplatesFolder,
                "*.json");

            foreach (string file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);

                    var settings =
                        JsonSerializer.Deserialize<CertificateTemplateSettings>(
                            json);

                    if (settings == null)
                        continue;

                    templates.Add(new SavedTemplate
                    {
                        FilePath = file,
                        Settings = settings,
                        Name = string.IsNullOrWhiteSpace(
                            settings.TemplateName)
                                ? "My Template"
                                : settings.TemplateName
                    });
                }
                catch (JsonException)
                {
                    // Skip invalid JSON files.
                }
                catch (IOException)
                {
                    // Skip files that cannot be read.
                }
                catch (UnauthorizedAccessException)
                {
                    // Skip files that cannot be accessed.
                }
            }

            return templates
                .OrderBy(
                    template => template.Name,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static string CreateTemplatePath(string name)
        {
            char[] invalidCharacters =
                Path.GetInvalidFileNameChars();

            string safeName = string.Join(
                "_",
                (name ?? string.Empty).Split(
                    invalidCharacters,
                    StringSplitOptions.RemoveEmptyEntries));

            safeName = safeName.Trim(' ', '.');

            if (string.IsNullOrWhiteSpace(safeName))
            {
                safeName = "CertificateTemplate";
            }

            // Keep the filename reasonably short.
            if (safeName.Length > 80)
            {
                safeName = safeName.Substring(0, 80);
            }

            return Path.Combine(
                TemplatesFolder,
                $"{safeName}_{Guid.NewGuid():N}.json");
        }
    }

    public class SavedTemplate
    {
        public string Name { get; set; } = "My Template";

        public string FilePath { get; set; } = "";

        public CertificateTemplateSettings Settings { get; set; } =
            new CertificateTemplateSettings();

        public string DisplayName =>
            $"{Settings?.TemplateName ?? Name} — " +
            $"{Settings?.CertificateTitle ?? string.Empty}";
    }
}
