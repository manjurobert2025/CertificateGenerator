
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace CertificateGenerator
{
    public class BulkCertificateRow
    {
        public string CertificateId { get; set; } = "";
        public string RecipientName { get; set; } = "";
        public string CourseName { get; set; } = "";
        public DateTime IssueDate { get; set; } = DateTime.Today;
        public string ValidationMessage { get; set; } = "";
    }

    public partial class BulkGenerationWindow : Window
    {
        private readonly ObservableCollection<BulkCertificateRow> _rows
            = new();

        public BulkGenerationWindow()
        {
            InitializeComponent();

            BulkCertificatesDataGrid.ItemsSource = _rows;
            UpdateSummary();
        }

        // Import CSV file
        private void ImportCsv_Click(
            object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Import Certificate Recipients",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                ImportCsvFile(dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not import CSV:\n{ex.Message}",
                    "Import Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ImportCsvFile(string filePath)
        {
            var lines = File.ReadAllLines(
                filePath, Encoding.UTF8);

            if (lines.Length < 2)
            {
                MessageBox.Show(
                    "The CSV must contain a header row and at least one record.");
                return;
            }

            var headers = ParseCsvLine(lines[0])
                .Select(h => h.Trim().Trim('\uFEFF'))
                .ToList();

            int recipientIndex =
                FindHeader(headers, "RecipientName");

            int courseIndex =
                FindHeader(headers, "CourseName");

            int idIndex =
                FindHeader(headers, "CertificateId");

            int dateIndex =
                FindHeader(headers, "IssueDate");

            if (recipientIndex < 0 || courseIndex < 0)
            {
                MessageBox.Show(
                    "CSV must contain RecipientName and CourseName columns.",
                    "Invalid CSV",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            _rows.Clear();

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                var fields = ParseCsvLine(lines[i]);

                string GetField(int index)
                {
                    return index >= 0 && index < fields.Count
                        ? fields[index].Trim()
                        : "";
                }

                var row = new BulkCertificateRow
                {
                    CertificateId = GetField(idIndex),
                    RecipientName = GetField(recipientIndex),
                    CourseName = GetField(courseIndex),
                    IssueDate = DateTime.Today
                };

                string dateText = GetField(dateIndex);

                if (!string.IsNullOrWhiteSpace(dateText))
                {
                    string[] formats =
                    {
                        "dd-MM-yyyy",
                        "d-M-yyyy",
                        "yyyy-MM-dd",
                        "MM/dd/yyyy",
                        "M/d/yyyy",
                        "dd/MM/yyyy"
                    };

                    if (DateTime.TryParseExact(
                        dateText,
                        formats,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime parsedDate))
                    {
                        row.IssueDate = parsedDate;
                    }
                    else
                    {
                        row.ValidationMessage =
                            "Invalid issue date";
                    }
                }

                _rows.Add(row);
            }

            FileStatusTextBlock.Text =
                $"Imported: {Path.GetFileName(filePath)}";

            ValidateRows();
            UpdateSummary();
        }

        private static int FindHeader(
            List<string> headers, string name)
        {
            return headers.FindIndex(
                h => string.Equals(
                    h.Trim(),
                    name,
                    StringComparison.OrdinalIgnoreCase));
        }

        // Supports quoted CSV fields and commas inside quotes.
        private static List<string> ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes &&
                        i + 1 < line.Length &&
                        line[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(field.ToString());
                    field.Clear();
                }
                else
                {
                    field.Append(c);
                }
            }

            if (inQuotes)
            {
                throw new FormatException(
                    "A CSV field contains an unclosed quotation mark.");
            }

            fields.Add(field.ToString());
            return fields;
        }

        // Validate imported or edited records.
        private void ValidateRows()
        {
            using var db = new CertificateDbContext();

            var existingIds = db.Certificates
                .AsNoTracking()
                .Select(c => c.CertificateId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var seenIds = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var row in _rows)
            {
                string originalMessage = row.ValidationMessage;

                row.ValidationMessage = "";

                if (string.IsNullOrWhiteSpace(row.RecipientName))
                {
                    row.ValidationMessage =
                        "Recipient name required";
                }
                else if (string.IsNullOrWhiteSpace(row.CourseName))
                {
                    row.ValidationMessage =
                        "Course name required";
                }
                else if (row.IssueDate == default)
                {
                    row.ValidationMessage =
                        "Invalid issue date";
                }
                else if (!string.IsNullOrWhiteSpace(row.CertificateId)
                    && !seenIds.Add(row.CertificateId.Trim()))
                {
                    row.ValidationMessage = "Duplicate CSV ID";
                }
                else if (!string.IsNullOrWhiteSpace(row.CertificateId)
                    && existingIds.Contains(row.CertificateId.Trim()))
                {
                    row.ValidationMessage = "ID already exists";
                }
                else if (originalMessage == "Invalid issue date")
                {
                    row.ValidationMessage = originalMessage;
                }
            }

            BulkCertificatesDataGrid.Items.Refresh();
            UpdateSummary();
        }

        // Generate and save all valid certificates.
        private void GenerateCertificates_Click(
            object sender, RoutedEventArgs e)
        {
            BulkCertificatesDataGrid.CommitEdit();
            BulkCertificatesDataGrid.CommitEdit();

            ValidateRows();

            var validRows = _rows
                .Where(r => string.IsNullOrWhiteSpace(
                    r.ValidationMessage))
                .ToList();

            if (validRows.Count == 0)
            {
                MessageBox.Show(
                    "There are no valid records to generate.",
                    "Bulk Generation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Generate {validRows.Count} certificates and save them to the database?",
                "Confirm Bulk Generation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using var db = new CertificateDbContext();

                // Reload IDs immediately before generating.
                var existingIds = db.Certificates
                    .AsNoTracking()
                    .Select(c => c.CertificateId)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                int currentYear = DateTime.Today.Year;
                string prefix = $"CERT-{currentYear}-";

                // Find the highest existing sequence for this year.
                int highestNumber = existingIds
                    .Where(id => id.StartsWith(
                        prefix, StringComparison.OrdinalIgnoreCase))
                    .Select(id =>
                    {
                        string suffix = id.Substring(prefix.Length);

                        return int.TryParse(
                            suffix, out int number)
                            ? number
                            : 0;
                    })
                    .DefaultIfEmpty(0)
                    .Max();

                int nextNumber = highestNumber + 1;
                int generated = 0;

                foreach (var row in validRows)
                {
                    string certificateId =
                        row.CertificateId.Trim();

                    // Generate ID if the CSV ID is blank.
                    if (string.IsNullOrWhiteSpace(certificateId))
                    {
                        do
                        {
                            certificateId =
                                $"{prefix}{nextNumber:D5}";

                            nextNumber++;
                        }
                        while (existingIds.Contains(certificateId));
                    }

                    // Protect against duplicate IDs.
                    if (!existingIds.Add(certificateId))
                    {
                        row.ValidationMessage =
                            "ID already exists";
                        continue;
                    }

                    db.Certificates.Add(new CertificateRecord
                    {
                        CertificateId = certificateId,
                        RecipientName = row.RecipientName.Trim(),
                        CourseName = row.CourseName.Trim(),
                        IssueDate = row.IssueDate.Date
                    });

                    row.CertificateId = certificateId;
                    generated++;
                }

                db.SaveChanges();

                BulkCertificatesDataGrid.Items.Refresh();

                FileStatusTextBlock.Text =
                    $"Successfully generated {generated} certificates.";

                ValidateRows();

                MessageBox.Show(
                    $"Successfully saved {generated} certificates.",
                    "Bulk Generation Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not save bulk certificates:\n{ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Load two test records without importing a file.
        private void LoadSampleCsv_Click(
            object sender, RoutedEventArgs e)
        {
            _rows.Clear();

            _rows.Add(new BulkCertificateRow
            {
                RecipientName = "Anu Thomas",
                CourseName = "C# Programming",
                IssueDate = DateTime.Today
            });

            _rows.Add(new BulkCertificateRow
            {
                RecipientName = "Rahul Kumar",
                CourseName = "Web Development",
                IssueDate = DateTime.Today
            });

            FileStatusTextBlock.Text =
                "Sample records loaded. Review before generating.";

            ValidateRows();
        }

        private void Clear_Click(
            object sender, RoutedEventArgs e)
        {
            _rows.Clear();

            FileStatusTextBlock.Text =
                "Records cleared. Import a CSV file to continue.";

            UpdateSummary();
        }

        private void UpdateSummary()
        {
            int total = _rows.Count;

            int valid = _rows.Count(
                r => string.IsNullOrWhiteSpace(
                    r.ValidationMessage));

            SummaryTextBlock.Text =
                $"Records: {total} | Valid: {valid} | Invalid: {total - valid}";
        }
    }
}
