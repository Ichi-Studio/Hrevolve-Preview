namespace Entities
{
    public class LeaveBalanceRecord
    {
        public int Id { get; set; }

        public DateTime? Time { get; set; }

        public string? Status { get; set; }

        public string? Description { get; set; }

        public string? Reason { get; set; }

        public decimal? Amount { get; set; }

        public decimal? Balance { get; set; }

        public DateTime? SubmissionDate { get; set; }

    }
}
