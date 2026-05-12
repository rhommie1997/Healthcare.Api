using Healthcare.Api.BusinessService.Interface;
using Healthcare.Api.Constants;
using Healthcare.Api.DatabaseContext;
using Healthcare.Api.Dto.Appointments;
using Healthcare.Api.Dto.Common;
using Healthcare.Api.Model;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Evaluation;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Healthcare.Api.BusinessService.Service
{
    public class AppointmentService : IAppointmentService
    {
        private readonly AppDbContext _context;
        private readonly string _rrulePattern;

        public AppointmentService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _rrulePattern = configuration.GetValue<string>("DoctorSettings:WorkingRRule") ?? "";
        }



        private bool CheckingPraktekRule(DateTime date)
        {
            CalendarEvent calendarEvent = new CalendarEvent
            {
                DtStart = new CalDateTime(date.Date),
                RecurrenceRule = new RecurrencePattern(_rrulePattern)
            };

            IEnumerable<Occurrence> occurrences = calendarEvent.GetOccurrences(
                new CalDateTime(date.Date)
            );

            bool isPractice = occurrences
               .Take(1)
               .Any(x => x.Period.StartTime.Value.Date == date.Date);
            
            return isPractice;
        }

        private List<string> GenerateAvailableSlots(DoctorSchedule? schedule, List<Appointment> existing,DateTime date,int slot)
        {
            List<string> availableSlots = new List<string>();
            if (schedule != null)
            {
                DateTimeOffset current = new DateTimeOffset(date.Date.Add(schedule.StartTime), TimeSpan.Zero);
                DateTimeOffset end = new DateTimeOffset(date.Date.Add(schedule.EndTime), TimeSpan.Zero);

                while (current.AddMinutes(slot) <= end)
                {
                    if (!existing.Any(a => current < a.EndTime && current.AddMinutes(slot) > a.StartTime))
                        availableSlots.Add(current.ToString("HH:mm"));

                    current = current.AddMinutes(slot);
                }
            }
            return availableSlots;
        }


        public async Task<ResponseDto> GetAvailabilityAsync(int doctorId, DateTime date, int slot)
        {
            bool isPractice = CheckingPraktekRule(date);
            if (!isPractice)
            {
                return new ResponseDto
                {
                    IsSuccess = false,
                    Message = AppConstants.NO_PRACTICE
                };
            }

            DoctorSchedule? schedule = await _context.DoctorSchedules
            .FirstOrDefaultAsync(s => s.DoctorId == doctorId && s.DayOfWeek == date.DayOfWeek);

            //if (schedule == null)
            //    return new ResponseDto { IsSuccess = false, Message = "Dokter tidak praktek di hari ini." };

            List<Appointment> existing = await _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.StartTime.Date == date.Date)
                .ToListAsync();

            List<string> availableSlots = GenerateAvailableSlots(schedule,existing,date,slot);
           
            return new ResponseDto { Data = availableSlots };
        }

        private async Task<ResponseDto> ValidateCreateAppointmentAsync(CreateAppointmentRequestDto request, DateTimeOffset startUtc, DateTimeOffset endUtc)
        {
            DoctorSchedule? schedule = await _context.DoctorSchedules
                .FirstOrDefaultAsync(s => s.DoctorId == request.DoctorId && s.DayOfWeek == startUtc.DayOfWeek);

            if (schedule == null || startUtc.TimeOfDay < schedule.StartTime || endUtc.TimeOfDay > schedule.EndTime)
            {
                return new ResponseDto { IsSuccess = false, Message = AppConstants.OUTSIDE_WORKING_HOURS };
            }

            bool isOverlap = await _context.Appointments.AnyAsync(a =>
                a.DoctorId == request.DoctorId &&
                startUtc < a.EndTime &&
                endUtc > a.StartTime
            );

            if (isOverlap)
            {
                return new ResponseDto { IsSuccess = false, Message = AppConstants.CONFLICT_MESSAGE };
            }

            return new ResponseDto { IsSuccess = true };
        }



        public async Task<ResponseDto> CreateAppointmentAsync(CreateAppointmentRequestDto request)
        {
            DateTimeOffset startUtc = request.Start.ToUniversalTime();
            DateTimeOffset endUtc = startUtc.AddMinutes(request.Duration);
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            try
            {
                ResponseDto responseDto = await ValidateCreateAppointmentAsync(request, startUtc, endUtc);

                if (responseDto.IsSuccess)
                {
                    Appointment appointment = new Appointment
                    {
                        DoctorId = request.DoctorId,
                        PatientId = request.PatientId,
                        StartTime = startUtc,
                        EndTime = endUtc
                    };

                    _context.Appointments.Add(appointment);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    responseDto.Message = AppConstants.BOOKING_INSERT_SUCCESS;
                    responseDto.Data = appointment;
                }

                return responseDto;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                if (ex.InnerException?.Message.Contains("UNIQUE") == true || ex.InnerException?.Message.Contains("duplicate") == true)
                {
                    return new ResponseDto { IsSuccess = false, Message = AppConstants.SLOT_ALREADY_TAKEN };
                }
                return new ResponseDto { IsSuccess = false, Message = AppConstants.BOOKING_INSERT_FAILED };
            }
        }
        public async Task<ResponseDto> CancelAppointmentAsync(int id)
        {
            Appointment? appt = await _context.Appointments.FindAsync(id);
            if (appt == null)
            {
                return new ResponseDto { IsSuccess = false, Message = AppConstants.APPOINTMENT_NOT_FOUND };
            }

            if (appt.StartTime - DateTimeOffset.UtcNow < TimeSpan.FromHours(2))
            {
                return new ResponseDto { IsSuccess = false, Message = AppConstants.CANNOT_CANCEL_WITH_LESS_THAN_2_HOURS };
            }

            _context.Appointments.Remove(appt);
            await _context.SaveChangesAsync();
            return new ResponseDto { Message = AppConstants.APPOINTMENT_INSERT_SUCCESS, IsSuccess = true,Data = appt };
        }
    }
}
