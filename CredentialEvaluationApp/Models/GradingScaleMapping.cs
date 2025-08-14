using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CredentialEvaluationApp.Models
{
    public class GradingScaleMapping
    {
        public int GradingScaleMappings_ID { get; set; }
        public int GradingScale_ID { get; set; }
        public string LocalGrade { get; set; }
        public string USEquivalent { get; set; }
        public string USLetter { get; set; }

    }
}
