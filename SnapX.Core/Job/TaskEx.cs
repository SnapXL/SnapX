// SPDX-License-Identifier: GPL-3.0-or-later



namespace SnapX.Core.Job;
public class TaskEx<T>
{
    public delegate void ProgressChangedEventHandler(T progress);
    public event ProgressChangedEventHandler ProgressChanged;

    public bool IsRunning { get; private set; }
    public bool IsCanceled { get; private set; }

    private Progress<T> p;
    private CancellationTokenSource cts;

    public async Task Run(Action action)
    {
        if (IsRunning)
        {
            throw new InvalidOperationException();
        }

        IsRunning = true;
        IsCanceled = false;

        p = new Progress<T>(OnProgressChanged);

        using (cts = new CancellationTokenSource())
        {
            try
            {
                await Task.Run(action, cts.Token);
            }
            catch (OperationCanceledException)
            {
                IsCanceled = true;
            }
            finally
            {
                IsRunning = false;
            }
        }
    }

    public void Report(T progress)
    {
        if (p != null)
        {
            ((IProgress<T>)p).Report(progress);
        }
    }

    public void Cancel()
    {
        cts?.Cancel();
    }

    public void ThrowIfCancellationRequested()
    {
        cts?.Token.ThrowIfCancellationRequested();
    }

    private void OnProgressChanged(T progress)
    {
        ProgressChanged?.Invoke(progress);
    }
}
