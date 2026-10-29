using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using PdrExamApp.Models;

namespace PdrExamApp.Services;

public class QuestionService
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private const string QuestionsImagesDir = @"C:\Kursach26\Questions\images";
    private const string RemoteApiEndpoint = "https://raw.githubusercontent.com/IntegraLover/pdr-questions-db/main/questions.json";

    public QuestionService()
    {
        try { Directory.CreateDirectory(QuestionsImagesDir); } catch { }
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        }
    }

    public async Task<(List<PdrQuestion> Questions, bool IsSuccess, string StatusMessage)> FetchOnlineQuestionsAsync(
        int count = 10, 
        string? categoryFilter = null)
    {
        List<PdrQuestion>? allQuestions = null;
        bool isOnline = false;
        string status = "🌐 Онлайн джерело (GitHub)";

        try
        {
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await _httpClient.GetAsync(RemoteApiEndpoint, cts.Token);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync(cts.Token);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                allQuestions = JsonSerializer.Deserialize<List<PdrQuestion>>(jsonString, options);
                if (allQuestions != null && allQuestions.Count > 0)
                {
                    isOnline = true;
                }
            }
        }
        catch { }

        if (allQuestions == null || allQuestions.Count == 0)
        {
            allQuestions = GetFallbackQuestions();
            isOnline = false;
            status = "📁 Локальний банк";
        }

        IEnumerable<PdrQuestion> filteredPool = allQuestions;
        if (!string.IsNullOrEmpty(categoryFilter) && categoryFilter != "Усі теми")
        {
            var categoryQuestions = allQuestions.Where(q => q.Category.Equals(categoryFilter, StringComparison.OrdinalIgnoreCase)).ToList();
            if (categoryQuestions.Count > 0)
            {
                filteredPool = categoryQuestions;
            }
        }

        var selectedQuestions = filteredPool
            .OrderBy(_ => Random.Shared.Next())
            .Take(count)
            .ToList();

        // Пошук та прив'язка PNG файлів
        foreach (var question in selectedQuestions)
        {
            if (!string.IsNullOrEmpty(question.ImagePath))
            {
                string rawName = Path.GetFileNameWithoutExtension(question.ImagePath); // sign_2_6 або UA_road_sign_2.6
                string numberOnly = rawName.Replace("sign_", "").Replace("UA_road_sign_", "").Replace("_", ".");

                string[] searchPatterns = new[]
                {
                    $"{rawName}.png",
                    $"sign_{numberOnly.Replace('.', '_')}.png",
                    $"UA_road_sign_{numberOnly}.png",
                    $"UA_road_sign_{numberOnly}.svg.png"
                };

                string? resolvedPath = null;
                foreach (var pattern in searchPatterns)
                {
                    string p1 = Path.Combine(QuestionsImagesDir, pattern);
                    string p2 = Path.Combine(AppContext.BaseDirectory, "Resources", "Images", pattern);

                    if (File.Exists(p1)) { resolvedPath = p1; break; }
                    if (File.Exists(p2)) { resolvedPath = p2; break; }
                }

                question.ImagePath = resolvedPath != null ? Path.GetFullPath(resolvedPath) : null;
            }
        }

        return (selectedQuestions, isOnline, status);
    }

    private List<PdrQuestion> GetFallbackQuestions()
    {
        return new List<PdrQuestion>
        {
            new PdrQuestion
            {
                Id = 1,
                Title = "Що вимагає від водія даний дорожній знак?",
                Category = "Дорожні знаки",
                Explanation = "Знак 2.1 'Дати дорогу' зобов'язує водія дати дорогу транспортним засобам, що рухаються по пересечуваній дорозі.",
                ImagePath = "sign_2_1.png",
                Options = new List<AnswerOption>
                {
                    new AnswerOption { Id = 1, Text = "Обов'язкову зупинку перед перехрестям", IsCorrect = false },
                    new AnswerOption { Id = 2, Text = "Дати дорогу транспортним засобам, що рухаються по пересечуваній дорозі", IsCorrect = true },
                    new AnswerOption { Id = 3, Text = "Перевагу в русі перед іншими транспортними засобами", IsCorrect = false }
                }
            }
        };
    }
}