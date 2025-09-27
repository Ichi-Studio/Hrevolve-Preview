namespace Entities
{
    public class LeaveBalance
    {
        public int Id { get; set; }

        public string? Avatar { get; set; }

        public string? StaffId { get; set; }

        public string? Name { get; set; }

        public string? LeavePolicy { get; set; }

        public decimal? CurrentBalance { get; set; }

        public decimal? OnPlan { get; set; }

        public decimal? Usable { get; set; }

        public string? EmploymentType { get; set; }

        public string? SalaryType { get; set; }

    }

}
