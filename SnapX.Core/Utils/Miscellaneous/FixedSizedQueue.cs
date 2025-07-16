// SPDX-License-Identifier: GPL-3.0-or-later



namespace SnapX.Core.Utils.Miscellaneous;

public class FixedSizedQueue<T> : Queue<T>
{
    public int Size { get; private set; }

    public FixedSizedQueue(int size)
    {
        Size = size;
    }

    public new void Enqueue(T obj)
    {
        base.Enqueue(obj);

        while (Count > Size)
        {
            Dequeue();
        }
    }
}

