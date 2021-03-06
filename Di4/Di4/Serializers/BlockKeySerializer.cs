﻿using CSharpTest.Net.Serialization;
using System;

namespace Polimi.DEIB.VahidJalili.DI4
{
    internal class BlockKeySerializer<C> : ISerializer<BlockKey<C>>
        where C : IComparable<C>, IFormattable
    {
        public BlockKeySerializer(ISerializer<C> coordinateSerializer)
        {
            _coordinateSerializer = coordinateSerializer;
        }

        private ISerializer<C> _coordinateSerializer { set; get; }

        public BlockKey<C> ReadFrom(System.IO.Stream stream)
        {
            return new BlockKey<C>(
                _coordinateSerializer.ReadFrom(stream),
                _coordinateSerializer.ReadFrom(stream));
        }

        public void WriteTo(BlockKey<C> value, System.IO.Stream stream)
        {
            _coordinateSerializer.WriteTo(value.leftEnd, stream);
            _coordinateSerializer.WriteTo(value.rightEnd, stream);
        }
    }
}
