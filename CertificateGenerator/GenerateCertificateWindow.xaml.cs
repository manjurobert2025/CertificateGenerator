
using Microsoft.Win32;
using QuestPDF;
using System;
using System.IO;
using System.Windows;

namespace CertificateGenerator
{
    public partial class GenerateCertificateWindow : Window
    {
        private CertificateRecord? _lastSavedCertificate;
        private string? _lastGeneratedPdfPath;


        private SavedTemplate? SelectedTemplate =>
            TemplatesComboBox.SelectedItem as SavedTemplate;

        //public class SavedTemplate
        //{
        //    public string Name { get; set; } = "My Template";

        //    public string FilePath { get; set; } = "";

        //    public CertificateTemplateSettings Settings { get; set; }
        //        = new CertificateTemplateSettings();
        //}



        private void LoadTemplates()
        {
            try
            {
                var templates = TemplateStorage.GetTemplates();

                TemplatesComboBox.ItemsSource = null;
                TemplatesComboBox.DisplayMemberPath = "Name";
                TemplatesComboBox.ItemsSource = templates;

                if (templates.Count > 0)
                {
                    // Select Test Template if it exists;
                    // otherwise select the first available template.
                    var testIndex = templates.FindIndex(t =>
                        string.Equals(
                            t.Settings?.TemplateName,
                            "Test Template",
                            StringComparison.OrdinalIgnoreCase));

                    TemplatesComboBox.SelectedIndex =
                        testIndex >= 0 ? testIndex : 0;
                }
                else
                {
                    StatusTextBlock.Text =
                        "No saved templates found. Create and save a template first.";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text =
                    $"Error loading templates: {ex.Message}";
            }
        }




        public GenerateCertificateWindow()
        {
            InitializeComponent();
            CertificateDatePicker.SelectedDate = DateTime.Today;
            LoadTemplates();
        }

        private void GenerateButton_Click(
            object sender, RoutedEventArgs e)
        {
          
            string recipientName = RecipientNameTextBox.Text.Trim();
            string courseName = CourseNameTextBox.Text.Trim();
           
            var selectedTemplate = TemplatesComboBox.SelectedItem as SavedTemplate;
            System.Windows.MessageBox.Show(
     $"Selected item type: " +
     $"{TemplatesComboBox.SelectedItem?.GetType().FullName ?? "NULL"}\n" +
     $"Selected template name: " +
     $"{selectedTemplate?.Name ?? "NULL"}\n" +
     $"Settings template name: " +
     $"{selectedTemplate?.Settings?.TemplateName ?? "NULL"}\n" +
     $"Custom elements: " +
     $"{selectedTemplate?.Settings?.CustomTextElements?.Count ?? 0}");

            if (string.IsNullOrWhiteSpace(recipientName))
            {
                System.Windows.MessageBox.Show("Please enter the recipient name.");
                RecipientNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(courseName))
            {
                System.Windows.MessageBox.Show("Please enter the course or event name.");
                CourseNameTextBox.Focus();
                return;
            }

            if (CertificateDatePicker.SelectedDate == null)
            {
                System.Windows.MessageBox.Show("Please select the certificate date.");
                return;
            }

            DateTime certificateDate =
                CertificateDatePicker.SelectedDate.Value;

            try
            {
                using var db = new CertificateDbContext();

                var certificate = new CertificateRecord
                {
                    RecipientName = recipientName,
                    CourseName = courseName,
                    IssueDate = certificateDate
                };

                db.Certificates.Add(certificate);
                db.SaveChanges();

                certificate.CertificateId =
                    $"CERT-{DateTime.Now.Year}-{certificate.Id:D5}";

                db.SaveChanges();

                _lastSavedCertificate = certificate;

                StatusTextBlock.Text =
                    $"Certificate saved successfully!\n\n" +
                    $"Certificate ID: {certificate.CertificateId}\n" +
                    $"Recipient: {recipientName}\n" +
                    $"Course: {courseName}\n" +
                    $"Date: {certificateDate:dd-MM-yyyy}";

                // Option 1: Generate PDF automatically.
                try
                {
                    string folder = Path.Combine(
                        Environment.GetFolderPath(
                            Environment.SpecialFolder.MyDocuments),
                        "CertificateGenerator");

                    Directory.CreateDirectory(folder);

                    string pdfPath = Path.Combine(
                        folder,
                        $"{certificate.CertificateId}.pdf");

                    if (SelectedTemplate == null)
                    {
                        System.Windows.MessageBox.Show(
                            "No template is selected. Please select a template first.");
                        return;
                    }

                    System.Windows.MessageBox.Show(
                        $"Template: {SelectedTemplate.Name}\n" +
                        $"File: {SelectedTemplate.FilePath}\n" +
                        $"Custom elements: " +
                        $"{SelectedTemplate.Settings.CustomTextElements?.Count ?? 0}");

                    var templateSettings = ApplyOrganizationSettings(
    SelectedTemplate.Settings);

                    CertificatePdfGenerator.Generate(
                        certificate.RecipientName,
                        certificate.CourseName,
                        certificate.IssueDate,
                        certificate.CertificateId,
                        pdfPath,
                        templateSettings);

                    var settings = SelectedTemplate?.Settings;

                    System.Windows.MessageBox.Show(
                        $"Template: {SelectedTemplate?.Name}\n" +
                        $"Custom elements: {settings?.CustomTextElements?.Count ?? 0}");

                    System.Windows.MessageBox.Show(
                        $"Certificate saved to the database.\n\n" +
                        $"PDF created at:\n{pdfPath}",
                        "Certificate Generated",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception pdfEx)
                {
                    System.Windows.MessageBox.Show(
                        "Certificate details were saved, but automatic " +
                        "PDF generation failed.\n\n" + pdfEx.Message +
                        "\n\nYou can try Export Certificate PDF.",
                        "PDF Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "Could not save certificate details.\n\n" +
                    ex.Message,
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Option 2: Export the last saved certificate separately.
        private void ExportPdfButton_Click(
            object sender, RoutedEventArgs e)
        {
            if (_lastSavedCertificate == null)
            {
                System.Windows.MessageBox.Show(
                    "Please generate and save a certificate first.",
                    "No Certificate",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export Certificate PDF",
                Filter = "PDF files (*.pdf)|*.pdf",
                FileName = $"{_lastSavedCertificate.CertificateId}.pdf",
                DefaultExt = ".pdf",
                AddExtension = true
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                var certificate = _lastSavedCertificate;
                var selectedTemplate = TemplatesComboBox.SelectedItem as SavedTemplate;
               System.Windows.MessageBox.Show(
    $"Selected item: {TemplatesComboBox.SelectedItem}\n" +
    $"Selected index: {TemplatesComboBox.SelectedIndex}\n" +
    $"Template loaded: {selectedTemplate?.Name ?? "NULL"}");
                if (selectedTemplate == null)
                {
                    System.Windows.MessageBox.Show("Please select a certificate template.");
                    return;
                }

                var templateSettings = ApplyOrganizationSettings(
     selectedTemplate.Settings);

                CertificatePdfGenerator.Generate(
                    certificate.RecipientName,
                    certificate.CourseName,
                    certificate.IssueDate,
                    certificate.CertificateId,
                    dialog.FileName,
                    templateSettings);

                System.Windows.MessageBox.Show(
                    "Certificate PDF exported successfully!",
                    "PDF Export",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "PDF export failed.\n\n" + ex.Message,
                    "PDF Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private CertificateTemplateSettings ApplyOrganizationSettings(
            CertificateTemplateSettings templateSettings)
        {
            var organization =
                OrganizationSettingsStorage.Load();

            templateSettings.OrganizationName =
                organization.OrganizationName;

            templateSettings.SignaturePath =
                organization.SignaturePath;

            templateSettings.SignatoryName =
                organization.SignatoryName;

            templateSettings.SignatoryDesignation =
                organization.SignatoryDesignation;

            return templateSettings;
        }



        private void PreviewCertificateButton_Click(
            object sender, RoutedEventArgs e)
        {
            try
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.MyDocuments),
                    "CertificateGenerator");

                if (!Directory.Exists(folder))
                {
                    System.Windows.MessageBox.Show("No certificate folder found.");
                    return;
                }

                var pdfFiles = Directory.GetFiles(folder, "*.pdf");

                if (pdfFiles.Length == 0)
                {
                    System.Windows.MessageBox.Show("No generated PDF certificates found.");
                    return;
                }

                string pdfPath;

                if (_lastSavedCertificate != null)
                {
                    pdfPath = Path.Combine(
                        folder,
                        $"{_lastSavedCertificate.CertificateId}.pdf");
                }
                else
                {
                    pdfPath = pdfFiles
                        .OrderByDescending(File.GetLastWriteTime)
                        .First();
                }

                if (!File.Exists(pdfPath))
                {
                    System.Windows.MessageBox.Show(
                        "The certificate PDF was not found. " +
                        "Please check the CertificateGenerator folder.");
                    return;
                }

                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = pdfPath,
                        UseShellExecute = true
                    });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "Could not preview the certificate.\n\n" + ex.Message);
            }
        }


    }
}
