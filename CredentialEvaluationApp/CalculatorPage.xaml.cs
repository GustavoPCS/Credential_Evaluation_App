using ClosedXML.Excel;
using CredentialEvaluationApp.Helpers;
using CredentialEvaluationApp.Models;
using CredentialEvaluationApp.Services;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Win32;
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
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using MigraTable = MigraDoc.DocumentObjectModel.Tables.Table;
using PdfSharp.Fonts;
using System.IO;


namespace CredentialEvaluationApp
{
    public partial class CalculatorPage : UserControl
    {

        Student student = new Student();
        TranscriptSection transcriptSection = new TranscriptSection();
        double hsGpa = 0.000;
        double uniGpa = -1;
        double totalHSCredits = 0;
        double totalUNICredits = 0;

        public ObservableCollection<CourseEntry> TranscriptCourses { get; set; }
        public ObservableCollection<string> AvailableGrades { get; set; } = new ObservableCollection<string>();
        ////////////////////////////////////////////////

        private List<TranscriptSection> allTranscripts = new List<TranscriptSection>();
        public int transcriptCount = 1;



        public CalculatorPage()
        {
            InitializeComponent();

            student.Student_Id = 0;

            //Database
            DatabaseHelper.TestConnection();

            //Transcript
            var newTranscript = new TranscriptSection { TranscriptTitle = $"Transcript {transcriptCount}" };
            newTranscript.DeleteButton.Visibility = Visibility.Collapsed;
            allTranscripts.Add(newTranscript);
            TranscriptPanel.Children.Add(newTranscript);

            AppEvents.GradingScaleUpdated += RefreshTranscriptSections;
        }


        private void RefreshTranscriptSections()
        {
            Dispatcher.Invoke(() =>
            {
                foreach (var transcript in allTranscripts)
                {
                    transcript.LoadCountries();
                    transcript.LoadGradingScales();
                }
            });
        }

