using Healthcare.Api.BusinessService.Interface;
using Healthcare.Api.Dto.Appointments;
using Healthcare.Api.Dto.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Healthcare.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;
        public AppointmentController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }


        [HttpGet("doctors/{id}/availability")]
        public async Task<IActionResult> GetAvailability(int id, [FromQuery] DateTime from, [FromQuery] int slot = 30)
        {
            // Validasi slot (15/30/60)
            if (slot != 15 && slot != 30 && slot != 60)
            {
                ResponseDto errorResponse = new ResponseDto
                {
                    IsSuccess = false,
                };
                return Ok(errorResponse);
            }

            ResponseDto result = await _appointmentService.GetAvailabilityAsync(id, from, slot);
            return Ok(result);
        }


        [HttpPost]
        public async Task<IActionResult> Create(CreateAppointmentRequestDto request)
        {
            if (request.Duration != 15 && request.Duration != 30 && request.Duration != 60)
            {
                ResponseDto errorResponse = new ResponseDto { IsSuccess = false, Message = "Durasi harus 15, 30, atau 60 menit." };
                return BadRequest(errorResponse);
            }

            if (request.Start.Minute % 5 != 0)
            {
                ResponseDto errorResponse = new ResponseDto { IsSuccess = false, Message = "Waktu mulai harus kelipatan 5 menit." };
                return BadRequest(errorResponse); 
            }

            ResponseDto result = await _appointmentService.CreateAppointmentAsync(request);

            if (!result.IsSuccess)
            {
                if (result.Message.Contains("Overlap") || result.Message.Contains("terisi"))
                {
                    return Conflict(result);
                }

                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Cancel(int id)
        {
            ResponseDto result = await _appointmentService.CancelAppointmentAsync(id);

            if (!result.IsSuccess)
            {
                return Conflict(result); 
            }

            return Ok(result);
        }
    }
}
