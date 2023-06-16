using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ShptCrm.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScreenshotUploadController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ScreenshotModel model)
        {
            using (FileStream fs = new FileStream(Guid.NewGuid().ToString() + "  screenshot.png", FileMode.CreateNew, FileAccess.Write))
                using(BinaryWriter bw = new BinaryWriter(fs))
            {
                byte[] data = Convert.FromBase64String(model.Screenshot);
                bw.Write(data);
                bw.Close();
            }
            return Ok();
        }

        public class ScreenshotModel
        {
            public int Id { get; set; }
            public string Screenshot { get; set; }
        }
    }
}