        public void LoadStudentData(int student_Id)
        {

            CalculatorScrollViewer.ScrollToVerticalOffset(0);

            // At the top:
            student = DatabaseHelper.GetStudentById(student_Id);
            if (student == null)
            {
                MessageBox.Show("Student not found.");
                return;
            }

            //MessageBox.Show($"Transcripts found: {student.Transcripts?.Count ?? 0}");


            hsGpa = student.HSGPA ?? 0.000;
            uniGpa = student.UniGPA ?? -1;

            if (student.UniGPA > -1)
            {
                UniGpaHeaderTextBlock.Text = "University";
                UniGpaTextBlock.Text = "GPA: ";
                UniGpaNumTextBlock.Text = uniGpa.ToString("F3");
                UniTotalCreditsTextBlock.Text = student.TotalUNICredits.ToString();
                UniStackPanel.Margin = new Thickness(50, 0, 0, 10);
            }
            else
            {
                UniGpaHeaderTextBlock.Text = "";
                UniGpaTextBlock.Text = "";
                UniGpaNumTextBlock.Text = "";
                UniTotalCreditsTextBlock.Text = "";
                UniStackPanel.Margin = new Thickness(0, 0, 0, 10);
            }

            firstNameTextBox.Text = student.FirstName;
            lastNameTextBox.Text = student.LastName;
            dateOfBirthPicker.SelectedDate = student.DOB;
            applicationTermTextBox.Text = student.Term;
            GpaTextBlock.Text = hsGpa.ToString("F3");
            TotalCreditsTextBlock.Text = student.TotalHSCredits.ToString("F2");

            // Reset
            transcriptCount = 1;
            allTranscripts.Clear();
            TranscriptPanel.Children.Clear();

            // Load from student.Transcripts
            foreach (var transcript in student.Transcripts)
            {
                var section = new TranscriptSection
                {
                    TranscriptTitle = transcript.TranscriptName,
                    TranscriptID = transcript.Transcript_ID,
                };

                GradingScale gradingScale = DatabaseHelper.GetGradingScaleById(transcript.GradingScale_ID);

                if (gradingScale != null)
                {

                    //Country
                    section.countryComboBox.SelectedItem = transcript.Country;

                    // Grading scale name
                    section.gradingScaleComboBox.SelectedItem = gradingScale.ScaleName;

                    //GPA and TotalCredits
                    section.TranscriptGPATextBlock.Text = transcript.TranscriptGPA.ToString("F3");
                    section.TranscriptCreditsTextBlock.Text = transcript.TranscriptCredits.ToString("F2");

                    // Courses
                    // Clear existing semesters
                    section.Semesters.Clear();

                    // Get courses for this transcript
                    transcript.Courses = DatabaseHelper.GetCoursesByTranscriptId(transcript.Transcript_ID);

                    // Group courses by SemesterName
                    var groupedCourses = transcript.Courses
                        .GroupBy(c => c.SemesterName) // assuming CourseEntry has SemesterName property
                        .OrderBy(g => g.Key); // optional: sort by semester name

                    foreach (var group in groupedCourses)
                    {
                        // Create a new Semester for this group
                        var semester = new Semester { SemesterName = group.Key };

                        // Add all courses for this semester
                        foreach (var course in group)
                        {
                            semester.Courses.Add(course);
                        }

                        // Add semester to the section
                        section.Semesters.Add(semester);
                    }

                    if (transcript.Multiplier != 1)
                    {
                        section.MultiplierTextBox.Text = transcript.Multiplier.ToString();
                    }



                }


                void OnTranscriptDeleted(object s, EventArgs args)
                {
                    transcriptCount--;
                    TranscriptPanel.Children.Remove((UIElement)s);
                    allTranscripts.Remove((TranscriptSection)s);
                    EvenlyWeightSection.Visibility = Visibility.Collapsed;
                    ToggleEvenlyWeightButton.Margin = new Thickness(0, 0, 0, 0);
                }

                section.TranscriptDeleted += OnTranscriptDeleted;
                allTranscripts.Add(section);
                TranscriptPanel.Children.Add(section);

                if (transcriptCount > 1)
                {
                    section.DeleteButton.Visibility = Visibility.Visible;
                }
                else
                {
                    section.DeleteButton.Visibility = Visibility.Collapsed;
                }

                transcriptCount++;
            }


            transcriptCount--;

            if (student.Transcripts.Count == 0)
            {
                var newTranscript = new TranscriptSection { TranscriptTitle = $"Transcript {transcriptCount}" };
                newTranscript.DeleteButton.Visibility = Visibility.Collapsed;
                allTranscripts.Add(newTranscript);
                TranscriptPanel.Children.Add(newTranscript);
            }

            EvenlyWeightSection.Visibility = Visibility.Collapsed;
            ToggleEvenlyWeightButton.Margin = new Thickness(0, 0, 0, 0);

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

        ////////////////////////////////////////////////

        private void AddTranscript_Click(object sender, RoutedEventArgs e)
        {
            transcriptCount++;
            var newTranscript = new TranscriptSection { TranscriptTitle = $"Transcript {transcriptCount}" };

            void OnTranscriptDeleted(object s, EventArgs args)
            {
                transcriptCount--;
                TranscriptPanel.Children.Remove((UIElement)s);
                allTranscripts.Remove((TranscriptSection)s);
                EvenlyWeightSection.Visibility = Visibility.Collapsed;
                ToggleEvenlyWeightButton.Margin = new Thickness(0, 0, 0, 0);
            }

            newTranscript.TranscriptDeleted += OnTranscriptDeleted;

            TranscriptPanel.Children.Add(newTranscript);
            allTranscripts.Add(newTranscript);

            EvenlyWeightSection.Visibility = Visibility.Collapsed;
            ToggleEvenlyWeightButton.Margin = new Thickness(0, 0, 0, 0);
        }

        private void ToggleEvenlyWeightSection(object sender, RoutedEventArgs e)
        {
            if (EvenlyWeightSection.Visibility == Visibility.Visible)
            {
                EvenlyWeightSection.Visibility = Visibility.Collapsed;
                ToggleEvenlyWeightButton.Content = "Evenly Weight Transcripts";
                ToggleEvenlyWeightButton.FontWeight = FontWeights.Regular;
            }
            else
            {
                TranscriptList.ItemsSource = null;
                // Bind the entire allTranscripts list directly
                TranscriptList.ItemsSource = allTranscripts;

                if (TranscriptList.Items.Count < 2)
                {
                    EvenlyWeightButton.Visibility = Visibility.Collapsed;
                    EvenlyWeightErrorTextBlock.Visibility = Visibility.Visible;
                }
                else
                {
                    EvenlyWeightButton.Visibility = Visibility.Visible;
                    EvenlyWeightErrorTextBlock.Visibility = Visibility.Collapsed;
                }

                EvenlyWeightSection.Visibility = Visibility.Visible;
                ToggleEvenlyWeightButton.Content = "Evenly Weight Transcripts";
                ToggleEvenlyWeightButton.FontWeight = FontWeights.Bold;
            }

        }


        private void UpdateTranscriptEnabledStates()
        {
            // Find all selected transcripts
            var selectedTranscripts = allTranscripts.Where(t => t.IsSelected).ToList();

            if (selectedTranscripts.Count == 0)
            {
                // Enable all when none selected
                foreach (var transcript in allTranscripts)
                {
                    transcript.IsEnabled = true;
                }

                // Show the button and hide the error text
                EvenlyWeightButton.Visibility = Visibility.Visible;
                EvenlyWeightErrorTextBlock.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Get the grading scale level of the first selected transcript
                string selectedLevel = selectedTranscripts[0].SelectedGradingScaleObject?.Level;

                // Enable only transcripts that match the selected level or are already selected
                foreach (var transcript in allTranscripts)
                {
                    transcript.IsEnabled = (transcript.SelectedGradingScaleObject?.Level == selectedLevel) || transcript.IsSelected;
                }

                // If exactly one transcript is selected, hide button and show error text
                if (selectedTranscripts.Count == 1)
                {
                    EvenlyWeightButton.Visibility = Visibility.Collapsed;
                    EvenlyWeightErrorTextBlock.Visibility = Visibility.Visible;
                }
                else
                {
                    // Otherwise, show button and hide error text
                    EvenlyWeightButton.Visibility = Visibility.Visible;
                    EvenlyWeightErrorTextBlock.Visibility = Visibility.Collapsed;
                }
            }
        }


        private void TranscriptCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateTranscriptEnabledStates();
        }

