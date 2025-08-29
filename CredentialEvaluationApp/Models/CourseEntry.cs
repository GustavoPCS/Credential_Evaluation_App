using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CredentialEvaluationApp.Models
{
    using System.ComponentModel;

    public class CourseEntry : INotifyPropertyChanged
    {

        public int Course_ID { get; set; }
        public int Transcript_ID { get; set; }
        public double USCreditHours { get; set; }


        private string _courseName;
        private string _grade;
        private double _creditHours;
        private string _usConvertedGrade;

        public string CourseName
        {
            get => _courseName;
            set
            {
                if (_courseName != value)
                {
                    _courseName = value;
                    OnPropertyChanged(nameof(CourseName));
                }
            }
        }

        public string Grade
        {
            get => _grade;
            set
            {
                if (_grade != value)
                {
                    _grade = value;
                    OnPropertyChanged(nameof(Grade));

                    // Clear USConvertedGrade when Grade is changed
                    USConvertedGrade = string.Empty;
                }
            }
        }

        public double CreditHours
        {
            get => _creditHours;
            set
            {
                if (_creditHours != value)
                {
                    _creditHours = value;
                    OnPropertyChanged(nameof(CreditHours));
                }
            }
        }

        public string USConvertedGrade
        {
            get => _usConvertedGrade;
            set
            {
                if (_usConvertedGrade != value)
                {
                    _usConvertedGrade = value;
                    OnPropertyChanged(nameof(USConvertedGrade));
                }
            }
        }

        public string SemesterName { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}
