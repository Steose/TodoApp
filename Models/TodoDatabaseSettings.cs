namespace TodoApp.Models
{
    public class TodoDatabaseSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string TodoCollectionName { get; set; } = string.Empty;
    }
}