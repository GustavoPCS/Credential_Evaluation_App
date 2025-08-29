using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CredentialEvaluationApp.Models
{
    public class Transcript : INotifyPropertyChanged
    {
        public int Transcript_ID { get; set; }
        public int Student_ID { get; set; }
        public int GradingScale_ID { get; set; }
        public string Country { get; set; }
        public double Multiplier { get; set; }
        public string TranscriptName { get; set; }
        public double TranscriptGPA { get; set; }
        public double TranscriptCredits { get; set; }
        public List<CourseEntry> Courses { get; set; }

        private GradingScale _gradingScale;
        public GradingScale GradingScale
        {
            get => _gradingScale;
            set
            {
                if (_gradingScale != value)
                {
                    _gradingScale = value;
                    OnPropertyChanged(nameof(GradingScale));
                }
            }
        }

        // Add properties for IsSelected and IsEnabled as before
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                    OnSelectionChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler OnSelectionChanged;

        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
