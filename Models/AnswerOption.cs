using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PdrExamApp.Models;

public class AnswerOption : INotifyPropertyChanged
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }

    private string _backgroundColor = "Transparent";
    public string BackgroundColor
    {
        get => _backgroundColor;
        set { _backgroundColor = value; OnPropertyChanged(); }
    }

    private string _textColor = "#212121";
    public string TextColor
    {
        get => _textColor;
        set { _textColor = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}