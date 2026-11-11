using ClosedXML.Excel;
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

        // Keeps track of an invalid imported date.
        public bool IsDateValid { get; set; } = true;
    }

    public partial class BulkGenerationWindow : Window
    {
        private readonly ObservableCollection<BulkCertificateRow> _rows
            = new();

        private List<SavedTemplate> _templates = new();

        public BulkGenerationWindow()
        {
            InitializeComponent();

            BulkCertificatesDataGrid.ItemsSource = _rows;

            LoadTemplates();

            UpdateSummary();
        }

        // ---------------------------------------------------------
        // TEMPLATE LOADING
        // ---------------------------------------------------------

        private void LoadTemplates()
        {
            try
            {
                _templates = TemplateStorage.GetTemplates();

                TemplatesComboBox.ItemsSource = _templates;

                if (_templates.Count > 0)
                {
                    TemplatesComboBox.SelectedIndex = 0;

                    TemplateStatusTextBlock.Text =
                        $"{_templates.Count} saved template(s) available.";
                }
                else
                {
                    TemplateStatusTextBlock.Text =
                        "No saved templates found. Create a template in Template Designer first.";
                }
            }
            catch (Exception ex)
            {
                TemplateStatusTextBlock.Text =
                    "Could not load templates: " + ex.Message;
            }
        }

        private void TemplatesComboBox_SelectionChanged(
            object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (TemplatesComboBox.SelectedItem is SavedTemplate template)
            {
                TemplateStatusTextBlock.Text =
                    $"Selected template: {template.DisplayName}";
            }
        }

        // ---------------------------------------------------------
        // CSV IMPORT
        // ---------------------------------------------------------

        private void ImportCsv_Click(
            object sender,
            RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import Certificate Recipients",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() != true)
                return;

            string filePath = dialog.FileName;

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                ImportCsvFile(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Could not import CSV:\n{ex.Message}",
                    "Import Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ImportCsvFile(string filePath)
        {
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);

            if (lines.Length < 2)
            {
                System.Windows.MessageBox.Show(
                    "The CSV must contain a header row and at least one record.");
                return;
            }

            var headers = ParseCsvLine(lines[0])
                .Select(h => h.Trim().Trim('\uFEFF'))
                .ToList();

            int recipientIndex = FindHeader(headers, "RecipientName");
            int courseIndex = FindHeader(headers, "CourseName");
            int idIndex = FindHeader(headers, "CertificateId");
            int dateIndex = FindHeader(headers, "IssueDate");

            if (recipientIndex < 0 || courseIndex < 0)
            {
                System.Windows.MessageBox.Show(
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
                    if (TryParseDate(dateText, out DateTime parsedDate))
                    {
                        row.IssueDate = parsedDate;
                    }
                    else
                    {
                        row.IsDateValid = false;
                    }
                }

                _rows.Add(row);
            }

            FileStatusTextBlock.Text =
                $"Imported CSV: {Path.GetFileName(filePath)}";

            ValidateRows();
            UpdateSummary();
        }

        // ---------------------------------------------------------
        // EXCEL IMPORT
        // ---------------------------------------------------------

        private void ImportExcel_Click(
            object sender,
            RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import Certificate Recipients",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() != true)
                return;

            string filePath = dialog.FileName;

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                ImportExcelFile(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Could not import Excel file:\n{ex.Message}",
                    "Excel Import Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ImportExcelFile(string filePath)
        {
            using var workbook = new XLWorkbook(filePath);

            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                System.Windows.MessageBox.Show("The Excel file does not contain a worksheet.");
                return;
            }

            var headerRow = worksheet.FirstRowUsed();

            if (headerRow == null)
            {
                System.Windows.MessageBox.Show("The Excel worksheet is empty.");
                return;
            }

            int headerNumber = headerRow.RowNumber();

            var headers = headerRow.CellsUsed()
                .ToDictionary(
                    c => c.Address.ColumnNumber,
                    c => c.GetString().Trim());

            int FindExcelHeader(string name)
            {
                var match = headers.FirstOrDefault(
                    h => string.Equals(
                        h.Value,
                        name,
                        StringComparison.OrdinalIgnoreCase));

                return match.Key;
            }

            int recipientColumn = FindExcelHeader("RecipientName");
            int courseColumn = FindExcelHeader("CourseName");
            int idColumn = FindExcelHeader("CertificateId");
            int dateColumn = FindExcelHeader("IssueDate");

            if (recipientColumn == 0 || courseColumn == 0)
            {
                System.Windows.MessageBox.Show(
                    "Excel must contain RecipientName and CourseName columns.",
                    "Invalid Excel File",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            _rows.Clear();

            var lastRow = worksheet.LastRowUsed();

            if (lastRow == null)
                return;

            for (int rowNumber = headerNumber + 1;
                 rowNumber <= lastRow.RowNumber();
                 rowNumber++)
            {
                var excelRow = worksheet.Row(rowNumber);

                if (excelRow.IsEmpty())
                    continue;

                string GetCellValue(int column)
                {
                    if (column <= 0)
                        return "";

                    return excelRow.Cell(column).GetFormattedString().Trim();
                }

                var row = new BulkCertificateRow
                {
                    CertificateId = GetCellValue(idColumn),
                    RecipientName = GetCellValue(recipientColumn),
                    CourseName = GetCellValue(courseColumn),
                    IssueDate = DateTime.Today
                };

                if (dateColumn > 0)
                {
                    var dateCell = excelRow.Cell(dateColumn);

                    if (!dateCell.IsEmpty())
                    {
                        if (dateCell.TryGetValue<DateTime>(out DateTime parsedDate))
                        {
                            row.IssueDate = parsedDate.Date;
                        }
                        else if (TryParseDate(
                                     dateCell.GetFormattedString(),
                                     out parsedDate))
                        {
                            row.IssueDate = parsedDate.Date;
                        }
                        else
                        {
                            row.IsDateValid = false;
                        }
                    }
                }

                _rows.Add(row);
            }

            FileStatusTextBlock.Text =
                $"Imported Excel: {Path.GetFileName(filePath)}";

            ValidateRows();
            UpdateSummary();
        }

        // ---------------------------------------------------------
        // DOWNLOAD SAMPLE EXCEL
        // ---------------------------------------------------------

        private void DownloadSampleExcel_Click(
            object sender,
            RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save Sample Excel Template",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = "CertificateRecipients_Sample.xlsx"
            };

            if (dialog.ShowDialog() != true)
                return;

            string filePath = dialog.FileName;

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                using var workbook = new XLWorkbook();

                var worksheet = workbook.Worksheets.Add("Recipients");

                worksheet.Cell(1, 1).Value = "CertificateId";
                worksheet.Cell(1, 2).Value = "RecipientName";
                worksheet.Cell(1, 3).Value = "CourseName";
                worksheet.Cell(1, 4).Value = "IssueDate";

                worksheet.Cell(2, 1).Value = "";
                worksheet.Cell(2, 2).Value = "Anu Thomas";
                worksheet.Cell(2, 3).Value = "C# Programming";
                worksheet.Cell(2, 4).Value = DateTime.Today;

                worksheet.Cell(3, 1).Value = "";
                worksheet.Cell(3, 2).Value = "Rahul Kumar";
                worksheet.Cell(3, 3).Value = "Web Development";
                worksheet.Cell(3, 4).Value = DateTime.Today;

                var header = worksheet.Range(1, 1, 1, 4);

                header.Style.Font.Bold = true;
                header.Style.Font.FontColor = XLColor.White;
                header.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563EB");

                worksheet.Column(1).Width = 22;
                worksheet.Column(2).Width = 25;
                worksheet.Column(3).Width = 28;
                worksheet.Column(4).Width = 18;

                worksheet.Column(4).Style.DateFormat.Format = "dd-MM-yyyy";

                worksheet.SheetView.FreezeRows(1);
                worksheet.RangeUsed().SetAutoFilter();

                workbook.SaveAs(dialog.FileName);

                FileStatusTextBlock.Text =
                    $"Sample Excel saved: {dialog.FileName}";

                System.Windows.MessageBox.Show(
                    "Sample Excel template downloaded successfully.",
                    "Download Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Could not create sample Excel:\n{ex.Message}",
                    "Excel Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ---------------------------------------------------------
        // COMMON HELPERS
        // ---------------------------------------------------------

        private static int FindHeader(
            List<string> headers,
            string name)
        {
            return headers.FindIndex(
                h => string.Equals(
                    h.Trim(),
                    name,
                    StringComparison.OrdinalIgnoreCase));
        }

        private static bool TryParseDate(
            string value,
            out DateTime date)
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

            return DateTime.TryParseExact(
                       value.Trim(),
                       formats,
                       CultureInfo.InvariantCulture,
                       DateTimeStyles.None,
                       out date)
                   || DateTime.TryParse(
                       value,
                       CultureInfo.CurrentCulture,
                       DateTimeStyles.None,
                       out date);
        }

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
                throw new FormatException(
                    "A CSV field contains an unclosed quotation mark.");

            fields.Add(field.ToString());

            return fields;
        }

        // ---------------------------------------------------------
        // VALIDATION
        // ---------------------------------------------------------

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
                row.ValidationMessage = "";

                if (string.IsNullOrWhiteSpace(row.RecipientName))
                {
                    row.ValidationMessage = "Recipient name required";
                }
                else if (string.IsNullOrWhiteSpace(row.CourseName))
                {
                    row.ValidationMessage = "Course name required";
                }
                else if (!row.IsDateValid)
                {
                    row.ValidationMessage = "Invalid issue date";
                }
                else if (row.IssueDate == default)
                {
                    row.ValidationMessage = "Invalid issue date";
                }
                else if (!string.IsNullOrWhiteSpace(row.CertificateId)
                    && !seenIds.Add(row.CertificateId.Trim()))
                {
                    row.ValidationMessage = "Duplicate CSV/Excel ID";
                }
                else if (!string.IsNullOrWhiteSpace(row.CertificateId)
                    && existingIds.Contains(row.CertificateId.Trim()))
                {
                    row.ValidationMessage = "ID already exists";
                }
            }

            BulkCertificatesDataGrid.Items.Refresh();
            UpdateSummary();
        }

        // ---------------------------------------------------------
        // SAVE RECORDS TO DATABASE
        // ---------------------------------------------------------

        private void GenerateCertificates_Click(
            object sender,
            RoutedEventArgs e)
        {
            BulkCertificatesDataGrid.CommitEdit();
            BulkCertificatesDataGrid.CommitEdit();

            ValidateRows();

            var validRows = _rows
                .Where(r => string.IsNullOrWhiteSpace(r.ValidationMessage))
                .ToList();

            if (validRows.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "There are no valid records to save.",
                    "Bulk Generation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var result = System.Windows.MessageBox.Show(
                $"Save {validRows.Count} certificate records to the database?",
                "Confirm Bulk Generation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using var db = new CertificateDbContext();

                var existingIds = db.Certificates
                    .AsNoTracking()
                    .Select(c => c.CertificateId)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                int currentYear = DateTime.Today.Year;
                int generated = 0;

                foreach (var row in validRows)
                {
                    string certificateId =
                        CertificateIdGenerator.GenerateNextId(
                            existingIds,
                            currentYear);
                    System.Windows.MessageBox.Show(
    $"New ID generated: {certificateId}");

                    // Add the new ID immediately so the next row receives
                    // the following number in the same bulk operation.
                    existingIds.Add(certificateId);

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
                    $"Successfully saved {generated} certificate records. You can now export the PDFs.";

                ValidateRows();

                System.Windows.MessageBox.Show(
                    $"Successfully saved {generated} certificate records.",
                    "Database Save Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Could not save certificates:\n{ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ---------------------------------------------------------
        // EXPORT BULK PDFS USING SELECTED TEMPLATE
        // ---------------------------------------------------------

        private void ExportBulkPdfs_Click(
            object sender,
            RoutedEventArgs e)
        {
            BulkCertificatesDataGrid.CommitEdit();
            BulkCertificatesDataGrid.CommitEdit();

            ValidateRows();

            if (TemplatesComboBox.SelectedItem is not SavedTemplate selectedTemplate)
            {
                System.Windows.MessageBox.Show(
                    "Please select a saved certificate template first.",
                    "Template Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var validRows = _rows
                .Where(r => string.IsNullOrWhiteSpace(r.ValidationMessage))
                .ToList();

            if (validRows.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "There are no valid records to export.",
                    "Bulk PDF Export",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var rowsWithoutIds = validRows
                .Where(r => string.IsNullOrWhiteSpace(r.CertificateId))
                .ToList();

            if (rowsWithoutIds.Count > 0)
            {
                System.Windows.MessageBox.Show(
                    "Some records do not have Certificate IDs.\n\nPlease click Save to Database first. This will assign IDs to new records.",
                    "Certificate IDs Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var folderDialog = new OpenFolderDialog
            {
                Title = "Select Folder for Bulk Certificate PDFs"
            };

            if (folderDialog.ShowDialog(this) != true)
                return;

            try
            {
                string outputFolder = folderDialog.FolderName;

                int exported = 0;
                var errors = new List<string>();

                foreach (var row in validRows)
                {
                    try
                    {
                        string safeId = MakeSafeFileName(row.CertificateId);

                        string outputPath = Path.Combine(
                            outputFolder,
                            $"{safeId}.pdf");

                        CertificatePdfGenerator.Generate(
                            row.RecipientName.Trim(),
                            row.CourseName.Trim(),
                            row.IssueDate.Date,
                            row.CertificateId.Trim(),
                            outputPath,
                            selectedTemplate.Settings);

                        exported++;
                        System.Windows.MessageBox.Show(
          $"PDF exists: {File.Exists(outputPath)}\n\n{outputPath}",
          "Generation Check");
                    }

                    catch (Exception ex)
                    {
                        errors.Add(
                            $"{row.RecipientName}: {ex.Message}");
                    }
                }

                FileStatusTextBlock.Text =
                    $"Exported {exported} of {validRows.Count} PDF certificates.";

                string message =
                    $"Successfully exported {exported} PDF certificate(s).\n\nFolder:\n{outputFolder}";

                if (errors.Count > 0)
                {
                    message += "\n\nErrors:\n" +
                               string.Join("\n", errors.Take(10));
                }

                System.Windows.MessageBox.Show(
                    message,
                    errors.Count == 0 ? "PDF Export Complete" : "PDF Export Finished with Errors",
                    MessageBoxButton.OK,
                    errors.Count == 0
                        ? MessageBoxImage.Information
                        : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                $"Could not export PDFs:\n{ex.Message}",
                "PDF Export Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            }
        }

        private static string MakeSafeFileName(string value)
        {
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                value = value.Replace(invalidChar, '_');

            return string.IsNullOrWhiteSpace(value)
                ? "Certificate"
                : value;
        }

        // ---------------------------------------------------------
        // SAMPLE RECORDS
        // ---------------------------------------------------------

        private void LoadSampleCsv_Click(
            object sender,
            RoutedEventArgs e)
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
                "Sample records loaded. Review them before saving.";

            ValidateRows();
        }

        // ---------------------------------------------------------
        // CLEAR
        // ---------------------------------------------------------

        private void Clear_Click(
            object sender,
            RoutedEventArgs e)
        {
            _rows.Clear();

            FileStatusTextBlock.Text =
                "Records cleared. Import a CSV or Excel file to continue.";

            UpdateSummary();
        }

        // ---------------------------------------------------------
        // SUMMARY
        // ---------------------------------------------------------

        private void UpdateSummary()
        {
            int total = _rows.Count;

            int valid = _rows.Count(
                r => string.IsNullOrWhiteSpace(r.ValidationMessage));

            SummaryTextBlock.Text =
                $"Records: {total} | Valid: {valid} | Invalid: {total - valid}";
        }
    }
}