using QuickLaunch.Core.Models;
using QuickLaunch.Core.Services;

namespace QuickLaunch.Tests;

public class ScoringServiceTests
{
    private readonly ScoringService _scorer = new();

    private static IndexItem CreateItem(
        string fileName = "test.exe",
        string path = @"C:\Users\test\Desktop\test.exe",
        ItemType type = ItemType.Exe,
        DateTime? lastModified = null,
        DateTime? lastAccessed = null)
    {
        return new IndexItem
        {
            FileName = fileName,
            FullName = fileName,
            Path = path,
            Type = type,
            LastModified = lastModified ?? DateTime.Now.AddDays(-60),
            LastAccessed = lastAccessed ?? DateTime.Now.AddDays(-60),
        };
    }

    [Fact]
    public void ExeFile_GetsPositiveScore()
    {
        var item = CreateItem("app.exe", @"C:\app.exe", ItemType.Exe);
        int score = _scorer.ScoreFile(item, @"C:\");
        Assert.True(score > 0, $"Exe should score positively, got {score}");
    }

    [Fact]
    public void ShortcutFile_ScoresHigherThanExe()
    {
        var lnk = CreateItem("app.lnk", @"C:\app.lnk", ItemType.Shortcut);
        var exe = CreateItem("app.exe", @"C:\app.exe", ItemType.Exe);

        int lnkScore = _scorer.ScoreFile(lnk, @"C:\");
        int exeScore = _scorer.ScoreFile(exe, @"C:\");

        Assert.True(lnkScore >= exeScore, $"Shortcut ({lnkScore}) should score >= Exe ({exeScore})");
    }

    [Fact]
    public void TmpFile_GetsPenalized()
    {
        var item = CreateItem("junk.tmp", @"C:\junk.tmp", ItemType.File);
        int score = _scorer.ScoreFile(item, @"C:\");
        Assert.True(score < 0, $"Temp file should score negatively, got {score}");
    }

    [Fact]
    public void Directory_GetsDirectoryScore()
    {
        var item = CreateItem("MyFolder", @"C:\MyFolder", ItemType.Directory);
        int score = _scorer.ScoreFile(item, @"C:\");
        Assert.True(score > 0, $"Directory should score positively, got {score}");
    }

    [Fact]
    public void UwpApp_GetsHighExtensionScore()
    {
        var item = CreateItem("Calculator", @"C:\Calculator", ItemType.UWP);
        int score = _scorer.ScoreFile(item, @"C:\");
        Assert.True(score > 50, $"UWP app should score high, got {score}");
    }

    // --- Depth scoring ---

    [Fact]
    public void DeeperFiles_ScoreLowerThanShallowFiles()
    {
        var shallow = CreateItem("app.exe", @"C:\Programs\app.exe", ItemType.Exe);
        var deep = CreateItem("app.exe", @"C:\Programs\sub\deep\nested\app.exe", ItemType.Exe);

        int shallowScore = _scorer.ScoreFile(shallow, @"C:\Programs");
        int deepScore = _scorer.ScoreFile(deep, @"C:\Programs");

        Assert.True(shallowScore > deepScore,
            $"Shallow file ({shallowScore}) should outscore deep file ({deepScore})");
    }

    // --- Name scoring ---

    [Fact]
    public void HelperApp_GetsPenalized()
    {
        var item = CreateItem("updater.exe", @"C:\updater.exe", ItemType.Exe);
        var normal = CreateItem("chrome.exe", @"C:\chrome.exe", ItemType.Exe);

        int helperScore = _scorer.ScoreFile(item, @"C:\");
        int normalScore = _scorer.ScoreFile(normal, @"C:\");

        Assert.True(helperScore < normalScore,
            $"Helper app ({helperScore}) should score lower than normal app ({normalScore})");
    }

    [Fact]
    public void VeryLongName_GetsPenalized()
    {
        string longName = new string('a', 60) + ".exe";
        var item = CreateItem(longName, @"C:\" + longName, ItemType.Exe);
        var normal = CreateItem("app.exe", @"C:\app.exe", ItemType.Exe);

        int longScore = _scorer.ScoreFile(item, @"C:\");
        int normalScore = _scorer.ScoreFile(normal, @"C:\");

        Assert.True(longScore < normalScore,
            $"Long name ({longScore}) should score lower than normal name ({normalScore})");
    }

    // --- Recency scoring ---

    [Fact]
    public void RecentlyModified_ScoresHigherThanOld()
    {
        var recent = CreateItem(lastModified: DateTime.Now.AddDays(-5), lastAccessed: DateTime.Now.AddDays(-60));
        var old = CreateItem(lastModified: DateTime.Now.AddDays(-365), lastAccessed: DateTime.Now.AddDays(-365));

        int recentScore = _scorer.ScoreFile(recent, @"C:\");
        int oldScore = _scorer.ScoreFile(old, @"C:\");

        Assert.True(recentScore > oldScore,
            $"Recently modified ({recentScore}) should outscore old file ({oldScore})");
    }

    [Fact]
    public void RecentlyAccessed_ScoresHigherThanOld()
    {
        var recent = CreateItem(lastModified: DateTime.Now.AddDays(-60), lastAccessed: DateTime.Now.AddDays(-5));
        var old = CreateItem(lastModified: DateTime.Now.AddDays(-60), lastAccessed: DateTime.Now.AddDays(-365));

        int recentScore = _scorer.ScoreFile(recent, @"C:\");
        int oldScore = _scorer.ScoreFile(old, @"C:\");

        Assert.True(recentScore > oldScore,
            $"Recently accessed ({recentScore}) should outscore old file ({oldScore})");
    }
}
