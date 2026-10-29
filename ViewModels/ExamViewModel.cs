using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using PdrExamApp.Models;
using PdrExamApp.Services;

namespace PdrExamApp.ViewModels;

public class ExamViewModel : INotifyPropertyChanged
{
    private readonly QuestionService _questionService;
    private readonly DispatcherTimer _timer;
    private TimeSpan _timeRemaining;

    private ObservableCollection<PdrQuestion> _activeQuestions = new();
    private int _currentIndex;
    private PdrQuestion? _currentQuestion;
    private AnswerOption? _selectedOption;
    private string _resultMessage = string.Empty;
    private string _aiAnalysis = string.Empty;
    private bool _isAnswered;
    private int _correctAnswersCount;

    private bool _isStartPageVisible = true;
    private bool _isExamFinished;
    private bool _isLoading;
    private bool _hasNetworkError;
    private string _errorMessage = string.Empty;
    private string _dataSourceStatus = "Підключення...";
    private string _timerText = "20:00";
    private string _selectedCategory = "Усі теми";

    public ExamViewModel()
    {
        _questionService = new QuestionService();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += Timer_Tick;

        StartFullExamCommand = new RelayCommand(async () => await StartExamAsync(isFullExam: true));
        StartTopicExamCommand = new RelayCommand(async () => await StartExamAsync(isFullExam: false));
        RetryNetworkCommand = new RelayCommand(async () => await InitializeCategoriesAsync());
        SubmitCommand = new RelayCommand(SubmitAnswer, () => SelectedOption != null && !IsAnswered);
        NextCommand = new RelayCommand(NextQuestion, () => IsAnswered || _currentIndex < ActiveQuestions.Count - 1);
        SelectQuestionCommand = new RelayCommand(param => { if (param is PdrQuestion q) SelectQuestion(q); });
        ReturnToHomeCommand = new RelayCommand(GoToHomeScreen);

        Categories = new ObservableCollection<string>
        {
            "Усі теми",
            "Дорожні знаки",
            "Наказові знаки",
            "Сигнали світлофора",
            "Швидкість руху",
            "Проїзд перехресть"
        };

        _ = InitializeCategoriesAsync();
    }

    public ObservableCollection<string> Categories { get; private set; }

