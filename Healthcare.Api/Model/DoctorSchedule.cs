namespace Healthcare.Api.Model
{
    public class DoctorSchedule
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public string RRulePattern { get; set; } = string.Empty;

        // Jam mulai dan selesai operasional
        public TimeSpan StartTime { get; set; } // Contoh: 09:00:00
        public TimeSpan EndTime { get; set; }   // Contoh: 12:00:00

        public Doctor? Doctor { get; set; }
    }
}
