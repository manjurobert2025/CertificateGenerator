
using System;
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
            QuestPDF.Settings.License = LicenseType.Community;

            settings ??= new CertificateTemplateSettings();

            float scaleX = 841.89f / CanvasWidth;
            float scaleY = 595.28f / CanvasHeight;

            string title = ValueOrDefault(
                settings.CertificateTitle,
                "CERTIFICATE OF ACHIEVEMENT");

            string subtitle = settings.Subtitle ?? "";
            string organization = settings.OrganizationName ?? "";
            string footer = settings.FooterText ?? "";

            string borderColor = GetColor(
                settings.BorderColor, "#D4AF37");

            byte[]? logoBytes = null;
            byte[]? signatureBytes = null;

            // Load organization logo
            if (settings.ShowLogo &&
                !string.IsNullOrWhiteSpace(settings.LogoPath) &&
                File.Exists(settings.LogoPath))
            {
                logoBytes = File.ReadAllBytes(settings.LogoPath);
            }

            // Load signature image
            if (settings.ShowSignature &&
                !string.IsNullOrWhiteSpace(settings.SignaturePath) &&
                File.Exists(settings.SignaturePath))
            {
                signatureBytes = File.ReadAllBytes(
                    settings.SignaturePath);
            }

            Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(0);
                    page.PageColor(Colors.White);

                    page.Content().Layers(layers =>
                    {
                        // Border
                        if (settings.ShowBorder)
                        {
                            layers.PrimaryLayer()
                                .Border(5)
                                .BorderColor(borderColor);
                        }

                        // Organization Logo
                        if (settings.ShowLogo && logoBytes != null)
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

                        // Signature Image
                        if (settings.ShowSignature &&
                            signatureBytes != null)
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

                            // Signatory name
                            if (!string.IsNullOrWhiteSpace(
                                settings.SignatoryName))
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

                            // Signatory designation
                            if (!string.IsNullOrWhiteSpace(
                                settings.SignatoryDesignation))
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

                        // Organization Name
                        if (settings.ShowOrganization)
                        {
                            AddText(
                                layers,
                                organization,
                                settings.OrganizationX,
                                settings.OrganizationY,
                                400,
                                18,
                                GetColor(
                                    settings.OrganizationColor,
                                    "#475569"),
                                scaleX,
                                scaleY,
                                bold: true);
                        }

                        // Certificate Title
                        if (settings.ShowTitle)
                        {
                            AddText(
                                layers,
                                title,
                                settings.TitleX,
                                settings.TitleY,
                                600,
                                26,
                                GetColor(
                                    settings.TitleColor,
                                    "#1D4ED8"),
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
                                GetColor(
                                    settings.SubtitleColor,
                                    "#000000"),
                                scaleX,
                                scaleY);
                        }

                        // Recipient Name
                        if (settings.ShowRecipient)
                        {
                            AddText(
                                layers,
                                recipientName,
                                settings.RecipientX,
                                settings.RecipientY,
                                500,
                                30,
                                GetColor(
                                    settings.RecipientColor,
                                    "#B8860B"),
                                scaleX,
                                scaleY,
                                bold: true);
                        }

                        // Course Name
                        if (settings.ShowCourse)
                        {
                            AddText(
                                layers,
                                courseName,
                                settings.CourseX,
                                settings.CourseY,
                                550,
                                18,
                                GetColor(
                                    settings.CourseColor,
                                    "#000000"),
                                scaleX,
                                scaleY,
                                bold: true);
                        }

                        // Certificate Date
                        if (settings.ShowDate)
                        {
                            AddText(
                                layers,
                                $"Date: {certificateDate:dd MMMM yyyy}",
                                settings.DateX,
                                settings.DateY,
                                300,
                                13,
                                GetColor(
                                    settings.DateColor,
                                    "#000000"),
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
                                GetColor(
                                    settings.FooterColor,
                                    "#000000"),
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
                                GetColor(
                                    settings.CertificateIdColor,
                                    "#64748B"),
                                scaleX,
                                scaleY);
                        }

                        // Custom Text Elements
                        if (settings.CustomTextElements != null)
                        {
                            foreach (var item in
                                settings.CustomTextElements)
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
                                    GetColor(
                                        item.Color,
                                        "#000000"),
                                    scaleX,
                                    scaleY,
                                    centered: false);
                            }
                        }
                    });
                });
            }).GeneratePdf(outputPath);
            System.Diagnostics.Debug.WriteLine(
    $"PDF generated at: {outputPath}");

            System.Diagnostics.Debug.WriteLine(
                $"PDF exists: {File.Exists(outputPath)}");
        }

        // Add positioned text
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
                return;

            float height = (float)Math.Max(
                35,
                fontSize * 2.5);

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

                    if (centered)
                        textContainer = textContainer.AlignCenter();
                    else
                        textContainer = textContainer.AlignLeft();

                    var textDescriptor = textContainer
                        .Text(text)
                        .FontSize((float)fontSize * scaleY)
                        .FontColor(color);

                    if (bold)
                        textDescriptor.Bold();
                });
        }

        // Add positioned elements
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

        // Default text values
        private static string ValueOrDefault(
            string? value,
            string defaultValue)
        {
            return string.IsNullOrWhiteSpace(value)
                ? defaultValue
                : value;
        }

        // Validate and retrieve colors
        private static string GetColor(
            string? value,
            string defaultColor)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultColor;

            string color = value.Trim();

            if (!color.StartsWith("#"))
                color = "#" + color;

            return color.Length == 7
                ? color
                : defaultColor;
        }
    }
}
