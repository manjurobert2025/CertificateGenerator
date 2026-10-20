
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CertificateGenerator
{
    public partial class MyTemplatesWindow : Window
    {
        public MyTemplatesWindow()
        {
            InitializeComponent();
            LoadTemplates();
        }

        private SavedTemplate? SelectedTemplate =>
            TemplatesListBox.SelectedItem as SavedTemplate;

        private void LoadTemplates()
        {
            var templates = TemplateStorage.GetTemplates();

            TemplatesListBox.ItemsSource = templates;

            TemplateCountTextBlock.Text =
                $"{templates.Count} saved template(s)";

            EmptyMessageTextBlock.Visibility =
                templates.Count == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private void TemplatesListBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            // The selected template is available through
            // the SelectedTemplate property.
        }

        private void NewTemplate_Click(
            object sender,
            RoutedEventArgs e)
        {
            var designer = new TemplateDesignerWindow
            {
                Owner = this
            };

            designer.ShowDialog();
            LoadTemplates();
        }

        private void Preview_Click(
            object sender,
            RoutedEventArgs e)
        {
            var template = SelectedTemplate;

            if (template == null)
            {
                MessageBox.Show(
                    "Please select a template to preview.",
                    "My Templates",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var preview = new TemplatePreviewWindow(
                template.Settings)
            {
                Owner = this
            };

            preview.ShowDialog();
        }

        private void Edit_Click(
            object sender,
            RoutedEventArgs e)
        {
            var template = SelectedTemplate;

            if (template == null)
            {
                MessageBox.Show(
                    "Please select a template to edit.");
                return;
            }

            var designer = new TemplateDesignerWindow(
                template.FilePath)
            {
                Owner = this
            };

            designer.ShowDialog();
            LoadTemplates();
        }

        private void Delete_Click(
            object sender,
            RoutedEventArgs e)
        {
            var template = SelectedTemplate;

            if (template == null)
            {
                MessageBox.Show(
                    "Please select a template to delete.");
                return;
            }

            var answer = MessageBox.Show(
                $"Are you sure you want to delete " +
                $"'{template.Settings.TemplateName}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (answer != MessageBoxResult.Yes)
                return;

            try
            {
                File.Delete(template.FilePath);
                LoadTemplates();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not delete the template:\n{ex.Message}",
                    "Delete Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Close_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }

    // Read-only preview of a saved certificate template.
    public class TemplatePreviewWindow : Window
    {
        public TemplatePreviewWindow(
            CertificateTemplateSettings settings)
        {
            Title = $"Preview - {settings.TemplateName}";
            Width = 900;
            Height = 680;
            MinWidth = 650;
            MinHeight = 500;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush(
                Color.FromRgb(244, 246, 250));

            var root = new Grid
            {
                Margin = new Thickness(20)
            };

            root.RowDefinitions.Add(
                new RowDefinition { Height = GridLength.Auto });

            root.RowDefinitions.Add(
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var heading = new TextBlock
            {
                Text = settings.TemplateName + " - Preview",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(23, 37, 84)),
                Margin = new Thickness(0, 0, 0, 15)
            };

            Grid.SetRow(heading, 0);
            root.Children.Add(heading);

            var scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = Brushes.LightGray
            };

            var canvas = new Canvas
            {
                Width = 800,
                Height = 565,
                Background = Brushes.White,
                ClipToBounds = true
            };

            Color accent = settings.TemplateName switch
            {
                "Modern Blue" => Colors.RoyalBlue,
                "Elegant Green" => Colors.ForestGreen,
                _ => settings.AccentColor switch
                {
                    "Blue" => Colors.RoyalBlue,
                    "Green" => Colors.ForestGreen,
                    "Purple" => Colors.MediumPurple,
                    _ => Color.FromRgb(212, 175, 55)
                }
            };

            canvas.Children.Add(new Border
            {
                Width = 770,
                Height = 535,
                BorderBrush = new SolidColorBrush(accent),
                BorderThickness = new Thickness(5),
                IsHitTestVisible = false
            });

            Canvas.SetLeft(canvas.Children[0], 15);
            Canvas.SetTop(canvas.Children[0], 15);

            AddText(canvas, settings.OrganizationName,
                settings.OrganizationX, settings.OrganizationY,
                400, 18, accent, FontWeights.SemiBold);

            AddText(canvas, settings.CertificateTitle,
                settings.TitleX, settings.TitleY,
                600, 26, accent, FontWeights.Bold);

            AddText(canvas, settings.Subtitle,
                settings.SubtitleX, settings.SubtitleY,
                500, 15, Colors.Black, FontWeights.Normal);

            AddText(canvas, "Recipient Name",
                settings.RecipientX, settings.RecipientY,
                500, 30, Color.FromRgb(184, 134, 11),
                FontWeights.Bold);

            AddText(canvas, "Course or Achievement",
                settings.CourseX, settings.CourseY,
                550, 18, Colors.Black, FontWeights.Normal);

            AddText(canvas, "Issue Date",
                settings.DateX, settings.DateY,
                300, 13, Colors.Black, FontWeights.Normal);

            AddText(canvas, settings.FooterText,
                settings.FooterX, settings.FooterY,
                500, 13, Colors.Black, FontWeights.Normal);

            AddText(canvas, "Certificate ID",
                settings.CertificateIdX,
                settings.CertificateIdY,
                300, 11, Colors.Gray, FontWeights.Normal);

            if (!string.IsNullOrWhiteSpace(settings.LogoPath) &&
                File.Exists(settings.LogoPath))
            {
                try
                {
                    var logo = new BitmapImage();
                    logo.BeginInit();
                    logo.CacheOption = BitmapCacheOption.OnLoad;
                    logo.UriSource = new Uri(settings.LogoPath);
                    logo.EndInit();
                    logo.Freeze();

                    var image = new Image
                    {
                        Source = logo,
                        Width = 120,
                        Height = 55,
                        Stretch = Stretch.Uniform
                    };

                    Canvas.SetLeft(image, settings.LogoX);
                    Canvas.SetTop(image, settings.LogoY);
                    canvas.Children.Add(image);
                }
                catch
                {
                    // Continue previewing if the logo is unavailable.
                }
            }

            scrollViewer.Content = canvas;
            Grid.SetRow(scrollViewer, 1);
            root.Children.Add(scrollViewer);

            Content = root;
        }

        private static void AddText(
            Canvas canvas,
            string text,
            double x,
            double y,
            double width,
            double fontSize,
            Color color,
            FontWeight weight)
        {
            var block = new TextBlock
            {
                Text = text,
                Width = width,
                FontSize = fontSize,
                FontWeight = weight,
                Foreground = new SolidColorBrush(color),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            Canvas.SetLeft(block, x);
            Canvas.SetTop(block, y);
            canvas.Children.Add(block);
        }
    }
}
