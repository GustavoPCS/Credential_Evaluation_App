using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CredentialEvaluationApp.Helpers;

namespace CredentialEvaluationApp.Services
{
    public class CountryService
    {
        public static List<string> GetCountries(string gradingScale = null)
        {
            if (!string.IsNullOrEmpty(gradingScale))
            {
                return DatabaseHelper.GetCountriesByGradingScale(gradingScale);
            }
            else
            {
                return DatabaseHelper.GetAllCountries();
            }

        }

    }
}
