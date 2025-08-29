using CredentialEvaluationApp.Helpers;
using CredentialEvaluationApp.Models;
using CredentialEvaluationApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;



namespace CredentialEvaluationApp
{
    public partial class TranscriptSection : UserControl, INotifyPropertyChanged
    {
        public ObservableCollection<Semester> Semesters { get; set; } = new ObservableCollection<Semester>();
        public ObservableCollection<string> AvailableGrades { get; set; } = new ObservableCollection<string>();
        public string SelectedGradingScale { get => gradingScaleComboBox.SelectedItem as string; }

        public double totalCredits = 0;
        public double gpa = 0;

        public int TranscriptID { get; set; } = 0;
        public List<CourseEntry> GetTranscriptCourses()
        {
            return Semesters
                .SelectMany(s => s.Courses)
                .ToList();
        }

        public ObservableCollection<Semester> GetSemesters()
        {
            return Semesters;
        }


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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private DataGridTemplateColumn deleteColumn;
        public event EventHandler TranscriptDeleted;

        public bool IsSelected { get; set; } = false;

        public GradingScale SelectedGradingScaleObject;

        public TranscriptSection()
        {
            InitializeComponent();

            // Add sample rows
            Semesters.Add(new Semester { SemesterName = "Semester 1" });
            Semesters.Last().Courses.Add(new CourseEntry { CourseName = "", Grade = "", CreditHours = 0 });

            DataContext = this;

            LoadCountries();
            LoadGradingScales();

            deleteColumn = new DataGridTemplateColumn
            {
                Header = "",
                Width = 100,
                CellTemplate = (DataTemplate)this.FindResource("DeleteButtonTemplate")
            };
        }


        public static readonly DependencyProperty TranscriptTitleProperty = DependencyProperty.Register("TranscriptTitle", typeof(string), typeof(TranscriptSection), new PropertyMetadata("Transcript"));
        public string TranscriptTitle
        {
            get { return (string)GetValue(TranscriptTitleProperty); }
            set { SetValue(TranscriptTitleProperty, value); }
        }


        private void DeleteTranscript_Click(object sender, RoutedEventArgs e)
        {
            // Raise a custom event to let the MainWindow handle the removal
            TranscriptDeleted?.Invoke(this, EventArgs.Empty);
        }

        public void LoadCountries(string gradingScale = null)
        {
            countryComboBox.ItemsSource = CountryService.GetCountries(gradingScale);
        }

        public void LoadGradingScales(string country = null)
        {
            var scaleNames = GradingScaleService.GetGradingScales(country);

            var tempGradingScale = gradingScaleComboBox.SelectedItem?.ToString() ?? string.Empty;
            gradingScaleComboBox.Items.Clear();

            foreach (var scale in scaleNames)
            {
                gradingScaleComboBox.Items.Add(scale);
            }

            gradingScaleComboBox.SelectedItem = tempGradingScale;
        }

        private void countryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (countryComboBox.SelectedItem != null)
            {
                string selectedCountry = countryComboBox.SelectedItem.ToString();
                LoadGradingScales(selectedCountry);
            }
            else
            {
                gradingScaleComboBox.SelectedIndex = -1;
                LoadGradingScales();
                LoadCountries();
            }
        }

