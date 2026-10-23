
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CertificateGenerator
{
    public partial class OrganizationSettingsWindow : Window
    {
        private string _signaturePath = "";

        public OrganizationSettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            OrganizationSettings settings =
                OrganizationSettingsStorage.Load();

            OrganizationNameTextBox.Text =
                settings.OrganizationName;

            ContactPersonTextBox.Text =
                settings.ContactPerson;

            EmailTextBox.Text = settings.Email;
            PhoneTextBox.Text = settings.Phone;
            WebsiteTextBox.Text = settings.Website;
            AddressTextBox.Text = settings.Address;
            LogoPathTextBox.Text = settings.LogoPath;

            // Load authorised signatory settings
            _signaturePath = settings.SignaturePath ?? "";

            SignaturePathTextBox.Text = _signaturePath;
            SignatoryNameTextBox.Text =
                settings.SignatoryName ?? "";

            SignatoryDesignationTextBox.Text =
                settings.SignatoryDesignation ?? "";

            // Restore signature preview
            if (!string.IsNullOrWhiteSpace(_signaturePath)
                && File.Exists(_signaturePath))
            {
                try
                {
                    SignaturePreviewImage.Source =
                        LoadImage(_signaturePath);
                }
                catch
                {
                    SignaturePreviewImage.Source = null;
                }
            }
        }

        private static BitmapImage LoadImage(string path)
        {
            var image = new BitmapImage();

            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(Path.GetFullPath(path));
            image.EndInit();
            image.Freeze();

            return image;
        }

        private void BrowseLogoButton_Click(
            object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Organization Logo",
                Filter =
                    "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|" +
                    "All files (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                LogoPathTextBox.Text = dialog.FileName;
            }
        }

        private void BrowseSignatureButton_Click(
            object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Authorised Signature",
                Filter =
                    "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                var image = LoadImage(dialog.FileName);

                _signaturePath = dialog.FileName;
                SignaturePathTextBox.Text = _signaturePath;
                SignaturePreviewImage.Source = image;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Could not load signature image.\n\n{ex.Message}",
                    "Signature Image",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SaveSettingsButton_Click(
            object sender, RoutedEventArgs e)
        {
            string organizationName =
                OrganizationNameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(organizationName))
            {
                System.Windows.MessageBox.Show(
                    "Please enter the organization name.",
                    "Organization Settings",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                OrganizationNameTextBox.Focus();
                return;
            }

            string logoPath = LogoPathTextBox.Text.Trim();

            if (!string.IsNullOrWhiteSpace(logoPath)
                && !File.Exists(logoPath))
            {
                System.Windows.MessageBox.Show(
                    "The selected logo file does not exist. Please select a valid logo.",
                    "Invalid Logo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (!string.IsNullOrWhiteSpace(_signaturePath)
                && !File.Exists(_signaturePath))
            {
                System.Windows.MessageBox.Show(
                    "The selected signature file does not exist. Please select a valid signature.",
                    "Invalid Signature",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var settings = new OrganizationSettings
            {
                OrganizationName = organizationName,
                ContactPerson = ContactPersonTextBox.Text.Trim(),
                Email = EmailTextBox.Text.Trim(),
                Phone = PhoneTextBox.Text.Trim(),
                Website = WebsiteTextBox.Text.Trim(),
                Address = AddressTextBox.Text.Trim(),
                LogoPath = logoPath,

                // Authorised signatory settings
                SignaturePath = _signaturePath,
                SignatoryName =
                    SignatoryNameTextBox.Text.Trim(),
                SignatoryDesignation =
                    SignatoryDesignationTextBox.Text.Trim()
            };

            try
            {
                OrganizationSettingsStorage.Save(settings);

                System.Windows.MessageBox.Show(
                    "Organization settings saved successfully.",
                    "Organization Settings",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Could not save organization settings.\n\n{ex.Message}",
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
