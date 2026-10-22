
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO;

namespace CertificateGenerator
{
    public partial class CertificateHistoryWindow : Window
    {
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

                var certificates = db.Certificates
                    .OrderByDescending(c => c.Id)
                    .ToList();

                CertificateDataGrid.ItemsSource = certificates;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "Could not load certificate history.\n\n" +
                    ex.Message,
                    "History Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void RefreshButton_Click(
            object sender, RoutedEventArgs e)
        {
            LoadCertificates();
        }

        private void SearchTextBox_TextChanged(
            object sender, TextChangedEventArgs e)
        {
            string search = SearchTextBox.Text.Trim()
                .ToLowerInvariant();

            try
            {
                using var db = new CertificateDbContext();

                var query = db.Certificates.AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(c =>
                        (c.CertificateId ?? "").ToLower().Contains(search) ||
                        (c.RecipientName ?? "").ToLower().Contains(search) ||
                        (c.CourseName ?? "").ToLower().Contains(search));
                }

                CertificateDataGrid.ItemsSource = query
                    .OrderByDescending(c => c.Id)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "Could not search certificates.\n\n" + ex.Message,
                    "Search Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void PreviewSelectedButton_Click(
            object sender, RoutedEventArgs e)
        {
            if (CertificateDataGrid.SelectedItem is not CertificateRecord certificate)
            {
                System.Windows.MessageBox.Show(
                    "Please select a certificate from the history list.",
                    "No Certificate Selected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.MyDocuments),
                    "CertificateGenerator");

                string pdfPath = Path.Combine(
                    folder,
                    $"{certificate.CertificateId}.pdf");

                if (!File.Exists(pdfPath))
                {
                    System.Windows.MessageBox.Show(
                        $"The PDF for certificate {certificate.CertificateId} was not found.\n\n" +
                        $"Expected location:\n{pdfPath}",
                        "PDF Not Found",
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
                    "Could not open the certificate PDF.\n\n" + ex.Message,
                    "Preview Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private void PrintSelectedButton_Click(
            object sender, RoutedEventArgs e)
        {
            if (CertificateDataGrid.SelectedItem is not CertificateRecord certificate)
            {
                System.Windows.MessageBox.Show(
                    "Please select a certificate from the history list.",
                    "No Certificate Selected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.MyDocuments),
                    "CertificateGenerator");

                string pdfPath = Path.Combine(
                    folder,
                    $"{certificate.CertificateId}.pdf");

                if (!File.Exists(pdfPath))
                {
                    System.Windows.MessageBox.Show(
                        $"Certificate PDF not found:\n{pdfPath}",
                        "PDF Not Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Open the existing PDF using the default PDF viewer.
                // The user can then choose Print in that application.
                Process.Start(new ProcessStartInfo
                {
                    FileName = pdfPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "Could not open the certificate PDF.\n\n" + ex.Message,
                    "Print Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


    }
}
