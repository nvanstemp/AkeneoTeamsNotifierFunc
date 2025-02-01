namespace AppInsights.Core.Models
{
    public class QueryResult
    {
        public List<Table> Tables { get; set; } = new();
    }

    public class Table
    {
        public string Name { get; set; } = string.Empty;
        public List<Column> Columns { get; set; } = new();
        public List<List<object>> Rows { get; set; } = new();
    }

    public class Column
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
} 