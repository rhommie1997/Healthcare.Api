namespace Healthcare.Api.Model
{
    public class Appointment
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public int PatientId { get; set; }

        // Wajib DateTimeOffset untuk kriteria "Timezone Aware"
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public Doctor? Doctor { get; set; }
    }
}
