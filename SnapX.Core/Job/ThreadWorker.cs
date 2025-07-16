// SPDX-License-Identifier: GPL-3.0-or-later



namespace SnapX.Core.Job;

public class ThreadWorker
{
    public event Action DoWork;
    public event Action Completed;

    private SynchronizationContext context;
    private Thread thread;

    public ThreadWorker()
    {
        context = SynchronizationContext.Current ?? new SynchronizationContext();
    }

    public void Start()
    {
        if (thread == null)
        {
            thread = new Thread(WorkThread);
            thread.IsBackground = true;
            thread.Start();
        }
    }

    private void WorkThread()
    {
        OnDoWork();
        OnCompleted();
    }

    private void OnDoWork()
    {
        DoWork?.Invoke();
    }

    private void OnCompleted()
    {
        if (Completed != null)
        {
            InvokeAsync(Completed);
        }
    }

    public void Invoke(Action action)
    {
        context.Send(state => action(), null);
    }

    public void InvokeAsync(Action action)
    {
        context.Post(state => action(), null);
    }
}

