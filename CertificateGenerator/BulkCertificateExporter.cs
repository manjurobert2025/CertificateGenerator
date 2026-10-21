
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using CertificateGenerator;

namespace CertificateGenerator
{
    public static class BulkCertificateExporter
    {
        public static int ExportFromCsv(
            string csvPath,
            string zipPath)
        {
            if (!File.Exists(csvPath))
                throw new FileNotFoundException(
                    "The CSV file was not found.", csvPath);

            var lines = File.ReadAllLines(csvPath);

            if (lines.Length < 2)
                throw new InvalidOperationException(
                    "The CSV must contain a header and at least one recipient.");

            var headers = ParseCsvLine(lines[0])
                .Select(h => h.Trim())
                .ToList();

            int nameIndex = FindHeader(headers, "RecipientName");
            int courseIndex = FindHeader(headers, "CourseName");
            int dateIndex = FindHeader(headers, "CertificateDate");
            int idIndex = FindHeader(headers, "CertificateId");

            if (nameIndex < 0 || courseIndex < 0 ||
                dateIndex < 0 || idIndex < 0)
            {
                throw new InvalidOperationException(
                    "Required columns: RecipientName, CourseName, " +
                    "CertificateDate, CertificateId.");
            }

            var recipients =
                new List<(string Name, string Course,
                          DateTime Date, string Id)>();

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                var fields = ParseCsvLine(lines[i]);

                int requiredIndex = Math.Max(
                    Math.Max(nameIndex, courseIndex),
                    Math.Max(dateIndex, idIndex));

                if (fields.Count <= requiredIndex)
                {
                    throw new InvalidOperationException(
                        $"Invalid CSV data on line {i + 1}.");
                }

                string name = fields[nameIndex].Trim();
                string course = fields[courseIndex].Trim();
                string dateText = fields[dateIndex].Trim();
                string certificateId = fields[idIndex].Trim();

                if (string.IsNullOrWhiteSpace(name) ||
                    string.IsNullOrWhiteSpace(course) ||
                    string.IsNullOrWhiteSpace(certificateId))
                {
                    throw new InvalidOperationException(
                        $"Missing required data on line {i + 1}.");
                }

                string[] formats =
                {
                    "dd-MM-yyyy",
                    "dd/MM/yyyy",
                    "yyyy-MM-dd",
                    "dd MMMM yyyy"
                };

                if (!DateTime.TryParseExact(
                    dateText,
                    formats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime certificateDate))
                {
                    throw new InvalidOperationException(
                        $"Invalid date on line {i + 1}: {dateText}. " +
                        "Use dd-MM-yyyy.");
                }

                recipients.Add((
                    name,
                    course,
                    certificateDate,
                    certificateId));
            }

            if (recipients.Count == 0)
                throw new InvalidOperationException(
                    "No valid recipients were found.");

            string fullZipPath = Path.GetFullPath(zipPath);
            string? zipDirectory = Path.GetDirectoryName(fullZipPath);

            if (!string.IsNullOrEmpty(zipDirectory))
                Directory.CreateDirectory(zipDirectory);

            string tempFolder = Path.Combine(
                Path.GetTempPath(),
                "CertificateBulk_" + Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(tempFolder);

            try
            {
                var usedFileNames =
                    new HashSet<string>(
                        StringComparer.OrdinalIgnoreCase);

                foreach (var recipient in recipients)
                {
                    string baseName =
                        $"{recipient.Id}_{recipient.Name}";

                    string safeName = MakeSafeFileName(baseName);
                    string fileName = safeName + ".pdf";

                    int suffix = 2;

                    while (!usedFileNames.Add(fileName))
                    {
                        fileName =
                            $"{safeName}_{suffix++}.pdf";
                    }

                    string pdfPath = Path.Combine(
                        tempFolder, fileName);

                    CertificatePdfGenerator.Generate(
                        recipient.Name,
                        recipient.Course,
                        recipient.Date,
                        recipient.Id,
                        pdfPath);
                }

                if (File.Exists(fullZipPath))
                    File.Delete(fullZipPath);

                using (var archive = ZipFile.Open(
                    fullZipPath, ZipArchiveMode.Create))
                {
                    foreach (string pdfPath in
                        Directory.GetFiles(tempFolder, "*.pdf"))
                    {
                        archive.CreateEntryFromFile(
                            pdfPath,
                            Path.GetFileName(pdfPath),
                            CompressionLevel.Optimal);
                    }
                }
            }
            finally
            {
                if (Directory.Exists(tempFolder))
                    Directory.Delete(tempFolder, true);
            }

            return recipients.Count;
        }

        private static int FindHeader(
            List<string> headers,
            string requiredName)
        {
            return headers.FindIndex(
                h => string.Equals(
                    h.Trim(),
                    requiredName,
                    StringComparison.OrdinalIgnoreCase));
        }

        private static string MakeSafeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            return name.Trim().TrimEnd('.');
        }

        private static List<string> ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var current = new System.Text.StringBuilder();
            bool insideQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (insideQuotes &&
                        i + 1 < line.Length &&
                        line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        insideQuotes = !insideQuotes;
                    }
                }
                else if (c == ',' && !insideQuotes)
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            if (insideQuotes)
                throw new FormatException(
                    "The CSV contains an unclosed quotation mark.");

            fields.Add(current.ToString());
            return fields;
        }
    }
}
