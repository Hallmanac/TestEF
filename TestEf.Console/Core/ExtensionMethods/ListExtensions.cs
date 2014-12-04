using System.Collections.Generic;
using System.Linq;

namespace TestEf.ConsoleMain.Core.ExtensionMethods
{
    public static class ListExtensions
    {
        public static List<List<T>> ToBatch<T>(this List<T> currentList, int batchSize)
        {
            var batchList = new List<List<T>>();
            var maxBatchCount = currentList.Count < batchSize ? currentList.Count : batchSize;
            var currentCount = 0;
            while(currentCount < currentList.Count)
            {
                var batch = new List<T>();
                batch.AddRange(currentList.Skip(currentCount).Take(maxBatchCount).ToList());
                batchList.Add(batch);
                currentCount += maxBatchCount;
            }
            return batchList;
        }
    }
}