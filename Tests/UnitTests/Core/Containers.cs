﻿using AngouriMath;
using AngouriMath.Core;
using AngouriMath.Extensions;
using System.Collections.Generic;
using Xunit;

namespace UnitTests.Core
{
    public sealed class Containers
    {
        [Fact]
        public void TestSimple1()
        {
            var container = new Container<int>(() => 4);
            Assert.Equal(4, container.Value);
            Assert.Equal(4, container.Value);
        }

        [Fact]
        public void TestSimpleIntNullable1()
        {
            var container = new Container<int?>(() => null);
            Assert.Null(container.Value);
            Assert.Null(container.Value);
        }

        [Fact]
        public void TestSimpleIntNullable2()
        {
            var container = new Container<int?>(() => 3);
            Assert.NotNull(container.Value);
            Assert.NotNull(container.Value);
        }

        [Fact]
        public void TestSimpleString()
        {
            var container = new Container<string>(() => "ss");
            Assert.Equal("ss", container.Value);
            Assert.Equal("ss", container.Value);
        }

        [Fact]
        public void TestSimpleStringNullable()
        {
            var container = new Container<string?>(() => null);
            Assert.Null(container.Value);
            Assert.Null(container.Value);
        }

        private record SomeTestRecord
        {
            public Dictionary<string, string> Dict => dict.Value;
            private Container<Dictionary<string, string>> dict = new(() => new());
        }

        [Fact]
        public void TestThreadSafety()
        {
            SomeTestRecord someInstance = new SomeTestRecord();

            void ChangeADict(int threadId)
            {
                someInstance.Dict["someSpecificKey"] = threadId.ToString();
            }

            new ThreadingChecker(ChangeADict).Run(iterCount: 10000);
        }
    }
}
