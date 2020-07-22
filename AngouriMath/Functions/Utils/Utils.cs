﻿
/* Copyright (c) 2019-2020 Angourisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy,
 * modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software
 * is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
 * CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AngouriMath.Core.Exceptions;
using AngouriMath.Core.Numerix;
using AngouriMath.Core.TreeAnalysis;
using AngouriMath.Functions;
using AngouriMath.Functions.Algebra.AnalyticalSolving;
using PeterO.Numbers;

namespace AngouriMath.Functions
{
    internal static class Utils
    {
        /// <summary>
        /// Sorts an expression into a polynomial.
        /// See more MathS.Utils.TryPolynomial
        /// </summary>
        /// <param name="expr"></param>
        /// <param name="variable"></param>
        /// <param name="dst"></param>
        /// <returns></returns>
        internal static bool TryPolynomial(Entity expr, VariableEntity variable,
            [System.Diagnostics.CodeAnalysis.NotNullWhen(true)]
            out Entity? dst)
        {
            dst = null;
            var children = TreeAnalyzer.LinearChildren(expr.Expand(), "sumf", "minusf", Const.FuncIfSum);
            var monomialsByPower = PolynomialSolver.GatherMonomialInformation<EInteger>(children, variable);
            if (monomialsByPower == null)
                return false;
            var newMonomialsByPower = new Dictionary<int, Entity>();
            var keys = monomialsByPower.Keys.ToList();
            keys.Sort((i, i1) => (i < i1 ? 1 : (i > i1 ? -1 : 0)));
            var terms = new List<Entity>();
            foreach (var index in keys)
            {
                var pair = new KeyValuePair<EInteger, Entity>(index, monomialsByPower[index]);
                Entity px;
                if (pair.Key.IsZero)
                {
                    terms.Add(pair.Value.Simplify());
                    continue;
                }

                if (pair.Key.Equals(EInteger.One))
                    px = variable;
                else
                    px = MathS.Pow(variable, IntegerNumber.Create(pair.Key));
                if (pair.Value == 1)
                {
                    terms.Add(px);
                    continue;
                }
                else
                    terms.Add(pair.Value.Simplify() * px);
            }

            if (terms.Count == 0)
                return false;
            dst = terms[0];
            for (int i = 1; i < terms.Count; i++)
                if (terms[i].Name == "mulf" &&
                    terms[i].Children[0] is NumberEntity { Value:RealNumber r }
                    && r < 0)
                    dst -= -r * terms[i].Children[1];
                else
                    dst += terms[i];
            dst = dst.InnerSimplify();
            return true;
        }


        /// <summary>
        /// Alike to ParseIndex, but strict on index: it should be a number
        /// </summary>
        /// <param name="name">
        /// Common name (e. g. "qua" or "phi_3")
        /// </param>
        /// <returns>
        /// (null, 0) if it's not a valid indexed-name with numeric index,
        /// (string prefix, int num) otherwise
        /// </returns>
        internal static (string? prefix, int num) ParseIndexNumeric(string name)
        {
            var (prefix, index) = ParseIndex(name);
            if (!(prefix is null) && int.TryParse(index, out var num))
                return (prefix, num);
            return (null, 0);
        }

        /// <summary>
        /// Extracts a variable's name and  index from its Name
        /// </summary>
        /// <param name="name">
        /// Common name (e. g. "qua" or "phi_3" or "qu_q")
        /// </param>
        /// <returns>
        /// If it contains _ and valid name and index, returns a pair of (string prefix, string index)
        /// (null, null) otherwise
        /// </returns>
        internal static (string? prefix, string? index) ParseIndex(string name)
        {
            var pos_ = name.IndexOf('_');
            if (pos_ != -1)
            {
                var varName = name.Substring(0, pos_);
                return (varName, name.Substring(pos_ + 1));
            }
            return (null, null);
        }

        /// <summary>
        /// Finds next var index name starting with 1, e. g.
        /// x + n_0 + n_a + n_3 + n_1
        /// will find n_2
        /// </summary>
        /// <param name="expr"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        internal static VariableEntity FindNextIndex(Entity expr, string prefix)
        {
            var indices = new HashSet<int>();
            foreach (var var in MathS.Utils.GetUniqueVariables(expr).FiniteSet())
            {
                var index = ParseIndexNumeric(var.Name);
                if (index.prefix == prefix)
                    indices.Add(index.num);
            }
            var i = 1;
            while (indices.Contains(i))
                i++;
            return new VariableEntity(prefix + "_" + i);
        }

        /// <summary>
        /// Finds greatest common divisor (with Euler's algorithm)
        /// </summary>
        /// <param name="a">
        /// First number
        /// </param>
        /// <param name="b">
        /// Second number
        /// </param>
        /// <returns>
        /// Returns such c that all are true
        /// a | c
        /// b | c
        /// !∃ d > c,
        ///     a | d
        ///     b | d
        /// </returns>
        private static EInteger _GCD(EInteger a, EInteger b)
        {
            a = a.Abs();
            b = b.Abs();
            while (a * b > 0)
            {
                if (a > b)
                    a %= b;
                else
                    b %= a;
            }

            return a.IsZero ? b : a;
        }

        private static long _GCD(long a, long b)
        {
            a = Math.Abs(a);
            b = Math.Abs(b);
            while (a * b > 0)
            {
                if (a > b)
                    a %= b;
                else
                    b %= a;
            }

            return a == 0 ? b : a;
        }

        /// <summary>
        /// Finds greatest common divisors of natural numbers
        /// </summary>
        /// <param name="numbers">
        /// Array of natural numbers
        /// </param>
        /// <returns>
        /// Greatest common divisor of numbers if numbers doesn't only consist of 0
        /// 1 otherwise
        /// </returns>
        internal static EInteger GCD(params EInteger[] numbers)
        {
            if (numbers.Length == 1)
                return numbers[0].IsZero ? 1 : numbers[0]; // technically, if number[0] == 0, then gcd = +oo
            if (numbers.Length == 2)
                return _GCD(numbers[0], numbers[1]);
            var rest = (new ArraySegment<EInteger>(numbers, 2, numbers.Length - 2)).ToList();
            rest.Add(_GCD(numbers[0], numbers[1]));
            return GCD(rest.ToArray());
        }

        internal static long GCD(params long[] numbers)
        {
            if (numbers.Length == 1)
                return numbers[0] == 0 ? 1 : numbers[0]; // technically, if number[0] == 0, then gcd = +oo
            if (numbers.Length == 2)
                return _GCD(numbers[0], numbers[1]);
            var rest = (new ArraySegment<long>(numbers, 2, numbers.Length - 2)).ToList();
            rest.Add(_GCD(numbers[0], numbers[1]));
            return GCD(rest.ToArray());
        }

        internal static EInteger LCM(params EInteger[] numbers)
        {
            if (numbers.Length == 1)
                return numbers[0];
            EInteger product = 1;
            foreach (var num in numbers)
                product = product.Multiply(num);
            return product.Divide(GCD(numbers));
        }
    }

    public class Setting<T> where T : notnull
    {
        private T currValue;
        internal Setting(T defaultValue)
        {
            currValue = defaultValue;
        }

        /// <summary>
        /// For example,
        /// MathS.Settings.Precision.As(100, () =>
        /// {
        /// // some code considering precision = 100
        /// });
        /// </summary>
        /// <param name="value">New value that will be automatically reverted after action is done</param>
        /// <param name="action">What should be done under this setting</param>
        public void As(T value, Action action)
        {
            var previousValue = currValue;
            currValue = value;
            lock (currValue) // TODO: it is probably impossible to access currValue from another thread since it's ThreadStatic
            {
                try
                {
                    action();
                }
                catch (Exception)
                {
                    currValue = previousValue;
                    throw;
                }
            }
            currValue = previousValue;
        }

        /// <summary>
        /// For example,
        /// var res = MathS.Settings.Precision.As(100, () =>
        /// {
        ///   // some code considering precision = 100
        ///   return 4;
        /// });
        /// </summary>
        /// <param name="value">New value that will be automatically reverted after action is done</param>
        /// <param name="action">What should be done under this setting</param>
        /// <returns></returns>
        public TReturnType As<TReturnType>(T value, Func<TReturnType> action)
        {
            var previousValue = currValue;
            currValue = value;
            TReturnType result;
            lock (currValue) // TODO: it is probably impossible to access currValue from another thread since it's ThreadStatic
            {
                try
                {
                    result = action();
                }
                catch
                {
                    currValue = previousValue;
                    throw;
                }
            }
            currValue = previousValue;
            return result;
        }

        public static implicit operator T(Setting<T> s)
        {
            return s.currValue;
        }

        public static implicit operator Setting<T>(T a)
        {
            return new Setting<T>(a);
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public T Value => currValue;
    }
}

namespace AngouriMath
{
    public static partial class MathS
    {
        public static partial class Settings
        {
            [ThreadStatic]
            private static Setting<bool>? downcastingEnabled;
            [ThreadStatic]
            private static Setting<int>? floatToRationalIterCount;
            [ThreadStatic]
            private static Setting<EInteger>? maxAbsNumeratorOrDenominatorValue;
            [ThreadStatic]
            private static Setting<EDecimal>? precisionErrorCommon;
            [ThreadStatic] 
            private static Setting<EDecimal>? precisionErrorZeroRange;
            [ThreadStatic]
            private static Setting<bool>? allowNewton;
            [ThreadStatic]
            private static Setting<Func<Entity, int>>? complexityCriteria;
            [ThreadStatic]
            private static Setting<NewtonSetting>? newtonSolver;
            [ThreadStatic]
            private static Setting<int>? maxExpansionTermCount;
            [ThreadStatic]
            private static Setting<EContext>? decimalPrecisionContext;
            private static Setting<T> GetCurrentOrDefault<T>(ref Setting<T>? setting, T defaultValue) where T : notnull
            {
                if (setting is null)
                    setting = defaultValue;
                return setting;
            }
        }
    }
}