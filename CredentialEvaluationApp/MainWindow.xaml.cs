using CredentialEvaluationApp.Helpers;
using CredentialEvaluationApp.Models;
using CredentialEvaluationApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
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
    public partial class MainWindow : Window
    {
        public SearchPage SearchPageInstance { get; set; } = new SearchPage();
        public CalculatorPage calculatorPage;
        public GradingScalePage gradingScalePage;
        public SettingsPage settingsPage;

        public MainWindow()
        {
            InitializeComponent();


            SearchPageInstance = new SearchPage();
            calculatorPage = new CalculatorPage();
            gradingScalePage = new GradingScalePage();
            settingsPage = new SettingsPage();


            NavigateToPage(calculatorPage); // Default page

            //Sidebar
            Sidebar sidebar = new Sidebar();
            Grid.SetColumn(sidebar, 0); // Place in the left column
            MainGrid.Children.Add(sidebar);

        }

        public void NavigateToPage(UserControl page)
        {
            if (MainContent.Content == page)
                return;


            if (MainContent.Content is UserControl currentPage)
            {
                var fadeOut = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(250),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                fadeOut.Completed += (s, e) =>
                {
                    MainContent.Content = page;
                };

                (currentPage.Content as UIElement)?.BeginAnimation(OpacityProperty, fadeOut);
            }
            else
            {
                MainContent.Content = page;
            }
        }

    }

}
