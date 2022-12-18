using System;
using System.Collections.Generic;

public class PriorityQueue<T> where T : IComparable
{
    private List<T> heap;

    public PriorityQueue()
    {
        heap = new List<T>();
    }

    public void Enqueue(T item)
    {
        heap.Add(item);
        int i = heap.Count - 1;
        while (i > 0 && heap[Parent(i)].CompareTo(heap[i]) > 0)
        {
            Swap(i, Parent(i));
            i = Parent(i);
        }
    }

    public T Dequeue()
    {
        if (heap.Count == 0)
        {
            throw new InvalidOperationException("Cannot dequeue from an empty priority queue");
        }

        T min = heap[0];
        heap[0] = heap[heap.Count - 1];
        heap.RemoveAt(heap.Count - 1);
        Heapify(0);
        return min;
    }

    public T Peek()
    {
        if (heap.Count == 0)
        {
            throw new InvalidOperationException("Cannot peek at an empty priority queue");
        }

        return heap[0];
    }

    public int Count
    {
        get
        {
            return heap.Count;
        }
    }

    private void Heapify(int i)
    {
        int left = LeftChild(i);
        int right = RightChild(i);
        int smallest = i;

        if (left < heap.Count && heap[left].CompareTo(heap[smallest]) < 0)
        {
            smallest = left;
        }

        if (right < heap.Count && heap[right].CompareTo(heap[smallest]) < 0)
        {
            smallest = right;
        }

        if (smallest != i)
        {
            Swap(i, smallest);
            Heapify(smallest);
        }
    }

    private void Swap(int i, int j)
    {
        T temp = heap[i];
        heap[i] = heap[j];
        heap[j] = temp;
    }

    private int Parent(int i)
    {
        return (i - 1) / 2;
    }

    private int LeftChild(int i)
    {
        return 2 * i + 1;
    }

    private int RightChild(int i)
    {
        return 2 * i + 2;
    }
}