
using System;
using System.Windows;
using Microsoft.Win32;
using System.IO;
namespace CertificateGenerator
{
    public partial class PdfExportWindow : Window
    {
        public PdfExportWindow()
        {
            InitializeComponent();
            LoadTemplates();
        }

        private void LoadTemplates()
        {
            TemplatesComboBox.ItemsSource = TemplateStorage.GetTemplates();
            TemplatesComboBox.DisplayMemberPath = "DisplayName";

            if (TemplatesComboBox.Items.Count > 0)
                TemplatesComboBox.SelectedIndex = 0;
        }

        private void BrowseExcelButton_Click(
            object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Excel File",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                ExcelFilePathTextBox.Text = dialog.FileName;
            }
        }

        private void ExportBulkPdf_Click(
            object sender, RoutedEventArgs e)
        {
            try
            {
                string excelPath = ExcelFilePathTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(excelPath) ||
                    !File.Exists(excelPath))
                {
                    System.Windows.MessageBox.Show(
                        "Please select a valid Excel file.",
                        "Missing Excel File",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var selectedTemplate =
                    TemplatesComboBox.SelectedItem as SavedTemplate;

                if (selectedTemplate == null)
                {
                    System.Windows.MessageBox.Show(
                        "Please select a certificate template.",
                        "Missing Template",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Save Certificate ZIP",
                    Filter = "ZIP Archive (*.zip)|*.zip",
                    FileName = "Certificates.zip",
                    DefaultExt = ".zip"
                };

                if (saveDialog.ShowDialog() != true)
                    return;

                // Create a separate copy of the selected template settings
                var templateSettings =
                    System.Text.Json.JsonSerializer.Deserialize<CertificateTemplateSettings>(
                        System.Text.Json.JsonSerializer.Serialize(
                            selectedTemplate.Settings))
                    ?? new CertificateTemplateSettings();

                // Load saved organization details
                var organization = OrganizationSettingsStorage.Load();

                if (organization != null)
                {
                    if (!string.IsNullOrWhiteSpace(organization.OrganizationName))
                    {
                        templateSettings.OrganizationName =
                            organization.OrganizationName;
                    }

                    // Apply organization logo only when the file exists.
                    // Otherwise, retain the logo saved in the template.
                    if (!string.IsNullOrWhiteSpace(organization.LogoPath) &&
                        File.Exists(organization.LogoPath))
                    {
                        templateSettings.LogoPath =
                            organization.LogoPath;
                    }

                    // Preserve existing signature functionality
                    templateSettings.SignaturePath =
                        organization.SignaturePath;

                    templateSettings.SignatoryName =
                        organization.SignatoryName;

                    templateSettings.SignatoryDesignation =
                        organization.SignatoryDesignation;
                }

                // Generate PDFs using the updated settings
                int count = BulkCertificateExporter.ExportFromExcel(
                    excelPath,
                    saveDialog.FileName,
                    templateSettings);

                System.Windows.MessageBox.Show(
                    $"{count} certificates generated successfully!\n\n" +
                    $"ZIP saved to:\n{saveDialog.FileName}",
                    "Export Completed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Certificate export failed:\n\n{ex.Message}",
                    "Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


    }
}