    public ObservableCollection<PdrQuestion> ActiveQuestions
    {
        get => _activeQuestions;
        set { _activeQuestions = value; OnPropertyChanged(); }
    }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set { _selectedCategory = value; OnPropertyChanged(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public bool HasNetworkError
    {
        get => _hasNetworkError;
        set { _hasNetworkError = value; OnPropertyChanged(); }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    public string DataSourceStatus
    {
        get => _dataSourceStatus;
        set { _dataSourceStatus = value; OnPropertyChanged(); }
    }

    public bool IsStartPageVisible
    {
        get => _isStartPageVisible;
        set 
        { 
            _isStartPageVisible = value; 
            OnPropertyChanged(); 
            OnPropertyChanged(nameof(IsExamActive));
        }
    }

    public bool IsExamFinished
    {
        get => _isExamFinished;
        set 
        { 
            _isExamFinished = value; 
            OnPropertyChanged(); 
            OnPropertyChanged(nameof(IsExamActive));
        }
    }

    public bool IsExamActive => !IsStartPageVisible && !IsExamFinished;

    public string TimerText
    {
        get => _timerText;
        set { _timerText = value; OnPropertyChanged(); }
    }

    public PdrQuestion? CurrentQuestion
    {
        get => _currentQuestion;
        set { _currentQuestion = value; OnPropertyChanged(); }
    }

    public AnswerOption? SelectedOption
    {
        get => _selectedOption;
        set 
        { 
            _selectedOption = value; 
            OnPropertyChanged(); 
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public string ResultMessage
    {
        get => _resultMessage;
        set { _resultMessage = value; OnPropertyChanged(); }
    }

    public string AiAnalysis
    {
        get => _aiAnalysis;
        set { _aiAnalysis = value; OnPropertyChanged(); }
    }

    public bool IsAnswered
    {
        get => _isAnswered;
        set 
        { 
            _isAnswered = value; 
            OnPropertyChanged(); 
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public string ProgressText => $"Питання {_currentIndex + 1} з {ActiveQuestions.Count}";
    public string ScoreText => $"Правильних відповідей: {_correctAnswersCount} з {ActiveQuestions.Count}";

    public ICommand StartFullExamCommand { get; }
    public ICommand StartTopicExamCommand { get; }
    public ICommand RetryNetworkCommand { get; }
    public ICommand SubmitCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand SelectQuestionCommand { get; }
    public ICommand ReturnToHomeCommand { get; }

    private async Task InitializeCategoriesAsync()
    {
        IsLoading = true;
        HasNetworkError = false;
        DataSourceStatus = "⏳ Перевірка з'єднання...";

        var (_, isSuccess, statusMessage) = await _questionService.FetchOnlineQuestionsAsync(count: 1);
        DataSourceStatus = statusMessage;

        if (!isSuccess)
        {
            HasNetworkError = true;
            ErrorMessage = statusMessage;
        }

        IsLoading = false;
    }

    private async Task StartExamAsync(bool isFullExam)
    {
        IsLoading = true;
        HasNetworkError = false;
        DataSourceStatus = "⏳ Завантаження питань з сервера...";

        string? category = isFullExam ? null : SelectedCategory;
        var (questions, isSuccess, statusMessage) = await _questionService.FetchOnlineQuestionsAsync(count: 10, categoryFilter: category);

        DataSourceStatus = statusMessage;

        if (!isSuccess || questions.Count == 0)
        {
            HasNetworkError = true;
            ErrorMessage = statusMessage;
            IsLoading = false;
            return;
        }

        SetupQuestions(questions);
        InitExam(enableTimer: isFullExam);

        IsLoading = false;
    }

    private void SetupQuestions(List<PdrQuestion> questions)
    {
        for (int i = 0; i < questions.Count; i++)
        {
            questions[i].DisplayIndex = i + 1;
            questions[i].IsAnswered = false;
            questions[i].IsCorrect = null;
            questions[i].SelectedOption = null;
            questions[i].IsCurrent = (i == 0);
            
            foreach (var opt in questions[i].Options)
            {
                opt.BackgroundColor = "Transparent";
                opt.TextColor = "#0F172A";
            }
            questions[i].UpdateStatus();
        }

        ActiveQuestions = new ObservableCollection<PdrQuestion>(questions);
    }

    private void InitExam(bool enableTimer)
    {
        _currentIndex = 0;
        _correctAnswersCount = 0;
        IsStartPageVisible = false;
        IsExamFinished = false;

        if (enableTimer)
        {
            _timeRemaining = TimeSpan.FromMinutes(20);
            TimerText = "20:00";
            _timer.Stop();
            _timer.Start();
        }
        else
        {
            _timer.Stop();
            TimerText = "Без обмежень";
        }

        LoadQuestionAtIndex(_currentIndex);
    }

    private void SelectQuestion(PdrQuestion question)
    {
        int index = ActiveQuestions.IndexOf(question);
        if (index >= 0)
        {
            LoadQuestionAtIndex(index);
        }
    }

    private void LoadQuestionAtIndex(int index)
    {
        if (ActiveQuestions.Count == 0 || index < 0 || index >= ActiveQuestions.Count) return;

        if (CurrentQuestion != null)
        {
            CurrentQuestion.IsCurrent = false;
        }

        _currentIndex = index;
        CurrentQuestion = ActiveQuestions[_currentIndex];
        CurrentQuestion.IsCurrent = true;

        SelectedOption = CurrentQuestion.SelectedOption;
        IsAnswered = CurrentQuestion.IsAnswered;

        if (IsAnswered)
        {
            if (CurrentQuestion.IsCorrect == true)
            {
                ResultMessage = "✅ Правильно!";
                AiAnalysis = "Аналіз ШІ: Відповідь вірна. Ви правильно застосували відповідний пункт ПДР.";
            }
            else
            {
                ResultMessage = "❌ Неправильно!";
                AiAnalysis = $"Аналіз ШІ: Помилка! Роз'яснення: {CurrentQuestion.Explanation}";
            }
        }
        else
        {
            ResultMessage = string.Empty;
            AiAnalysis = string.Empty;
        }

        OnPropertyChanged(nameof(ProgressText));
    }

    private void SubmitAnswer()
    {
        if (SelectedOption == null || CurrentQuestion == null || IsAnswered) return;

        IsAnswered = true;
        CurrentQuestion.IsAnswered = true;
        CurrentQuestion.SelectedOption = SelectedOption;
        CurrentQuestion.IsCorrect = SelectedOption.IsCorrect;

        foreach (var option in CurrentQuestion.Options)
        {
            if (option.IsCorrect)
            {
                option.BackgroundColor = "#DCFCE7";
                option.TextColor = "#15803D";
            }
            else if (option == SelectedOption && !option.IsCorrect)
            {
                option.BackgroundColor = "#FEE2E2";
                option.TextColor = "#B91C1C";
            }
        }

        if (SelectedOption.IsCorrect)
        {
            _correctAnswersCount++;
            ResultMessage = "✅ Правильно!";
            AiAnalysis = "Аналіз ШІ: Відповідь вірна. Ви правильно застосували відповідний пункт ПДР.";
        }
        else
        {
            ResultMessage = "❌ Неправильно!";
            AiAnalysis = $"Аналіз ШІ: Помилка! Роз'яснення: {CurrentQuestion.Explanation}";
        }

        CurrentQuestion.UpdateStatus();
    }

    private void NextQuestion()
    {
        if (_currentIndex < ActiveQuestions.Count - 1)
        {
            LoadQuestionAtIndex(_currentIndex + 1);
        }
        else
        {
            _timer.Stop();
            IsExamFinished = true;
            OnPropertyChanged(nameof(ScoreText));
        }
    }

    private void GoToHomeScreen()
    {
        _timer.Stop();
        IsStartPageVisible = true;
        IsExamFinished = false;
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (_timeRemaining.TotalSeconds > 0)
        {
            _timeRemaining = _timeRemaining.Subtract(TimeSpan.FromSeconds(1));
            TimerText = _timeRemaining.ToString(@"mm\:ss");
        }
        else
        {
            _timer.Stop();
            TimerText = "00:00";
            IsExamFinished = true;
            OnPropertyChanged(nameof(ScoreText));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}