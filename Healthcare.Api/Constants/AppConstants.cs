namespace Healthcare.Api.Constants
{
    public static class AppConstants
    {
        //Error Messages
        public const string CONFLICT_MESSAGE = "Slot sudah terisi (Overlap).";
        public const string OUTSIDE_WORKING_HOURS = "Dokter tidak praktek di hari ini.";
        public const string NO_PRACTICE = "Dokter tidak praktek di hari ini.";
        public const string SLOT_ALREADY_TAKEN = "Gagal. Slot baru saja terisi oleh pasien lain.";
        public const string APPOINTMENT_NOT_FOUND = "Appointment tidak ditemukan.";
        public const string CANNOT_CANCEL_WITH_LESS_THAN_2_HOURS = "Tidak bisa membatalkan dalam waktu kurang dari 2 jam.";
        public const string DURATION_CASE = "Durasi harus 15, 30, atau 60 menit.";
        public const string MULTIPLE_CASE = "Waktu mulai harus kelipatan 5 menit.";

        //Messages
        public const string BOOKING_INSERT_SUCCESS = "Booking berhasil dibuat.";
        public const string BOOKING_INSERT_FAILED = "Booking gagal dibuat.";
        public const string APPOINTMENT_INSERT_SUCCESS = "Appointment berhasil dibatalkan.";

    }
}
