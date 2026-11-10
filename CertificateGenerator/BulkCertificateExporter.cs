
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace CertificateGenerator
{
    public static class BulkCertificateExporter
    {
        private const int MaxBatchSize = 500;
        public static int ExportFromExcel(
            string excelPath,
            string zipPath,
            CertificateTemplateSettings settings)
        {
            if (!File.Exists(excelPath))
                throw new FileNotFoundException(
                    "The Excel file was not found.", excelPath);

            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (string.IsNullOrWhiteSpace(zipPath))
                throw new ArgumentException(
                    "A ZIP output path is required.", nameof(zipPath));

            // 1. Read and validate Excel data.
            var recipients = ReadRecipients(excelPath);

            // 2. Validate maximum batch size.
            if (recipients.Count > MaxBatchSize)
            {
                throw new InvalidOperationException(
                    $"Your Excel file contains {recipients.Count} certificates.\n\n" +
                    $"The maximum allowed per batch is {MaxBatchSize}.\n\n" +
                    "Please split your Excel file into smaller batches.");
            }

            // 3. Generate IDs for missing IDs.
            GenerateMissingIds(recipients);

            // 4. Check database for existing IDs.
            CheckExistingCertificateIds(recipients);

            string fullZipPath = Path.GetFullPath(zipPath);
            string? zipDirectory = Path.GetDirectoryName(fullZipPath);

            if (!string.IsNullOrWhiteSpace(zipDirectory))
                Directory.CreateDirectory(zipDirectory);

            string tempFolder = Path.Combine(
     Environment.GetFolderPath(
         Environment.SpecialFolder.MyDocuments),
     "CertificateGenerator");

            Directory.CreateDirectory(tempFolder);
            string temporaryZipPath = Path.Combine(
                zipDirectory ?? Path.GetTempPath(),
                Guid.NewGuid().ToString("N") + ".zip");

            bool finalZipCreated = false;

            try
            {
                // 5. Generate individual PDFs.
                var usedFileNames = new HashSet<string>(
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
                        fileName = $"{safeName}_{suffix}.pdf";
                        suffix++;
                    }

                    string folder = Path.Combine(
     Environment.GetFolderPath(
         Environment.SpecialFolder.MyDocuments),
     "CertificateGenerator");

                    Directory.CreateDirectory(folder);

                    string pdfPath = Path.Combine(folder, fileName);

                    CertificatePdfGenerator.Generate(
                        recipient.Name,
                        recipient.Course,
                        recipient.IssueDate,
                        recipient.Id,
                        pdfPath,
                        settings);
                    System.Windows.MessageBox.Show(
    $"Recipient ID: {recipient.Id}\n\n" +
    $"Generated filename: {fileName}\n\n" +
    $"Full path: {pdfPath}\n\n" +
    $"File exists: {File.Exists(pdfPath)}",
    "Bulk PDF Diagnostic");
                }

                // 6. Create ZIP in a temporary location.
                using (var archive = ZipFile.Open(
                    temporaryZipPath, ZipArchiveMode.Create))
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

                // 7. Save records and publish ZIP together as closely
                // as possible. A failed database save removes the ZIP.
                using (var db = new CertificateDbContext())
                using (var transaction = db.Database.BeginTransaction())
                {
                    // Check again immediately before inserting.
                    var ids = recipients.Select(r => r.Id).ToList();

                    var existingIds = db.Certificates
                        .AsNoTracking()
                        .Where(c => ids.Contains(c.CertificateId))
                        .Select(c => c.CertificateId)
                        .ToList();

                    if (existingIds.Count > 0)
                    {
                        throw new InvalidOperationException(
                            "Some certificate IDs already exist:\n" +
                            string.Join(", ", existingIds));
                    }

                    foreach (var recipient in recipients)
                    {
                        db.Certificates.Add(new CertificateRecord
                        {
                            CertificateId = recipient.Id,
                            RecipientName = recipient.Name,
                            CourseName = recipient.Course,
                            IssueDate = recipient.IssueDate
                        });
                    }

                    db.SaveChanges();

                    // Move completed ZIP to its final destination.
                    File.Move(
                        temporaryZipPath,
                        fullZipPath,
                        overwrite: true);

                    finalZipCreated = true;

                    transaction.Commit();
                }

                return recipients.Count;
            }
            catch
            {
                // Do not leave a ZIP behind if the database operation fails.
                if (finalZipCreated && File.Exists(fullZipPath))
                {
                    try
                    {
                        File.Delete(fullZipPath);
                    }
                    catch
                    {
                        // Preserve the original exception.
                    }
                }

                throw;
            }
            finally
            {
                if (File.Exists(temporaryZipPath))
                    File.Delete(temporaryZipPath);

                //if (Directory.Exists(tempFolder))
                //    Directory.Delete(tempFolder, true);
            }
        }

        private static List<Recipient> ReadRecipients(string excelPath)
        {
            var recipients = new List<Recipient>();

            using (var workbook = new XLWorkbook(excelPath))
            {
                var worksheet = workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                    throw new InvalidOperationException(
                        "The Excel file does not contain a worksheet.");

                var headerRow = worksheet.Row(1);

                var headers = headerRow.CellsUsed()
                    .ToDictionary(
                        c => c.GetString().Trim(),
                        c => c.Address.ColumnNumber,
                        StringComparer.OrdinalIgnoreCase);

                int nameColumn =
                    GetRequiredColumn(headers, "RecipientName");

                int courseColumn =
                    GetRequiredColumn(headers, "CourseName");

                int issueDateColumn =
                    GetRequiredColumn(headers, "IssueDate");

                // CertificateId is optional.
                int idColumn = headers.TryGetValue(
                    "CertificateId", out int foundIdColumn)
                    ? foundIdColumn
                    : -1;

                var lastRow = worksheet.LastRowUsed();

                if (lastRow == null || lastRow.RowNumber() < 2)
                    throw new InvalidOperationException(
                        "The Excel file contains no recipient records.");

                for (int rowNumber = 2;
                     rowNumber <= lastRow.RowNumber();
                     rowNumber++)
                {
                    var row = worksheet.Row(rowNumber);

                    if (row.IsEmpty())
                        continue;

                    string name =
                        row.Cell(nameColumn).GetString().Trim();

                    string course =
                        row.Cell(courseColumn).GetString().Trim();

                    if (string.IsNullOrWhiteSpace(name))
                        throw new InvalidOperationException(
                            $"RecipientName is missing on row {rowNumber}.");

                    if (string.IsNullOrWhiteSpace(course))
                        throw new InvalidOperationException(
                            $"CourseName is missing on row {rowNumber}.");

                    DateTime issueDate = ReadIssueDate(
                        row.Cell(issueDateColumn), rowNumber);

                    string certificateId = idColumn > 0
                        ? row.Cell(idColumn).GetString().Trim()
                        : "";

                    recipients.Add(new Recipient
                    {
                        Name = name,
                        Course = course,
                        IssueDate = issueDate,
                        Id = certificateId
                    });
                }
            }

            if (recipients.Count == 0)
                throw new InvalidOperationException(
                    "No valid recipients were found.");

            // Check supplied IDs for duplicates within the Excel file.
            var suppliedIds = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var recipient in recipients)
            {
                if (string.IsNullOrWhiteSpace(recipient.Id))
                    continue;

                if (!suppliedIds.Add(recipient.Id))
                {
                    throw new InvalidOperationException(
                        $"Duplicate CertificateId '{recipient.Id}' " +
                        "was found in the Excel file.");
                }
            }

            return recipients;
        }

        private static void GenerateMissingIds(
            List<Recipient> recipients)
        {
            string batchToken = Guid.NewGuid()
                .ToString("N")
                .Substring(0, 8)
                .ToUpperInvariant();

            var usedIds = new HashSet<string>(
                recipients
                    .Where(r => !string.IsNullOrWhiteSpace(r.Id))
                    .Select(r => r.Id),
                StringComparer.OrdinalIgnoreCase);

            int sequence = 1;

            foreach (var recipient in recipients)
            {
                if (!string.IsNullOrWhiteSpace(recipient.Id))
                    continue;

                string generatedId;

                do
                {
                    generatedId =
                        $"CERT-{DateTime.Now.Year}-{batchToken}-{sequence:D3}";

                    sequence++;
                }
                while (!usedIds.Add(generatedId));

                recipient.Id = generatedId;
            }
        }

        private static void CheckExistingCertificateIds(
            List<Recipient> recipients)
        {
            var ids = recipients.Select(r => r.Id).ToList();

            using (var db = new CertificateDbContext())
            {
                var existingIds = db.Certificates
                    .AsNoTracking()
                    .Where(c => ids.Contains(c.CertificateId))
                    .Select(c => c.CertificateId)
                    .ToList();

                if (existingIds.Count > 0)
                {
                    throw new InvalidOperationException(
                        "These certificate IDs already exist in history:\n" +
                        string.Join(", ", existingIds));
                }
            }
        }

        private static int GetRequiredColumn(
            Dictionary<string, int> headers,
            string columnName)
        {
            if (!headers.TryGetValue(columnName, out int column))
            {
                throw new InvalidOperationException(
                    $"Required Excel column '{columnName}' is missing.");
            }

            return column;
        }

        private static DateTime ReadIssueDate(
            IXLCell cell,
            int rowNumber)
        {
            if (cell.TryGetValue<DateTime>(out DateTime date))
                return date;

            string dateText = cell.GetString().Trim();

            string[] formats =
            {
                "dd-MM-yyyy",
                "dd/MM/yyyy",
                "yyyy-MM-dd",
                "dd MMMM yyyy",
                "d-M-yyyy",
                "d/M/yyyy"
            };

            if (DateTime.TryParseExact(
                dateText,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date))
            {
                return date;
            }

            if (DateTime.TryParse(
                dateText,
                CultureInfo.CurrentCulture,
                DateTimeStyles.None,
                out date))
            {
                return date;
            }

            throw new InvalidOperationException(
                $"Invalid IssueDate on Excel row {rowNumber}: " +
                $"'{dateText}'.");
        }

        private static string MakeSafeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            return name.Trim().TrimEnd('.');
        }

        private class Recipient
        {
            public string Name { get; set; } = "";
            public string Course { get; set; } = "";
            public DateTime IssueDate { get; set; }
            public string Id { get; set; } = "";
        }
    }
}
