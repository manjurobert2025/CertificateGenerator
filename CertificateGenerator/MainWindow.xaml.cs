
using System;
using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;

namespace CertificateGenerator
{
    public partial class MainWindow : Window
    {
        private CertificateRecord? _lastSavedCertificate;
        public MainWindow()
        {
            InitializeComponent();

            using var db = new CertificateDbContext();
            db.Database.EnsureCreated();

            LoadDashboardData();
        }



        private void LoadDashboardData()
        {
            try
            {
                using var db = new CertificateDbContext();

                // Load total certificate count
                int totalCertificates = db.Certificates.Count();

                TotalCertificatesTextBlock.Text =
                    totalCertificates.ToString();

                // Load the five most recent certificates
                var recentCertificates = db.Certificates
                    .AsNoTracking()
                    .OrderByDescending(c => c.IssueDate)
                    .Take(5)
                    .ToList();

                RecentCertificatesDataGrid.ItemsSource =
                    recentCertificates;

                // Load saved template count
                var templates = TemplateStorage.GetTemplates();

                AvailableTemplatesTextBlock.Text =
                    templates.Count.ToString();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Could not load dashboard data:\n{ex.Message}",
                    "Dashboard Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }


        private void BulkGeneration_Click(
    object sender, RoutedEventArgs e)
        {
            var window = new BulkGenerationWindow
            {
                Owner = this
            };

            window.ShowDialog();

            // Refresh dashboard after bulk generation.
            LoadDashboardData();
        }

        private void InitializeDatabase()
        {
            try
            {
                using var db = new CertificateDbContext();
                db.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Database initialization failed:\n{ex.Message}",
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
                    "Please generate and save a certificate first.");
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export Certificate PDF",
                Filter = "PDF files (*.pdf)|*.pdf",
                FileName = $"{_lastSavedCertificate.CertificateId}.pdf"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                var certificate = _lastSavedCertificate;

                var organizationSettings = OrganizationSettingsStorage.Load();

                var templateSettings = new CertificateTemplateSettings(); 

                templateSettings.OrganizationName =
                    organizationSettings.OrganizationName;

                // Use the authorised signatory details from Organization Settings.
                templateSettings.SignaturePath =
                    organizationSettings.SignaturePath;

                templateSettings.SignatoryName =
                    organizationSettings.SignatoryName;

                templateSettings.SignatoryDesignation =
                    organizationSettings.SignatoryDesignation;

                CertificatePdfGenerator.Generate(
                    certificate.RecipientName,
                    certificate.CourseName,
                    certificate.IssueDate,
                    certificate.CertificateId,
                    dialog.FileName,
                    templateSettings);

                System.Windows.MessageBox.Show("Certificate PDF exported successfully!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"PDF export failed:\n{ex.Message}");
            }
        }
        private void GenerateCertificate_Click(
            object sender, RoutedEventArgs e)
        {
            GenerateCertificateWindow window =
                new GenerateCertificateWindow();

            window.ShowDialog();
        }
        private void CertificateHistory_Click(
    object sender, RoutedEventArgs e)
        {
            var historyWindow = new CertificateHistoryWindow
            {
                Owner = this
            };

            historyWindow.ShowDialog();
        }


        private void BulkPdfExport_Click(
            object sender,
            RoutedEventArgs e)
        {
            var csvDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Recipient CSV File",
                Filter = "CSV Files (*.csv)|*.csv"
            };

            if (csvDialog.ShowDialog() != true)
                return;

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save Bulk Certificate ZIP",
                Filter = "ZIP Files (*.zip)|*.zip",
                FileName = "Certificates.zip"
            };

            if (saveDialog.ShowDialog() != true)
                return;

            try
            {
                int count = BulkCertificateExporter.ExportFromCsv(
                    csvDialog.FileName,
                    saveDialog.FileName);

                System.Windows.MessageBox.Show(
                    $"{count} certificates generated successfully!\n\n" +
                    $"Saved to:\n{saveDialog.FileName}",
                    "Bulk Export Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    ex.Message,
                    "Bulk Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OrganizationSettingsButton_Click(
    object sender, RoutedEventArgs e)
        {
            OrganizationSettingsWindow window =
                new OrganizationSettingsWindow();

            window.Owner = this;
            window.ShowDialog();
        }

        private void MyTemplates_Click(
    object sender, RoutedEventArgs e)
        {
            var window = new MyTemplatesWindow
            {
                Owner = this
            };

            window.ShowDialog();

            LoadDashboardData();
        }
        private void TemplateDesigner_Click(
    object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Button clicked.");

            try
            {
                var window = new TemplateDesignerWindow
                {
                    Owner = this
                };

                System.Windows.MessageBox.Show("Window created successfully.");

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    ex.ToString(),
                    "Template Designer Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
    }

