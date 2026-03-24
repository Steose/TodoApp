namespace TodoApp.Models
{
    public class TodoDatabaseSettings
    {
        public string ConnectionString { get; set; } = null!;
        public string DatabaseName { get; set; } = null!;
        public string TodoCollectionName { get; set; } = null!;
    }
}