        private void gradingScaleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gradingScaleComboBox.SelectedItem != null)
            {
                LoadCountries(gradingScaleComboBox.SelectedItem.ToString());
                int gradingScaleID = DatabaseHelper.GetGradingScaleIdByName(gradingScaleComboBox.SelectedItem as string);
                LoadAvailableGrades(gradingScaleID);

                SelectedGradingScaleObject = DatabaseHelper.GetGradingScaleByName(gradingScaleComboBox.SelectedItem.ToString());
            }
        }

        private void LoadAvailableGrades(int gradingScaleId)
        {
            AvailableGrades.Clear();

            var grades = DatabaseHelper.GetLocalGradesByGradingScale(gradingScaleId);

            foreach (var grade in grades)
            {
                AvailableGrades.Add(grade);
            }
        }


        private void ToggleEditMode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                // Get the Semester this button belongs to
                if (button.Tag is not Semester semester)
                    return;

                semester.IsEditMode = !semester.IsEditMode;

                // Find the DataGrid in the same parent container
                var dataGrid = FindDataGridForButton(button);
                if (dataGrid == null)
                {
                    MessageBox.Show("Edit mode toggled");
                    return;
                }

                // Check if Delete column already exists
                var deleteColumn = dataGrid.Columns.FirstOrDefault(c => c.Header?.ToString() == "");
                if (deleteColumn != null)
                {
                    // Remove the Delete column
                    dataGrid.Columns.Remove(deleteColumn);
                    button.Content = "Edit";
                }
                else
                {
                    // Add Delete column
                    deleteColumn = new DataGridTemplateColumn
                    {
                        Header = "",
                        Width = 80,
                        CellTemplate = (DataTemplate)FindResource("DeleteButtonTemplate")
                    };
                    dataGrid.Columns.Add(deleteColumn);
                    button.Content = "Close";

                }
            }
        }

        private DataGrid FindDataGridForButton(DependencyObject source)
        {
            // 1) Find the ItemsControl that owns this template
            var itemsControl = FindAncestor<ItemsControl>(source);
            if (itemsControl == null) return null;

            // 2) Get the specific item container (ContentPresenter) that contains this button
            var container = ItemsControl.ContainerFromElement(itemsControl, source) as ContentPresenter;
            if (container == null) return null;

            // 3) Look up the named DataGrid in THIS item's DataTemplate only
            //    (make sure your DataGrid has x:Name="transcriptDataGrid" in the DataTemplate)
            var dg = container.ContentTemplate?.FindName("transcriptDataGrid", container) as DataGrid;
            if (dg != null) return dg;

            // Fallback: scoped visual search under just this container (not the whole ItemsControl)
            return FindChild<DataGrid>(container);
        }

        // Helpers
        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T match) return match;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private static T FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;
                var result = FindChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }





        private void DeleteSemester_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Semester semesterToDelete)
            {
                // Remove from the collection
                if (Semesters.Contains(semesterToDelete))
                {
                    Semesters.Remove(semesterToDelete);
                }

            }
        }




        private void AddCourse_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Semester semester)
            {
                semester.Courses.Add(new CourseEntry { CourseName = "", Grade = "", CreditHours = 0 });
            }
        }

        private void DeleteCourse_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CourseEntry course)
            {
                // Remove from UI (in-memory collection)
                var semester = FindSemesterForCourse(course);
                if (semester != null)
                {
                    semester.Courses.Remove(course);
                }

                // Remove from database
                if (course.Course_ID > 0) // only delete if it exists in DB
                {
                    DatabaseHelper.DeleteCourse(course.Course_ID);
                }
            }
        }


        // Helper: find the semester of a course
        private Semester FindSemesterForCourse(CourseEntry course)
        {
            // Assuming you have a ViewModel or collection of semesters called Semesters
            foreach (var sem in Semesters)
            {
                if (sem.Courses.Contains(course))
                    return sem;
            }
            return null;
        }


        private void AddSemester_Click(object sender, RoutedEventArgs e)
        {
            int semesterNumber = Semesters.Count + 1;
            Semesters.Add(new Semester { SemesterName = $"Semester {semesterNumber}" });
            Semesters.Last().Courses.Add(new CourseEntry { CourseName = "", Grade = "", CreditHours = 0 });
        }

        public double MultiplierValue
        {
            get
            {
                double multiplier;
                if (double.TryParse(MultiplierTextBox.Text, out multiplier))
                {
                    return multiplier;
                }
                return 1.0;
            }
        }

        private void DataGridCell_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGridCell cell = sender as DataGridCell;
            if (cell != null && !cell.IsEditing && !cell.IsReadOnly)
            {
                if (!cell.IsFocused)
                {
                    cell.Focus();
                }

                DataGrid dataGrid = FindParent<DataGrid>(cell);
                if (dataGrid != null)
                {
                    dataGrid.BeginEdit();
                }

                e.Handled = true;
            }
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null)
                return null;

            if (parentObject is T parent)
            {
                return parent;
            }

            return FindParent<T>(parentObject);
        }

    }
}