namespace TodoApp.Options
{
    public class MongoDbOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = "TodoAppDb";
        public string TodoCollectionName { get; set; } = "Todos";
    }
}