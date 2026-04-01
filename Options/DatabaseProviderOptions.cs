namespace TodoApp.Options
{
    public class DatabaseProviderOptions
    {
        // Allowed values:
        // "MongoDb"
        // "CosmosMongo"
        public string Provider { get; set; } = "MongoDb";
    }
}