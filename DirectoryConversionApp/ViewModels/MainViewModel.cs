using Ionic.Zip;
using Microsoft.Win32;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;

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
            directoryType = DirectoryType.Default;
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
                TryLoad();
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

        public DirectoryType DirectoryType
        {
            get { return directoryType; }
            set
            {
                directoryType = value; OnPropertyChanged(nameof(DirectoryType));
                if (DirectoryGuids.TryGetValue(DirectoryType, out Guid guid))
                    DirectoryGuid = guid.ToString("D");
                else
                    DirectoryGuid = Guid.NewGuid().ToString("D");
                if (DirectoryNames.TryGetValue(DirectoryType, out string name))
                    Name = name;
                TryLoad();
            }
        }
        private DirectoryType directoryType;

        public CustomClassIf CustomClassIf { get; private set; }

        #region ConvertCommand

        public ICommand ConvertCommand { get; }

        private void Convert()
        {
            Validate();
            if (!CanConvert())
                return;

            var filename = $"Classif_{DirectoryGuid}.xml";
            using (var zipFile = new ZipFile())
            {
                using (var stream = new MemoryStream())
                {
                    var document = CreateXml();
                    document.Save(stream, SaveOptions.None);
                    zipFile.AddEntry(filename, stream.ToArray());
                    zipFile.Save(OutPath);
                }
            }
            MessageBox.Show($"Файл создан и сохранен!", "Выполнено", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private XDocument CreateXml()
        {
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

            return XDocument.Parse(sb.ToString());
        }

        private bool CanConvert()
        {
            return !HasErrors && CustomClassIf != null;
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

        private void TryLoad()
        {
            if (ValidateInputPath())
            {
                if (!outPathSetedByUser)
                    OutPath = Path.Combine(Path.GetDirectoryName(InputPath), $"{Path.GetFileNameWithoutExtension(InputPath)}_{DateTime.Now.ToString("yyyy.MM.dd-HH.mm.ss")}.zip");

                Task.Run(async () =>
                {
                    var file = new FileInfo(InputPath);
                    IsBusy = true;

                    var task = Task.Run(() =>
                    {
                        using (var package = new ExcelPackage(file))
                        {
                            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                            if (worksheet is null)
                                throw new Exception($"Файл {file.FullName} не содержит ни одного листа!");

                            switch (DirectoryType)
                            {
                                case DirectoryType.MunicipalServiceFrguCodes: CustomClassIf = MunicipalServiceFrguCodesHelper.Parse(worksheet); break;
                                default: CustomClassIf = CustomClassIfHelper.Parse(worksheet); break;
                            }

                            ToDataTable(CustomClassIf);
                        }
                    });
                    await task.ConfigureAwait(false);
                    IsBusy = false;
                    if (task.Status == TaskStatus.Faulted)
                        throw task.Exception.GetBaseException();
                });
            }
        }

        private void ToDataTable(CustomClassIf classIf)
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
            if (Name.Length > 128)
            {
                AddError(nameof(Name), "слошком длинное название. ограничение - 128 символов");
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

        private readonly Dictionary<DirectoryType, Guid> DirectoryGuids = new Dictionary<DirectoryType, Guid>()
        {
           { DirectoryType.MunicipalServiceFrguCodes, Guid.Parse("fa101c64-0e12-4ee7-ba9e-3c5b7c263d90") }
        };

        private readonly Dictionary<DirectoryType, string> DirectoryNames = new Dictionary<DirectoryType, string>()
        {
           { DirectoryType.MunicipalServiceFrguCodes, "Коды ФРГУ муниципальных услуг" }
        };

        public static Dictionary<DirectoryType, string> DirectoryTypes
        {
            get
            {
                if (directoryTypes is null)
                    directoryTypes = Enum.GetValues(typeof(DirectoryType)).Cast<DirectoryType>().ToDictionary(x => x, x => x.GetDescription());
                return directoryTypes;
            }
        }
        private static Dictionary<DirectoryType, string> directoryTypes;
    }

    enum DirectoryType
    {
        [Description("Обычный словарь")]
        Default,
        [Description("Коды ФРГУ муниципальных услуг")]
        MunicipalServiceFrguCodes
    }
}
