using System;
using System.Deployment.Application;
using System.IO;
using System.Reflection;
using System.Windows;
using IWshRuntimeLibrary;

namespace Scorer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args != null && e.Args.Length > 0)
            {
                Properties["FileToOpen"] = e.Args[0];
            }

            try
            {
                if (ApplicationDeployment.CurrentDeployment.IsFirstRun)
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    var company = ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyCompanyAttribute), false)).Company;

                    var shortcutName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), company, "Scorer Documentation.lnk");
                    var targetPath = Path.Combine(Path.GetDirectoryName(assembly.Location), "Documentation");

                    var shell = new WshShell();
                    var shortcut = (IWshShortcut)shell.CreateShortcut(shortcutName);
                    shortcut.TargetPath = targetPath;
                    shortcut.Save();
                }
            }
            catch (InvalidDeploymentException)
            { }
        }
    }
}
