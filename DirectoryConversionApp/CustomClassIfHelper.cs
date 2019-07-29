using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DirectoryConversionApp
{
    static class CustomClassIfHelper
    {
        private const int HEADER_ROW = 1;
        private const int FIRST_COLUMN = 1;
        public static CustomClassIf Parse(ExcelWorksheet worksheet)
        {
            if (worksheet.Dimension.End.Column < FIRST_COLUMN)
                throw new Exception($"Лист {worksheet.Name} содержит число столбцов {worksheet.Dimension.End.Column}, что меньше минимально допустимого!");
            if (worksheet.Dimension.End.Row < HEADER_ROW)
                throw new Exception($"Лист {worksheet.Name} содержит число строк {worksheet.Dimension.End.Row}, что меньше минимально допустимого!");

            var table = new CustomClassIf();

            table.FieldNames = Enumerable.Range(FIRST_COLUMN, worksheet.Dimension.End.Column)
                .Select(column => worksheet.Cells[HEADER_ROW, column].Value?.ToString())
                .ToArray();

            var rows = new List<CustomClassIfRow>();
            for (int row = HEADER_ROW + 1; row <= worksheet.Dimension.End.Row; row++)
            {
                var values = Enumerable.Range(FIRST_COLUMN, table.FieldCount)
                .Select(column => worksheet.Cells[row, column].Value?.ToString())
                .ToArray();
                rows.Add(new CustomClassIfRow() { FieldValues = values });
            }
            table.Rows = rows.ToArray();
            return table;
        }
    }
}
