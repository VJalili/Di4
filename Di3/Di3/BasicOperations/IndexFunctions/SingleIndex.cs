﻿using CSharpTest.Net.Collections;
using Polimi.DEIB.VahidJalili.IGenomics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace Polimi.DEIB.VahidJalili.DI3
{
    /// <summary>
    /// Provides efficient means of inserting an 
    /// interval to Di3; i.e., Di3 indexding.
    /// </summary>
    /// <typeparam name="C">Represents the c/domain
    /// type (e.g,. int, double, Time).</typeparam>
    /// <typeparam name="I">Represents generic type of the interval.
    /// (e.g., time span, interval on natural numbers)
    /// <para>For intervals of possibly different types,
    /// it is recommended to define this generic type
    /// parameter in terms of Lowest Common Denominator.
    /// </para></typeparam>
    /// <typeparam name="M">Represents generic
    /// type of pointer to descriptive hashKey cooresponding
    /// to the interval.</typeparam>
    internal class SingleIndex<C, I, M>
        where C : IComparable<C>, IFormattable
        where I : IInterval<C, M>
        where M : IMetaData, new()
    {
        /// <summary>
        /// Provides efficient means of inserting an 
        /// interval to Polimi.DEIB.VahidJalili.DI3; i.e., _di3_1R indexding.
        /// </summary>
        /// <param name="_di3_1R">The reference _di3_1R to be 
        /// manipulated.</param>
        internal SingleIndex(BPlusTree<C, IIB> di3)
        {
            _di3 = di3;
        }
        internal SingleIndex(BPlusTree<C, IIB> di3, List<I> intervals, int start, int stop, IndexingMode mode, ConcurrentDictionary<int, int> addedBookmarks)
        {
            _di3 = di3;
            _intervals = intervals;
            _start = start;
            _stop = stop;
            _mode = mode;
            _addedBookmarks = addedBookmarks;
            _bCounter = new BookmarkCounter();
            update.bookmarkCounter = _bCounter;
        }



        /// <summary>
        /// Sets and gets the _di3_1R data structure
        /// to be manipulated. This data structure
        /// is in common between all classes of 
        /// namespace.
        /// </summary>
        private BPlusTree<C, IIB> _di3 { set; get; }
        private IndexingMode _mode { set; get; }
        private int _start { set; get; }
        private int _stop { set; get; }
        /// <summary>
        /// Sets and Gets the interval to 
        /// be added to _di3_1R. 
        /// </summary>
        private I _interval { set; get; }
        private List<I> _intervals { set; get; }
        private ConcurrentDictionary<int, int> _addedBookmarks { set; get; }
        private BookmarkCounter _bCounter { set; get; }
        private AddUpdateValue update = new AddUpdateValue();


        public void Index()
        {
            int i;
            switch (_mode)
            {
                case IndexingMode.SinglePass:
                    for (i = _start; i < _stop; i++)
                    {
                        // default value of hashkey;
                        // shall be updated if the type
                        //  of hashkey is changed
                        if (_intervals[i].hashKey == 0)
                            continue;

                        Index(_intervals[i]);
                    }
                    break;

                case IndexingMode.MultiPass:
                    for (i = _start; i < _stop; i++)
                    {
                        // default value of hashkey;
                        // shall be updated if the type
                        //  of hashkey is changed
                        if (_intervals[i].hashKey == 0)
                            continue; 

                        update.atI = _intervals[i].hashKey;
                        update.iC = Phi.LeftEnd;
                        _di3.AddOrUpdate(_intervals[i].left, ref update);

                        update.iC = Phi.RightEnd;
                        _di3.AddOrUpdate(_intervals[i].right, ref update);
                    }
                    break;
            }
            _addedBookmarks.TryAdd(_start, _bCounter.value);
        }
        private void Index(I interval)
        {
            _interval = interval;
            bool isLeftEnd = true;
            bool enumerated = false;
            update.atI = _interval.hashKey;
            update.iC = Phi.LeftEnd;
            int compareResult;

            foreach (var item in _di3.EnumerateFrom(_interval.left))
            {
                enumerated = true;
                update.NextBookmark = null;

                if (isLeftEnd)
                {
                    if (HandleFirstItem(item)) break;
                    isLeftEnd = false;
                }
                else
                {
                    compareResult = _interval.right.CompareTo(item.Key);

                    if (compareResult == 0)
                    {
                        update.iC = Phi.RightEnd;
                        break;
                    }
                    else if (compareResult == 1)// interval.right is bigger than keyBookmark.Key
                    {
                        update.iC = Phi.Middle;
                        _di3.AddOrUpdate(item.Key, ref update);
                    }
                    else
                    {
                        update.iC = Phi.RightEnd;
                        update.NextBookmark = item.Value;
                        break;
                    }
                }

                /// this will be useful when the iteration reaches the 
                /// end of collection while right-end is not handled yet.
                update.iC = Phi.RightEnd;
            }

            if (enumerated)
            {
                _di3.AddOrUpdate(_interval.right, ref update);
            }
            else
            {
                update.NextBookmark = null;
                update.iC = Phi.LeftEnd;
                update.atI = _interval.hashKey;
                _di3.AddOrUpdate(_interval.left, ref update);

                update.iC = Phi.RightEnd;
                _di3.AddOrUpdate(_interval.right, ref update);
            }
        }

        public void SecondPass()
        {
            KeyValuePair<C, IIB> firstItem;
            _di3.TryGetFirst(out firstItem);

            int mu = 0;
            UInt16 omega = 0;
            ReadOnlyCollection<Lambda> currentBookmarkLambda = null;
            var t = new Dictionary<uint, bool>();
            KeyValueUpdate<C, IIB> updateFunction = delegate(C k, IIB i) { return i.Update(ref mu, ref omega, currentBookmarkLambda); };
            List<uint> keysToRemove = new List<uint>();

            foreach (var bookmark in _di3.EnumerateFrom(firstItem.Key))
            {
                // mu update option A:
                mu = t.Count - bookmark.Value.omega;

                foreach (var lambda in bookmark.Value.lambda)
                    if (!t.Remove(lambda.atI))
                        t.Add(lambda.atI, true);

                // mu update option B:
                // mu = t.Count - bookmark.Value.lambda.Count + bookmark.Value.omega; // ;-)
                if (bookmark.Value.mu != mu)
                {
                    omega = bookmark.Value.omega;
                    currentBookmarkLambda = bookmark.Value.lambda;
                    _di3.TryUpdate(bookmark.Key, updateFunction);
                }
            }
        }
        private bool UpdateRequired(ReadOnlyCollection<Lambda> lambda, Dictionary<uint, Lambda> lambdaCarrier)
        {
            if (lambda.Count != lambdaCarrier.Count) return true;
            foreach (var item in lambda)
                if (!lambdaCarrier.ContainsKey(item.atI))
                    return true;
            return false;
        }

        private bool HandleFirstItem(KeyValuePair<C, IIB> item)
        {
            if (_interval.left.Equals(item.Key))
            {
                _di3.AddOrUpdate(_interval.left, ref update);
                return false;
            }
            else
            {
                update.NextBookmark = item.Value;

                switch (_interval.right.CompareTo(item.Key))
                {
                    case 1: // _interval.right is bigger than item.Key
                        _di3.AddOrUpdate(_interval.left, ref update);

                        update.iC = Phi.Middle;
                        update.NextBookmark = null;
                        _di3.AddOrUpdate(item.Key, ref update);
                        return false;

                    case 0:
                    case -1:
                        _di3.AddOrUpdate(_interval.left, ref update);
                        update.iC = Phi.RightEnd;
                        return true;
                }
            }

            return true;
        }



        struct AddUpdateValue : ICreateOrUpdateValue<C, IIB>, IRemoveValue<C, IIB>
        {
            public IIB oldValue;
            public Phi iC { set; get; }
            public uint atI { set; get; }

            public BookmarkCounter bookmarkCounter { set; get; }

            public IIB NextBookmark { set; get; }

            public bool CreateValue(C key, out IIB value)
            {
                oldValue = null;

                if (NextBookmark == null)
                    value = new IIB(phi: iC, atI: atI);
                else
                    value = new IIB(phi: iC, atI: atI, nextBookmark: NextBookmark);

                // TODO 1 : check the commented-out parts
                //if (atI != 0)
                //{
                    bookmarkCounter.value++;
                    return true;
                //}
                //return false;
            }
            public bool UpdateValue(C key, ref IIB value)
            {
                oldValue = value;
                value = value.Update(atI: atI, condition: iC);
                //return atI != 0;
                return true; // TODO 1 : check this
            }
            public bool RemoveValue(C key, IIB value)
            {
                oldValue = value;

                if (value == value.Update(atI: atI, condition: iC))
                {
                    bookmarkCounter.value--;
                    return true;
                }
                return false;
            }
        }

        private class BookmarkCounter
        {
            public int value { set; get; }
        }
    }
}
