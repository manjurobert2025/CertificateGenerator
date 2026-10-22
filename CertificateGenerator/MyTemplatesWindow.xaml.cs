
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
                System.Windows.MessageBox.Show(
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
                System.Windows.MessageBox.Show(
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
                System.Windows.MessageBox.Show(
                    "Please select a template to delete.");
                return;
            }

            var answer = System.Windows.MessageBox.Show(
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
                System.Windows.MessageBox.Show(
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
                System.Windows.Media.Color.FromRgb(244, 246, 250));

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
                    System.Windows.Media.Color.FromRgb(23, 37, 84)),
                Margin = new Thickness(0, 0, 0, 15)
            };

            Grid.SetRow(heading, 0);
            root.Children.Add(heading);

            var scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = System.Windows.Media.Brushes.LightGray,
            };

            var canvas = new Canvas
            {
                Width = 800,
                Height = 565,
                Background = System.Windows.Media.Brushes.White,
                ClipToBounds = true
            };

            System.Windows.Media.Color accent = settings.TemplateName switch
            {
                "Modern Blue" => Colors.RoyalBlue,
                "Elegant Green" => Colors.ForestGreen,
                _ => settings.AccentColor switch
                {
                    "Blue" => Colors.RoyalBlue,
                    "Green" => Colors.ForestGreen,
                    "Purple" => Colors.MediumPurple,
                    _ => System.Windows.Media.Color.FromRgb(212, 175, 55)
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

            AddText(canvas, settings.CertificateTitle,
      settings.TitleX, settings.TitleY,
      600, 26,
      System.Drawing.Color.FromArgb(
          accent.A, accent.R, accent.G, accent.B),
      FontWeights.Bold);

            AddText(canvas, settings.Subtitle,
                settings.SubtitleX, settings.SubtitleY,
                500, 15, System.Drawing.Color.Black, FontWeights.Normal);

            AddText(canvas, "Recipient Name",
                settings.RecipientX, settings.RecipientY,
                500, 30, System.Drawing.Color.FromArgb(184, 134, 11),
                FontWeights.Bold);

            AddText(canvas, "Course or Achievement",
                settings.CourseX, settings.CourseY,
                550, 18, System.Drawing.Color.Black, FontWeights.Normal);

            AddText(canvas, "Issue Date",
                settings.DateX, settings.DateY,
                300, 13, System.Drawing.Color.Black, FontWeights.Normal);

            AddText(canvas, settings.FooterText,
                settings.FooterX, settings.FooterY,
                500, 13, System.Drawing.Color.Black, FontWeights.Normal);

            AddText(canvas, "Certificate ID",
                settings.CertificateIdX,
                settings.CertificateIdY,
                300, 11, System.Drawing.Color.Gray, FontWeights.Normal);
            // Display custom text elements saved with this template.
            if (settings.CustomTextElements != null)
            {
                foreach (var item in settings.CustomTextElements)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.Text))
                        continue;

                    System.Windows.Media.Brush textBrush =
                        System.Windows.Media.Brushes.Black;

                    try
                    {
                        if (!string.IsNullOrWhiteSpace(item.Color))
                        {
                            textBrush = (System.Windows.Media.Brush)
                                new System.Windows.Media.BrushConverter()
                                    .ConvertFromString(item.Color);
                        }
                    }
                    catch
                    {
                        // Keep black if the saved color is invalid.
                    }

                    var customTextBlock = new TextBlock
                    {
                        Text = item.Text,
                        FontSize = item.FontSize,
                        Foreground = textBrush,
                        Width = 400,
                        TextWrapping = TextWrapping.Wrap
                    };

                    Canvas.SetLeft(customTextBlock, item.X);
                    Canvas.SetTop(customTextBlock, item.Y);

                    canvas.Children.Add(customTextBlock);
                }
            }

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

                    var image = new System.Windows.Controls.Image
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
            System.Drawing.Color color,
            FontWeight weight)
        {
            var block = new TextBlock
            {
                Text = text,
                Width = width,
                FontSize = fontSize,
                FontWeight = weight,
                Foreground = new System.Windows.Media.SolidColorBrush(
    System.Windows.Media.Color.FromArgb(
        color.A, color.R, color.G, color.B)),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            Canvas.SetLeft(block, x);
            Canvas.SetTop(block, y);
            canvas.Children.Add(block);
        }
    }
}
