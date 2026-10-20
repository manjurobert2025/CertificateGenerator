
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CertificateGenerator
{
    public static class CertificatePdfGenerator
    {
        public static void Generate(
            string recipientName,
            string courseName,
            DateTime certificateDate,
            string certificateId,
            string outputPath)
        {
            QuestPDF.Settings.License =
                LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.PageColor(Colors.White);

                    page.Content()
                        .Border(5)
                        .BorderColor("#D4AF37")
                        .Padding(30)
                        .Column(column =>
                        {
                            column.Spacing(18);

                            column.Item()
                                .AlignCenter()
                                .Text("CERTIFICATE OF ACHIEVEMENT")
                                .FontSize(27)
                                .Bold()
                                .FontColor(Colors.Blue.Darken3);

                            column.Item()
                                .AlignCenter()
                                .Text("This certificate is proudly presented to")
                                .FontSize(16);

                            column.Item()
                                .AlignCenter()
                                .Text(recipientName)
                                .FontSize(32)
                                .Bold()
                                .FontColor("#B8860B");

                            column.Item()
                                .AlignCenter()
                                .Text("For successfully completing")
                                .FontSize(16);

                            column.Item()
                                .AlignCenter()
                                .Text(courseName)
                                .FontSize(23)
                                .Bold();

                            column.Item()
                                .AlignCenter()
                                .Text(
                                    $"Date: {certificateDate:dd MMMM yyyy}")
                                .FontSize(14);

                            column.Item()
                                .AlignCenter()
                                .Text($"Certificate ID: {certificateId}")
                                .FontSize(11);
                        });
                });
            }).GeneratePdf(outputPath);
        }
    }
}
