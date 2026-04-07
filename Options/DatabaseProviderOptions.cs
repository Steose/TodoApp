namespace TodoApp.Options
{
    public class DatabaseProviderOptions
    {
        // Allowed values:
        // "InMemory"
        // "MongoDb"
        // "CosmosMongo"
        public string Provider { get; set; } = "InMemory";
    }
}
