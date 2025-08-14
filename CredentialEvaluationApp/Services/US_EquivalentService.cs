using CredentialEvaluationApp.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CredentialEvaluationApp.Models;

namespace CredentialEvaluationApp.Services
{
    public class US_EquivalentService
    {

        public Dictionary<string, double> USGrades { get; set; } = new Dictionary<string, double>
                {
                    { "A", 4.0 },
                    { "A-", 3.7 },
                    { "B+", 3.3 },
                    { "B", 3.0 },
                    { "B-", 2.7 },
                    { "C+", 2.3 },
                    { "C", 2.0 },
                    { "C-", 1.7 },
                    { "D+", 1.3 },
                    { "D", 1.0 },
                    { "D-", 0.7 },
                    { "F", 0.0 }
                };

        public ObservableCollection<string> GetUS_Grades()
        {
            return new ObservableCollection<string>(USGrades.Keys);
        }

        public double Convert(string letter)
        {

            if (USGrades.TryGetValue(letter, out double score))
            {
                return score;
            }

            throw new ArgumentException($"Invalid grade letter: {letter}");

        }

        public string Convert(double num)
        {

            foreach (var kvp in USGrades)
            {
                if (kvp.Value == num)
                {
                    return kvp.Key;
                }
            }

            throw new ArgumentException($"Invalid grade value: {num}");

        }



        public void ConversionToNumGrade(ref ObservableCollection<GradingScaleMapping> Mappings)
        {

            foreach (var mapping in Mappings)
            {

                mapping.USEquivalent = Convert(mapping.USLetter).ToString("F1");

            }

        }

        public static string ConversionToLetterGrade(double num)
        {
            var service = new US_EquivalentService();
            return service.Convert(num);
        }

    }
}
