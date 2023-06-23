using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShptCrm.Models.RecordModel
{
    public record RecordStartModel(int actId, IEnumerable<int> cams);
}
