
using System;

namespace CertificateGenerator
{
    public class CertificateRecord
    {
        public int Id { get; set; }

        public string CertificateId { get; set; } = "";

        public string RecipientName { get; set; } = "";

        public string CourseName { get; set; } = "";

        public DateTime IssueDate { get; set; }
    }
}