        private void TranscriptCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateTranscriptEnabledStates();
        }





        private void EvenlyWeightButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedTranscripts = allTranscripts.Where(t => t.IsSelected).ToList();


            // Step 1: Calculate each transcript's total current credit hours
            var totals = new List<double>();
            foreach (var transcript in selectedTranscripts)
            {
                double transcriptTotal = transcript.Semesters
                                                   .SelectMany(s => s.Courses)
                                                   .Sum(c => c.CreditHours);

                totals.Add(transcriptTotal);
            }


            // Step 2: Compute the average total
            double averageTotal = totals.Average();

            // Step 3: Adjust each transcript's course hours proportionally
            for (int i = 0; i < selectedTranscripts.Count; i++)
            {
                var transcript = selectedTranscripts[i];
                double originalTotal = totals[i];

                if (originalTotal == 0)
                    continue; // avoid division by zero

                double scaleFactor = averageTotal / originalTotal;
                MessageBox.Show($"Transcript {transcript.TranscriptTitle} - Scale Factor: {scaleFactor:F2}");

                // Scale each course's credit hours
                foreach (var semester in transcript.Semesters)
                {
                    foreach (var course in semester.Courses)
                    {
                        course.CreditHours = Math.Round(course.CreditHours * scaleFactor, 2);
                    }
                }

            }

