using Healthcare.Api.Model;
using Microsoft.EntityFrameworkCore;

namespace Healthcare.Api.DatabaseContext
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<DoctorSchedule> DoctorSchedules { get; set; }
        public DbSet<Appointment> Appointments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Appointment>()
           .HasIndex(a => new { a.DoctorId, a.StartTime })
           .IsUnique();

            modelBuilder.Entity<Appointment>()
            .Property(a => a.StartTime)
            .HasPrecision(0);

            modelBuilder.Entity<Appointment>()
            .Property(a => a.EndTime)
            .HasPrecision(0);


            modelBuilder.Entity<Doctor>().HasData(
                new Doctor { Id = 1, Name = "dr. Smith", Specialty = "General Practitioner" }
            );

            modelBuilder.Entity<DoctorSchedule>().HasData(
                new DoctorSchedule
                {
                    Id = 1,
                    DoctorId = 1,
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(12, 0, 0)
                },  // Rabu
                new DoctorSchedule
                {
                    Id = 2,
                    DoctorId = 1,
                    DayOfWeek = DayOfWeek.Wednesday,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(12, 0, 0)
                },
                // Jumat
                new DoctorSchedule
                {
                    Id = 3,
                    DoctorId = 1,
                    DayOfWeek = DayOfWeek.Friday,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(12, 0, 0)
                }
            );
        }
    }
}
