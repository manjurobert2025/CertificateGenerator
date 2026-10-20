
using Microsoft.Win32;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CertificateGenerator
{
    public class CertificateTemplateSettings
    {
        public string TemplateName { get; set; } = "Classic Gold";
        public string CertificateTitle { get; set; } =
            "CERTIFICATE OF ACHIEVEMENT";
        public string Subtitle { get; set; } =
            "This certificate is proudly presented to";
        public string OrganizationName { get; set; } =
            "Your Organization";
        public string FooterText { get; set; } =
            "Congratulations on your achievement!";
        public string AccentColor { get; set; } = "Gold";
        public string LogoPath { get; set; } = "";

        public double LogoX { get; set; } = 340;
        public double LogoY { get; set; } = 35;

        public double OrganizationX { get; set; } = 200;
        public double OrganizationY { get; set; } = 100;

        public double TitleX { get; set; } = 100;
        public double TitleY { get; set; } = 155;

        public double SubtitleX { get; set; } = 150;
        public double SubtitleY { get; set; } = 225;

        public double RecipientX { get; set; } = 150;
        public double RecipientY { get; set; } = 270;

        public double CourseX { get; set; } = 125;
        public double CourseY { get; set; } = 330;

        public double DateX { get; set; } = 250;
        public double DateY { get; set; } = 385;

        public double FooterX { get; set; } = 150;
        public double FooterY { get; set; } = 440;

        public double CertificateIdX { get; set; } = 250;
        public double CertificateIdY { get; set; } = 485;
    }

    public partial class TemplateDesignerWindow : Window
    {
        private string? _settingsPath;
        private string _legacySettingsPath = "";
        private bool _initializing;

        private UIElement? _draggedElement;
        private Point _dragStartPoint;
        private double _elementStartLeft;
        private double _elementStartTop;

        public TemplateDesignerWindow(string? templatePath = null)
        {
            InitializeComponent();

            string folder = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "CertificateGenerator");

            Directory.CreateDirectory(folder);

            _legacySettingsPath = Path.Combine(
                folder,
                "template-settings.json");

            // Null means creating a new template.
            // A path means editing an existing template.
            _settingsPath = templatePath;

            LoadTemplate();
        }

        // LOAD SAVED SETTINGS AND POSITIONS
        private void LoadTemplate()
        {
            _initializing = true;
            string loadPath = _settingsPath ?? _legacySettingsPath;
            try
            {
                if (File.Exists(loadPath))
                {
                    string json = File.ReadAllText(loadPath);

                    var settings =
                        JsonSerializer.Deserialize<CertificateTemplateSettings>(json);


                    if (settings != null)
                    {
                        TitleTextBox.Text =
                            settings.CertificateTitle;

                        SubtitleTextBox.Text =
                            settings.Subtitle;

                        OrganizationTextBox.Text =
                            settings.OrganizationName;

                        FooterTextBox.Text =
                            settings.FooterText;

                        SelectComboItem(
                            TemplateComboBox,
                            settings.TemplateName);

                        SelectComboItem(
                            AccentColorComboBox,
                            settings.AccentColor);

                        SetPosition(
                            LogoImage,
                            settings.LogoX,
                            settings.LogoY);

                        SetPosition(
                            PreviewOrganizationTextBlock,
                            settings.OrganizationX,
                            settings.OrganizationY);

                        SetPosition(
                            PreviewTitleTextBlock,
                            settings.TitleX,
                            settings.TitleY);

                        SetPosition(
                            PreviewSubtitleTextBlock,
                            settings.SubtitleX,
                            settings.SubtitleY);

                        SetPosition(
                            PreviewRecipientTextBlock,
                            settings.RecipientX,
                            settings.RecipientY);

                        SetPosition(
                            PreviewCourseTextBlock,
                            settings.CourseX,
                            settings.CourseY);

                        SetPosition(
                            PreviewDateTextBlock,
                            settings.DateX,
                            settings.DateY);

                        SetPosition(
                            PreviewFooterTextBlock,
                            settings.FooterX,
                            settings.FooterY);

                        SetPosition(
                            PreviewCertificateIdTextBlock,
                            settings.CertificateIdX,
                            settings.CertificateIdY);

                        if (!string.IsNullOrWhiteSpace(
                                settings.LogoPath) &&
                            File.Exists(settings.LogoPath))
                        {
                            LoadLogo(settings.LogoPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text =
                    $"Could not load saved template: {ex.Message}";
            }
            finally
            {
                _initializing = false;
            }

            UpdatePreview();
        }

        private static void SelectComboItem(
            ComboBox comboBox,
            string value)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (string.Equals(
                    item.Content?.ToString(),
                    value,
                    StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
        }

        private static void SetPosition(
            UIElement element,
            double left,
            double top)
        {
            Canvas.SetLeft(element, left);
            Canvas.SetTop(element, top);
        }

        // TEXT AND COLOR SETTINGS
        private void Settings_TextChanged(
            object sender,
            RoutedEventArgs e)
        {
            if (!_initializing)
                UpdatePreview();
        }

        private void TemplateComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!_initializing)
                UpdatePreview();
        }

        private string SelectedText(ComboBox comboBox)
        {
            return (comboBox.SelectedItem as ComboBoxItem)
                ?.Content?.ToString() ?? "";
        }

        private Color GetAccentColor()
        {
            string color = SelectedText(AccentColorComboBox);

            return color switch
            {
                "Blue" => Colors.RoyalBlue,
                "Green" => Colors.ForestGreen,
                "Purple" => Colors.MediumPurple,
                _ => Color.FromRgb(212, 175, 55)
            };
        }

        // UPDATE LIVE PREVIEW
        private void UpdatePreview()
        {
            // Guard against WPF events firing before initialization.
            if (TitleTextBox == null ||
                SubtitleTextBox == null ||
                OrganizationTextBox == null ||
                FooterTextBox == null ||
                AccentColorComboBox == null ||
                TemplateComboBox == null ||
                PreviewTitleTextBlock == null ||
                PreviewSubtitleTextBlock == null ||
                PreviewOrganizationTextBlock == null ||
                PreviewFooterTextBlock == null ||
                PreviewBorder == null)
            {
                return;
            }

            PreviewTitleTextBlock.Text = TitleTextBox.Text;
            PreviewSubtitleTextBlock.Text = SubtitleTextBox.Text;

            PreviewOrganizationTextBlock.Text =
                OrganizationTextBox.Text;

            PreviewFooterTextBlock.Text = FooterTextBox.Text;

            Color accent = GetAccentColor();

            string design = SelectedText(TemplateComboBox);

            if (design == "Modern Blue")
                accent = Colors.RoyalBlue;
            else if (design == "Elegant Green")
                accent = Colors.ForestGreen;

            PreviewBorder.BorderBrush =
                new SolidColorBrush(accent);

            PreviewTitleTextBlock.Foreground =
                new SolidColorBrush(accent);

            PreviewOrganizationTextBlock.Foreground =
                new SolidColorBrush(accent);
        }

        // CHOOSE ORGANIZATION LOGO
        private void ChooseLogo_Click(
            object sender,
            RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Choose Organization Logo",
                Filter =
                    "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                LoadLogo(dialog.FileName);
                StatusTextBlock.Text = "Logo loaded.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not load logo:\n{ex.Message}",
                    "Logo Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void LoadLogo(string path)
        {
            var image = new BitmapImage();

            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path);
            image.EndInit();
            image.Freeze();

            LogoImage.Source = image;
            LogoPathTextBlock.Text = path;
        }

        // START DRAGGING AN ELEMENT
        private void DesignElement_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is not UIElement element)
                return;

            _draggedElement = element;

            _dragStartPoint = e.GetPosition(DesignCanvas);

            _elementStartLeft = Canvas.GetLeft(element);
            _elementStartTop = Canvas.GetTop(element);

            if (double.IsNaN(_elementStartLeft))
                _elementStartLeft = 0;

            if (double.IsNaN(_elementStartTop))
                _elementStartTop = 0;

            element.CaptureMouse();

            e.Handled = true;
        }

        // MOVE ELEMENT WHILE DRAGGING
        private void DesignElement_MouseMove(
            object sender,
            MouseEventArgs e)
        {
            if (_draggedElement == null ||
                e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Point currentPoint = e.GetPosition(DesignCanvas);

            double newLeft =
                _elementStartLeft +
                currentPoint.X -
                _dragStartPoint.X;

            double newTop =
                _elementStartTop +
                currentPoint.Y -
                _dragStartPoint.Y;

            double elementWidth =
                _draggedElement.RenderSize.Width;

            double elementHeight =
                _draggedElement.RenderSize.Height;

            newLeft = Math.Clamp(
                newLeft,
                0,
                Math.Max(0, DesignCanvas.Width - elementWidth));

            newTop = Math.Clamp(
                newTop,
                0,
                Math.Max(0, DesignCanvas.Height - elementHeight));

            Canvas.SetLeft(_draggedElement, newLeft);
            Canvas.SetTop(_draggedElement, newTop);
        }

        // FINISH DRAGGING
        private void DesignElement_MouseLeftButtonUp(
            object sender,
            MouseButtonEventArgs e)
        {
            if (_draggedElement == null)
                return;

            _draggedElement.ReleaseMouseCapture();
            _draggedElement = null;

            e.Handled = true;
        }

        // SAVE SETTINGS AND POSITIONS
        private void SaveTemplate_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                var settings = new CertificateTemplateSettings
                {
                    TemplateName =
                        SelectedText(TemplateComboBox),

                    CertificateTitle =
                        TitleTextBox.Text.Trim(),

                    Subtitle =
                        SubtitleTextBox.Text.Trim(),

                    OrganizationName =
                        OrganizationTextBox.Text.Trim(),

                    FooterText =
                        FooterTextBox.Text.Trim(),

                    AccentColor =
                        SelectedText(AccentColorComboBox),

                    LogoPath =
                        LogoPathTextBlock.Text == "No logo selected"
                            ? ""
                            : LogoPathTextBlock.Text,

                    LogoX = Canvas.GetLeft(LogoImage),
                    LogoY = Canvas.GetTop(LogoImage),

                    OrganizationX =
                        Canvas.GetLeft(PreviewOrganizationTextBlock),

                    OrganizationY =
                        Canvas.GetTop(PreviewOrganizationTextBlock),

                    TitleX =
                        Canvas.GetLeft(PreviewTitleTextBlock),

                    TitleY =
                        Canvas.GetTop(PreviewTitleTextBlock),

                    SubtitleX =
                        Canvas.GetLeft(PreviewSubtitleTextBlock),

                    SubtitleY =
                        Canvas.GetTop(PreviewSubtitleTextBlock),

                    RecipientX =
                        Canvas.GetLeft(PreviewRecipientTextBlock),

                    RecipientY =
                        Canvas.GetTop(PreviewRecipientTextBlock),

                    CourseX =
                        Canvas.GetLeft(PreviewCourseTextBlock),

                    CourseY =
                        Canvas.GetTop(PreviewCourseTextBlock),

                    DateX =
                        Canvas.GetLeft(PreviewDateTextBlock),

                    DateY =
                        Canvas.GetTop(PreviewDateTextBlock),

                    FooterX =
                        Canvas.GetLeft(PreviewFooterTextBlock),

                    FooterY =
                        Canvas.GetTop(PreviewFooterTextBlock),

                    CertificateIdX =
                        Canvas.GetLeft(PreviewCertificateIdTextBlock),

                    CertificateIdY =
                        Canvas.GetTop(PreviewCertificateIdTextBlock)
                };

                if (string.IsNullOrWhiteSpace(
                    settings.CertificateTitle))
                {
                    MessageBox.Show(
                        "Please enter a certificate title.");

                    return;
                }

                string json = JsonSerializer.Serialize(
                    settings,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                if (string.IsNullOrWhiteSpace(_settingsPath))
                {
                    _settingsPath = TemplateStorage.CreateTemplatePath(
                        settings.TemplateName);
                }

                File.WriteAllText(_settingsPath, json);

                StatusTextBlock.Text =
                    "Template and positions saved successfully.";

                MessageBox.Show(
                    "Template saved successfully.",
                    "Template Designer",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not save template:\n{ex.Message}",
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
