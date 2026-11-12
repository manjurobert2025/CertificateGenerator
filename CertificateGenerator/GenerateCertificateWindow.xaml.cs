
using System;
using System.IO;
using System.Linq;
using System.Windows;
using CertificateGenerator.Data;
using CertificateGenerator.Models;
using CertificateGenerator.Services;

namespace CertificateGenerator
{
    public partial class GenerateCertificateWindow : Window
    {
        private CertificateRecord? _lastSavedCertificate;

        // Allows MainWindow to access the certificate saved in this window.
        public CertificateRecord? LastSavedCertificate
        {
            get { return _lastSavedCertificate; }
        }

        private SavedTemplate? SelectedTemplate =>
            TemplatesComboBox.SelectedItem as SavedTemplate;

        public GenerateCertificateWindow()
        {
            InitializeComponent();

            CertificateDatePicker.SelectedDate = DateTime.Today;

            LoadTemplates();
        }

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
                    int testIndex = templates.FindIndex(t =>
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

        private void GenerateButton_Click(
            object sender, RoutedEventArgs e)
        {
            string recipientName = RecipientNameTextBox.Text.Trim();
            string courseName = CourseNameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(recipientName))
            {
                System.Windows.MessageBox.Show("Please enter the recipient name.");
                RecipientNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(courseName))
            {
                System.Windows.MessageBox.Show(
                    "Please enter the course or event name.");

                CourseNameTextBox.Focus();
                return;
            }

            if (CertificateDatePicker.SelectedDate == null)
            {
                System.Windows.MessageBox.Show("Please select the certificate date.");
                return;
            }

            if (SelectedTemplate == null)
            {
                System.Windows.MessageBox.Show(
                    "Please select a certificate template.");

                return;
            }

            DateTime certificateDate =
                CertificateDatePicker.SelectedDate.Value;

            try
            {
                using var db = new CertificateDbContext();

                var existingIds = db.Certificates
                    .Select(c => c.CertificateId)
                    .ToList();

                var certificate = new CertificateRecord
                {
                    CertificateId =
                        CertificateIdGenerator.GenerateNextId(
                            existingIds,
                            DateTime.Today.Year),

                    RecipientName = recipientName,
                    CourseName = courseName,
                    IssueDate = certificateDate
                };

                db.Certificates.Add(certificate);
                db.SaveChanges();

                // Keep the saved certificate available in this window.
                _lastSavedCertificate = certificate;

                StatusTextBlock.Text =
                    $"Certificate saved successfully!\n\n" +
                    $"Certificate ID: {certificate.CertificateId}\n" +
                    $"Recipient: {recipientName}\n" +
                    $"Course: {courseName}\n" +
                    $"Date: {certificateDate:dd-MM-yyyy}";

                // Automatically generate the PDF.
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

                    var templateSettings =
                        ApplyOrganizationSettings(
                            SelectedTemplate.Settings);

                    CertificatePdfGenerator.Generate(
                        certificate.RecipientName,
                        certificate.CourseName,
                        certificate.IssueDate,
                        certificate.CertificateId,
                        pdfPath,
                        templateSettings);

                    certificate.PdfPath = pdfPath;

                    db.SaveChanges();

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
                        "PDF generation failed.\n\n" +
                        pdfEx.Message +
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
                FileName =
                    $"{_lastSavedCertificate.CertificateId}.pdf",
                DefaultExt = ".pdf",
                AddExtension = true
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                var certificate = _lastSavedCertificate;
                var selectedTemplate = SelectedTemplate;

                if (selectedTemplate == null)
                {
                    System.Windows.MessageBox.Show(
                        "Please select a certificate template.");

                    return;
                }

                var templateSettings =
                    ApplyOrganizationSettings(
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
            var organization = OrganizationSettingsStorage.Load();

            if (organization == null)
                return templateSettings;

            if (!string.IsNullOrWhiteSpace(
                organization.OrganizationName))
            {
                templateSettings.OrganizationName =
                    organization.OrganizationName;
            }

            if (!string.IsNullOrWhiteSpace(
                    organization.LogoPath) &&
                File.Exists(organization.LogoPath))
            {
                templateSettings.LogoPath =
                    organization.LogoPath;
            }

            if (!string.IsNullOrWhiteSpace(
                organization.SignaturePath))
            {
                templateSettings.SignaturePath =
                    organization.SignaturePath;
            }

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
                    System.Windows.MessageBox.Show(
                        "No certificate folder found.");

                    return;
                }

                var pdfFiles =
                    Directory.GetFiles(folder, "*.pdf");

                if (pdfFiles.Length == 0)
                {
                    System.Windows.MessageBox.Show(
                        "No generated PDF certificates found.");

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
                    "Could not preview the certificate.\n\n" +
                    ex.Message);
            }
        }
    }
}
