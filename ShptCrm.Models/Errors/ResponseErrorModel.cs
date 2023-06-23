using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShptCrm.Models.Errors
{
    public record ResponseErrorModel(string traceId, string Error, string? InnerError);
}
