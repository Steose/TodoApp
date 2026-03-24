namespace TodoApp.Models
{
    public class TodoDatabaseSettings
    {
        // CosmosDB specific settings
        public string? CosmosEndpoint { get; set; }
        public string? CosmosKey { get; set; }
        public string DatabaseName { get; set; } = string.Empty;
        public string ContainerName { get; set; } = string.Empty;

        // Backward compatibility with MongoDB settings (deprecated)
        [System.Obsolete("Use CosmosEndpoint and CosmosKey instead")]
        public string ConnectionString { get; set; } = string.Empty;

        [System.Obsolete("Use ContainerName instead")]
        public string TodoCollectionName { get; set; } = string.Empty;
    }
}