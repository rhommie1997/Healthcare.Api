using Healthcare.Api.BusinessService.Service;
using Healthcare.Api.DatabaseContext;
using Healthcare.Api.Dto.Appointments;
using Healthcare.Api.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Healthcare.Api.Tests
{
    public class AppointmentServiceConcTests
    {
        private static readonly string _dbName = Guid.NewGuid().ToString();
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(_dbName) 
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new AppDbContext(options);
        }

        private IConfiguration GetConfig()
        {
            Dictionary<string, string> settings = new Dictionary<string, string> {
            {"DoctorSettings:WorkingRRule", "FREQ=WEEKLY;BYDAY=MO,WE,FR"}
        };
            return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        }

        private async Task Seeder(AppDbContext context)
        {
            context.DoctorSchedules.Add(new DoctorSchedule
            {
                DoctorId = 1,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(12, 0, 0)
            });
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task CreateAppointment_ShouldPreventDoubleBooking()
        {
            using (var seedContext = GetDbContext())
            {
                await Seeder(seedContext);
            }

            var request = new CreateAppointmentRequestDto
            {
                DoctorId = 1,
                PatientId = 1,
                Duration = 30,
                Start = DateTimeOffset.Parse("2026-05-11T10:30:00+07:00")
            };

            var tasks = Enumerable.Range(0, 20)
                .Select(async _ =>
                {
                    using var context = GetDbContext(); // 🔥 penting
                    var service = new AppointmentService(context, GetConfig());

                    return await service.CreateAppointmentAsync(request);
                });

            var results = await Task.WhenAll(tasks);

            int sukses = results.Count(r => r.IsSuccess);
            int gagal = results.Count(r => !r.IsSuccess);

            Assert.Equal(1, sukses);
            Assert.Equal(19, gagal);

            using (var verifyContext = GetDbContext())
            {
                Assert.Equal(1, await verifyContext.Appointments.CountAsync());
            }
        }
    }
}
