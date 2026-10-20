
using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace CertificateGenerator
{
    public partial class GenerateCertificateWindow : Window
    {
        private CertificateRecord? _lastSavedCertificate;
        private string? _lastGeneratedPdfPath;

        public GenerateCertificateWindow()
        {
            InitializeComponent();
            CertificateDatePicker.SelectedDate = DateTime.Today;
        }

        private void GenerateButton_Click(
            object sender, RoutedEventArgs e)
        {
            string recipientName = RecipientNameTextBox.Text.Trim();
            string courseName = CourseNameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(recipientName))
            {
                MessageBox.Show("Please enter the recipient name.");
                RecipientNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(courseName))
            {
                MessageBox.Show("Please enter the course or event name.");
                CourseNameTextBox.Focus();
                return;
            }

            if (CertificateDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Please select the certificate date.");
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

                    CertificatePdfGenerator.Generate(
                        certificate.RecipientName,
                        certificate.CourseName,
                        certificate.IssueDate,
                        certificate.CertificateId,
                        pdfPath);

                    MessageBox.Show(
                        $"Certificate saved to the database.\n\n" +
                        $"PDF created at:\n{pdfPath}",
                        "Certificate Generated",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception pdfEx)
                {
                    MessageBox.Show(
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
                MessageBox.Show(
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
                MessageBox.Show(
                    "Please generate and save a certificate first.",
                    "No Certificate",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
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

                CertificatePdfGenerator.Generate(
                    certificate.RecipientName,
                    certificate.CourseName,
                    certificate.IssueDate,
                    certificate.CertificateId,
                    dialog.FileName);

                MessageBox.Show(
                    "Certificate PDF exported successfully!",
                    "PDF Export",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "PDF export failed.\n\n" + ex.Message,
                    "PDF Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
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
                    MessageBox.Show("No certificate folder found.");
                    return;
                }

                var pdfFiles = Directory.GetFiles(folder, "*.pdf");

                if (pdfFiles.Length == 0)
                {
                    MessageBox.Show("No generated PDF certificates found.");
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
                    MessageBox.Show(
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
                MessageBox.Show(
                    "Could not preview the certificate.\n\n" + ex.Message);
            }
        }


    }
}
