
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Collections.Concurrent;
using SixLabors.ImageSharp;

namespace SnapX.Core.ScreenCapture.ScreenRecording;

public abstract class ImageCache : IDisposable
{
    public bool IsWorking { get; protected set; }
    public ScreenRecordingOptions Options { get; set; }

    protected Thread task;
    protected BlockingCollection<Image> imageQueue;

    public ImageCache()
    {
        imageQueue = [];
    }

    public void AddImageAsync(Image img)
    {
        if (!IsWorking)
        {
            StartConsumerThread();
        }

        imageQueue.Add(img);
    }

    protected virtual void StartConsumerThread()
    {
        if (!IsWorking)
        {
            IsWorking = true;

            task = new Thread(() =>
            {
                try
                {
                    while (!imageQueue.IsCompleted)
                    {
                        Image img = null;

                        try
                        {
                            img = imageQueue.Take();

                            if (img != null)
                            {
                                //using (new DebugTimer("WriteFrame"))
                                WriteFrame(img);
                            }
                        }
                        catch (InvalidOperationException)
                        {
                        }
                        finally
                        {
                            if (img != null) img.Dispose();
                        }
                    }
                }
                finally
                {
                    IsWorking = false;
                }
            });

            task.Start();
        }
    }

    protected abstract void WriteFrame(Image img);

    public void Finish()
    {
        if (IsWorking)
        {
            imageQueue.CompleteAdding();
            task.Join();
        }

        Dispose();
    }

    public virtual void Dispose()
    {
        if (imageQueue != null)
        {
            imageQueue.Dispose();
        }
    }
}
