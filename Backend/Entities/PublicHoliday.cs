namespace Entities
{
    public class PublicHoliday
    {
        public string? Code { get; set; }

        public string? PolicyName { get; set; }

        public string? Type { get; set; }

        public DateOnly Date { get; set; }

        public string? Week { get; set; }
    }
}
