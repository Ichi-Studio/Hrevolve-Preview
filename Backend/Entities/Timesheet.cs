namespace Entities
{
    public class Timesheet
    {
        public int Id { get; set; }

        public string? StaffId { get; set; }

        public string? Name { get; set; }

        public string? Type { get; set; }

        public string? WorkingDayType { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public DateTime? MealTime { get; set; }

        public TimeSpan? Duration { get; set; }

        public string? Location { get; set; }

        public string? Reason { get; set; }

        public string? Approver { get; set; }

        public DateTime? SubmissionTime { get; set; }

        public string? Status { get; set; }

    }
}
