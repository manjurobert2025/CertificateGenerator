
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace CertificateGenerator
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

            foreach (string file in Directory.GetFiles(
                         TemplatesFolder, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(file);

                    var settings =
                        JsonSerializer.Deserialize<CertificateTemplateSettings>(
                            json);

                    if (settings != null)
                    {
                        templates.Add(new SavedTemplate
                        {
                            FilePath = file,
                            Settings = settings
                        });
                    }
                }
                catch (Exception)
                {
                    // Skip invalid JSON files.
                }
            }

            return templates
                .OrderBy(t => t.Settings.TemplateName)
                .ToList();
        }

        public static string CreateTemplatePath(string name)
        {
            string safeName = string.Join(
                "_",
                name.Split(Path.GetInvalidFileNameChars()));

            if (string.IsNullOrWhiteSpace(safeName))
                safeName = "CertificateTemplate";

            return Path.Combine(
                TemplatesFolder,
                $"{safeName}_{Guid.NewGuid():N}.json");
        }
    }

    public class SavedTemplate
    {
        public string FilePath { get; set; } = "";

        public CertificateTemplateSettings Settings { get; set; } =
            new CertificateTemplateSettings();

        public string DisplayName =>
            $"{Settings.TemplateName} — {Settings.CertificateTitle}";
    }
}