            MessageBox.Show("Selected transcripts have been evenly weighted.");
        }



        //////////////////////////////////////////////////////////////////////////////////////

        private void CalculateButton_Click(object sender, RoutedEventArgs e)
        {

            double highSchoolPoints = 0, highSchoolCredits = 0;
            bool uni = false;
            double universityPoints = 0, universityCredits = 0;
            double multiplier = 1;
            double HS_USConvertedCredits = 0;
            double Uni_USConvertedCredits = 0;

            double TranscriptPoints = 0;
            double TranscriptCredits = 0;
            totalHSCredits = 0;
            totalUNICredits = 0;


            System.Diagnostics.Debug.WriteLine($"Total Transcripts: {allTranscripts.Count}");

            foreach (var transcript in allTranscripts)
            {
                TranscriptCredits = 0;
                TranscriptPoints = 0;

                string selectedScale = transcript.SelectedGradingScale;
                var gradeMap = DatabaseHelper.GetGradeMappingForScale(selectedScale);
                var gradingScale = DatabaseHelper.GetGradingScaleByName(selectedScale);
                multiplier = transcript.MultiplierValue;

                var courses = transcript.GetTranscriptCourses();
                foreach (var course in courses)
                {

                    if (gradeMap.TryGetValue(course.Grade, out double gpa))
                    {
                        if (gradingScale.Level == "High School")
                        {
                            highSchoolPoints += gpa * (course.CreditHours * multiplier);

                            highSchoolCredits += course.CreditHours;
                            HS_USConvertedCredits += course.CreditHours * multiplier;
                            totalHSCredits += course.CreditHours * multiplier;
                        }
                        else if (gradingScale.Level == "University")
                        {
                            uni = true;
                            universityPoints += gpa * course.CreditHours;

                            universityCredits += course.CreditHours;
                            Uni_USConvertedCredits += course.CreditHours * multiplier;
                            totalUNICredits += course.CreditHours * multiplier;
                        }

                        TranscriptPoints += gpa * course.CreditHours * multiplier;
                        TranscriptCredits += course.CreditHours * multiplier;

                        course.USConvertedGrade = US_EquivalentService.ConversionToLetterGrade(gpa);

                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Missing mapping for grade '{course.Grade}' in scale '{selectedScale}'", "Warning");
                    }
                }

                transcript.totalCredits = TranscriptCredits;
                transcript.TranscriptCreditsTextBlock.Text = transcript.totalCredits.ToString("F2");

                transcript.gpa = TranscriptCredits > 0 ? TranscriptPoints / TranscriptCredits : 0;
                transcript.TranscriptGPATextBlock.Text = transcript.gpa.ToString("F3");

            }

            hsGpa = HS_USConvertedCredits > 0 ? highSchoolPoints / HS_USConvertedCredits : 0;
            GpaTextBlock.Text = hsGpa.ToString("F3");
            TotalCreditsTextBlock.Text = totalHSCredits.ToString("F2");
            System.Diagnostics.Debug.WriteLine($"Total GPA: {hsGpa:F2}", "GPA");

            if (uni)
            {
                uniGpa = Uni_USConvertedCredits > 0 ? universityPoints / Uni_USConvertedCredits : 0;
                UniGpaHeaderTextBlock.Text = "University";
                UniGpaTextBlock.Text = "GPA: ";
                UniGpaNumTextBlock.Text = uniGpa.ToString("F3");
                UniCreditsTextBlock.Text = "Total Credits: ";
                UniTotalCreditsTextBlock.Text = totalUNICredits.ToString("F2");
                UniStackPanel.Margin = new Thickness(50, 0, 0, 10);
                UniExportGrid.Visibility = Visibility.Visible;

            }
            else
            {
                uniGpa = -1;
                UniGpaHeaderTextBlock.Text = "";
                UniGpaTextBlock.Text = "";
                UniGpaNumTextBlock.Text = "";
                UniCreditsTextBlock.Text = "";
                UniTotalCreditsTextBlock.Text = "";
                UniStackPanel.Margin = new Thickness(0, 0, 0, 10);
                UniExportGrid.Visibility = Visibility.Collapsed;
            }


            //transcriptSection.transcriptDataGrid.Items.Refresh();       

        }



        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string firstName = firstNameTextBox.Text.Trim();
            string lastName = lastNameTextBox.Text.Trim();
            DateTime? dob = dateOfBirthPicker.SelectedDate;
            string appTerm = applicationTermTextBox.Text.Trim();

            int studentId;
            if (student.Student_Id == 0)
            {
                // INSERT
                studentId = DatabaseHelper.SaveStudentAndReturnId(firstName, lastName, dob, appTerm, hsGpa, uniGpa, totalHSCredits, totalUNICredits);
                student.Student_Id = studentId; // store for later use
            }
            else
            {
                // UPDATE
                studentId = student.Student_Id;
                DatabaseHelper.UpdateStudent(studentId, firstName, lastName, dob, appTerm, hsGpa, uniGpa, totalHSCredits, totalUNICredits);
            }


            if (studentId > 0)
            {
                foreach (var transcript in allTranscripts)
                {
                    string selectedScale = transcript.SelectedGradingScale;
                    var gradingScale = DatabaseHelper.GetGradingScaleByName(selectedScale);
                    int? gradingScaleId = gradingScale?.GradingScale_ID;
                    string country = transcript.countryComboBox.Text ?? string.Empty;

                    int transcriptId;

                    if (transcript.TranscriptID == 0)
                    {
                        // INSERT transcript
                        transcriptId = DatabaseHelper.SaveTranscript(studentId, gradingScaleId, country, transcript.MultiplierValue, transcript.TranscriptTitleTextBox.Text, transcript.gpa, transcript.totalCredits);
                        transcript.TranscriptID = transcriptId;
                    }
                    else
                    {
                        // UPDATE transcript
                        transcriptId = transcript.TranscriptID;
                        DatabaseHelper.UpdateTranscript(transcriptId, gradingScaleId, country, transcript.MultiplierValue, transcript.TranscriptTitleTextBox.Text, transcript.gpa, transcript.totalCredits);
                    }

                    var semesters = transcript.GetSemesters();
                    foreach (var semester in semesters)
                    {
                        foreach (var course in semester.Courses)
                        {
                            if (course.Course_ID == 0)
                            {
                                DatabaseHelper.SaveCourse(transcriptId, course.CourseName, course.Grade, course.CreditHours, (course.CreditHours * transcript.MultiplierValue), course.USConvertedGrade, semester.SemesterName);
                            }
                            else
                            {
                                DatabaseHelper.UpdateCourse(course.Course_ID, course.CourseName, course.Grade, course.CreditHours, (course.CreditHours * transcript.MultiplierValue), course.USConvertedGrade, semester.SemesterName);
                            }
                        }
                    }

                }

                MessageBox.Show("Student and transcript data saved successfully.");
                ((MainWindow)Application.Current.MainWindow).SearchPageInstance.LoadStudents();
            }
            else
            {
                MessageBox.Show("Failed to save student.");
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            firstNameTextBox.Text = string.Empty;
            lastNameTextBox.Text = string.Empty;
            dateOfBirthPicker.SelectedDate = null;
            applicationTermTextBox.Text = string.Empty;

            GpaTextBlock.Text = "0.00";
            TotalCreditsTextBlock.Text = "0";
            uniGpa = -1;
            UniGpaHeaderTextBlock.Text = "";
            UniGpaTextBlock.Text = "";
            UniGpaNumTextBlock.Text = "";
            UniCreditsTextBlock.Text = "";
            UniTotalCreditsTextBlock.Text = "";
            UniStackPanel.Margin = new Thickness(0, 0, 0, 10);
            UniExportGrid.Visibility = Visibility.Collapsed;
            EvenlyWeightSection.Visibility = Visibility.Collapsed;
            ToggleEvenlyWeightButton.Content = "Evenly Weight Transcripts";
            ToggleEvenlyWeightButton.FontWeight = FontWeights.Regular;

            TranscriptPanel.Children.Clear();
            allTranscripts.Clear();
            transcriptCount = 1;
            var newTranscript = new TranscriptSection { TranscriptTitle = $"Transcript {transcriptCount}" };
            newTranscript.DeleteButton.Visibility = Visibility.Collapsed;
            allTranscripts.Add(newTranscript);
            TranscriptPanel.Children.Add(newTranscript);
            EvenlyWeightSection.Visibility = Visibility.Collapsed;

            student.Student_Id = 0;

        }

        private void DeleteStudent_Click(object sender, RoutedEventArgs e)
        {
            if (student.Student_Id == 0)
            {
                MessageBox.Show("No student found to delete.");
                return;
            }

            var result = MessageBox.Show(
                "Are you sure you want to delete this student and all associated data (transcripts, courses)?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    DatabaseHelper.DeleteStudent(student.Student_Id);

                    MessageBox.Show("Student and all associated data deleted successfully.");

                    // Clear fields on the page
                    firstNameTextBox.Text = "";
                    lastNameTextBox.Text = "";
                    dateOfBirthPicker.SelectedDate = null;
                    applicationTermTextBox.Text = "";
                    allTranscripts.Clear();
                    student = new Student(); // reset student object

                    // Refresh student list in SearchPage
                    ((MainWindow)Application.Current.MainWindow).SearchPageInstance.LoadStudents();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting student: " + ex.Message);
                }
            }
        }


        //////////////////////////////////////////////////////////////////////////////////////

        private void ExportToExcell_Click(string ExportLevel)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = "CalculatorExport.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var workbook = new XLWorkbook())
                {
                    var ws = workbook.Worksheets.Add("Calculator Data");

                    // Student info
                    ws.Cell(1, 1).Value = "Name:";
                    ws.Cell(1, 2).Value = (student.FirstName + " " + student.LastName);
                    ws.Cell(2, 1).Value = "DOB:";
                    ws.Cell(2, 2).Value = student.DOB;
                    ws.Cell(3, 1).Value = "Application Term:";
                    ws.Cell(3, 2).Value = student.Term;
                    ws.Range(1, 1, 3, 1).Style.Font.Bold = true;

                    int row = 4;
                    int num = 0;

                    // ✅ Filter transcripts with Level = "High School"
                    var highSchoolTranscripts = allTranscripts
                        .Where(t => t.SelectedGradingScaleObject.Level != null && t.SelectedGradingScaleObject.Level.Equals(ExportLevel, StringComparison.OrdinalIgnoreCase));

                    foreach (var transcript in highSchoolTranscripts)
                    {
                        row++;
                        num++;
                        ws.Cell(row, 1).Value = "Transcript " + num + ": ";
                        if (transcript.TranscriptTitleTextBox.Text != ("Transcript " + num))
                        {
                            ws.Cell(row, 2).Value = transcript.TranscriptTitleTextBox.Text;
                        }
                        ws.Cell(row++, 1).Style.Font.Bold = true;

                        ws.Cell(row, 1).Value = "Country: ";
                        ws.Cell(row, 2).Value = (transcript.countryComboBox.SelectedItem?.ToString() ?? "");
                        ws.Cell(row++, 1).Style.Font.Bold = true;

                        ws.Cell(row, 1).Value = "Grading Scale: ";
                        ws.Cell(row, 2).Value = (transcript.gradingScaleComboBox.SelectedItem?.ToString() ?? "");
                        ws.Cell(row++, 1).Style.Font.Bold = true;

                        ws.Cell(row, 1).Value = "Course Name";
                        ws.Cell(row, 2).Value = "Local Grade";
                        ws.Cell(row, 3).Value = "US Grade";
                        ws.Cell(row, 4).Value = "Credit Hours";
                        ws.Cell(row, 5).Value = "US Credit Hours";
                        ws.Range(row, 1, row, 5).Style.Font.Bold = true;
                        ws.Cell(row++, 1).Style.Fill.BackgroundColor = XLColor.LightGray;

                        foreach (var semester in transcript.Semesters)
                        {
                            // Semester header
                            row++;
                            ws.Cell(row, 1).Value = semester.SemesterName;
                            ws.Range(row, 1, row, 5).Merge().Style.Font.Bold = true;

                            foreach (var course in semester.Courses)
                            {
                                row++;
                                ws.Cell(row, 1).Value = course.CourseName;
                                ws.Cell(row, 2).Value = course.Grade;
                                ws.Cell(row, 3).Value = course.USConvertedGrade;
                                ws.Cell(row, 4).Value = course.CreditHours;
                                ws.Cell(row, 5).Value = course.USCreditHours;
                            }
                        }

                        row++;
                        ws.Cell(row, 1).Value = "Multiplier: ";
                        ws.Cell(row, 2).Value = transcript.MultiplierTextBox.Text.Length > 0
                            ? transcript.MultiplierTextBox.Text
                            : "1";
                        ws.Cell(row++, 1).Style.Font.Bold = true;
                    }

                    row++;
                    ws.Cell(row, 1).Value = "High School GPA: ";
                    ws.Cell(row, 2).Value = hsGpa.ToString("F3");
                    ws.Cell(row++, 1).Style.Font.Bold = true;

                    if (uniGpa > -1)
                    {
                        ws.Cell(row, 1).Value = "University GPA: ";
                        ws.Cell(row, 2).Value = uniGpa.ToString("F3");
                        ws.Cell(row++, 1).Style.Font.Bold = true;
                    }

                    workbook.SaveAs(saveFileDialog.FileName);
                }
            }
        }

        public class CustomFontResolver : IFontResolver
        {
            public string DefaultFontName => "Arial";

            public byte[] GetFont(string faceName)
            {
                // Load Arial from Windows Fonts folder
                string fontPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
                    "arial.ttf"
                );
                return File.ReadAllBytes(fontPath);
            }

            public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
            {
                // Normalize
                string name = familyName.Replace(" ", "").ToLowerInvariant();

                if (name.Contains("courier"))
                {
                    return new FontResolverInfo("Arial");
                }

                // Force everything to Arial if needed
                return new FontResolverInfo("Arial");
            }
        }

        private void ExportToPDF_Click(string ExportLevel)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = "CalculatorExport.pdf"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                // Create PDF document
                GlobalFontSettings.FontResolver = new CustomFontResolver();

                Document document = new Document();
                document.Styles["Normal"].Font.Name = "Arial";
                var section = document.AddSection();

                // Title
                var title = section.AddParagraph("Calculator Data");
                title.Format.Font.Size = 16;
                title.Format.Font.Bold = true;
                title.Format.SpaceAfter = "0.5cm";

                // Student Info Table
                var studentTable = section.AddTable();
                studentTable.Borders.Width = 0.5;
                studentTable.AddColumn(MigraDoc.DocumentObjectModel.Unit.FromCentimeter(5));
                studentTable.AddColumn(MigraDoc.DocumentObjectModel.Unit.FromCentimeter(10));

                AddRow(studentTable, "Name:", $"{student.FirstName} {student.LastName}", true);
                AddRow(studentTable, "DOB:", student.DOB.ToString(), true);
                AddRow(studentTable, "Application Term:", student.Term, true);

                int num = 0;

                // ✅ Filter transcripts for High School
                var highSchoolTranscripts = allTranscripts
                    .Where(t => t.SelectedGradingScaleObject.Level != null && t.SelectedGradingScaleObject.Level.Equals(ExportLevel, StringComparison.OrdinalIgnoreCase));

                foreach (var transcript in highSchoolTranscripts)
                {
                    num++;
                    section.AddParagraph(); // space between transcripts

                    var transcriptTitle = section.AddParagraph($"Transcript {num}:");
                    transcriptTitle.Format.Font.Bold = true;

                    if (transcript.TranscriptTitleTextBox.Text != $"Transcript {num}")
                    {
                        section.AddParagraph(transcript.TranscriptTitleTextBox.Text);
                    }

                    // Transcript info table
                    var tTable = section.AddTable();
                    tTable.Borders.Width = 0.5;
                    tTable.AddColumn(MigraDoc.DocumentObjectModel.Unit.FromCentimeter(5));
                    tTable.AddColumn(MigraDoc.DocumentObjectModel.Unit.FromCentimeter(10));

                    AddRow(tTable, "Country:", transcript.countryComboBox.SelectedItem?.ToString() ?? "", true);
                    AddRow(tTable, "Grading Scale:", transcript.gradingScaleComboBox.SelectedItem?.ToString() ?? "", true);

                    // Course table
                    var courseTable = section.AddTable();
                    courseTable.Borders.Width = 0.5;
                    courseTable.AddColumn(MigraDoc.DocumentObjectModel.Unit.FromCentimeter(4));
                    courseTable.AddColumn(MigraDoc.DocumentObjectModel.Unit.FromCentimeter(3));
                    courseTable.AddColumn(MigraDoc.DocumentObjectModel.Unit.FromCentimeter(3));
                    courseTable.AddColumn(MigraDoc.DocumentObjectModel.Unit.FromCentimeter(3));
                    courseTable.AddColumn(MigraDoc.DocumentObjectModel.Unit.FromCentimeter(3));

                    var headerRow = courseTable.AddRow();
                    headerRow.Shading.Color = MigraDoc.DocumentObjectModel.Colors.LightGray;
                    headerRow.HeadingFormat = true;
                    headerRow.Cells[0].AddParagraph("Course Name").Format.Font.Bold = true;
                    headerRow.Cells[1].AddParagraph("Local Grade").Format.Font.Bold = true;
                    headerRow.Cells[2].AddParagraph("US Grade").Format.Font.Bold = true;
                    headerRow.Cells[3].AddParagraph("Credit Hours").Format.Font.Bold = true;
                    headerRow.Cells[4].AddParagraph("US Credit Hours").Format.Font.Bold = true;

                    foreach (var semester in transcript.Semesters)
                    {
                        // Semester header
                        var semesterHeaderRow = courseTable.AddRow();
                        semesterHeaderRow.Cells[0].MergeRight = 4;
                        semesterHeaderRow.Cells[0].AddParagraph(semester.SemesterName);

                        foreach (var course in semester.Courses)
                        {
                            var row = courseTable.AddRow();
                            row.Cells[0].AddParagraph(course.CourseName);
                            row.Cells[1].AddParagraph(course.Grade);
                            row.Cells[2].AddParagraph(course.USConvertedGrade);
                            row.Cells[3].AddParagraph(course.CreditHours.ToString());
                            row.Cells[4].AddParagraph(course.USCreditHours.ToString());
                        }
                    }

                    // Multiplier row
                    AddRow(tTable, "Multiplier:",
                        transcript.MultiplierTextBox.Text.Length > 0 ? transcript.MultiplierTextBox.Text : "1", true);
                }

                section.AddParagraph();
                var gpaTable = section.AddTable();
                gpaTable.Borders.Width = 0.5;
                gpaTable.AddColumn(MigraDoc.DocumentObjectModel.Unit.FromCentimeter(5));
                gpaTable.AddColumn(MigraDoc.DocumentObjectModel.Unit.FromCentimeter(10));

                AddRow(gpaTable, "High School GPA:", hsGpa.ToString("F3"), true);
                if (uniGpa > -1)
                {
                    AddRow(gpaTable, "University GPA:", uniGpa.ToString("F3"), true);
                }

                // Render and save
                var renderer = new PdfDocumentRenderer(true)
                {
                    Document = document
                };

                renderer.RenderDocument();
                renderer.Save(saveFileDialog.FileName);
            }
        }


        // Helper function to add table rows
        private void AddRow(MigraTable table, string label, string value, bool boldLabel = false)
        {
            var row = table.AddRow();
            var labelPara = row.Cells[0].AddParagraph(label);
            labelPara.Format.Font.Bold = boldLabel;
            row.Cells[1].AddParagraph(value ?? "");
        }

        private void ExportHSToExcell_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcell_Click("High School");
        }

        private void ExportHSToPDF_Click(object sender, RoutedEventArgs e)
        {
            ExportToPDF_Click("High School");
        }

        private void ExportUniToExcell_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcell_Click("University");
        }

        private void ExportUniToPDF_Click(object sender, RoutedEventArgs e)
        {
            ExportToPDF_Click("University");
        }
    }
}
