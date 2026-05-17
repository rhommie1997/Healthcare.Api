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
using System.Data;
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


        private bool CheckingPraktekRule(DateTimeOffset date)
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

        private List<string> GenerateAvailableSlots(DoctorSchedule? schedule, List<Appointment> existing,DateTimeOffset date,int slot)
        {
            List<string> availableSlots = new List<string>();
            if (schedule != null)
            {
                DateTimeOffset current = new DateTimeOffset(date.Date.Add(schedule.StartTime), date.Offset);
                DateTimeOffset end = new DateTimeOffset(date.Date.Add(schedule.EndTime), date.Offset);

                while (current.AddMinutes(slot) <= end)
                {
                    if (!existing.Any(a => current < a.EndTime && current.AddMinutes(slot) > a.StartTime))
                    {
                        availableSlots.Add(current.ToString("HH:mm"));
                    }

                    current = current.AddMinutes(slot);
                }
            }
            return availableSlots;
        }


        public async Task<ResponseDto> GetAvailabilityAsync(int doctorId, DateTimeOffset date, int slot)
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

            DateTimeOffset startOfDayLocal = new DateTimeOffset(date.Date, date.Offset);
            DateTimeOffset endOfDayLocal = startOfDayLocal.AddDays(1);

            List<Appointment> existing = await _context.Appointments
            .Where(a => a.DoctorId == doctorId &&
                        a.StartTime >= startOfDayLocal &&
                        a.StartTime < endOfDayLocal)
            .ToListAsync();

            List<string> availableSlots = GenerateAvailableSlots(schedule,existing,date.DateTime,slot);
           
            return new ResponseDto { Data = availableSlots };
        }

        private async Task<ResponseDto> ValidateCreateAppointmentAsync(
    CreateAppointmentRequestDto request,
    DateTimeOffset startLocal,
    DateTimeOffset endLocal)
        {
            DayOfWeek inputDayOfWeek = startLocal.DayOfWeek;

            DoctorSchedule? schedule = await _context.DoctorSchedules
                .FirstOrDefaultAsync(s =>
                    s.DoctorId == request.DoctorId &&
                    s.DayOfWeek == inputDayOfWeek);

            if (schedule == null)
            {
                return new ResponseDto
                {
                    IsSuccess = false,
                    Message = AppConstants.NO_PRACTICE
                };
            }

            // ✅ FIX 1: pakai TimeOfDay dari startLocal
            TimeSpan localStart = startLocal.TimeOfDay;
            TimeSpan localEnd = endLocal.TimeOfDay;

            if (localStart < schedule.StartTime || localEnd > schedule.EndTime)
            {
                return new ResponseDto
                {
                    IsSuccess = false,
                    Message = AppConstants.OUTSIDE_WORKING_HOURS
                };
            }

            // ✅ FIX 2: pakai END TIME REAL (bukan 30 menit hardcoded)
            bool isOverlap = await _context.Appointments.AnyAsync(a =>
                a.DoctorId == request.DoctorId &&
                startLocal < a.EndTime &&
                endLocal > a.StartTime
            );

            if (isOverlap)
            {
                return new ResponseDto
                {
                    IsSuccess = false,
                    Message = AppConstants.CONFLICT_MESSAGE
                };
            }

            return new ResponseDto { IsSuccess = true };
        }



        public async Task<ResponseDto> CreateAppointmentAsync(CreateAppointmentRequestDto request)
        {
            DateTimeOffset startLocal = request.Start.ToLocalTime();
            DateTimeOffset endLocal = startLocal.AddMinutes(request.Duration);

            await using var transaction =
                await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            try
            {
                // 🔥 STEP 1: VALIDASI (PAKE METHOD KAMU)
                ResponseDto validationResult =
                    await ValidateCreateAppointmentAsync(request, startLocal, endLocal);

                if (!validationResult.IsSuccess)
                {
                    await transaction.RollbackAsync();
                    return validationResult;
                }

                // 🔥 STEP 2: INSERT
                Appointment appointment = new Appointment
                {
                    DoctorId = request.DoctorId,
                    PatientId = request.PatientId,
                    StartTime = startLocal,
                    EndTime = endLocal
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return new ResponseDto
                {
                    IsSuccess = true,
                    Message = AppConstants.BOOKING_INSERT_SUCCESS,
                    Data = appointment
                };
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();

                return new ResponseDto
                {
                    IsSuccess = false,
                    Message = AppConstants.BOOKING_INSERT_FAILED
                };
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
            return new ResponseDto { Message = AppConstants.APPOINTMENT_DELETE_SUCCESS, IsSuccess = true,Data = appt };
        }
    }
}
