
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

                // Display certificates in the dashboard table
                RecentCertificatesDataGrid.ItemsSource =
                    recentCertificates;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
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
                MessageBox.Show(
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
                MessageBox.Show(
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

                CertificatePdfGenerator.Generate(
                    certificate.RecipientName,
                    certificate.CourseName,
                    certificate.IssueDate,
                    certificate.CertificateId,
                    dialog.FileName);

                MessageBox.Show("Certificate PDF exported successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF export failed:\n{ex.Message}");
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

                MessageBox.Show(
                    $"{count} certificates generated successfully!\n\n" +
                    $"Saved to:\n{saveDialog.FileName}",
                    "Bulk Export Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Bulk Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
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
            MessageBox.Show("Button clicked.");

            try
            {
                var window = new TemplateDesignerWindow
                {
                    Owner = this
                };

                MessageBox.Show("Window created successfully.");

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "Template Designer Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
    }

