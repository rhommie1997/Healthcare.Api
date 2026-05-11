namespace Healthcare.Api.Dto.Appointments
{
    public class AvailabilityResponseDto
    {
        public int DoctorId { get; set; }
        public DateTime Date { get; set; }
        public List<string> AvailableSlots { get; set; } = new List<string>();
    }
}
