using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CredentialEvaluationApp.Helpers
{
    public static class AppEvents
    {
        public static event Action GradingScaleUpdated;

        public static void OnGradingScaleUpdated()
        {
            GradingScaleUpdated?.Invoke();
        }
    }
}
