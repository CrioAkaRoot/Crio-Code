namespace CrioCode;

public class TabItem
{
    public string Title { get; set; }
    public string Content { get; set; }
    public string? FilePath { get; set; }
    public bool IsModified { get; set; }

    public TabItem(string title, string content, string? filePath = null)
    {
        Title = title;
        Content = content;
        FilePath = filePath;
        IsModified = false;
    }
} 