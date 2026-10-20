using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace CertificateGenerator
{
    public class CertificateDbContext : DbContext
    {
        public DbSet<CertificateRecord> Certificates
            => Set<CertificateRecord>();

        protected override void OnConfiguring(
            DbContextOptionsBuilder optionsBuilder)
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "CertificateGenerator");

            Directory.CreateDirectory(folder);

            string databasePath = Path.Combine(
                folder, "certificates.db");

            optionsBuilder.UseSqlite(
                $"Data Source={databasePath}");
        }
    }
}