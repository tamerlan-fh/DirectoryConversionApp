using System;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace DirectoryConversionApp
{
    public partial class App : Application
    {
        internal App()
        {
            Dispatcher.UnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomainAssemblyResolve);
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var message = $"Ой, что-то пошло не так! {e.Exception.Message}";
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private Assembly CurrentDomainAssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.Contains("DotNetZip"))
                return Assembly.Load(DirectoryConversionApp.Properties.Resources.DotNetZip);

            if (args.Name.Contains("EPPlus"))
                return Assembly.Load(DirectoryConversionApp.Properties.Resources.EPPlus);

            return null;
        }
    }
}
