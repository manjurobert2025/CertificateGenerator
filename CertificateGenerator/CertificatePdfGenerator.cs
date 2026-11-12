using System;
using System.Globalization;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CertificateGenerator
{
    public static class CertificatePdfGenerator
    {
        private const float CanvasWidth = 800f;
        private const float CanvasHeight = 565f;

        public static void Generate(
            string recipientName,
            string courseName,
            DateTime certificateDate,
            string certificateId,
            string outputPath)
        {
            Generate(
                recipientName,
                courseName,
                certificateDate,
                certificateId,
                outputPath,
                null);
        }

        public static void Generate(
            string recipientName,
            string courseName,
            DateTime certificateDate,
            string certificateId,
            string outputPath,
            CertificateTemplateSettings? settings)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

            string fullOutputPath = Path.GetFullPath(outputPath);
            string? outputDirectory = Path.GetDirectoryName(fullOutputPath);

            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            QuestPDF.Settings.License = LicenseType.Community;

            settings ??= new CertificateTemplateSettings();

            float scaleX = 841.89f / CanvasWidth;
            float scaleY = 595.28f / CanvasHeight;

            string title = ValueOrDefault(
                settings.CertificateTitle,
                "CERTIFICATE OF ACHIEVEMENT");

            string subtitle = settings.Subtitle ?? string.Empty;
            string organization = settings.OrganizationName ?? string.Empty;
            string footer = settings.FooterText ?? string.Empty;

            string borderColor = GetColor(
                settings.BorderColor,
                "#D4AF37");

            byte[]? logoBytes = LoadImageBytes(
                settings.ShowLogo,
                settings.LogoPath);

            byte[]? signatureBytes = LoadImageBytes(
                settings.ShowSignature,
                settings.SignaturePath);

            Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(0);
                    page.PageColor(Colors.White);

                    page.Content().Layers(layers =>
                    {
                        // Certificate border
                        if (settings.ShowBorder)
                        {
                            layers.PrimaryLayer()
                                .Border(5)
                                .BorderColor(borderColor);
                        }

                        // Organization logo
                        if (logoBytes != null)
                        {
                            AddPositioned(
                                layers,
                                settings.LogoX,
                                settings.LogoY,
                                120,
                                55,
                                scaleX,
                                scaleY,
                                element => element
                                    .Image(logoBytes)
                                    .FitArea());
                        }

                        // Signature image
                        if (signatureBytes != null)
                        {
                            AddPositioned(
                                layers,
                                settings.SignatureX,
                                settings.SignatureY,
                                (float)settings.SignatureWidth,
                                (float)settings.SignatureHeight,
                                scaleX,
                                scaleY,
                                element => element
                                    .Image(signatureBytes)
                                    .FitArea());
                        }

                        // Signatory details
                        if (settings.ShowSignature)
                        {
                            double signatoryNameY =
                                settings.SignatureY +
                                settings.SignatureHeight + 4;

                            double signatoryDesignationY =
                                signatoryNameY + 14;

                            if (!string.IsNullOrWhiteSpace(settings.SignatoryName))
                            {
                                AddText(
                                    layers,
                                    settings.SignatoryName,
                                    settings.SignatureX,
                                    signatoryNameY,
                                    settings.SignatureWidth,
                                    10,
                                    "#000000",
                                    scaleX,
                                    scaleY,
                                    bold: true);
                            }

                            if (!string.IsNullOrWhiteSpace(settings.SignatoryDesignation))
                            {
                                AddText(
                                    layers,
                                    settings.SignatoryDesignation,
                                    settings.SignatureX,
                                    signatoryDesignationY,
                                    settings.SignatureWidth,
                                    9,
                                    "#475569",
                                    scaleX,
                                    scaleY);
                            }
                        }

                        // Organization name
                        if (settings.ShowOrganization)
                        {
                            AddText(
                                layers,
                                organization,
                                settings.OrganizationX,
                                settings.OrganizationY,
                                400,
                                18,
                                GetColor(settings.OrganizationColor, "#475569"),
                                scaleX,
                                scaleY,
                                bold: true);
                        }

                        // Certificate title
                        if (settings.ShowTitle)
                        {
                            AddText(
                                layers,
                                title,
                                settings.TitleX,
                                settings.TitleY,
                                600,
                                26,
                                GetColor(settings.TitleColor, "#1D4ED8"),
                                scaleX,
                                scaleY,
                                bold: true);
                        }

                        // Subtitle
                        if (settings.ShowSubtitle)
                        {
                            AddText(
                                layers,
                                subtitle,
                                settings.SubtitleX,
                                settings.SubtitleY,
                                500,
                                15,
                                GetColor(settings.SubtitleColor, "#000000"),
                                scaleX,
                                scaleY);
                        }

                        // Recipient name
                        if (settings.ShowRecipient)
                        {
                            AddText(
                                layers,
                                recipientName,
                                settings.RecipientX,
                                settings.RecipientY,
                                500,
                                30,
                                GetColor(settings.RecipientColor, "#B8860B"),
                                scaleX,
                                scaleY,
                                bold: true);
                        }

                        // Course name
                        if (settings.ShowCourse)
                        {
                            AddText(
                                layers,
                                courseName,
                                settings.CourseX,
                                settings.CourseY,
                                550,
                                18,
                                GetColor(settings.CourseColor, "#000000"),
                                scaleX,
                                scaleY,
                                bold: true);
                        }

                        // Certificate date
                        if (settings.ShowDate)
                        {
                            AddText(
                                layers,
                                $"Date: {certificateDate:dd MMMM yyyy}",
                                settings.DateX,
                                settings.DateY,
                                300,
                                13,
                                GetColor(settings.DateColor, "#000000"),
                                scaleX,
                                scaleY);
                        }

                        // Footer
                        if (settings.ShowFooter)
                        {
                            AddText(
                                layers,
                                footer,
                                settings.FooterX,
                                settings.FooterY,
                                500,
                                13,
                                GetColor(settings.FooterColor, "#000000"),
                                scaleX,
                                scaleY);
                        }

                        // Certificate ID
                        if (settings.ShowCertificateId)
                        {
                            AddText(
                                layers,
                                $"Certificate ID: {certificateId}",
                                settings.CertificateIdX,
                                settings.CertificateIdY,
                                300,
                                11,
                                GetColor(settings.CertificateIdColor, "#64748B"),
                                scaleX,
                                scaleY);
                        }

                        // Custom text elements
                        if (settings.CustomTextElements != null)
                        {
                            foreach (var item in settings.CustomTextElements)
                            {
                                if (item == null ||
                                    string.IsNullOrWhiteSpace(item.Text))
                                {
                                    continue;
                                }

                                AddText(
                                    layers,
                                    item.Text,
                                    item.X,
                                    item.Y,
                                    400,
                                    item.FontSize,
                                    GetColor(item.Color, "#000000"),
                                    scaleX,
                                    scaleY,
                                    centered: false);
                            }
                        }
                    });
                });
            }).GeneratePdf(fullOutputPath);
        }

        private static byte[]? LoadImageBytes(bool shouldLoad, string? imagePath)
        {
            if (!shouldLoad || string.IsNullOrWhiteSpace(imagePath))
            {
                return null;
            }

            if (!File.Exists(imagePath))
            {
                return null;
            }

            // File access errors are allowed to surface so the caller can report them.
            return File.ReadAllBytes(imagePath);
        }

        private static void AddText(
            LayersDescriptor layers,
            string? text,
            double x,
            double y,
            double width,
            double fontSize,
            string color,
            float scaleX,
            float scaleY,
            bool bold = false,
            bool centered = true)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            float height = (float)Math.Max(35, fontSize * 2.5);

            AddPositioned(
                layers,
                x,
                y,
                (float)width,
                height,
                scaleX,
                scaleY,
                element =>
                {
                    var textContainer = element.AlignTop();
                    textContainer = centered
                        ? textContainer.AlignCenter()
                        : textContainer.AlignLeft();

                    var textDescriptor = textContainer
                        .Text(text)
                        .FontSize((float)fontSize * scaleY)
                        .FontColor(color);

                    if (bold)
                    {
                        textDescriptor.Bold();
                    }
                });
        }

        private static void AddPositioned(
            LayersDescriptor layers,
            double x,
            double y,
            float width,
            float height,
            float scaleX,
            float scaleY,
            Action<IContainer> content)
        {
            layers.Layer()
                .TranslateX((float)x * scaleX)
                .TranslateY((float)y * scaleY)
                .Width(width * scaleX)
                .Height(height * scaleY)
                .Element(content);
        }

        private static string ValueOrDefault(
            string? value,
            string defaultValue)
        {
            return string.IsNullOrWhiteSpace(value)
                ? defaultValue
                : value;
        }

        private static string GetColor(
            string? value,
            string defaultColor)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultColor;
            }

            string color = value.Trim();

            if (!color.StartsWith("#", StringComparison.Ordinal))
            {
                color = "#" + color;
            }

            if (color.Length != 7 ||
                !uint.TryParse(
                    color.AsSpan(1),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out _))
            {
                return defaultColor;
            }

            return color;
        }
    }
}
