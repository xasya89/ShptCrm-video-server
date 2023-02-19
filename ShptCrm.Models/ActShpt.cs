using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShptCrm.Models
{
    public class ActShpt
    {
        public int Id { get; set; }
        public int ActNum { get; set; }
        public DateTime ActDate { get; set; }
        public string ActDateStr { get => ActDate.ToString("dd.MM.yy"); }
        public string OrgName { get; set; }
        public string Fahrer { get; set; }
        public string CarNum { get; set; }
        public string Note { get; set; }
    }
}
