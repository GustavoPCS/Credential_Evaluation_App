using CredentialEvaluationApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace CredentialEvaluationApp.Helpers
{
    public static class DatabaseHelper
    {
        ////////////////////////////////////////////////
        private static string connectionString;
        private static bool databaseMissingPopupShown = false; // To prevent multiple popups

        static DatabaseHelper()
        {
            // Load saved path from settings
            string savedPath = Properties.Settings.Default.DatabasePath;

            if (!string.IsNullOrWhiteSpace(savedPath) && File.Exists(savedPath))
            {
                SetDatabasePath(savedPath);
            }
            else
            {
                // Set a default path in Downloads folder
                string downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                string defaultDbPath = Path.Combine(downloadsFolder, "CredentialDB.accdb");

                if (File.Exists(defaultDbPath))
                {
                    SetDatabasePath(defaultDbPath);
                }
                else
                {
                    // Database not found
                    connectionString = null; // no valid path
                    ShowDatabaseMissingPopup();
                }
            }
        }

        public static void SetDatabasePath(string path)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show("The selected database file does not exist.", "Invalid Database", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            connectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={path}";

            // Save path to user settings
            Properties.Settings.Default.DatabasePath = path;
            Properties.Settings.Default.Save();
        }

        public static bool TestConnection()
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                ShowDatabaseMissingPopup();
                return false;
            }

            try
            {
                using (OleDbConnection conn = new OleDbConnection(connectionString))
                {
                    conn.Open();
                    System.Diagnostics.Debug.WriteLine("Database connection succeeded.");
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    mainWindow?.SearchPageInstance.LoadStudents();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database connection failed: " + ex.Message);
                MessageBox.Show("Failed to connect to the database. Please check the database path in Settings.",
                    "Database Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private static void ShowDatabaseMissingPopup()
        {
            if (!databaseMissingPopupShown)
            {
                databaseMissingPopupShown = true;
                MessageBox.Show("Database file not found! Please select the database location in the Settings page.",
                    "Database Missing", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        ////////////////////////////////////////////////



        ////////////////////////////////////////////////

        public static List<GradingScale> GetGradingScalesByCountry(string country)
        {
            List<GradingScale> gradingScales = new List<GradingScale>();

            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                string query = "SELECT GradingScale_ID, Country, ScaleName, [Level] FROM GradingScales WHERE Country = ? OR Country = 'All'";
                using (OleDbCommand command = new OleDbCommand(query, connection))
                {
                    command.Parameters.AddWithValue("?", country);
                    connection.Open();

                    using (OleDbDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            gradingScales.Add(new GradingScale
                            {
                                GradingScale_ID = reader.GetInt32(0),
                                Country = reader.GetString(1),
                                ScaleName = reader.GetString(2),
                                Level = reader.GetString(3)
                            });
                        }
                    }
                }
            }

            return gradingScales;
        }
        public static List<GradingScale> GetAllGradingScales()
        {
            List<GradingScale> gradingScales = new List<GradingScale>();

            // Check if connection string is missing
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return gradingScales; // return empty list
            }

            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                string query = "SELECT GradingScale_ID, Country, ScaleName, [Level] FROM GradingScales";
                using (OleDbCommand command = new OleDbCommand(query, connection))
                {
                    connection.Open();

                    using (OleDbDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            gradingScales.Add(new GradingScale
                            {
                                GradingScale_ID = reader.GetInt32(0),
                                Country = reader.GetString(1),
                                ScaleName = reader.GetString(2),
                                Level = reader.GetString(3)
                            });
                        }
                    }
                }
            }

            return gradingScales;
        }
        public static List<string> GetCountriesByGradingScale(string gradingScaleName)
        {
            List<string> countries = new List<string>();

            using (OleDbConnection conn = new OleDbConnection(connectionString))
            {
                conn.Open();

                string query = "SELECT DISTINCT Country FROM GradingScales WHERE ScaleName = @gradingScaleName";

                using (OleDbCommand cmd = new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@gradingScaleName", gradingScaleName);

                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string country = reader["Country"].ToString();
                            if (country.Equals("All", StringComparison.OrdinalIgnoreCase))
                            {
                                // If the grading scale is universal, return all countries except "All"
                                return GetAllCountries();
                            }
                            else
                            {
                                countries.Add(country);
                            }
                        }
                    }
                }
            }

            return countries;
        }
        public static List<string> GetAllCountries()
        {
            List<string> countries = new List<string>();

            // Check if connection string is missing
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return countries; // return empty list
            }

            string query = "SELECT DISTINCT Country FROM GradingScales WHERE Country <> 'All' ORDER BY Country";

            using (OleDbConnection connection = new OleDbConnection(connectionString))
            using (OleDbCommand command = new OleDbCommand(query, connection))
            {
                try
                {
                    connection.Open();
                    using (OleDbDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            countries.Add(reader["Country"].ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error fetching countries: " + ex.Message);
                }
            }

            return countries;
        }


        ////////////////////////////////////////////////

        public static int GetGradingScaleIdByName(string scaleName)
        {
            int gradingScaleId = -1; // or 0 or throw exception if not found

            string query = "SELECT GradingScale_ID FROM GradingScales WHERE ScaleName = ?";

            try
            {
                using (OleDbConnection connection = new OleDbConnection(connectionString))
                using (OleDbCommand command = new OleDbCommand(query, connection))
                {
                    command.Parameters.AddWithValue("?", scaleName);

                    connection.Open();
                    var result = command.ExecuteScalar();
                    if (result != null && int.TryParse(result.ToString(), out int id))
                    {
                        gradingScaleId = id;
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle/log exception
                Console.WriteLine("Error getting grading scale ID: " + ex.Message);
            }

            return gradingScaleId;
        }

        public static string GetGradingScaleNameById(int gradingScaleId)
        {
            string scaleName = string.Empty;
            string query = "SELECT ScaleName FROM GradingScales WHERE GradingScale_ID = ?";

            try
            {
                using (OleDbConnection connection = new OleDbConnection(connectionString))
                using (OleDbCommand command = new OleDbCommand(query, connection))
                {
                    command.Parameters.AddWithValue("?", gradingScaleId);

                    connection.Open();
                    var result = command.ExecuteScalar();
                    if (result != null)
                    {
                        scaleName = result.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle or log the error appropriately
                Console.WriteLine("Error getting grading scale name: " + ex.Message);
            }

            return scaleName;
        }

        public static List<string> GetLocalGradesByGradingScale(int gradingScaleId)
        {
            var grades = new List<string>();

            string query = "SELECT DISTINCT LocalGrade FROM GradingScaleMappings WHERE GradingScale_ID = ?";

            try
            {
                using (OleDbConnection connection = new OleDbConnection(connectionString))
                using (OleDbCommand command = new OleDbCommand(query, connection))
                {
                    command.Parameters.AddWithValue("?", gradingScaleId);

                    connection.Open();
                    using (OleDbDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            grades.Add(reader["LocalGrade"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // You can log or handle exceptions here
                Console.WriteLine("Error fetching grades: " + ex.Message);
            }

            return grades;
        }

        public static List<GradingScaleMapping> GetMappingsByScaleId(int gradingScaleId)
        {
            List<GradingScaleMapping> mappings = new List<GradingScaleMapping>();

            using (var conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                var cmd = new OleDbCommand("SELECT * FROM GradingScaleMappings WHERE GradingScale_ID = @id", conn);
                cmd.Parameters.AddWithValue("@id", gradingScaleId);

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    mappings.Add(new GradingScaleMapping
                    {
                        GradingScaleMappings_ID = Convert.ToInt32(reader["GradingScaleMappings_ID"]),
                        GradingScale_ID = Convert.ToInt32(reader["GradingScale_ID"]),
                        LocalGrade = reader["LocalGrade"].ToString(),
                        USEquivalent = reader["USEquivalent"].ToString(),
                        USLetter = reader["USLetter"].ToString()
                    });
                }
            }

            return mappings;
        }

        ////////////////////////////////////////////////

        public static Dictionary<string, double> GetGradeMappingForScale(string gradingScaleName)
        {
            var gradeMap = new Dictionary<string, double>();

            using (var connection = new OleDbConnection(connectionString))
            {
                connection.Open();

                string query = @"
                                SELECT gsm.LocalGrade, gsm.USEquivalent
                                FROM GradingScaleMappings gsm
                                INNER JOIN GradingScales gs ON gsm.GradingScale_ID = gs.GradingScale_ID
                                WHERE gs.ScaleName = ? ";

                using (var command = new OleDbCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ScaleName", gradingScaleName);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string localGrade = reader["LocalGrade"].ToString();
                            double usGradePoint = Convert.ToDouble(reader["USEquivalent"]);
                            gradeMap[localGrade] = usGradePoint;
                        }
                    }
                }
            }

            return gradeMap;
        }
        public static GradingScale GetGradingScaleByName(string scaleName)
        {

            if (string.IsNullOrWhiteSpace(scaleName))
            {
                return null;
            }

            using (OleDbConnection conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT TOP 1 * FROM GradingScales WHERE ScaleName = ?";
                using (OleDbCommand cmd = new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("?", scaleName);

                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new GradingScale
                            {
                                GradingScale_ID = reader.GetInt32(0),
                                Country = reader.GetString(1),
                                ScaleName = reader.GetString(2),
                                Level = reader.GetString(3)
                            };
                        }
                    }
                }
            }

            return null;
        }

        ////////////////////////////////////////////////

        public static int InsertGradingScale(GradingScale scale)
        {
            using (var conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                var cmd = new OleDbCommand("INSERT INTO GradingScales (Country, ScaleName, [Level]) VALUES (?, ?, ?)", conn);
                cmd.Parameters.AddWithValue("?", scale.Country);
                cmd.Parameters.AddWithValue("?", scale.ScaleName);
                cmd.Parameters.AddWithValue("?", scale.Level);
                cmd.ExecuteNonQuery();

                // Get the newly inserted ID
                cmd = new OleDbCommand("SELECT @@IDENTITY", conn);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static void UpdateGradingScale(GradingScale scale)
        {
            using (var conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                var cmd = new OleDbCommand("UPDATE GradingScales SET Country = ?, ScaleName = ?, [Level] = ? WHERE GradingScale_ID = ?", conn);
                cmd.Parameters.AddWithValue("?", scale.Country);
                cmd.Parameters.AddWithValue("?", scale.ScaleName);
                cmd.Parameters.AddWithValue("?", scale.Level);
                cmd.Parameters.AddWithValue("?", scale.GradingScale_ID);
                cmd.ExecuteNonQuery();
            }
        }



        public static void DeleteMappingsByScaleId(int scaleId)
        {
            using (var conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                var cmd = new OleDbCommand("DELETE FROM GradingScaleMappings WHERE GradingScale_ID = ?", conn);
                cmd.Parameters.AddWithValue("?", scaleId);
                cmd.ExecuteNonQuery();
            }
        }

        public static void InsertGradingScaleMapping(GradingScaleMapping mapping)
        {
            using (var conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                var cmd = new OleDbCommand("INSERT INTO GradingScaleMappings (GradingScale_ID, LocalGrade, USEquivalent, USLetter) VALUES (?, ?, ?, ?)", conn);
                cmd.Parameters.AddWithValue("?", mapping.GradingScale_ID);
                cmd.Parameters.AddWithValue("?", mapping.LocalGrade);
                cmd.Parameters.AddWithValue("?", mapping.USEquivalent);
                cmd.Parameters.AddWithValue("?", mapping.USLetter);
                cmd.ExecuteNonQuery();
            }
        }

        public static bool DeleteGradingScale(int gradingScaleId)
        {
            try
            {
                using (var conn = new OleDbConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new OleDbCommand("DELETE FROM GradingScaleMappings WHERE GradingScale_ID = @id", conn);
                    cmd.Parameters.AddWithValue("@id", gradingScaleId);
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "DELETE FROM GradingScales WHERE GradingScale_ID = @id";
                    cmd.ExecuteNonQuery();
                }
                return true;
            }
            catch (Exception ex)
            {
                // Log or handle exception
                return false;
            }
        }


        ////////////////////////////////////////////////


        public static int SaveStudentAndReturnId(string firstName, string lastName, DateTime? dob, string term, double? hsGpa, double? uniGpa, double? totalHSCredits, double? totalUNICredits)
        {
            int studentId = -1;

            using (var conn = new OleDbConnection(connectionString))
            {
                conn.Open();

                var cmd = new OleDbCommand(
                    "INSERT INTO Students (FirstName, LastName, DOB, Term, HS_GPA, Uni_GPA, TotalHSCredits, TotalUNICredits) VALUES (?, ?, ?, ?, ?, ?, ?, ?)", conn);

                cmd.Parameters.AddWithValue("?", firstName);
                cmd.Parameters.AddWithValue("?", lastName);
                cmd.Parameters.AddWithValue("?", dob ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("?", string.IsNullOrEmpty(term) ? (object)DBNull.Value : term);
                cmd.Parameters.AddWithValue("?", hsGpa ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("?", uniGpa ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("?", totalHSCredits ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("?", totalUNICredits ?? (object)DBNull.Value);

                cmd.ExecuteNonQuery();

                // Get last inserted ID
                cmd = new OleDbCommand("SELECT @@IDENTITY", conn);
                studentId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            return studentId;
        }


        public static void UpdateStudent(int studentId, string firstName, string lastName, DateTime? dob, string term, double? hsGpa, double? uniGpa, double? totalHSCredits, double? totalUNICredits)
        {
            using (var conn = new OleDbConnection(connectionString))
            {
                conn.Open();

                var cmd = new OleDbCommand(
                    "UPDATE Students SET FirstName = ?, LastName = ?, DOB = ?, Term = ?, HS_GPA = ?, Uni_GPA = ?, TotalHSCredits = ?, TotalUNICredits = ? WHERE Student_ID = ?", conn);

                cmd.Parameters.AddWithValue("?", firstName);
                cmd.Parameters.AddWithValue("?", lastName);
                cmd.Parameters.AddWithValue("?", dob ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("?", string.IsNullOrEmpty(term) ? (object)DBNull.Value : term);
                cmd.Parameters.AddWithValue("?", hsGpa ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("?", uniGpa ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("?", totalHSCredits ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("?", totalUNICredits ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("?", studentId);

                cmd.ExecuteNonQuery();
            }
        }





        public static int SaveTranscript(int studentId, int? gradingScaleId, string country, double? multiplier, string transcriptName, double? transcriptGPA, double? transcriptCredits)
        {
            int transcriptId = -1;

            using (var conn = new OleDbConnection(connectionString))
            {
                conn.Open();

                var cmd = new OleDbCommand(
                    "INSERT INTO StudentTranscripts (Student_ID, GradingScale_ID, Country, Multiplier, TranscriptName, TranscriptGPA, TranscriptCredits) VALUES (?, ?, ?, ?, ?, ?, ?)", conn);
                cmd.Parameters.AddWithValue("?", studentId);
                cmd.Parameters.AddWithValue("?", gradingScaleId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("?", string.IsNullOrEmpty(country) ? (object)DBNull.Value : country);
                cmd.Parameters.AddWithValue("?", multiplier ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("?", string.IsNullOrEmpty(transcriptName) ? (object)DBNull.Value : transcriptName);
                cmd.Parameters.AddWithValue("?", transcriptGPA ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("?", transcriptCredits ?? (object)DBNull.Value);
                cmd.ExecuteNonQuery();

                // Get last inserted ID
                cmd = new OleDbCommand("SELECT @@IDENTITY", conn);
                transcriptId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            return transcriptId;
        }

        public static void UpdateTranscript(int transcriptId, int? gradingScaleId, string country, double? multiplier, string transcriptName, double? transcriptGPA, double? transcriptCredits)
        {
            using (var conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                var cmd = new OleDbCommand(
                    "UPDATE StudentTranscripts SET GradingScale_ID = ?, Country = ?, Multiplier = ?, TranscriptName = ?, TranscriptGPA = ?, TranscriptCredits = ? WHERE Transcript_ID = ?", conn);

                cmd.Parameters.AddWithValue("?", gradingScaleId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("?", string.IsNullOrWhiteSpace(country) ? (object)DBNull.Value : country);
                cmd.Parameters.AddWithValue("?", multiplier ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("?", string.IsNullOrWhiteSpace(transcriptName) ? (object)DBNull.Value : transcriptName);
                cmd.Parameters.AddWithValue("?", transcriptGPA ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("?", transcriptCredits ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("?", transcriptId);

                cmd.ExecuteNonQuery();
            }
        }





        public static void SaveCourse(int? transcriptId, string courseName, string grade, double? creditHours, double? usCreditHours, string usGrade, string semesterName)
        {
            using (var conn = new OleDbConnection(connectionString))
            {
                conn.Open();

                var cmd = new OleDbCommand(
                    "INSERT INTO TranscriptCourses (Transcript_ID, CourseName, Grade, CreditHours, USCreditHours, USGrade, Semester) VALUES (?, ?, ?, ?, ?, ?, ?)", conn);
                cmd.Parameters.AddWithValue("?", transcriptId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("?", string.IsNullOrEmpty(courseName) ? (object)DBNull.Value : courseName);
                cmd.Parameters.AddWithValue("?", string.IsNullOrEmpty(grade) ? (object)DBNull.Value : grade);
                cmd.Parameters.AddWithValue("?", creditHours ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("?", usCreditHours ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("?", string.IsNullOrEmpty(usGrade) ? (object)DBNull.Value : usGrade);
                cmd.Parameters.AddWithValue("?", string.IsNullOrEmpty(semesterName) ? (object)DBNull.Value : semesterName);

                cmd.ExecuteNonQuery();
            }
        }


        public static void UpdateCourse(int courseId, string courseName, string grade, double? creditHours, double? usCreditHours, string usGrade, string semesterName)
        {
            using (var conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                var cmd = new OleDbCommand(
                    "UPDATE TranscriptCourses SET CourseName = ?, Grade = ?, CreditHours = ?, USCreditHours = ?, USGrade = ?, Semester = ? WHERE Course_ID = ?", conn);

                cmd.Parameters.AddWithValue("?", string.IsNullOrEmpty(courseName) ? (object)DBNull.Value : courseName);
                cmd.Parameters.AddWithValue("?", string.IsNullOrEmpty(grade) ? (object)DBNull.Value : grade);
                cmd.Parameters.AddWithValue("?", creditHours ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("?", usCreditHours ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("?", string.IsNullOrEmpty(usGrade) ? (object)DBNull.Value : usGrade);
                cmd.Parameters.AddWithValue("?", string.IsNullOrEmpty(semesterName) ? (object)DBNull.Value : semesterName);
                cmd.Parameters.AddWithValue("?", courseId);

                cmd.ExecuteNonQuery();
            }
        }








        public static void DeleteStudent(int studentId)
        {
            using (var conn = new OleDbConnection(connectionString))
            {
                conn.Open();

                // Delete courses related to each transcript
                var getTranscriptsCmd = new OleDbCommand("SELECT Transcript_ID FROM StudentTranscripts WHERE Student_ID = ?", conn);
                getTranscriptsCmd.Parameters.AddWithValue("?", studentId);

                var transcriptReader = getTranscriptsCmd.ExecuteReader();
                while (transcriptReader.Read())
                {
                    int transcriptId = Convert.ToInt32(transcriptReader["Transcript_ID"]);

                    var deleteCoursesCmd = new OleDbCommand("DELETE FROM TranscriptCourses WHERE Transcript_ID = ?", conn);
                    deleteCoursesCmd.Parameters.AddWithValue("?", transcriptId);
                    deleteCoursesCmd.ExecuteNonQuery();
                }

                transcriptReader.Close();

                // Delete transcripts
                var deleteTranscriptsCmd = new OleDbCommand("DELETE FROM StudentTranscripts WHERE Student_ID = ?", conn);
                deleteTranscriptsCmd.Parameters.AddWithValue("?", studentId);
                deleteTranscriptsCmd.ExecuteNonQuery();

                // Finally, delete student
                var deleteStudentCmd = new OleDbCommand("DELETE FROM Students WHERE Student_ID = ?", conn);
                deleteStudentCmd.Parameters.AddWithValue("?", studentId);
                deleteStudentCmd.ExecuteNonQuery();
            }
        }




        ////////////////////////////////////////////////

        public static ObservableCollection<Student> GetAllStudents()
        {

            if (string.IsNullOrWhiteSpace(connectionString) || !File.Exists(Properties.Settings.Default.DatabasePath))
            {
                return null;
            }

            ObservableCollection<Student> students = new ObservableCollection<Student>();

            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                string query = "SELECT Student_ID, FirstName, LastName, DOB, Term FROM Students";

                OleDbCommand command = new OleDbCommand(query, connection);
                connection.Open();
                OleDbDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Student student = new Student
                    {
                        Student_Id = reader["Student_ID"] != DBNull.Value ? Convert.ToInt32(reader["Student_ID"]) : 0,
                        FirstName = reader["FirstName"]?.ToString(),
                        LastName = reader["LastName"]?.ToString(),
                        DOB = reader["DOB"] != DBNull.Value ? Convert.ToDateTime(reader["DOB"]) : DateTime.MinValue,
                        Term = reader["Term"]?.ToString()
                    };

                    students.Add(student);
                }

                reader.Close();
            }

            return students;
        }


        public static Student GetStudentById(int studentId)
        {
            using (OleDbConnection conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM Students WHERE Student_ID = ?";

                using (OleDbCommand cmd = new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("?", studentId);

                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var student = new Student
                            {
                                Student_Id = reader["Student_ID"] != DBNull.Value ? Convert.ToInt32(reader["Student_ID"]) : 0,
                                FirstName = reader["FirstName"]?.ToString() ?? string.Empty,
                                LastName = reader["LastName"]?.ToString() ?? string.Empty,
                                DOB = reader["DOB"] != DBNull.Value ? Convert.ToDateTime(reader["DOB"]) : DateTime.MinValue,
                                Term = reader["Term"]?.ToString() ?? string.Empty,
                                HSGPA = reader["HS_GPA"] != DBNull.Value ? (double?)Convert.ToDouble(reader["HS_GPA"]) : null,
                                UniGPA = reader["Uni_GPA"] != DBNull.Value ? (double?)Convert.ToDouble(reader["Uni_GPA"]) : null,
                                Transcripts = GetTranscriptsByStudentId(studentId)
                            };

                            return student;
                        }
                    }
                }
            }

            return null;
        }



        public static List<Transcript> GetTranscriptsByStudentId(int studentId)
        {
            var transcripts = new List<Transcript>();

            using (OleDbConnection conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM StudentTranscripts WHERE Student_ID = ?";

                using (OleDbCommand cmd = new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("?", studentId);

                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var transcript = new Transcript
                            {
                                Transcript_ID = reader["Transcript_ID"] != DBNull.Value
                            ? Convert.ToInt32(reader["Transcript_ID"])
                            : -1,

                                Student_ID = reader["Student_ID"] != DBNull.Value
                            ? Convert.ToInt32(reader["Student_ID"])
                            : -1,

                                GradingScale_ID = reader["GradingScale_ID"] != DBNull.Value
                            ? Convert.ToInt32(reader["GradingScale_ID"])
                            : -1,

                                Country = reader["Country"] != DBNull.Value
                            ? reader["Country"].ToString()
                            : string.Empty,

                                Multiplier = reader["Multiplier"] != DBNull.Value
                            ? Convert.ToDouble(reader["Multiplier"])
                            : 1.0,

                                TranscriptName = reader["TranscriptName"] != DBNull.Value
                            ? reader["TranscriptName"].ToString()
                            : string.Empty,

                                TranscriptGPA = reader["TranscriptGPA"] != DBNull.Value
                            ? (double)Convert.ToDouble(reader["TranscriptGPA"])
                            : 0,

                                TranscriptCredits = reader["TranscriptCredits"] != DBNull.Value
                            ? (double)Convert.ToDouble(reader["TranscriptCredits"])
                            : 0,

                                Courses = new List<CourseEntry>() // Empty for now
                            };

                            transcripts.Add(transcript);
                        }
                    }
                }
            }

            return transcripts;
        }


        public static GradingScale GetGradingScaleById(int gradingScaleId)
        {
            using (OleDbConnection conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM GradingScales WHERE GradingScale_ID = ?";

                using (OleDbCommand cmd = new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("?", gradingScaleId);

                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new GradingScale
                            {
                                GradingScale_ID = Convert.ToInt32(reader["GradingScale_ID"]),
                                Country = reader["Country"]?.ToString() ?? string.Empty,
                                ScaleName = reader["ScaleName"]?.ToString() ?? string.Empty,
                                Level = reader["Level"]?.ToString() ?? string.Empty
                            };
                        }
                    }
                }
            }

            return null;
        }



        public static List<CourseEntry> GetCoursesByTranscriptId(int transcriptId)
        {
            var courses = new List<CourseEntry>();

            using (OleDbConnection conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM TranscriptCourses WHERE Transcript_ID = ?";

                using (OleDbCommand cmd = new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("?", transcriptId);

                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var course = new CourseEntry
                            {
                                Course_ID = Convert.ToInt32(reader["Course_ID"]),
                                Transcript_ID = Convert.ToInt32(reader["Transcript_ID"]),
                                CourseName = reader["CourseName"].ToString(),
                                Grade = reader["Grade"].ToString(),
                                CreditHours = reader.GetDouble(reader.GetOrdinal("CreditHours")),
                                USCreditHours = Convert.ToDouble(reader["USCreditHours"]),
                                USConvertedGrade = reader["USGrade"].ToString(), // Or null if you prefer
                                SemesterName = reader["Semester"] != DBNull.Value ? reader["Semester"].ToString() : string.Empty
                            };

                            courses.Add(course);
                        }
                    }
                }
            }

            return courses;
        }


        public static void DeleteCourse(int courseId)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return; // No DB connection available

            string query = "DELETE FROM TranscriptCourses WHERE Course_ID = @CourseId";

            using (OleDbConnection connection = new OleDbConnection(connectionString))
            using (OleDbCommand command = new OleDbCommand(query, connection))
            {
                command.Parameters.AddWithValue("@CourseId", courseId);

                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new Exception("Error deleting course: " + ex.Message);
                }
            }
        }





    }
}

