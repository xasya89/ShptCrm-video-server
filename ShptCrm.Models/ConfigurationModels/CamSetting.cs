using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShptCrm.Models.ConfigurationModels
{
    public class CamSettings
    {
        [Required]
        public int DevId { get; set; }
        [Required]
        public string PathName { get; set; }
        [Required]
        public string IpAdress { get; set; }
    }

    public class CamsSettings
    {
        [Required]
        public string DvrServer { get; set; }
        [Required]
        public string DvrAgentFolder { get; set; }
        [Required]
        public IEnumerable<CamSettings> Settings { get; set; }
    }
}
