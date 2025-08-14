using CredentialEvaluationApp.Helpers;
using CredentialEvaluationApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CredentialEvaluationApp
{
    public partial class SearchPage : UserControl
    {

        private ObservableCollection<Student> allStudents;
        private ObservableCollection<Student> filteredStudents;

        public SearchPage()
        {
            InitializeComponent();
            this.Loaded += SearchPage_Loaded;

        }


        ////////////////////////////////////////////////
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            PageContent.BeginAnimation(OpacityProperty, fadeIn);

        }


        private void SearchPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadStudents();
        }
        public void LoadStudents()
        {
            var students = DatabaseHelper.GetAllStudents();

            if (students == null)
            {
                // Database not available yet, exit safely
                return;
            }

            allStudents = new ObservableCollection<Student>(students);
            filteredStudents = new ObservableCollection<Student>(students);
            StudentGrid.ItemsSource = filteredStudents;

        }


        private void StudentSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (filteredStudents == null || allStudents == null || StudentSearchBox.Text == "Search by name...")
                return;

            string query = StudentSearchBox.Text.Trim().ToLower();

            filteredStudents.Clear();
            foreach (var student in allStudents)
            {
                if ((student.FirstName != null && student.FirstName.ToLower().Contains(query)) || (student.LastName != null && student.LastName.ToLower().Contains(query)))
                {
                    filteredStudents.Add(student);
                }
            }

        }

        private void StudentSearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (StudentSearchBox.Text == "Search by name...")
            {
                StudentSearchBox.Text = "";
                StudentSearchBox.Foreground = Brushes.Black;
            }
        }

        private void StudentSearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(StudentSearchBox.Text))
            {
                StudentSearchBox.Text = "Search by name...";
                StudentSearchBox.Foreground = Brushes.Gray;
            }
        }

        private void ViewStudent_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.DataContext is Student selectedStudent)
            {
                // Use the existing calculatorPage instance
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.calculatorPage.LoadStudentData(selectedStudent.Student_Id);
                mainWindow?.NavigateToPage(mainWindow.calculatorPage);
            }
        }

    }
}
