namespace GerenciadorTasks.Application.DTOs.Task
{
    public class CreateTaskRequest
    {
        string Title { get; set; }
        public string Description { get; set; }
        int AssignedToId { get; set; }
        public DateTime? DueData { get; set; }
        public int RewardPonits { get; set; }
    }
}
