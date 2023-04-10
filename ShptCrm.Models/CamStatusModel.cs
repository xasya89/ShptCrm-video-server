using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShptCrm.Models
{
    public class CamStatusModel
    {
        public int DevId { get; set; }
        public string PathName { get; set; }
        public int? ActId { get; set; } = null;
        public int? ActNum { get; set; } = null;
        public DateTime? ActDate { get; set; }
        public string ActDateStr { get => ActDate?.ToString("dd.MM.yy") ?? ""; }
        public bool IsOnline { get; set; } = false;
        public bool IsRecord { get; set; } = false;
    }
}
