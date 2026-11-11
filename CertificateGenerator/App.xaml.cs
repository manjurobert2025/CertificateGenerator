using System;
using System.Windows;

namespace CertificateGenerator
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                using (var db = new CertificateDbContext())
                {
                    db.EnsurePdfPathColumn();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "Database update failed:\n" + ex.Message,
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown();
                return;
            }

            base.OnStartup(e);
        }
    }
}