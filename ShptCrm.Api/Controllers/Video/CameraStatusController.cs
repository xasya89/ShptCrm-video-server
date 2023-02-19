using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShptCrm.Api.Services;

namespace ShptCrm.Api.Controllers.Video
{
    [Route("api/video/[controller]")]
    [ApiController]
    public class CameraStatusController : ControllerBase
    {
        private readonly CamStatusService _camService;
        public CameraStatusController(CamStatusService camService)
        {
            _camService = camService;
        }
        [HttpGet]
        public async Task<IEnumerable<CamStatusModel>> GetStatus() => await _camService.GetStatus();
    }
}
