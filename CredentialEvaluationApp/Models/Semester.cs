using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CredentialEvaluationApp.Models
{
    public class Semester : INotifyPropertyChanged
    {

        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                _isEditMode = value;
                OnPropertyChanged(nameof(IsEditMode));
            }
        }

        public string SemesterName { get; set; }
        public ObservableCollection<CourseEntry> Courses { get; set; } = new();

        private bool _showDeleteColumn;
        public bool ShowDeleteColumn
        {
            get => _showDeleteColumn;
            set
            {
                _showDeleteColumn = value;
                OnPropertyChanged(nameof(ShowDeleteColumn));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }


}
