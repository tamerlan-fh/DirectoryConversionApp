using Ionic.Zip;
using Microsoft.Win32;
using OfficeOpenXml;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DirectoryConversionApp.ViewModels
{
    class MainViewModel : NotifyErrorViewModel
    {
        public MainViewModel()
        {
            ConvertCommand = new RelayCommand(obj => Convert(), obj => CanConvert());
            SetInputPathCommand = new RelayCommand(obj => SetInputPath());
            SetOutPathCommand = new RelayCommand(obj => SetOutPath());
            directoryGuid = Guid.NewGuid().ToString("D");
        }

        public string Name
        {
            get { return name; }
            set
            {
                if (Name == value) return;
                name = value; OnPropertyChanged(nameof(Name));
                ValidateName();
            }
        }
        private string name;

        public string DirectoryGuid
        {
            get { return directoryGuid; }
            set
            {
                if (DirectoryGuid == value) return;
                directoryGuid = value; OnPropertyChanged(nameof(DirectoryGuid));
                ValidateDirectoryGuid();
            }
        }
        private string directoryGuid;

        public string InputPath
        {
            get { return inputPath; }
            set
            {
                if (InputPath == value) return;
                inputPath = value; OnPropertyChanged(nameof(InputPath));
                if (ValidateInputPath())
                {
                    if (!outPathSetedByUser)
                        OutPath = Path.Combine(Path.GetDirectoryName(InputPath), $"{Path.GetFileNameWithoutExtension(InputPath)}_{DateTime.Now.ToString("yyyy.MM.dd-HH.mm.ss")}.zip");

                    Load(new FileInfo(InputPath));
                }
            }
        }
        private string inputPath;

        public string OutPath
        {
            get { return outPath; }
            set
            {
                if (OutPath == value) return;
                outPath = value; OnPropertyChanged(nameof(OutPath));
            }
        }
        private string outPath;
        private bool outPathSetedByUser = false;

        public bool IsBusy
        {
            get { return isBusy; }
            set { isBusy = value; OnPropertyChanged(nameof(IsBusy)); }
        }
        private bool isBusy = false;

        public CustomClassIf CustomClassIf { get; private set; }

        #region ConvertCommand

        public ICommand ConvertCommand { get; }

        private void Convert()
        {
            Validate();
            if (!CanConvert())
                return;

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<ClassifCard xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
            sb.AppendLine("<TableId>custom_classif</TableId>");
            sb.AppendLine("<Custom>");
            sb.AppendLine($"<isn_classif>{DirectoryGuid}</isn_classif>");
            sb.AppendLine($"<name>{Name}</name>");
            sb.AppendLine($"<field_count>{CustomClassIf.FieldCount}</field_count>");
            sb.AppendLine("<fields>");
            for (int index = 0; index < CustomClassIf.FieldCount; index++)
            {
                sb.AppendLine($"<name{index}>{CustomClassIf.FieldNames[index]}</name{index}>");
            }
            sb.AppendLine("</fields>");
            sb.AppendLine("<is_hierarchical>false</is_hierarchical>");
            sb.AppendLine("</Custom>");

            sb.AppendLine("<CustomRows>");
            foreach (var row in CustomClassIf.Rows)
            {
                sb.AppendLine("<custom_classif_row>");
                sb.AppendLine($"<isn_node>{Guid.NewGuid()}</isn_node>");
                sb.AppendLine("<isn_parent_node xsi:nil=\"true\" />");
                sb.AppendLine("<is_parent>false</is_parent>");
                for (int index = 0; index < CustomClassIf.FieldCount; index++)
                {
                    sb.AppendLine($"<field{index}>{row.FieldValues[index]}</field{index}>");
                }
                sb.AppendLine("</custom_classif_row>");
            }
            sb.AppendLine("</CustomRows>");
            sb.AppendLine("</ClassifCard>");

            var filename = $"Classif_{DirectoryGuid}.xml";
            using (var zipFile = new ZipFile())
            {
                zipFile.AddEntry(filename, sb.ToString(), Encoding.UTF8);
                zipFile.Save(OutPath);
            }
            MessageBox.Show($"Файл создан и сохранен!", "Выполнено", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool CanConvert()
        {
            return !HasErrors;
        }

        #endregion ConvertCommand

        #region SetInputPathCommand

        public ICommand SetInputPathCommand { get; }

        private void SetInputPath()
        {
            var filedialog = new OpenFileDialog();
            filedialog.Filter = "Microsoft Excel(*.xls;*.xlsx)|*.xls;*.xlsx|All files(*.*)|*.*";
            if (filedialog.ShowDialog() != true)
                return;
            InputPath = filedialog.FileName;
        }

        #endregion SetInputPathCommand

        #region SetOutPathCommand

        public ICommand SetOutPathCommand { get; }

        private void SetOutPath()
        {
            var filedialog = new SaveFileDialog();
            filedialog.Filter = "Zip-file (*.zip)|*.zip|All files(*.*)|*.*";
            if (filedialog.ShowDialog() != true)
                return;
            OutPath = filedialog.FileName;
            outPathSetedByUser = true;
        }

        #endregion SetOutPathCommand

        private async void Load(FileInfo file)
        {
            IsBusy = true;

            var task = Task.Run(() =>
            {
                using (var package = new ExcelPackage(file))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet is null)
                        throw new Exception($"Файл {file.FullName} не содержит ни одного листа!");
                    CustomClassIf = CustomClassIfHelper.Parse(worksheet);
                    ToDataBase(CustomClassIf);
                }
            });
            await task;
            IsBusy = false;
            if (task.Status == TaskStatus.Faulted)
                throw task.Exception.GetBaseException();
        }

        private void ToDataBase(CustomClassIf classIf)
        {
            var table = new DataTable(Name);
            foreach (var columnName in classIf.FieldNames)
            {
                var column = new DataColumn(columnName, typeof(string));
                table.Columns.Add(column);
            }
            foreach (var row in classIf.Rows)
            {
                table.Rows.Add(row.FieldValues);
            }
            DataTable = table;
        }

        public DataTable DataTable
        {
            get { return dataTable; }
            private set { dataTable = value; OnPropertyChanged(nameof(DataTable)); }
        }
        private DataTable dataTable;

        #region validation

        private void Validate()
        {
            ValidateName();
            ValidateDirectoryGuid();
            ValidateInputPath();
            ValidateOutPath();
        }
        private void ValidateName()
        {
            RemoveError(nameof(Name));

            if (string.IsNullOrWhiteSpace(Name))
            {
                AddError(nameof(Name), "это поле не может быть пустым");
                return;
            }
        }

        private void ValidateDirectoryGuid()
        {
            RemoveError(nameof(DirectoryGuid));

            if (string.IsNullOrWhiteSpace(DirectoryGuid))
            {
                AddError(nameof(DirectoryGuid), "это поле не может быть пустым");
                return;
            }

            if (!Guid.TryParse(DirectoryGuid, out Guid value))
            {
                AddError(nameof(DirectoryGuid), "Значение невозможно преобразовать в GUID");
                return;
            }
            else
                DirectoryGuid = value.ToString("D");

        }

        private bool ValidateInputPath()
        {
            RemoveError(nameof(InputPath));
            if (string.IsNullOrWhiteSpace(InputPath))
            {
                AddError(nameof(InputPath), "это поле не может быть пустым");
                return false;
            }
            if (!File.Exists(InputPath))
            {
                AddError(nameof(InputPath), "Не удалось найти файл по указанному пути");
                return false;
            }

            return true;
        }

        private void ValidateOutPath()
        {
            RemoveError(nameof(OutPath));
            if (string.IsNullOrWhiteSpace(OutPath))
            {
                AddError(nameof(OutPath), "это поле не может быть пустым");
                return;
            }
        }

        #endregion validation
    }
}
