namespace Entities
{
    public class LeaveApplication
    {

        public int Id { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public string? ApplicationNo { get; set; }

        public string? Reason { get; set; }

        public string? Status { get; set; }

        public string? People { get; set; } 

        public string? LeaveType { get; set; }

        public TimeSpan? Duration { get; set; }

    }
}
