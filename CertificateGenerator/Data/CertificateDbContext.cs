using CertificateGenerator.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace CertificateGenerator.Data
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
        public void EnsurePdfPathColumn()
        {
            Database.OpenConnection();

            try
            {
                using var command = Database.GetDbConnection()
                    .CreateCommand();

                command.CommandText = "PRAGMA table_info(Certificates);";

                bool columnExists = false;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader["name"].ToString() == "PdfPath")
                        {
                            columnExists = true;
                            break;
                        }
                    }
                }

                if (!columnExists)
                {
                    command.CommandText =
                        "ALTER TABLE Certificates " +
                        "ADD COLUMN PdfPath TEXT NOT NULL DEFAULT '';";

                    command.ExecuteNonQuery();
                }
            }
            finally
            {
                Database.CloseConnection();
            }
        }
    }
}