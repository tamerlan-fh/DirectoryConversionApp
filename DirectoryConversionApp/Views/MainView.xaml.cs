using DirectoryConversionApp.ViewModels;
using System;
using System.Linq;
using System.Windows;

namespace DirectoryConversionApp.Views
{
    public partial class MainView : Window
    {
        private readonly MainViewModel model = new MainViewModel();
        public MainView()
        {
            InitializeComponent();
            AllowDrop = true;
            DataContext = model;
        }

        protected override void OnDrop(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && e.Data.GetData(DataFormats.FileDrop) is string[] paths)
            {
                if (paths.Length > 1)
                    throw new Exception("Пернести на форму можно лишь один файл!");

                model.InputPath = paths.FirstOrDefault();
            }
            base.OnDrop(e);
        }
    }
}
