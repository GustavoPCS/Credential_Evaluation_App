using CredentialEvaluationApp.Helpers;
using CredentialEvaluationApp.Models;
using CredentialEvaluationApp.Services;
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

    public partial class GradingScalePage : UserControl
    {
        private GradingScale scale = new GradingScale();

        private ObservableCollection<GradingScale> allGradingScales;
        private ObservableCollection<GradingScale> filteredGradingScales;
        public ObservableCollection<GradingScaleMapping> Mappings { get; set; } = new ObservableCollection<GradingScaleMapping>();
        public ObservableCollection<string> USGrades { get; set; } = new ObservableCollection<string>();

        private DataGridTemplateColumn deleteColumn;
        private bool isEditMode = false;
        public GradingScalePage()
        {
            InitializeComponent();
            LoadGradingScales();
            LoadUSGrades();

            GradingScaleInfo.Visibility = Visibility.Collapsed;

            // Add sample rows
            Mappings.Add(new GradingScaleMapping { LocalGrade = "", USEquivalent = "", USLetter = "" });
            Mappings.Add(new GradingScaleMapping { LocalGrade = "", USEquivalent = "", USLetter = "" });
            Mappings.Add(new GradingScaleMapping { LocalGrade = "", USEquivalent = "", USLetter = "" });

            GradingScaleMappingsDataGrid.ItemsSource = Mappings;
            DataContext = this;

            deleteColumn = new DataGridTemplateColumn
            {
                Header = "",
                Width = 100,
                CellTemplate = (DataTemplate)this.FindResource("DeleteButtonTemplate")
            };
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
        private void FadeElement(UIElement element, bool fadeIn, double durationSeconds = 0.3, Action onComplete = null)
        {
            element.Visibility = Visibility.Visible;

            var animation = new DoubleAnimation
            {
                From = fadeIn ? 0 : 1,
                To = fadeIn ? 1 : 0,
                Duration = TimeSpan.FromSeconds(durationSeconds),
                FillBehavior = FillBehavior.Stop
            };

            animation.Completed += (s, e) =>
            {
                if (!fadeIn)
                    element.Visibility = Visibility.Collapsed;
                else
                    element.Opacity = 1;

                onComplete?.Invoke();
            };

            element.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        ////////////////////////////////////////////////

        private void LoadGradingScales()
        {

            var gradingScales = DatabaseHelper.GetAllGradingScales();

            allGradingScales = new ObservableCollection<GradingScale>(gradingScales);
            filteredGradingScales = new ObservableCollection<GradingScale>(allGradingScales);
            GradingScaleGrid.ItemsSource = filteredGradingScales;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (filteredGradingScales == null || allGradingScales == null || SearchBox.Text == "Search by name...")
                return;

            string query = SearchBox.Text.Trim().ToLower();

            filteredGradingScales.Clear();
            foreach (var scale in allGradingScales)
            {
                if (scale.ScaleName != null && scale.ScaleName.ToLower().Contains(query))
                {
                    filteredGradingScales.Add(scale);
                }
            }

        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text == "Search by name...")
            {
                SearchBox.Text = "";
                SearchBox.Foreground = Brushes.Black;
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchBox.Text = "Search by name...";
                SearchBox.Foreground = Brushes.Gray;
            }
        }

        private void CreateNew_Click(object sender, RoutedEventArgs e)
        {
            if (CreateNewButton.Content.ToString() == "Create New")
            {
                scale.GradingScale_ID = 0;

                FadeElement(GradingScaleGrid, false, 0.2, () =>
                {
                    FadeElement(SearchBox, false, 0.2, () =>
                    {
                        FadeElement(DeleteGradingScaleButton, false, 0.2, () =>
                        {

                            FadeElement(GradingScaleInfo, true, 0.2);

                        });
                    });
                });


                GradingScaleNameTextBox.Text = "";
                GradingScaleCountryTextBox.Text = "";
                GradingScaleLevelTextBox.Text = "";
                Mappings.Clear();
                Mappings.Add(new GradingScaleMapping { LocalGrade = "", USEquivalent = "", USLetter = "" });
                Mappings.Add(new GradingScaleMapping { LocalGrade = "", USEquivalent = "", USLetter = "" });
                Mappings.Add(new GradingScaleMapping { LocalGrade = "", USEquivalent = "", USLetter = "" });

                CreateNewButton.Content = "Back";
            }
            else
            {

                FadeElement(GradingScaleInfo, false, 0.2, () =>
                {

                    FadeElement(GradingScaleGrid, true);
                    FadeElement(SearchBox, true);
                    FadeElement(DeleteGradingScaleButton, true);

                });

                CreateNewButton.Content = "Create New";
            }

        }

        private void EditGradingScale_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is GradingScale selectedScale)
            {
                scale.GradingScale_ID = selectedScale.GradingScale_ID;

                // Step 1: Populate the TextBoxes
                GradingScaleNameTextBox.Text = selectedScale.ScaleName;
                GradingScaleCountryTextBox.Text = selectedScale.Country;
                GradingScaleLevelTextBox.Text = selectedScale.Level;

                // Step 2: Load mappings from the database using the GradingScale_ID
                var mappings = DatabaseHelper.GetMappingsByScaleId(selectedScale.GradingScale_ID);


                // Step 3: Bind mappings to the DataGrid
                Mappings.Clear();
                foreach (var mapping in mappings)
                {
                    Mappings.Add(new GradingScaleMapping { LocalGrade = mapping.LocalGrade, USLetter = mapping.USLetter });
                }


                // Step 4: Toggle visibility
                FadeElement(GradingScaleGrid, false, 0.2, () =>
                {
                    FadeElement(SearchBox, false, 0.2, () =>
                    {
                        FadeElement(GradingScaleInfo, true, 0.2);
                    });
                });
                CreateNewButton.Content = "Back";
            }



        }


        ////////////////////////////////////////////////

        private void LoadUSGrades()
        {

            var usEquivalentService = new US_EquivalentService();
            USGrades = usEquivalentService.GetUS_Grades();

        }

        private void ToggleEditMode_Click(object sender, RoutedEventArgs e)
        {
            isEditMode = !isEditMode;

            if (isEditMode)
            {
                if (!GradingScaleMappingsDataGrid.Columns.Contains(deleteColumn))
                {
                    GradingScaleMappingsDataGrid.Columns.Add(deleteColumn);
                }

                EditToggleButton.Content = "Close";
            }
            else
            {
                if (GradingScaleMappingsDataGrid.Columns.Contains(deleteColumn))
                {
                    GradingScaleMappingsDataGrid.Columns.Remove(deleteColumn);
                }

                EditToggleButton.Content = "Edit";
            }
        }

        private void DeleteScore_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is GradingScaleMapping score)
            {
                Mappings.Remove(score);
            }
        }

        private void AddScore_Click(object sender, RoutedEventArgs e)
        {
            Mappings.Add(new GradingScaleMapping { LocalGrade = "", USEquivalent = "", USLetter = "" });
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

        ////////////////////////////////////////////////
        public event Action GradingScaleUpdated;
        private void Save_Click(object sender, RoutedEventArgs e)
        {

            // Step 1: Validate current GradingScale info (optional)
            if (string.IsNullOrWhiteSpace(GradingScaleNameTextBox.Text) || string.IsNullOrWhiteSpace(GradingScaleCountryTextBox.Text) || string.IsNullOrWhiteSpace(GradingScaleLevelTextBox.Text) || Mappings.Count == 0)
            {
                MessageBox.Show("Please fill out the grading scale details and mappings.");
                return;
            }

            scale.Country = GradingScaleCountryTextBox.Text.Trim();
            scale.ScaleName = GradingScaleNameTextBox.Text.Trim();
            scale.Level = GradingScaleLevelTextBox.Text.Trim();

            // Step 2: Insert or Update GradingScale
            if (scale.GradingScale_ID == 0)
            {
                // New grading scale
                int newId = DatabaseHelper.InsertGradingScale(scale); // returns new ID
                scale.GradingScale_ID = newId;
            }
            else
            {
                // Update existing scale
                DatabaseHelper.UpdateGradingScale(scale);
            }


            // Step 3: Save all mappings (delete old and insert new for simplicity)
            DatabaseHelper.DeleteMappingsByScaleId(scale.GradingScale_ID);

            US_EquivalentService service = new US_EquivalentService();
            var mappingsCopy = new ObservableCollection<GradingScaleMapping>(Mappings);
            service.ConversionToNumGrade(ref mappingsCopy);

            foreach (var mapping in mappingsCopy)
            {
                mapping.GradingScale_ID = scale.GradingScale_ID; // make sure foreign key is set
                DatabaseHelper.InsertGradingScaleMapping(mapping);
            }

            LoadGradingScales();
            MessageBox.Show("Grading scale saved successfully.");

            // Notify all listeners
            AppEvents.OnGradingScaleUpdated();


            FadeElement(GradingScaleInfo, false, 0.2, () =>
            {

                FadeElement(GradingScaleGrid, true);
                FadeElement(SearchBox, true);
                FadeElement(DeleteGradingScaleButton, true);

            });

            CreateNewButton.Content = "Create New";
        }

        private void DeleteGradingScale_Click(object sender, RoutedEventArgs e)
        {
            Button deleteButton = sender as Button;
            if (deleteButton == null) return;

            scale.ScaleName = GradingScaleNameTextBox.Text.Trim();

            // Step 3: Confirm deletion with the user
            MessageBoxResult result = MessageBox.Show(
                $"Are you sure you want to delete '{scale.ScaleName}'?",
                "Confirm Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );


            if (result != MessageBoxResult.Yes)
                return;

            bool success = DatabaseHelper.DeleteGradingScale(scale.GradingScale_ID);

            if (success)
            {
                LoadGradingScales();
                MessageBox.Show("Grading scale deleted successfully.");
                AppEvents.OnGradingScaleUpdated();
            }
            else
            {
                MessageBox.Show("Failed to delete grading scale.");
            }


            FadeElement(GradingScaleInfo, false, 0.2, () =>
            {

                FadeElement(GradingScaleGrid, true);
                FadeElement(SearchBox, true);
                FadeElement(DeleteGradingScaleButton, true);

            });

            CreateNewButton.Content = "Create New";

        }

    }

}
