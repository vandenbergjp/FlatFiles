﻿using System;

namespace FlatFiles
{
    internal sealed class CircularQueue<T>
    {
        private T[] items;
        private int front;
        private int back;

        public int Count { get; private set; }

        public void Enqueue(T item)
        {
            if (items == null)
            {
                items = new T[10];
            }
            else if (Count == items.Length)
            {
                T[] newItems = new T[Count * 2];
                int middle = Count - front;
                Array.Copy(items, front, newItems, 0, middle);
                Array.Copy(items, 0, newItems, middle, front);
                front = 0;
                back = Count;
                items = newItems;
            }
            items[back] = item;
            ++back;
            if (back == items.Length)
            {
                back = 0;
            }
            ++Count;
        }

        public void EnqueueRange(T[] values, int valuesLength)
        {
            if (valuesLength == 0)
            {
                return;
            }
            if (items == null)
            {
                items = new T[valuesLength];
                Array.Copy(values, 0, items, 0, valuesLength);
                Count += valuesLength;
            }
            else if (Count + valuesLength > items.Length)
            {
                T[] newItems = new T[Math.Max(Count + valuesLength, items.Length * 2)];
                if (back >= front)
                {
                    int length = back - front;
                    Array.Copy(items, front, newItems, 0, length);
                    Array.Copy(values, 0, newItems, length, valuesLength);
                }
                else
                {
                    int length = items.Length - front;
                    Array.Copy(items, front, newItems, 0, length);
                    Array.Copy(items, 0, newItems, length, back);
                    Array.Copy(values, 0, newItems, Count, valuesLength);
                }
                items = newItems;
                front = 0;
                Count += valuesLength;
                back = Count;
            }
            else if (back >= front)
            {
                int length = items.Length - back;
                if (length > valuesLength)
                {
                    length = valuesLength;
                }
                Array.Copy(values, 0, items, back, length);
                Array.Copy(values, length, items, 0, valuesLength - length);
                Count += valuesLength;
                back += valuesLength;
                if (back >= items.Length)
                {
                    back -= items.Length;
                }
            }
            else
            {
                Array.Copy(values, 0, items, back, valuesLength);
                Count += valuesLength;
                back += valuesLength;
            }
        }

        public T Peek()
        {
            return items[front];
        }

        public T Peek(int index)
        {
            int position = front + index;
            if (position >= items.Length)
            {
                position -= items.Length;
            }
            return items[position];
        }

        public void Dequeue()
        {
            ++front;
            if (front == items.Length)
            {
                front = 0;
            }
            --Count;
        }

        public void Dequeue(int count)
        {
            int remaining = count;
            while (remaining != 0)
            {
                ++front;
                if (front == items.Length)
                {
                    front = 0;
                }
                --remaining;
            }
            Count -= count;
        }
    }
}