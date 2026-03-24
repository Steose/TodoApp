
using System.ComponentModel.DataAnnotations;

namespace TodoApp.Models
{
    public class TodoItem
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string? Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Task Title")]
        public string Title { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Description { get; set; }

        [Display(Name = "Completed")]
        public bool IsCompleted { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [System.Text.Json.Serialization.JsonPropertyName("partitionKey")]
        public string PartitionKey { get; set; } = "TodoItem";
    }
}