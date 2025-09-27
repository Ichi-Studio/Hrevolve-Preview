namespace Entities
{
    public class Attendance
    {
        public int Id { get; set; }

        public string? StaffId { get; set; }

        public string? Name { get; set; }

        public string? Position { get; set; }

        public string? Department { get; set; }

        public string? Period { get; set; }

        public decimal? ScheduledHours { get; set; }

        public decimal? HoursWorked { get; set; }

        public decimal? StatutoryDayOvertime { get; set; }

        public decimal? RestDayOvertime { get; set; }

        public decimal? WorkingDayOvertime { get; set; }

        public DateTime? BelateLength { get; set; }

        public DateTime? LeaveTime { get; set; }

        public DateTime? AbsenceLength { get; set; }

        public string? Adjust { get; set; }

        public string? TimeOffInLieu { get; set; }

    }
}
