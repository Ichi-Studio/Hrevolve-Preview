namespace Entities
{
    public class Location
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public string? AddressName { get; set; }
        public string? FullAddress { get; set; }
        public bool IsGpsActive { get; set; }
        public string? Status { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedTime { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedTime { get; set; }

    }
}
