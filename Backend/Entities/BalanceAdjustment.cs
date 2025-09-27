namespace Entities
{
    public class BalanceAdjustment
    {

        public int Id { get; set; }

        public string? StaffId { get; set; }

        public string? Name { get; set; }

        public decimal? Amount { get; set; }

        public string? LeaveType { get; set; }

        public DateTime? Time { get; set; }

        public string? Reason { get; set; }

    }
}
