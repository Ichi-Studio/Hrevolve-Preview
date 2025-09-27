namespace Entities
{
    public class LeaveType
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? LeaveCode { get; set; }

        public string? LeaveTypeValue { get; set; } 

        public TimeSpan? MinimumLength { get; set; }

        public string? LeaveBalanceCalculationFormula { get; set; }

    }
}
