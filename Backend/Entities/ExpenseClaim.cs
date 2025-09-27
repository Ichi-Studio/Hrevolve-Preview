namespace Entities
{
    public class ExpenseClaim
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? ExpenseContent { get; set; }
        public DateOnly Date { get; set; }

        public string? Amount { get; set; }

        public string? Reason { get; set; }

        public string? SubmissionTime { get; set; }

        public string? Approver {  get; set; }

        public bool Status { get; set; }


    }
}
