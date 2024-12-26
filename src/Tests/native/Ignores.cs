public sealed class _7zTheoryAttribute : TheoryAttribute
{
    public _7zTheoryAttribute()
    {
        if (ArchiveUtils.Find7ZExecutable() is null)
        {
            Skip = "7Z executable not found";
        }
    }
}

public sealed class RarTheoryAttribute : TheoryAttribute
{
    public RarTheoryAttribute()
    {
        if (ArchiveUtils.FindRarExecutable() is null)
        {
            Skip = "Rar executable not found";
        }
    }
}
