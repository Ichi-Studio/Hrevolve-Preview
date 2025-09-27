namespace Entities
{
    public class AttendanceData
    {
        public int Id { get; set; }

        public string? StaffId { get; set; }

        public string? Name { get; set; }

        public string? Department { get; set; }

        public DateTime? ClockInTime { get; set; }

        public string? AttendanceType { get; set; }

        public string? GPS { get; set; }

        public string? DeviceName { get; set; }

        public string? CodeSource { get; set; }

        public string? FieldWork { get; set; }

        public string? BirdDeviceId { get; set; }

    }
}
