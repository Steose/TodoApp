using System; // Gives access to DateTime
using System.ComponentModel.DataAnnotations; // Validation attributes
using MongoDB.Bson; // MongoDB BSON types
using MongoDB.Bson.Serialization.Attributes; // MongoDB mapping attributes

namespace TodoApp.Models
{
    public class TodoItem
    {
        [BsonId] // Marks this property as MongoDB document Id
        [BsonRepresentation(BsonType.ObjectId)] // Allows string in C# while stored as ObjectId in MongoDB
        public string? Id { get; set; }

        [Required(ErrorMessage = "Title is required.")] // Makes Title required
        [StringLength(100, ErrorMessage = "Title cannot be longer than 100 characters.")] // Title length limit
        [BsonElement("title")] // Mongo field name
        public string Title { get; set; } = string.Empty;

        [StringLength(300, ErrorMessage = "Description cannot be longer than 300 characters.")] // Description limit
        [BsonElement("description")] // Mongo field name
        public string? Description { get; set; }

        [Display(Name = "Completed")] // Friendly label in UI
        [BsonElement("isCompleted")] // Mongo field name
        public bool IsCompleted { get; set; }

        [Display(Name = "Created At")] // Friendly label in UI
        [BsonElement("createdAt")] // Mongo field name
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("partitionKey")] // Mongo/Cosmos partition field
        public string PartitionKey { get; set; } = "TodoItem";
    }
}