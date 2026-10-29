using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace PdrExamApp.Models;

public class PdrQuestion : INotifyPropertyChanged
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = "Загальні правила";

    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = string.Empty;

    private string? _imagePath;
    [JsonPropertyName("imagePath")]
    public string? ImagePath
    {
        get => _imagePath;
        set { _imagePath = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("options")]
    public List<AnswerOption> Options { get; set; } = new();

    [JsonIgnore]
    public int DisplayIndex { get; set; }

    [JsonIgnore]
    public bool IsAnswered { get; set; }

    [JsonIgnore]
    public bool? IsCorrect { get; set; }

    [JsonIgnore]
    public AnswerOption? SelectedOption { get; set; }

    private bool _isCurrent;
    [JsonIgnore]
    public bool IsCurrent
    {
        get => _isCurrent;
        set { _isCurrent = value; OnPropertyChanged(); OnPropertyChanged(nameof(BorderColor)); }
    }

    [JsonIgnore]
    public string StatusColor
    {
        get
        {
            if (!IsAnswered) return "#E2E8F0";
            return IsCorrect == true ? "#22C55E" : "#EF4444";
        }
    }

    [JsonIgnore]
    public string TextColor => IsAnswered ? "#FFFFFF" : "#0F172A";

    [JsonIgnore]
    public string BorderColor => IsCurrent ? "#2563EB" : "Transparent";

    public void UpdateStatus()
    {
        OnPropertyChanged(nameof(StatusColor));
        OnPropertyChanged(nameof(TextColor));
        OnPropertyChanged(nameof(BorderColor));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}