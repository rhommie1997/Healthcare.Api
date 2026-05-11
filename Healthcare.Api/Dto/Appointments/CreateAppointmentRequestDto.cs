namespace Healthcare.Api.Dto.Appointments
{
    public class CreateAppointmentRequestDto
    {
        public int DoctorId { get; set; }
        public int PatientId { get; set; }
        public DateTimeOffset Start { get; set; }
        public int Duration { get; set; }
    }
}
