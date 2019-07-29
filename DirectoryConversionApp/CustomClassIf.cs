namespace DirectoryConversionApp
{
    class CustomClassIf
    {
        public int FieldCount { get { return FieldNames?.Length ?? 0; } }

        public string[] FieldNames { get; set; }

        public CustomClassIfRow[] Rows { get; set; }
    }
}
