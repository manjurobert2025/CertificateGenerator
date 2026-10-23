
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushConverter = System.Windows.Media.BrushConverter;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;
using WpfColors = System.Windows.Media.Colors;

namespace CertificateGenerator
{
    public class CustomTextElement
    {
        public string Text { get; set; } = "Additional Text";
        public double X { get; set; } = 100;
        public double Y { get; set; } = 500;
        public double FontSize { get; set; } = 16;
        public string Color { get; set; } = "#000000";

        public override string ToString() => Text;
    }

    public class CertificateTemplateSettings
    {
        public string TemplateName { get; set; } = "Classic Gold";
        public string CertificateTitle { get; set; } = "CERTIFICATE OF ACHIEVEMENT";
        public string Subtitle { get; set; } = "This certificate is proudly presented to";
        public string OrganizationName { get; set; } = "Your Organization";
        public string FooterText { get; set; } = "Congratulations on your achievement!";
        public string AccentColor { get; set; } = "Gold";
        public string LogoPath { get; set; } = "";

        public string BorderColor { get; set; } = "";
        public string OrganizationColor { get; set; } = "";
        public string TitleColor { get; set; } = "";
        public string SubtitleColor { get; set; } = "";
        public string RecipientColor { get; set; } = "";
        public string CourseColor { get; set; } = "";
        public string DateColor { get; set; } = "";
        public string FooterColor { get; set; } = "";
        public string CertificateIdColor { get; set; } = "";

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
        public bool ShowBorder { get; set; } = true;
        public bool ShowLogo { get; set; } = true;
        public bool ShowOrganization { get; set; } = true;
        public bool ShowTitle { get; set; } = true;
        public bool ShowSubtitle { get; set; } = true;
        public bool ShowRecipient { get; set; } = true;
        public bool ShowCourse { get; set; } = true;
        public bool ShowDate { get; set; } = true;
        public bool ShowFooter { get; set; } = true;
        public bool ShowCertificateId { get; set; } = true;

        // Added for custom text support. Older templates remain compatible.
        public List<CustomTextElement> CustomTextElements { get; set; } = new();
        public string SignaturePath { get; set; } = "";
        public string SignatoryName { get; set; } = "";
        public string SignatoryDesignation { get; set; } = "";

        public double SignatureX { get; set; } = 250;
        public double SignatureY { get; set; } = 400;
        public double SignatureWidth { get; set; } = 120;
        public double SignatureHeight { get; set; } = 60;

        public bool ShowSignature { get; set; } = true;
    }

    public partial class TemplateDesignerWindow : Window
    {
        private string? _settingsPath;
        private string _signaturePath = "";
        private string _legacySettingsPath = "";
        private bool _initializing;

        private UIElement? _draggedElement;
        private System.Windows.Point _dragStartPoint;
        private double _elementStartLeft;
        private double _elementStartTop;

        private string _borderColor = "";
        private string _organizationColor = "";
        private string _titleColor = "";
        private string _subtitleColor = "";
        private string _recipientColor = "";
        private string _courseColor = "";
        private string _dateColor = "";
        private string _footerColor = "";
        private string _certificateIdColor = "";
        public bool ShowLogo { get; set; } = true;
        public bool ShowOrganization { get; set; } = true;
        public bool ShowTitle { get; set; } = true;
        public bool ShowSubtitle { get; set; } = true;
        public bool ShowRecipient { get; set; } = true;
        public bool ShowCourse { get; set; } = true;
        public bool ShowDate { get; set; } = true;
        public bool ShowFooter { get; set; } = true;
        public bool ShowCertificateId { get; set; } = true;

        private readonly List<CustomTextElement> _customTextElements = new();

        private readonly Dictionary<CustomTextElement, TextBlock>
            _customTextBlocks = new();

        public TemplateDesignerWindow(string? templatePath = null)
        {
            InitializeComponent();

            string folder = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "CertificateGenerator");

            Directory.CreateDirectory(folder);

            _legacySettingsPath =
                Path.Combine(folder, "template-settings.json");

