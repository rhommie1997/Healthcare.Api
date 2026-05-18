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
                new Doctor { Id = 1, Name = "dr. Smith", Specialty = "General Practitioner" },
                new Doctor { Id = 2, Name = "dr. Test", Specialty = "Teeth Doctor" }
            );

            modelBuilder.Entity<DoctorSchedule>().HasData(
                new DoctorSchedule
                {
                    Id = 1,
                    DoctorId = 1,
                    RRulePattern = "FREQ=WEEKLY;BYDAY=MO,WE,FR",
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(12, 0, 0)
                }, 
                new DoctorSchedule
                {
                    Id = 2,
                    DoctorId = 2,
                    RRulePattern = "FREQ=WEEKLY;BYDAY=TU,TH",
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(12, 0, 0)
                }
               
            );
        }
    }
}
