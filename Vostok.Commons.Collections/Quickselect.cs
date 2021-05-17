using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Vostok.Commons.Collections
{
    [PublicAPI]
    public static class Quickselect
    {
        public static T QuickSelect<T>(this T[] list, int top, IComparer<T> comparer = null, QuickselectSortOrder order = QuickselectSortOrder.Ascending)
        {
            if (top > list.Length)
                throw new InvalidOperationException($"Requested to select top {top} items from array with size {list.Length}.");

            if (top == list.Length)
                return list[top - 1];

            if (top <= 0)
                return list[0];
            
            if (comparer == null)
                comparer = Comparer<T>.Default;

            var startIndex = 0;
            var endIndex = list.Length - 1;
            var pivotIndex = top;

            while (endIndex > startIndex)
            {
                pivotIndex = list.Partition(startIndex, endIndex, pivotIndex, comparer, order);

                if (pivotIndex == top)
                    break;

                if (pivotIndex > top)
                    endIndex = pivotIndex - 1;
                else
                    startIndex = pivotIndex + 1;

                pivotIndex = ThreadSafeRandom.Next(startIndex, endIndex);
            }

            return list[pivotIndex];
        }

        private static int Partition<T>(this T[] list, int startIndex, int endIndex, int pivotIndex, IComparer<T> comparer, QuickselectSortOrder order)
        {
            Swap(list, pivotIndex, startIndex);

            var pivot = list[startIndex];
            var i = startIndex;
            var j = endIndex + 1;

            while (true)
            {
                do
                {
                    i++;
                } while (i <= endIndex && comparer.Compare(list[i], pivot) * (int)order < 0);

                do
                {
                    j--;
                } while (comparer.Compare(list[j], pivot) * (int)order > 0);

                if (i >= j)
                {
                    Swap(list, startIndex, j);
                    return j;
                }

                Swap(list, i, j);
            }
        }

        private static void Swap<T>(this T[] list, int index1, int index2)
        {
            var temp = list[index1];
            list[index1] = list[index2];
            list[index2] = temp;
        }
    }
}