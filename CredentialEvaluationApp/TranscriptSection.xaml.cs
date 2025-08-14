using CredentialEvaluationApp.Helpers;
using CredentialEvaluationApp.Models;
using CredentialEvaluationApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public partial class TranscriptSection : UserControl
    {
        public ObservableCollection<CourseEntry> TranscriptCourses { get; set; } = new ObservableCollection<CourseEntry>();
        public ObservableCollection<string> AvailableGrades { get; set; } = new ObservableCollection<string>();
        public string SelectedGradingScale { get => gradingScaleComboBox.SelectedItem as string; }

        public int TranscriptID { get; set; } = 0;

        public List<CourseEntry> GetTranscriptCourses()
        {
        
            return transcriptDataGrid.ItemsSource.Cast<CourseEntry>().ToList(); 
        
        }


        private bool isEditMode = false;
        private DataGridTemplateColumn deleteColumn;
        public event EventHandler TranscriptDeleted;

        public bool IsSelected { get; set; } = false;

        public GradingScale SelectedGradingScaleObject;

        public TranscriptSection()
        {
            InitializeComponent();

            // Add sample rows
            TranscriptCourses.Add(new CourseEntry { CourseName = "", Grade = "", CreditHours = 0 });
            TranscriptCourses.Add(new CourseEntry { CourseName = "", Grade = "", CreditHours = 0 });
            TranscriptCourses.Add(new CourseEntry { CourseName = "", Grade = "", CreditHours = 0 });

            transcriptDataGrid.ItemsSource = TranscriptCourses;
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

        private void LoadCountries(string gradingScale = null)
        {
            countryComboBox.ItemsSource = CountryService.GetCountries(gradingScale);
        }

        private void LoadGradingScales(string country = null)
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
            isEditMode = !isEditMode;

            if (isEditMode)
            {
                if (!transcriptDataGrid.Columns.Contains(deleteColumn))
                {
                    transcriptDataGrid.Columns.Add(deleteColumn);
                }

                EditToggleButton.Content = "Close";
            }
            else
            {
                if (transcriptDataGrid.Columns.Contains(deleteColumn))
                {
                    transcriptDataGrid.Columns.Remove(deleteColumn);
                }

                EditToggleButton.Content = "Edit";
            }
        }

        private void AddCourse_Click(object sender, RoutedEventArgs e)
        {
            TranscriptCourses.Add(new CourseEntry { CourseName = "", Grade = "", CreditHours = 0 });
        }

        private void DeleteCourse_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CourseEntry course)
            {
                TranscriptCourses.Remove(course);
            }
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