            _settingsPath = templatePath;

            LoadTemplate();

            // Apply organization settings only to a new template.
            if (string.IsNullOrWhiteSpace(templatePath))
            {
                ApplyOrganizationDefaults();
            }
        }

        // LOAD SAVED TEMPLATE
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
                        JsonSerializer.Deserialize<CertificateTemplateSettings>(
                            json);

                    if (settings != null)
                    {
                        TemplateNameTextBox.Text =
                            settings.TemplateName ?? "Classic Gold";

                        TitleTextBox.Text =
                            settings.CertificateTitle ?? "";

                        SubtitleTextBox.Text =
                            settings.Subtitle ?? "";

                        OrganizationTextBox.Text =
                            settings.OrganizationName ?? "";

                        FooterTextBox.Text =
                            settings.FooterText ?? "";

                        SelectComboItem(TemplateComboBox, settings.TemplateName);
                        SelectComboItem(AccentColorComboBox, settings.AccentColor);

                        _borderColor = settings.BorderColor ?? "";
                        _organizationColor = settings.OrganizationColor ?? "";
                        _titleColor = settings.TitleColor ?? "";
                        _subtitleColor = settings.SubtitleColor ?? "";
                        _recipientColor = settings.RecipientColor ?? "";
                        _courseColor = settings.CourseColor ?? "";
                        _dateColor = settings.DateColor ?? "";
                        _footerColor = settings.FooterColor ?? "";
                        _certificateIdColor =
                            settings.CertificateIdColor ?? "";

                        SetPosition(
                            LogoImage, settings.LogoX, settings.LogoY);

                        SetPosition(
                            PreviewOrganizationTextBlock,
                            settings.OrganizationX, settings.OrganizationY);

                        SetPosition(
                            PreviewTitleTextBlock,
                            settings.TitleX, settings.TitleY);

                        SetPosition(
                            PreviewSubtitleTextBlock,
                            settings.SubtitleX, settings.SubtitleY);

                        SetPosition(
                            PreviewRecipientTextBlock,
                            settings.RecipientX, settings.RecipientY);

                        SetPosition(
                            PreviewCourseTextBlock,
                            settings.CourseX, settings.CourseY);

                        SetPosition(
                            PreviewDateTextBlock,
                            settings.DateX, settings.DateY);

                        SetPosition(
                            PreviewFooterTextBlock,
                            settings.FooterX, settings.FooterY);

                        SetPosition(
                            PreviewCertificateIdTextBlock,
                            settings.CertificateIdX,
                            settings.CertificateIdY);

                        CertificateBorder.Visibility =
                            settings.ShowBorder ? Visibility.Visible : Visibility.Collapsed;

                        LogoImage.Visibility =
                            settings.ShowLogo ? Visibility.Visible : Visibility.Collapsed;

                        PreviewOrganizationTextBlock.Visibility =
                            settings.ShowOrganization ? Visibility.Visible : Visibility.Collapsed;

                        PreviewTitleTextBlock.Visibility =
                            settings.ShowTitle ? Visibility.Visible : Visibility.Collapsed;

                        PreviewSubtitleTextBlock.Visibility =
                            settings.ShowSubtitle ? Visibility.Visible : Visibility.Collapsed;

                        PreviewRecipientTextBlock.Visibility =
                            settings.ShowRecipient ? Visibility.Visible : Visibility.Collapsed;

                        PreviewCourseTextBlock.Visibility =
                            settings.ShowCourse ? Visibility.Visible : Visibility.Collapsed;

                        PreviewDateTextBlock.Visibility =
                            settings.ShowDate ? Visibility.Visible : Visibility.Collapsed;

                        PreviewFooterTextBlock.Visibility =
                            settings.ShowFooter ? Visibility.Visible : Visibility.Collapsed;

                        PreviewCertificateIdTextBlock.Visibility =
                            settings.ShowCertificateId ? Visibility.Visible : Visibility.Collapsed;


                        if (!string.IsNullOrWhiteSpace(settings.LogoPath)
                            && File.Exists(settings.LogoPath))
                        {
                            LoadLogo(settings.LogoPath);
                        }
                        // Restore signature settings
                        _signaturePath = settings.SignaturePath ?? "";

                        SignatureImage.Width = settings.SignatureWidth > 0
                            ? settings.SignatureWidth
                            : 120;

                        SignatureImage.Height = settings.SignatureHeight > 0
                            ? settings.SignatureHeight
                            : 60;

                        SetPosition(
                            SignatureImage,
                            settings.SignatureX,
                            settings.SignatureY);

                        if (!string.IsNullOrWhiteSpace(_signaturePath)
                            && File.Exists(_signaturePath))
                        {
                            var signature = new BitmapImage();

                            signature.BeginInit();
                            signature.CacheOption = BitmapCacheOption.OnLoad;
                            signature.UriSource = new Uri(_signaturePath);
                            signature.EndInit();
                            signature.Freeze();

                            SignatureImage.Source = signature;

                            SignatureImage.Visibility = settings.ShowSignature
                                ? Visibility.Visible
                                : Visibility.Collapsed;
                        }
                        else
                        {
                            SignatureImage.Source = null;
                            SignatureImage.Visibility = Visibility.Collapsed;
                        }

                        LoadCustomTextElements(
                            settings.CustomTextElements);
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
            UpdateSelectedColorPreview();
        }

