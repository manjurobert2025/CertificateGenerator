
using CertificateGenerator.Data;
using CertificateGenerator.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace CertificateGenerator
{
    public partial class CertificateHistoryWindow : Window
    {
        private List<CertificateRecord> _allCertificates = new();

        public CertificateHistoryWindow()
        {
            InitializeComponent();
            LoadCertificates();
        }

        private void LoadCertificates()
        {
            try
            {
                using var db = new CertificateDbContext();

                _allCertificates = db.Certificates
                    .AsNoTracking()
                    .OrderByDescending(c => c.Id)
                    .ToList();

                ApplySearch();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Unable to load certificate history.\n\n{ex.Message}",
                    "Certificate History",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ApplySearch()
        {
            string searchText = SearchTextBox.Text.Trim();

            IEnumerable<CertificateRecord> filteredCertificates =
                _allCertificates;

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filteredCertificates = _allCertificates.Where(c =>
                    (c.CertificateId ?? string.Empty)
                        .Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    ||
                    (c.RecipientName ?? string.Empty)
                        .Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    ||
                    (c.CourseName ?? string.Empty)
                        .Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            CertificateDataGrid.ItemsSource = filteredCertificates.ToList();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
            LoadCertificates();
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
            ApplySearch();
        }

        private void SearchTextBox_TextChanged(
            object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            ApplySearch();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            ApplySearch();
        }

        private static string? GetExistingPdfPath(CertificateRecord certificate)
        {
            // Prefer the actual PDF path saved with the certificate.
            if (!string.IsNullOrWhiteSpace(certificate.PdfPath) &&
                File.Exists(certificate.PdfPath))
            {
                return certificate.PdfPath;
            }

            // Fallback for certificates created before PdfPath was saved.
            string legacyPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "CertificateGenerator",
                $"{certificate.CertificateId}.pdf");

            return File.Exists(legacyPath) ? legacyPath : null;
        }

        private void PreviewSelectedButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (CertificateDataGrid.SelectedItem is not CertificateRecord certificate)
            {
                System.Windows.MessageBox.Show("Please select a certificate.");
                return;
            }

            try
            {
                string? pdfPath = GetExistingPdfPath(certificate);

                if (pdfPath == null)
                {
                    System.Windows.MessageBox.Show(
                        "The PDF file could not be found.",
                        "Preview Certificate",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = pdfPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Unable to preview the certificate.\n\n{ex.Message}",
                    "Preview Certificate",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void PrintSelectedButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (CertificateDataGrid.SelectedItem is not CertificateRecord certificate)
            {
                System.Windows.MessageBox.Show("Please select a certificate.");
                return;
            }

            try
            {
                string? pdfPath = GetExistingPdfPath(certificate);

                if (pdfPath == null)
                {
                    System.Windows.MessageBox.Show(
                        "The PDF file could not be found.",
                        "Print Certificate",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                // Opens the PDF in the default application.
                // The user can print it from there.
                Process.Start(new ProcessStartInfo
                {
                    FileName = pdfPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Unable to open the certificate for printing.\n\n{ex.Message}",
                    "Print Certificate",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DeleteSelectedButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (CertificateDataGrid.SelectedItem is not CertificateRecord certificate)
            {
                System.Windows.MessageBox.Show("Please select a certificate.");
                return;
            }

            MessageBoxResult result = System.Windows.MessageBox.Show(
                $"Are you sure you want to remove the history record for certificate '{certificate.CertificateId}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using var db = new CertificateDbContext();

                CertificateRecord? record = db.Certificates
                    .FirstOrDefault(c => c.Id == certificate.Id);

                if (record == null)
                {
                    System.Windows.MessageBox.Show("The certificate record could not be found.");
                    LoadCertificates();
                    return;
                }

                db.Certificates.Remove(record);
                db.SaveChanges();

                System.Windows.MessageBox.Show("Certificate history record removed successfully.");

                LoadCertificates();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Unable to delete the certificate history record.\n\n{ex.Message}",
                    "Delete Certificate",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
