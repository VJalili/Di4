﻿using System;
using System.Collections.Generic;
using Polimi.DEIB.VahidJalili.IGenomics;
using CSharpTest.Net.Collections;
using System.Collections;
using System.Collections.ObjectModel;

namespace Polimi.DEIB.VahidJalili.DI4.Inv
{
    internal class CoverSummit<C, I, M, O>
        where C : IComparable<C>, IFormattable
        where I : IInterval<C, M>
        where M : IMetaData, new()
    {
        internal CoverSummit(
            object lockOnMe,
            BPlusTree<C, B> di4)
        {
            _di4_1R = di4;
            _lockOnMe = lockOnMe;
            _lambdas = new HashSet<uint>();
        }

        internal CoverSummit(
            object lockOnMe,
            BPlusTree<C, B> di4_1R,
            IOutput<C, I, M, O> outputStrategy,
            List<I> intervals,
            int start,
            int stop)
        {
            _stop = stop;
            _start = start;
            _di4_1R = di4_1R;
            _lockOnMe = lockOnMe;
            _intervals = intervals;
            _lambdas = new HashSet<uint>();
            _outputStrategy = outputStrategy;
        }

        internal CoverSummit(
            object lockOnMe,
            BPlusTree<C, B> di4_1R,
            BPlusTree<BlockKey<C>, BlockValue> di4_2R,
            IOutput<C, I, M, O> outputStrategy,
            BlockKey<C> left,
            BlockKey<C> right,
            int minAcc,
            int maxAcc)
        {   
            _left = left;
            _right = right;
            _di4_1R = di4_1R;
            _di4_2R = di4_2R;
            _minAcc = minAcc;
            _maxAcc = maxAcc;
            _lockOnMe = lockOnMe;
            _lambdas = new HashSet<uint>();
            _outputStrategy = outputStrategy;
        }

        private BPlusTree<C, B> _di4_1R { set; get; }
        private BPlusTree<BlockKey<C>, BlockValue> _di4_2R { set; get; }
        private int _start { set; get; }
        private int _stop { set; get; }
        private BlockKey<C> _left { set; get; }
        private BlockKey<C> _right { set; get; }
        private int _minAcc { set; get; }
        private int _maxAcc { set; get; }
        private List<I> _intervals { set; get; }
        private IOutput<C, I, M, O> _outputStrategy { set; get; }
        internal IOutput<C, I, M, O> outputStrategy { get { return _outputStrategy; } }
        private HashSet<uint> _lambdas { set; get; }
        private object _lockOnMe { set; get; }

        internal void Cover()
        {
            foreach (var block in _di4_2R.EnumerateRange(_left, _right))
                if (_minAcc <= block.Value.boundariesUpperBound)
                    _Cover(block.Key.leftEnd, block.Key.rightEnd);
        }
        private void _Cover(C left, C right)
        {
            C markedKey = default(C);
            int markedAcc = -1;
            byte accumulation = 0;
            _lambdas.Clear();

            foreach (var bookmark in _di4_1R.EnumerateRange(left, right))
            {
                accumulation = (byte)(bookmark.Value.lambda.Count - bookmark.Value.omega);

                if (markedAcc == -1 &&
                    accumulation >= _minAcc &&
                    accumulation <= _maxAcc)
                {
                    markedKey = bookmark.Key;
                    markedAcc = accumulation;
                    UpdateLambdas(bookmark.Value.lambda);
                }
                else if (markedAcc != -1)
                {
                    if (accumulation < _minAcc ||
                        accumulation > _maxAcc)
                    {
                        UpdateLambdas(bookmark.Value.lambda);
                        _outputStrategy.Output(left: markedKey, right: bookmark.Key, intervals: new List<uint>(_lambdas), lockOnMe: _lockOnMe);

                        markedKey = default(C);
                        markedAcc = -1;
                        _lambdas.Clear();
                    }
                    else if (accumulation >= _minAcc &&
                        accumulation <= _maxAcc)
                    {
                        UpdateLambdas(bookmark.Value.lambda);
                    }
                }
            }
        }

        internal void Summit()
        {
            foreach (var block in _di4_2R.EnumerateRange(_left, _right))
                if (_minAcc <= block.Value.boundariesUpperBound)
                    _Summit(block.Key.leftEnd, block.Key.rightEnd);
        }
        private void _Summit(C left, C right)
        {
            C markedKey = default(C);
            int markedAcc = -1;
            byte accumulation = 0;
            _lambdas.Clear();

            foreach (var bookmark in _di4_1R.EnumerateRange(left, right))
            {
                accumulation = (byte)(bookmark.Value.lambda.Count - bookmark.Value.omega);

                if (markedAcc < accumulation &&
                    accumulation >= _minAcc &&
                    accumulation <= _maxAcc)
                {
                    markedKey = bookmark.Key;
                    markedAcc = accumulation;
                    UpdateLambdas(bookmark.Value.lambda);
                }
                else if (markedAcc > accumulation ||
                    (markedAcc < accumulation && (
                    accumulation < _minAcc ||
                    accumulation > _maxAcc) &&
                    markedAcc != -1))
                {
                    UpdateLambdas(bookmark.Value.lambda);
                    _outputStrategy.Output(left: markedKey, right: bookmark.Key, intervals: new List<uint>(_lambdas), lockOnMe: _lockOnMe);

                    markedKey = default(C);
                    markedAcc = -1;
                    _lambdas.Clear();
                }
                else if (accumulation >= _minAcc &&
                    accumulation <= _maxAcc &&
                    markedAcc != -1)
                {
                    UpdateLambdas(bookmark.Value.lambda);
                }
            }
        }

        private void UpdateLambdas(ReadOnlyCollection<Lambda> lambdas)
        {
            foreach (var lambda in lambdas)
                if(!_lambdas.Contains(lambda.atI))
                    _lambdas.Add(lambda.atI);
        }
    }
}
