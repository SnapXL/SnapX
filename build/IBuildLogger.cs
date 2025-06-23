namespace DefaultNamespace;

public interface IBuildLogger
{
    void Information(string message);
    void Error(string message);
    void Warning(string message);
    void Debug(string message);
}
