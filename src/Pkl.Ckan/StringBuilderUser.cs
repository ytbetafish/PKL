using System.Text;

namespace Pkl.Ckan;

/// <summary>
/// IUser implementation that accumulates all CKAN log/progress/error messages into a StringBuilder.
/// Used to back the "view log" window after install/uninstall runs.
/// </summary>
public sealed class StringBuilderUser : CKAN.IUser
{
    private readonly StringBuilder _sb = new();
    private readonly object _lock = new();

    public string Dump() { lock (_lock) return _sb.ToString(); }
    public void Clear() { lock (_lock) _sb.Clear(); }

    public bool Headless => true;

    public bool RaiseYesNoDialog(string question)
    {
        Append($"[YesNo] {question}");
        return true;
    }

    public int RaiseSelectionDialog(string message, params object[] args)
    {
        Append($"[Select] {message} :: {string.Join(", ", args)}");
        return 0;
    }

    public void RaiseError(string message, params object[] args)
    {
        Append($"[Error] {string.Format(message, args)}");
    }

    public void RaiseProgress(string message, int percent)
    {
        Append($"[Progress {percent}%] {message}");
    }

    public void RaiseProgress(CKAN.ByteRateCounter rateCounter) { /* ignore */ }

    public void RaiseMessage(string message, params object[] args)
    {
        Append($"[Msg] {string.Format(message, args)}");
    }

    private void Append(string line)
    {
        lock (_lock) _sb.AppendLine(line);
    }
}
