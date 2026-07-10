using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TimeTracker.Core.Categorization;
using TimeTracker.Core.Storage;
using TimeTracker.Core.Storage.Entities;
using TimeTracker.Core.Tracking;

namespace TimeTracker.App.ViewModels;

public partial class RulesViewModel : ObservableObject
{
    private readonly Func<TimeTrackerDbContext> _contextFactory;
    private readonly ActivityTracker _tracker;
    private readonly ILogger _log;

    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<CategoryRule> Rules { get; } = new();

    [ObservableProperty] private Category? _selectedCategory;

    partial void OnSelectedCategoryChanged(Category? value)
    {
        LoadRules(value);
    }

    public RulesViewModel(Func<TimeTrackerDbContext> contextFactory, ActivityTracker tracker, ILogger log)
    {
        _contextFactory = contextFactory;
        _tracker = tracker;
        _log = log;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            await using var db = _contextFactory();
            var categories = await db.Categories
                .AsNoTracking()
                .Include(c => c.Rules.OrderBy(r => r.Priority))
                .OrderBy(c => c.Name)
                .ToListAsync();

            Categories.Clear();
            foreach (var c in categories)
                Categories.Add(c);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не удалось загрузить категории");
        }
    }

    private void LoadRules(Category? category)
    {
        Rules.Clear();
        if (category == null) return;
        foreach (var r in category.Rules.OrderBy(r => r.Priority))
            Rules.Add(r);
    }

    [RelayCommand]
    private async Task AddCategoryAsync()
    {
        try
        {
            await using var db = _contextFactory();
            var category = new Category
            {
                Name = "Новая категория",
                ColorHex = "#9CA3AF",
                Icon = "📁",
            };
            db.Categories.Add(category);
            await db.SaveChangesAsync();

            category.Rules = new List<CategoryRule>();
            Categories.Add(category);
            SelectedCategory = category;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не удалось добавить категорию");
        }
    }

    [RelayCommand]
    private async Task DeleteCategoryAsync()
    {
        if (SelectedCategory == null) return;
        try
        {
            await using var db = _contextFactory();
            var entity = await db.Categories.FindAsync(SelectedCategory.Id);
            if (entity != null)
            {
                db.Categories.Remove(entity);
                await db.SaveChangesAsync();
            }

            Categories.Remove(SelectedCategory);
            SelectedCategory = null;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не удалось удалить категорию");
        }
    }

    [RelayCommand]
    private async Task AddRuleAsync()
    {
        if (SelectedCategory == null) return;
        try
        {
            await using var db = _contextFactory();
            var rule = new CategoryRule
            {
                CategoryId = SelectedCategory.Id,
                FieldType = RuleFieldType.ProcessName,
                MatchType = RuleMatchType.Substring,
                Pattern = "",
                Priority = 100,
                IsEnabled = true,
            };
            db.Rules.Add(rule);
            await db.SaveChangesAsync();

            rule.Category = SelectedCategory;
            Rules.Add(rule);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не удалось добавить правило");
        }
    }

    [RelayCommand]
    private async Task DeleteRuleAsync(CategoryRule? rule)
    {
        if (rule == null) return;
        try
        {
            await using var db = _contextFactory();
            var entity = await db.Rules.FindAsync(rule.Id);
            if (entity != null)
            {
                db.Rules.Remove(entity);
                await db.SaveChangesAsync();
            }

            Rules.Remove(rule);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не удалось удалить правило");
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            await using var db = _contextFactory();
            foreach (var category in Categories)
            {
                var entity = await db.Categories.FindAsync(category.Id);
                if (entity == null) continue;
                entity.Name = category.Name;
                entity.ColorHex = category.ColorHex;
                entity.Icon = category.Icon;
            }

            foreach (var rule in Rules)
            {
                var entity = await db.Rules.FindAsync(rule.Id);
                if (entity == null) continue;
                entity.FieldType = rule.FieldType;
                entity.MatchType = rule.MatchType;
                entity.Pattern = rule.Pattern;
                entity.Priority = rule.Priority;
                entity.IsEnabled = rule.IsEnabled;
            }

            await db.SaveChangesAsync();
            _tracker.RefreshCategories();
            _log.Information("Правила сохранены и кэш обновлён");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не удалось сохранить правила");
        }
    }
}
