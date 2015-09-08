﻿using CSharpTest.Net.Collections;
using Polimi.DEIB.VahidJalili.IGenomics;
using System;
using System.Collections.Concurrent;

namespace Polimi.DEIB.VahidJalili.DI3
{
    /// <summary>
    /// Second resolution index.
    /// </summary>
    internal class SingleIndex2R<C, I, M>
        where C : IComparable<C>, IFormattable
        where I : IInterval<C, M>
        where M : IMetaData, new()
    {
        internal SingleIndex2R(BPlusTree<C, IIB> di31R, BPlusTree<BlockKey<C>, BlockValue> di32R, C left, C right, ConcurrentDictionary<C, int> addedBlocks)
        {
            _di31R = di31R;
            _di32R = di32R;
            _left = left;
            _right = right;
            _addedBlocks = addedBlocks;
            _bCounter = new BlockCounter();
        }

        private BPlusTree<C, IIB> _di31R { set; get; }
        private BPlusTree<BlockKey<C>, BlockValue> _di32R { set; get; }
        private C _left { set; get; }
        private C _right { set; get; }
        private ConcurrentDictionary<C, int> _addedBlocks { set; get; }
        private BlockCounter _bCounter { set; get; }


        public void Index()
        {
            int maxAccumulation = 0;
            int distinctIntervalsCount = 0;
            C currentBlockLeftEnd = _left;
            bool startNewBlock = true;
            foreach (var bookmark in _di31R.EnumerateRange(_left, _right))
            {
                maxAccumulation = Math.Max(maxAccumulation, bookmark.Value.mu + bookmark.Value.lambda.Count - bookmark.Value.omega);
                distinctIntervalsCount += bookmark.Value.lambda.Count - bookmark.Value.omega;

                if (startNewBlock)
                {
                    currentBlockLeftEnd = bookmark.Key;
                    startNewBlock = false;
                    continue;
                }

                if (bookmark.Value.lambda.Count == bookmark.Value.omega && bookmark.Value.mu == 0)
                {
                    Update(currentBlockLeftEnd, bookmark.Key, maxAccumulation, distinctIntervalsCount);
                    maxAccumulation = 0;
                    distinctIntervalsCount = 0;
                    startNewBlock = true;
                }
            }

            _addedBlocks.TryAdd(_left, _bCounter.value);
        }

        private void Update(C leftEnd, C rightEnd, int maxAccumulation, int count)
        {
            /// lambda is an element of di3_1R that intersects newKey.
            var newKey = new BlockKey<C>(leftEnd, rightEnd);
            var newValue = new BlockValue(maxAccumulation, count);

            foreach (var item in _di32R.EnumerateFrom(newKey))
            {
                /// The same key already exist.
                if (newKey.leftEnd.CompareTo(item.Key.leftEnd) == 0 &&
                    newKey.rightEnd.CompareTo(item.Key.rightEnd) == 0)
                    return;

                /// "lambda" occures after "newKey" and does not intersect with it.
                if (newKey.rightEnd.CompareTo(item.Key.leftEnd) == -1) // newKey.rightEnd < lambda.key.start
                    break;

                /// The keyBookmark that is already in di3_1R covers new interval,
                /// therefore no update is required.
                if (newKey.leftEnd.CompareTo(item.Key.leftEnd) == 1 &&  // newKey.LeftEnd > lambda.newKey.LeftEnd
                    newKey.rightEnd.CompareTo(item.Key.rightEnd) == -1) // newKey.rightEnd < lambda.newKey.rightEnd
                    return;

                /// Theoretically, these conditions may not be needed ever !!
                //if (newKey.start.CompareTo(lambda.Key.start) == 1) // newKey.start > lambda.newKey.start
                    //newKey = newKey.UpdateLeft(LeftEnd: lambda.Key.start);
                //if (newKey.rightEnd.CompareTo(lambda.Key.rightEnd) == -1) // newKey.rightEnd < lambda.newKey.rightEnd
                    //newKey = newKey.UpdateRight(RightEnd: lambda.Key.rightEnd);

                _bCounter.value--;
                _di32R.Remove(item.Key);


                /// yeah, true ;-) process only one lambda. 
                /// maybe there would be a better way to do this. 
                /// possibly using: _di3_2R.EnumerateFrom(newKey).GetEnumerator().Current
                /// we can do this iteration. But GetEnumerator throws an exception when tree
                /// is empty, althougth that can be handled by a try-catch-finally but I guess
                /// this method is more clean ;-)
                break;
            }
            _bCounter.value++;
            _di32R.TryAdd(newKey, newValue);
        }

        private class BlockCounter
        {
            public int value { set; get; }
        }
    }
}
