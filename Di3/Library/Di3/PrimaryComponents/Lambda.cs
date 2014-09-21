﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IInterval;
using ICPMD;
using ProtoBuf;

namespace DI3
{
    /// <summary>
    /// Represents the interval intersecting with 
    /// the c of corresponding block. 
    /// <para>For intervals of possibly different types,
    /// it is recommended to define this generic type
    /// parameter in terms of Lowest Common Denominator.
    /// </para>
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
    /// type of pointer to descriptive metadata cooresponding
    /// to the interval.</typeparam>
    [ProtoContract]
    public class Lambda<C, M>//I, M>
        where C : IComparable<C>
        //where I : IInterval<C, M>
        where M : IMetaData<C>
    {
        /// <summary>
        /// Represents the interval intersecting with 
        /// the c of corresponding block. 
        /// <para>For intervals of possibly different types,
        /// it is recommended to define this generic type
        /// parameter in terms of Lowest Common Denominator.
        /// </para>
        /// </summary>
        internal Lambda() { }


        /// <summary>
        /// Represents the interval intersecting with 
        /// the c of corresponding block. 
        /// <para>For intervals of possibly different types,
        /// it is recommended to define this generic type
        /// parameter in terms of Lowest Common Denominator.
        /// </para>
        /// </summary>
        /// <param name="tau">The intersection type of interval
        /// wtih c of corresponding block.</param>
        /// <param name="atI">Descriptive metadata of the intereval.</param>
        internal Lambda(char tau, M atI)
        {
            this.tau = tau;
            this.atI = atI;
        }


        /// <summary>
        /// Gets the intersection type of interval
        /// wtih c of corresponding block.
        /// <para>[value] = L  ::>  Left-end  intersecting the coordiante.</para>
        /// <para>[value] = M  ::>  Middle    intersecting the coordiante.</para>
        /// <para>[value] = R  ::>  Right-end intersecting the coordiante.</para>
        /// </summary>
        [ProtoMember(1)]
        internal char tau { private set; get; }


        /// <summary>
        /// Gets descriptive metadata of the intereval
        /// represented by generic type M.
        /// </summary>
        [ProtoMember(2)]
        internal M atI { private set; get; }
    }
}
