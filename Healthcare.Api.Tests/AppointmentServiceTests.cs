using Healthcare.Api.BusinessService.Service;
using Healthcare.Api.DatabaseContext;
using Healthcare.Api.Dto.Appointments;
using Healthcare.Api.Dto.Common;
using Healthcare.Api.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Healthcare.Api.Tests;

public class AppointmentServiceTests
{
    private AppDbContext GetDbContext()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
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

    [Fact]
    public async Task GetAvailability_ShouldReturn6Slots_OnMonday()
    {
        AppDbContext context = GetDbContext();
        AppointmentService service = new AppointmentService(context, GetConfig());

        context.DoctorSchedules.Add(new DoctorSchedule
        {
            DoctorId = 1,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(12, 0, 0)
        });
        await context.SaveChangesAsync();

        ResponseDto result = await service.GetAvailabilityAsync(1, DateTime.Parse("2026-05-11"), 30);
        List<string> slots = (List<string>)result.Data!;

        Assert.Equal(6, slots.Count); // Test Case 1
    }

    [Fact]
    public async Task CreateAppointment_ShouldSucceed_WhenNoOverlap()
    {
        AppDbContext context = GetDbContext();
        AppointmentService service = new AppointmentService(context, GetConfig());

        context.DoctorSchedules.Add(new DoctorSchedule
        {
            DoctorId = 1,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(12, 0, 0)
        });
        await context.SaveChangesAsync();

        CreateAppointmentRequestDto request = new CreateAppointmentRequestDto
        {
            DoctorId = 1,
            PatientId = 1,
            Duration = 30,
            Start = DateTimeOffset.Parse("2026-05-11T09:00:00Z")
        };

        ResponseDto result = await service.CreateAppointmentAsync(request);

        Assert.True(result.IsSuccess); // Test Case 2
        Assert.Equal("Booking berhasil dibuat.", result.Message);
    }

    [Fact]
    public async Task CreateAppointment_ShouldFail_WhenOverlapPersis()
    {
        AppDbContext context = GetDbContext();
        AppointmentService service = new AppointmentService(context, GetConfig());

        context.Appointments.Add(new Appointment
        {
            DoctorId = 1,
            StartTime = DateTimeOffset.Parse("2026-05-11T09:30:00Z"),
            EndTime = DateTimeOffset.Parse("2026-05-11T10:00:00Z")
        });
        await context.SaveChangesAsync();

        CreateAppointmentRequestDto request = new CreateAppointmentRequestDto
        {
            DoctorId = 1,
            PatientId = 1,
            Duration = 30,
            Start = DateTimeOffset.Parse("2026-05-11T09:45:00Z")
        };

        ResponseDto result = await service.CreateAppointmentAsync(request);

        Assert.False(result.IsSuccess); // Test Case 3
    }

    [Fact]
    public async Task CreateAppointment_ShouldFail_OutsideWorkingHours()
    {
        AppDbContext context = GetDbContext();
        AppointmentService service = new AppointmentService(context, GetConfig());

        context.DoctorSchedules.Add(new DoctorSchedule
        {
            DoctorId = 1,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(12, 0, 0)
        });
        await context.SaveChangesAsync();

        CreateAppointmentRequestDto request = new CreateAppointmentRequestDto
        {
            DoctorId = 1,
            PatientId = 1,
            Duration = 30,
            Start = DateTimeOffset.Parse("2026-05-11T12:00:00Z")
        };

        ResponseDto result = await service.CreateAppointmentAsync(request);

        Assert.False(result.IsSuccess); // Test Case 4
        Assert.Contains("jam kerja", result.Message);
    }

    [Fact]
    public async Task CancelAppointment_ShouldSucceed_WhenBeforeCutOff()
    {
        AppDbContext context = GetDbContext();
        AppointmentService service = new AppointmentService(context, GetConfig());

        DateTimeOffset startTime = DateTimeOffset.UtcNow.AddHours(10);
        Appointment appt = new Appointment { Id = 55, StartTime = startTime };
        context.Appointments.Add(appt);
        await context.SaveChangesAsync();

        ResponseDto result = await service.CancelAppointmentAsync(55);

        Assert.True(result.IsSuccess); // Test Case 5
        Assert.Equal("Appointment berhasil dibatalkan.", result.Message);
    }

    [Fact]
    public async Task CancelAppointment_ShouldFail_WhenPassingCutOff()
    {
        AppDbContext context = GetDbContext();
        AppointmentService service = new AppointmentService(context, GetConfig());

        DateTimeOffset startTime = DateTimeOffset.UtcNow.AddHours(1);
        context.Appointments.Add(new Appointment { Id = 1, StartTime = startTime });
        await context.SaveChangesAsync();

        ResponseDto result = await service.CancelAppointmentAsync(1);

        Assert.False(result.IsSuccess); // Test Case 6
    }

    [Fact]
    public async Task CreateAppointment_ShouldPreventDoubleBooking()
    {
        AppDbContext context = GetDbContext();
        AppointmentService service = new AppointmentService(context, GetConfig());

        CreateAppointmentRequestDto req1 = new CreateAppointmentRequestDto
        {
            DoctorId = 1,
            Start = DateTimeOffset.Parse("2026-05-11T10:30:00Z"),
            Duration = 30
        };
        CreateAppointmentRequestDto req2 = new CreateAppointmentRequestDto
        {
            DoctorId = 1,
            Start = DateTimeOffset.Parse("2026-05-11T10:30:00Z"),
            Duration = 30
        };

        await service.CreateAppointmentAsync(req1);
        ResponseDto result2 = await service.CreateAppointmentAsync(req2);

        Assert.False(result2.IsSuccess); // Test Case 7
    }
}
