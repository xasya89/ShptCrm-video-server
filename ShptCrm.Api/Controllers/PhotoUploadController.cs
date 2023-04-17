using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShptCrm.Api.Services;

namespace ShptCrm.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhotoUploadController : ControllerBase
    {
        private readonly PhotoUploadService _service;
        public PhotoUploadController(PhotoUploadService service) => _service = service;

        [HttpGet("{fileName}")]
        public FileStreamResult Get(string fileName) =>
            new FileStreamResult(_service.Get(fileName), "application/octet-stream");

        [HttpPost]
        public async Task<IEnumerable<string>> Upload(IFormCollection form) =>
            await _service.Upload(Convert.ToInt32( form.Where(f=>f.Key== "actId").Single().Value), form.Files);

        /*
        [HttpPost]
        public async Task<IActionResult> Upload([FromBody] ImageUploadModel model)
        {
            
            await _service.Upload(model);
            return Ok();
        }
        */
    }

    public class ImageUploadModel
    {
        public string File { get; set; }
    }

    public class FileUploadModel
    {
        public IFormFile File { get; set; }
    }
}
