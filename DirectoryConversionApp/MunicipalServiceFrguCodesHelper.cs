using OfficeOpenXml;
using System;
using System.Collections.Generic;

namespace DirectoryConversionApp
{
    static class MunicipalServiceFrguCodesHelper
    {
        private const int ServiceCodeColumn = 1;
        private const int ServiceNameColumn = 2;
        private const int FIRST_COLUMN = 3;

        private const int departmentNameRow = 1;
        private const int departmentCodeRow = 2;
        private const int FIRST_ROW = 3;


        public static CustomClassIf Parse(ExcelWorksheet worksheet)
        {
            if (worksheet.Dimension.End.Column < FIRST_COLUMN)
                throw new Exception($"Лист {worksheet.Name} содержит число столбцов {worksheet.Dimension.End.Column}, что меньше минимально допустимого!");
            if (worksheet.Dimension.End.Row < FIRST_ROW)
                throw new Exception($"Лист {worksheet.Name} содержит число строк {worksheet.Dimension.End.Row}, что меньше минимально допустимого!");

            var table = new CustomClassIf()
            {
                FieldNames = new string[] { "Код ведомства", "Наименование ведомства", "Код услуги", "Наименование услуги", "Код ФРГУ" }
            };

            var rows = new List<CustomClassIfRow>();

            for (int column = FIRST_COLUMN; column <= worksheet.Dimension.End.Column; column++)
            {
                var departmentCode = worksheet.Cells[departmentCodeRow, column].Value?.ToString();
                if (string.IsNullOrEmpty(departmentCode))
                    continue;

                var departmentName = worksheet.Cells[departmentNameRow, column].Value?.ToString();
                if (string.IsNullOrEmpty(departmentName))
                    continue;

                for (int row = FIRST_ROW; row <= worksheet.Dimension.End.Row; row++)
                {
                    var serviceCode = worksheet.Cells[row, ServiceCodeColumn].Value?.ToString();
                    if (string.IsNullOrEmpty(serviceCode))
                        continue;

                    var serviceName = worksheet.Cells[row, ServiceNameColumn].Value?.ToString();
                    if (string.IsNullOrEmpty(serviceName))
                        continue;

                    var codeFrgu = worksheet.Cells[row, column]?.Text.Trim().ToUpper();
                    if (!string.IsNullOrEmpty(codeFrgu) && !(codeFrgu.StartsWith("ВПР(") || codeFrgu.StartsWith("VLOOKUP(")))
                    {
                        rows.Add(new CustomClassIfRow()
                        {
                            FieldValues = new string[] { departmentCode, departmentName, serviceCode, serviceName, codeFrgu }
                        });
                    }
                }
            }
            table.Rows = rows.ToArray();
            return table;
        }
    }
}
