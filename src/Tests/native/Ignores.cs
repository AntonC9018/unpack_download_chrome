public sealed class _7zFactAttribute : FactAttribute
{
    public _7zFactAttribute()
    {
        if (ArchiveUtils.Find7ZExecutable() is null)
        {
            Skip = "7Z executable not found";
        }
    }
}

public sealed class RarFactAttribute : FactAttribute
{
    public RarFactAttribute()
    {
        if (ArchiveUtils.FindRarExecutable() is null)
        {
            Skip = "Rar executable not found";
        }
    }
}
