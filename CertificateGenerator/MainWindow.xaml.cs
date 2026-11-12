
using System;
using System.Linq;
using System.Windows;
using CertificateGenerator.Data;
using CertificateGenerator.Models;
using CertificateGenerator.Services;
using Microsoft.EntityFrameworkCore;

namespace CertificateGenerator
{
    public partial class MainWindow : Window
    {
        private CertificateRecord? _lastSavedCertificate;

        public MainWindow()
        {
            InitializeComponent();

            InitializeDatabase();

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

        private void LoadDashboardData()
        {
            try
            {
                using var db = new CertificateDbContext();

                // Total certificates
                TotalCertificatesTextBlock.Text =
                    db.Certificates.Count().ToString();

                // Available templates
                var templates = TemplateStorage.GetTemplates();

                AvailableTemplatesTextBlock.Text =
                    templates.Count.ToString();

                // Recent certificates
                var recentCertificates = db.Certificates
                    .AsNoTracking()
                    .OrderByDescending(c => c.IssueDate)
                    .Take(10)
                    .ToList();

                RecentCertificatesDataGrid.ItemsSource =
                    recentCertificates;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Could not load dashboard data:\n{ex.Message}",
                    "Dashboard Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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

                var organizationSettings =
                    OrganizationSettingsStorage.Load();

                var templateSettings =
                    new CertificateTemplateSettings();

                templateSettings.OrganizationName =
                    organizationSettings.OrganizationName;

                // Authorized signatory details
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

                System.Windows.MessageBox.Show(
                    "Certificate PDF exported successfully!",
                    "PDF Export",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"PDF export failed:\n{ex.Message}",
                    "PDF Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void GenerateCertificate_Click(
            object sender, RoutedEventArgs e)
        {
            var window = new GenerateCertificateWindow
            {
                Owner = this
            };

            window.ShowDialog();

            // Refresh dashboard after certificate generation.
            LoadDashboardData();
        }

        private void CertificateHistory_Click(
            object sender, RoutedEventArgs e)
        {
            var historyWindow = new CertificateHistoryWindow
            {
                Owner = this
            };

            historyWindow.ShowDialog();

            // Refresh dashboard after history window closes.
            LoadDashboardData();
        }

        private void PdfExportButton_Click(
            object sender, RoutedEventArgs e)
        {
            var window = new PdfExportWindow
            {
                Owner = this
            };

            window.ShowDialog();

            LoadDashboardData();
        }

        private void OrganizationSettingsButton_Click(
            object sender, RoutedEventArgs e)
        {
            var window = new OrganizationSettingsWindow
            {
                Owner = this
            };

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

            window.Owner = this;
            window.ShowDialog();

            LoadDashboardData();
        }

        private void TemplateDesigner_Click(
            object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new TemplateDesignerWindow
                {
                    Owner = this
                };

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
