using System;
using System.Windows.Forms;
using Taskora.БазаДанных;
using Taskora.Формы;

namespace Taskora
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            DatabaseHelper.InitializeDatabase();

            Application.Run(new LoginForm());
        }
    }
}