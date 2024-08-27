using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Test
{
    public class Customer
    {
        private static readonly Lazy<Customer> lazy = new Lazy<Customer>(() => new Customer(), LazyThreadSafetyMode.ExecutionAndPublication);
        public static Customer Instance
        {
            get
            {
                return lazy.Value;
            }
        }
        private Customer()
        {
            _scores.AddRange(initialData);
        }


        private readonly ConcurrentDictionary<long, decimal> _scores = new ConcurrentDictionary<long, decimal>();
        private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();
        private bool _isCacheValid;
        private List<LeaderboardCustomer>? _cachedLeaderboard;
        IEnumerable<KeyValuePair<long, decimal>> initialData = new[]
        {
            new KeyValuePair<long, decimal>(15514665, 124),
            new KeyValuePair<long, decimal>(81546541, 113),
            new KeyValuePair<long, decimal>(1745431, 100),
            new KeyValuePair<long, decimal>(76786448, 100),
            new KeyValuePair<long, decimal>(254814111, 96),
            new KeyValuePair<long, decimal>(53274324, 95),
            new KeyValuePair<long, decimal>(6144320, 93),
            new KeyValuePair<long, decimal>(8009471, 93),
            new KeyValuePair<long, decimal>(11028481, 93),
            new KeyValuePair<long, decimal>(38819, 92),
         };

        
        public decimal UpdateCustomerScore(long customerId, decimal scoreDelta)
        {
            if (scoreDelta < -1000 || scoreDelta > 1000)
            {
                throw new ArgumentException("Score delta must be within [-1000, +1000].");
            }

            decimal newScore = _scores.AddOrUpdate(customerId, scoreDelta, (id, oldScore) => oldScore + scoreDelta);

            _cacheLock.EnterWriteLock();
            try
            {
                _isCacheValid = false;
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }

            return newScore;
        }

        
        //public List<LeaderboardCustomer> GetLeaderboard()
        //{
        //    _cacheLock.EnterReadLock();
        //    try
        //    {
        //        if (_isCacheValid)
        //        {
        //            // 返回缓存的排行榜（如果可用且最新）  
        //            return _cachedLeaderboard;
        //        }
        //    }
        //    finally
        //    {
        //        _cacheLock.ExitReadLock();
        //    }

        //    return updateCachedLeaderboard(); 

        //}

        
        public List<LeaderboardCustomer> GetCustomersByRank(int start, int end)
        {
            if (start < 1 || end < start)
            {
                throw new ArgumentException("Invalid start or end rank.");
            }

            _cacheLock.EnterReadLock();
            try
            {
                if (_isCacheValid)
                { 
                    return _cachedLeaderboard.Skip(start - 1) 
                                             .Take(end - start + 1) 
                                             .ToList(); 
                }
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }

            return updateCachedLeaderboard().Skip(start - 1)
                                            .Take(end - start + 1)
                                            .ToList();
        }

        public List<LeaderboardCustomer> GetCustomerWithNeighborsById(long customerId, int high = 0, int low = 0)
        {
            if (high < 0 || low < 0)
            {
                throw new ArgumentException("Number of neighbors cannot be negative.");
            }
            if (!_scores.TryGetValue(customerId, out decimal score) || score < 0)
            {
                throw new KeyNotFoundException($"Customer with ID {customerId} not in leaderboard.");
            }

            _cacheLock.EnterReadLock();
            try
            {
                if (_isCacheValid)
                {
                    var _customer= _cachedLeaderboard.FirstOrDefault(pair => pair.CustomerId == customerId);
                    var _startIndex = Math.Max(0, _customer.Rank - high - 1);
                    var _endIndex = Math.Min(_cachedLeaderboard.Count - 1, _customer.Rank + low - 1); 

                    return _cachedLeaderboard.GetRange(_startIndex, _endIndex - _startIndex + 1);
                }
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
            var leaderboard = updateCachedLeaderboard();

            var customer = leaderboard.FirstOrDefault(pair => pair.CustomerId == customerId);
            var startIndex = Math.Max(0, customer.Rank - high - 1);
            var endIndex = Math.Min(leaderboard.Count - 1, customer.Rank + low - 1);

            return leaderboard.GetRange(startIndex, endIndex - startIndex + 1);
        }


        private List<LeaderboardCustomer> updateCachedLeaderboard()
        {
            var sortedScores = _scores
                .Where(score => score.Value > 0)
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key)
                .Select((pair, index) => new LeaderboardCustomer { CustomerId = pair.Key, Score = pair.Value, Rank = index + 1 })
                .ToList();

            
            _cacheLock.EnterWriteLock();
            try
            {
                _cachedLeaderboard = sortedScores;
                _isCacheValid = true;
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
            return sortedScores;
        }
    }
    public static class ConcurrentDictionaryExtensions
    {
        public static void AddRange<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs)
        {
            foreach (var kvp in keyValuePairs)
            {
                dictionary.TryAdd(kvp.Key, kvp.Value);
            }
        }
    }
}
