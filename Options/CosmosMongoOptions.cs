namespace TodoApp.Options
{
    public class CosmosMongoOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = "TodoAppDb";
        public string TodoCollectionName { get; set; } = "Todos";
    }
}