        private void UploadSignature_Click(
    object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Signature Image",
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(dialog.FileName);
                image.EndInit();
                image.Freeze();

                SignatureImage.Source = image;
                _signaturePath = dialog.FileName;
                SignatureImage.Visibility = Visibility.Visible;

                Canvas.SetLeft(SignatureImage, 250);
                Canvas.SetTop(SignatureImage, 400);

                StatusTextBlock.Text =
                    "Signature added. Drag it into position, then save the template.";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Could not load signature: {ex.Message}",
                    "Signature Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        private static void SelectComboItem(
            WpfComboBox comboBox,
            string? value)
        {
            foreach (System.Windows.Controls.ComboBoxItem item in comboBox.Items)
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
            UIElement element, double left, double top)
        {
            Canvas.SetLeft(element, left);
            Canvas.SetTop(element, top);
        }

        private void Settings_TextChanged(
            object sender, RoutedEventArgs e)
        {
            if (!_initializing)
                UpdatePreview();
        }

        private void TemplateComboBox_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (!_initializing)
                UpdatePreview();
        }

        private void ElementColorComboBox_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (!_initializing)
                UpdateSelectedColorPreview();
        }

        private string SelectedText(WpfComboBox comboBox)
        {
            return (comboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)
                ?.Content?.ToString() ?? "";
        }

        private WpfColor GetAccentColor()
        {
            string color = SelectedText(AccentColorComboBox);

            return color switch
            {
                "Blue" => WpfColors.RoyalBlue,
                "Green" => WpfColors.ForestGreen,
                "Purple" => WpfColors.MediumPurple,
                _ => WpfColor.FromRgb(212, 175, 55)
            };
        }

        // UPDATE THE STANDARD CERTIFICATE PREVIEW
        private void UpdatePreview()
        {
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
                PreviewBorder == null ||
                CertificateBorder == null)
            {
                return;
            }

            PreviewTitleTextBlock.Text = TitleTextBox.Text;
            PreviewSubtitleTextBlock.Text = SubtitleTextBox.Text;
            PreviewOrganizationTextBlock.Text = OrganizationTextBox.Text;
            PreviewFooterTextBlock.Text = FooterTextBox.Text;

            WpfColor accent = GetAccentColor();
            string design = SelectedText(TemplateComboBox);

            if (design == "Modern Blue")
                accent = Colors.RoyalBlue;
            else if (design == "Elegant Green")
                accent = Colors.ForestGreen;

            PreviewBorder.BorderBrush = new SolidColorBrush(accent);

            PreviewTitleTextBlock.Foreground =
                new SolidColorBrush(accent);

            PreviewOrganizationTextBlock.Foreground =
                new SolidColorBrush(accent);

            ApplyCustomBorderColor(CertificateBorder, _borderColor);
            ApplyCustomColor(PreviewOrganizationTextBlock, _organizationColor);
            ApplyCustomColor(PreviewTitleTextBlock, _titleColor);
            ApplyCustomColor(PreviewSubtitleTextBlock, _subtitleColor);
            ApplyCustomColor(PreviewRecipientTextBlock, _recipientColor);
            ApplyCustomColor(PreviewCourseTextBlock, _courseColor);
            ApplyCustomColor(PreviewDateTextBlock, _dateColor);
            ApplyCustomColor(PreviewFooterTextBlock, _footerColor);
            ApplyCustomColor(
                PreviewCertificateIdTextBlock, _certificateIdColor);

        }

        private static void ApplyCustomColor(
            TextBlock element, string? hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return;

            try
            {
                var brush = new System.Windows.Media.BrushConverter()
     .ConvertFromString(hex) as System.Windows.Media.Brush;

                if (brush != null)
                    element.Foreground = brush;
            }
            catch
            {
                // Keep the current color if the saved color is invalid.
            }
        }

        private static void ApplyCustomBorderColor(
            Border element, string? hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return;

            try
            {
                var brush = new System.Windows.Media.BrushConverter()
     .ConvertFromString(hex) as System.Windows.Media.Brush;

                if (brush != null)
                    element.BorderBrush = brush;
            }
            catch
            {
                // Keep the current border color if invalid.
            }
        }

        private string GetSelectedElementColor()
        {
            return SelectedText(ElementColorComboBox) switch
            {
                "Certificate Border" => _borderColor,
                "Organization Name" => _organizationColor,
                "Certificate Title" => _titleColor,
                "Subtitle" => _subtitleColor,
                "Recipient Name" => _recipientColor,
                "Course Name" => _courseColor,
                "Issue Date" => _dateColor,
                "Footer Text" => _footerColor,
                "Certificate ID" => _certificateIdColor,
                _ => ""
            };
        }

        private void UpdateSelectedColorPreview()
        {
            if (SelectedColorPreview == null ||
                ElementColorComboBox == null)
                return;

            string hex = GetSelectedElementColor();

            if (string.IsNullOrWhiteSpace(hex))
            {
                SelectedColorPreview.Background =
                    new SolidColorBrush(GetAccentColor());
                return;
            }

            try
            {
                var brush = new System.Windows.Media.BrushConverter()
                    .ConvertFromString(hex) as System.Windows.Media.Brush;

                if (brush != null)
                    SelectedColorPreview.Background = brush;
            }
            catch
            {
                SelectedColorPreview.Background =
                    System.Windows.Media.Brushes.Transparent;
            }
        }

        private void ChooseElementColor_Click(
            object sender, RoutedEventArgs e)
        {
            string element = SelectedText(ElementColorComboBox);

            using var dialog = new Forms.ColorDialog
            {
                FullOpen = true
            };

            if (dialog.ShowDialog() != Forms.DialogResult.OK)
                return;

            var c = dialog.Color;
            string hex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";

            SaveElementColor(element, hex);
            UpdatePreview();
            UpdateSelectedColorPreview();

            StatusTextBlock.Text =
                $"{element} color changed to {hex}. Click Save Template to keep it.";
        }

        private void SaveElementColor(string element, string hex)
        {
            switch (element)
            {
                case "Certificate Border":
                    _borderColor = hex;
                    break;
                case "Organization Name":
                    _organizationColor = hex;
                    break;
                case "Certificate Title":
                    _titleColor = hex;
                    break;
                case "Subtitle":
                    _subtitleColor = hex;
                    break;
                case "Recipient Name":
                    _recipientColor = hex;
                    break;
                case "Course Name":
                    _courseColor = hex;
                    break;
                case "Issue Date":
                    _dateColor = hex;
                    break;
                case "Footer Text":
                    _footerColor = hex;
                    break;
                case "Certificate ID":
                    _certificateIdColor = hex;
                    break;
            }
        }

        // CHOOSE ORGANIZATION LOGO
        private void ChooseLogo_Click(
            object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Choose Organization Logo",
                Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                LoadLogo(dialog.FileName);
                StatusTextBlock.Text =
                    "Logo loaded. Click Save Template to keep the logo path.";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
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

        // ADD A CUSTOM TEXT ELEMENT
        private void AddTextElement_Click(
            object sender, RoutedEventArgs e)
        {
            string text = CustomTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(text))
            {
                System.Windows.MessageBox.Show("Please enter some text first.");
                return;
            }

            var item = new CustomTextElement
            {
                Text = text,
                X = 100,
                Y = 500,
                FontSize = 16,
                Color = "#000000"
            };

            AddCustomTextToCanvas(item);

            CustomTextElementsListBox.SelectedItem = item;

            StatusTextBlock.Text =
                "Text added. Drag it to position it, then save the template.";
        }

        // CREATE A CANVAS TEXT BLOCK FROM A SAVED MODEL
        private void AddCustomTextToCanvas(CustomTextElement item)
        {
            var block = new TextBlock
            {
                Text = item.Text,
                FontSize = item.FontSize,
                Foreground = CreateBrush(item.Color),
                Width = 400,
                TextWrapping = TextWrapping.Wrap,
                Cursor = System.Windows.Input.Cursors.SizeAll,
                Tag = "CustomText",
                Padding = new Thickness(2)
            };

            block.MouseLeftButtonDown += DesignElement_MouseLeftButtonDown;
            block.MouseMove += DesignElement_MouseMove;
            block.MouseLeftButtonUp += DesignElement_MouseLeftButtonUp;

            DesignCanvas.Children.Add(block);
            SetPosition(block, item.X, item.Y);

            _customTextElements.Add(item);
            _customTextBlocks[item] = block;
            CustomTextElementsListBox.Items.Add(item);
        }

        private static WpfBrush CreateBrush(string? hex)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(hex))
                {
                    var brush = new WpfBrushConverter()
                        .ConvertFromString(hex) as WpfBrush;

                    if (brush != null)
                        return brush;
                }
            }
            catch
            {
                // Use black for an invalid saved custom color.
            }

            return WpfBrushes.Black;
        }

        // RESTORE CUSTOM TEXT FROM A SAVED TEMPLATE
        private void LoadCustomTextElements(
            List<CustomTextElement>? savedItems)
        {
            foreach (var block in _customTextBlocks.Values.ToList())
                DesignCanvas.Children.Remove(block);

            _customTextElements.Clear();
            _customTextBlocks.Clear();
            CustomTextElementsListBox.Items.Clear();

            if (savedItems == null)
                return;

            foreach (CustomTextElement item in savedItems)
            {
                if (item == null)
                    continue;

                AddCustomTextToCanvas(item);
            }
        }

        // DELETE THE SELECTED CUSTOM TEXT
        private void DeleteTextElement_Click(
            object sender, RoutedEventArgs e)
        {
            if (CustomTextElementsListBox.SelectedItem
                is not CustomTextElement item)
            {
                System.Windows.MessageBox.Show(
                    "Please select a text element to delete.");
                return;
            }

            if (_customTextBlocks.TryGetValue(item, out TextBlock? block))
            {
                if (_draggedElement == block)
                    _draggedElement = null;

                block.ReleaseMouseCapture();
                DesignCanvas.Children.Remove(block);
                _customTextBlocks.Remove(item);
            }

            _customTextElements.Remove(item);
            CustomTextElementsListBox.Items.Remove(item);

            StatusTextBlock.Text = "Selected text element deleted.";
        }

        // SELECT A CUSTOM TEXT ITEM
        private void CustomTextElementsListBox_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (CustomTextElementsListBox.SelectedItem
                is CustomTextElement item)
            {
                CustomTextBox.Text = item.Text;
            }
        }

        // START DRAGGING ANY SUPPORTED CANVAS ELEMENT
        private void DesignElement_MouseLeftButtonDown(
            object sender, MouseButtonEventArgs e)
        {
            if (sender is not UIElement element)
                return;
            _selectedElement = element;
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
            object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_draggedElement == null ||
                e.LeftButton != MouseButtonState.Pressed)
                return;

            System.Windows.Point currentPoint = e.GetPosition(DesignCanvas);

            double newLeft = _elementStartLeft
                + currentPoint.X - _dragStartPoint.X;

            double newTop = _elementStartTop
                + currentPoint.Y - _dragStartPoint.Y;

            double elementWidth = _draggedElement.RenderSize.Width;
            double elementHeight = _draggedElement.RenderSize.Height;

            newLeft = Math.Clamp(
                newLeft, 0,
                Math.Max(0, DesignCanvas.Width - elementWidth));

            newTop = Math.Clamp(
                newTop, 0,
                Math.Max(0, DesignCanvas.Height - elementHeight));

            Canvas.SetLeft(_draggedElement, newLeft);
            Canvas.SetTop(_draggedElement, newTop);
        }
        private UIElement? _selectedElement;

        private void DeleteSelectedElement_Click(
            object sender, RoutedEventArgs e)
        {
            if (_selectedElement == null)
            {
                System.Windows.MessageBox.Show(
                    "Click an element on the certificate first.");
                return;
            }

            UIElement element = _selectedElement;

            // Hide the border but retain the control.
            if (element == CertificateBorder)
            {
                CertificateBorder.Visibility = Visibility.Collapsed;
            }
            else if (element == LogoImage)
            {
                LogoImage.Visibility = Visibility.Collapsed;
                LogoPathTextBlock.Text = "No logo selected";
            }
            else if (element == PreviewOrganizationTextBlock ||
                     element == PreviewTitleTextBlock ||
                     element == PreviewSubtitleTextBlock ||
                     element == PreviewRecipientTextBlock ||
                     element == PreviewCourseTextBlock ||
                     element == PreviewDateTextBlock ||
                     element == PreviewFooterTextBlock ||
                     element == PreviewCertificateIdTextBlock)
            {
                element.Visibility = Visibility.Collapsed;
            }
            else
            {
                // If it is custom text, remove it from every collection.
                var match = _customTextBlocks.FirstOrDefault(
                    pair => pair.Value == element);

                if (match.Key != null)
                {
                    _customTextElements.Remove(match.Key);
                    _customTextBlocks.Remove(match.Key);
                    CustomTextElementsListBox.Items.Remove(match.Key);
                }

                DesignCanvas.Children.Remove(element);
            }

            if (_draggedElement == element)
            {
                element.ReleaseMouseCapture();
                _draggedElement = null;
            }

            _selectedElement = null;

            StatusTextBlock.Text =
                "Element deleted. Save the template to keep the change.";
        }

        // FINISH DRAGGING
        private void DesignElement_MouseLeftButtonUp(
            object sender, MouseButtonEventArgs e)
        {
            if (_draggedElement == null)
                return;

            _draggedElement.ReleaseMouseCapture();
            _draggedElement = null;
            e.Handled = true;
        }

        // SAVE SETTINGS, COLORS, POSITIONS AND CUSTOM TEXT

        private void ApplyOrganizationDefaults()
        {
            OrganizationSettings organization =
                OrganizationSettingsStorage.Load();

            if (!string.IsNullOrWhiteSpace(organization.OrganizationName))
            {
                OrganizationTextBox.Text =
                    organization.OrganizationName;
            }

            if (!string.IsNullOrWhiteSpace(organization.LogoPath) &&
                File.Exists(organization.LogoPath))
            {
                LoadLogo(organization.LogoPath);
            }
            UpdatePreview();
        }

        private void SaveTemplate_Click(
            object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = new CertificateTemplateSettings
                {
                    TemplateName = TemplateNameTextBox.Text.Trim(),
                    CertificateTitle = TitleTextBox.Text.Trim(),
                    Subtitle = SubtitleTextBox.Text.Trim(),
                    OrganizationName = OrganizationTextBox.Text.Trim(),
                    FooterText = FooterTextBox.Text.Trim(),
                    AccentColor = SelectedText(AccentColorComboBox),

                    BorderColor = _borderColor,
                    OrganizationColor = _organizationColor,
                    TitleColor = _titleColor,
                    SubtitleColor = _subtitleColor,
                    RecipientColor = _recipientColor,
                    CourseColor = _courseColor,
                    DateColor = _dateColor,
                    FooterColor = _footerColor,
                    CertificateIdColor = _certificateIdColor,
                    SignaturePath = _signaturePath,
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

                    TitleX = Canvas.GetLeft(PreviewTitleTextBlock),
                    TitleY = Canvas.GetTop(PreviewTitleTextBlock),

                    SubtitleX = Canvas.GetLeft(PreviewSubtitleTextBlock),
                    SubtitleY = Canvas.GetTop(PreviewSubtitleTextBlock),

                    RecipientX = Canvas.GetLeft(PreviewRecipientTextBlock),
                    RecipientY = Canvas.GetTop(PreviewRecipientTextBlock),

                    CourseX = Canvas.GetLeft(PreviewCourseTextBlock),
                    CourseY = Canvas.GetTop(PreviewCourseTextBlock),

                    DateX = Canvas.GetLeft(PreviewDateTextBlock),
                    DateY = Canvas.GetTop(PreviewDateTextBlock),

                    FooterX = Canvas.GetLeft(PreviewFooterTextBlock),
                    FooterY = Canvas.GetTop(PreviewFooterTextBlock),

                    CertificateIdX =
                        Canvas.GetLeft(PreviewCertificateIdTextBlock),
                    CertificateIdY =
                        Canvas.GetTop(PreviewCertificateIdTextBlock),
                    ShowBorder =
    CertificateBorder.Visibility == Visibility.Visible,

                    ShowLogo =
    LogoImage.Visibility == Visibility.Visible,

                    ShowOrganization =
    PreviewOrganizationTextBlock.Visibility == Visibility.Visible,

                    ShowTitle =
    PreviewTitleTextBlock.Visibility == Visibility.Visible,

                    ShowSubtitle =
    PreviewSubtitleTextBlock.Visibility == Visibility.Visible,

                    ShowRecipient =
    PreviewRecipientTextBlock.Visibility == Visibility.Visible,

                    ShowCourse =
    PreviewCourseTextBlock.Visibility == Visibility.Visible,

                    ShowDate =
    PreviewDateTextBlock.Visibility == Visibility.Visible,

                    ShowFooter =
    PreviewFooterTextBlock.Visibility == Visibility.Visible,

                    ShowCertificateId =
    PreviewCertificateIdTextBlock.Visibility == Visibility.Visible,                   

                    SignatureX = Canvas.GetLeft(SignatureImage),
                    SignatureY = Canvas.GetTop(SignatureImage),
                    SignatureWidth = SignatureImage.Width,
                    SignatureHeight = SignatureImage.Height,
                    ShowSignature = SignatureImage.Visibility == Visibility.Visible,

                    CustomTextElements = _customTextElements
                        .Select(item =>
                        {
                            TextBlock block = _customTextBlocks[item];

                            return new CustomTextElement
                            {
                                Text = block.Text,
                                X = Canvas.GetLeft(block),
                                Y = Canvas.GetTop(block),
                                FontSize = block.FontSize,
                                Color = (block.Foreground as SolidColorBrush)
                                    ?.Color.ToString() ?? "#000000"
                            };
                        })
                        .ToList()
                };

                if (string.IsNullOrWhiteSpace(settings.CertificateTitle))
                {
                    System.Windows.MessageBox.Show(
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
                    _settingsPath =
                        TemplateStorage.CreateTemplatePath(
                            settings.TemplateName);
                }

                File.WriteAllText(_settingsPath, json);

                StatusTextBlock.Text =
                    "Template, colors, positions and custom text saved successfully.";

                System.Windows.MessageBox.Show(
                    "Template saved successfully.",
                    "Template Designer",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Could not save template:\n{ex.Message}",
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
