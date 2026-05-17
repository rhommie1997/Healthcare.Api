using Healthcare.Api.Dto.Appointments;
using Healthcare.Api.Dto.Common;

namespace Healthcare.Api.BusinessService.Interface
{
    public interface IAppointmentService
    {
        Task<ResponseDto> GetAvailabilityAsync(int doctorId, DateTimeOffset date, int slot);
        Task<ResponseDto> CreateAppointmentAsync(CreateAppointmentRequestDto request);
        Task<ResponseDto> CancelAppointmentAsync(int id);
    }
}
