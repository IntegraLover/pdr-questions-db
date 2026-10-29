using System.Windows;
using System.Windows.Controls;
using PdrExamApp.Models;
using PdrExamApp.ViewModels;

namespace PdrExamApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Option_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton radio && radio.DataContext is AnswerOption option)
        {
            if (DataContext is ExamViewModel vm)
            {
                vm.SelectedOption = option;
            }
        }
    }
}