using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShptCrm.Models
{
    public class CameraStatusModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public CameraStatusData Data { get; set; }
    }

    public class CameraStatusData
    {
        public bool Online { get; set; }
        public bool Recording { get; set; }
    }
}
