using System;
using System.Collections.Generic;

public class Heap<T> where T : IHeapItem<T> {
    T[] items;
    int currentItemCount;

    public Heap(int maxSize) {
        items = new T[maxSize];
    }

    public void Add(T item) {
        item.HeapIndex = currentItemCount;
        items[currentItemCount] = item;
        SortUp(item);
        currentItemCount++;
    }

    public T RemoveFirst() {
        T first = items[0];
        currentItemCount--;
        items[0] = items[currentItemCount];
        items[0].HeapIndex = 0;
        SortDown(items[0]);
        return first;
    }

    void SortDown(T item) {
        while (true) {
            int childLeft = item.HeapIndex * 2 + 1;
            int childRight = item.HeapIndex * 2 + 2;
            int swapIndex = 0;

            if (childLeft < currentItemCount) {
                swapIndex = childLeft;
                if (childRight < currentItemCount && items[childLeft].CompareTo(items[childRight]) < 0)
                    swapIndex = childRight;

                if (item.CompareTo(items[swapIndex]) < 0)
                    Swap(item, items[swapIndex]);
                else
                    return;
            }
            else return;
        }
    }

    void SortUp(T item) {
        int parentIndex = (item.HeapIndex - 1) / 2;
        while (true) {
            T parent = items[parentIndex];
            if (item.CompareTo(parent) > 0)
                Swap(item, parent);
            else
                break;
            parentIndex = (item.HeapIndex - 1) / 2;
        }
    }

    void Swap(T a, T b) {
        items[a.HeapIndex] = b;
        items[b.HeapIndex] = a;
        (a.HeapIndex, b.HeapIndex) = (b.HeapIndex, a.HeapIndex);
    }

    public int Count => currentItemCount;
    public bool Contains(T item) => Equals(items[item.HeapIndex], item);
}

public interface IHeapItem<T> : IComparable<T> {
    int HeapIndex { get; set; }
}