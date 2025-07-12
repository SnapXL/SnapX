namespace SnapX.Core.Interfaces;

public interface IFilePicker
{
    /// <summary>
    /// Prompts the user to select one or more files.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="initialDirectory">Optional initial directory.</param>
    /// <param name="allowMultiple">Allow selecting multiple files.</param>
    /// <returns>Array of selected file paths, or empty array if cancelled.</returns>
    Task<string[]> PickFilesAsync(string title, string initialDirectory, bool allowMultiple);
}
