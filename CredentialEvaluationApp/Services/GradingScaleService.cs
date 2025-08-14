using CredentialEvaluationApp.Helpers;
using CredentialEvaluationApp.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CredentialEvaluationApp.Services
{
    public class GradingScaleService
    {
        public static List<string> GetGradingScales(string country = null)
        {
            List<GradingScale> gradingScales;

            if (!string.IsNullOrEmpty(country))
            {
                gradingScales = DatabaseHelper.GetGradingScalesByCountry(country);
            }
            else
            {
                gradingScales = DatabaseHelper.GetAllGradingScales();
            }

            var scaleNames = new List<string>();
            foreach (var scale in gradingScales)
            {
                scaleNames.Add(scale.ScaleName);
            }

            return scaleNames;
        }

    }
}
