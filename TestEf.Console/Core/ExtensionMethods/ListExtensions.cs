using System.Collections.Generic;

namespace TestEf.Console.Core.ExtensionMethods
{
    public static class ListExtensions
    {
        public static List<List<T>> ToBatch<T>(this List<T> currentList, int batchSize)
        {
            var batchList = new List<List<T>>();

            var maxBatchCount = currentList.Count < batchSize ? currentList.Count : batchSize;
            var remainingCount = currentList.Count;
            while (remainingCount != 0)
            {
                var batch = new List<T>();
                for (var i = remainingCount; i > remainingCount - maxBatchCount; i--)
                {
                    batch.Add(currentList[i - 1]);
                }
                batchList.Add(batch);
                remainingCount = remainingCount - maxBatchCount;
                if (maxBatchCount > remainingCount)
                    maxBatchCount = remainingCount;
            }
            return batchList;
        }  
    }
}