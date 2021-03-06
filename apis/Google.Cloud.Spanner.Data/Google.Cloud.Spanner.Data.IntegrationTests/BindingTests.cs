﻿// Copyright 2017 Google Inc. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#region

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Google.Cloud.Spanner.Data.IntegrationTests
{
    [Collection(nameof(TestDatabaseFixture))]
    public class BindingTests
    {
        // ReSharper disable once UnusedParameter.Local
        public BindingTests(TestDatabaseFixture testFixture, ITestOutputHelper outputHelper)
        {
            _testFixture = testFixture;
#if LoggingOn
            SpannerConnection.ConnectionPoolOptions.LogLevel = LogLevel.Debug;
            SpannerConnection.ConnectionPoolOptions.LogPerformanceTraces = true;
            SpannerConnection.ConnectionPoolOptions.PerformanceTraceLogInterval = 1000;
#endif
            TestLogger.TestOutputHelper = outputHelper;
        }

        private readonly TestDatabaseFixture _testFixture;

        public static TheoryData<SpannerDbType> BindNullData { get; } = new TheoryData<SpannerDbType>
        {
            SpannerDbType.Bool,
            SpannerDbType.String,
            SpannerDbType.Int64,
            SpannerDbType.Timestamp,
            SpannerDbType.Float64,
            SpannerDbType.Date,
            SpannerDbType.Bytes,
            SpannerDbType.ArrayOf(SpannerDbType.Bool),
            SpannerDbType.ArrayOf(SpannerDbType.String),
            SpannerDbType.ArrayOf(SpannerDbType.Int64),
            SpannerDbType.ArrayOf(SpannerDbType.Timestamp),
            SpannerDbType.ArrayOf(SpannerDbType.Float64),
            SpannerDbType.ArrayOf(SpannerDbType.Date),
            SpannerDbType.ArrayOf(SpannerDbType.Bytes),
        };        

        [Theory]
        [MemberData(nameof(BindNullData))]
        public async Task BindNull(SpannerDbType parameterType)
        {
            using (var connection = await _testFixture.GetTestDatabaseConnectionAsync())
            {
                var cmd = connection.CreateSelectCommand(
                    "SELECT @v",
                    new SpannerParameterCollection { new SpannerParameter("v", parameterType) });

                cmd.Parameters["v"].Value = null;
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    Assert.True(await reader.ReadAsync());

                    Assert.True(reader.IsDBNull(0));
                    Assert.Equal(DBNull.Value, reader.GetValue(0));

                    Assert.False(await reader.ReadAsync());
                }
            }
        }

        private async Task TestBindNonNull<T>(SpannerDbType parameterType, T value, Func<SpannerDataReader, T> typeSpecificReader = null)
        {
            int rowsRead;
            var valueRead = default(T);

            using (var connection = await _testFixture.GetTestDatabaseConnectionAsync())
            {
                var cmd = connection.CreateSelectCommand(
                    "SELECT @v",
                    new SpannerParameterCollection {new SpannerParameter("v", parameterType)});

                cmd.Parameters["v"].Value = value;
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    rowsRead = 0;
                    while (await reader.ReadAsync())
                    {
                        valueRead = reader.GetFieldValue<T>(0);

                        // optional extra test for certain built in types
                        if (typeSpecificReader != null)
                        {
                            Assert.Equal(typeSpecificReader(reader), valueRead);
                            // ReSharper disable once CompareNonConstrainedGenericWithNull
                            Assert.False(reader.IsDBNull(0));
                        }

                        rowsRead++;
                    }
                }
            }
            Assert.Equal(1, rowsRead);
            var valueAsArray = value as Array;
            if (valueAsArray != null)
            {
                var valueReadAsArray = valueRead as Array;
                Assert.NotNull(valueReadAsArray);
                Assert.Equal(valueAsArray.Length, valueReadAsArray.Length);
                for (int i = 0; i < valueAsArray.Length; i++)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    Assert.Equal(valueAsArray.GetValue(i), valueReadAsArray.GetValue(i));
                }
            }
            else
            {
                Assert.Equal(value, valueRead);
            }
        }

        [Fact]
        public Task BindBoolean() => TestBindNonNull(SpannerDbType.Bool, true, r => r.GetBoolean(0));

        [Fact]
        public Task BindBooleanArray() => TestBindNonNull(
            SpannerDbType.ArrayOf(SpannerDbType.Bool),
            new bool?[] {true, null, false});

        [Fact]
        public Task BindBooleanEmptyArray() => TestBindNonNull(
            SpannerDbType.ArrayOf(SpannerDbType.Bool),
            new bool[] { });

        [Fact]
        public Task BindByteArray() => TestBindNonNull(
            SpannerDbType.Bytes,
            new byte[] {1, 2, 3});

        [Fact]
        public Task BindByteArrayList() => TestBindNonNull(
            SpannerDbType.ArrayOf(SpannerDbType.Bytes),
            new List<byte[]> {
                new byte[] { 1, 2, 3 },
                new byte[] { 4, 5, 6 },
                null
            });

        [Fact]
        public Task BindEmptyByteArrayList() => TestBindNonNull(
            SpannerDbType.ArrayOf(SpannerDbType.Bytes),
            new List<byte[]>());

        [Fact]
        public Task BindDate() => TestBindNonNull(
            SpannerDbType.Date,
            new DateTime(2017, 5, 26));

        [Fact]
        public Task BindDateArray() => TestBindNonNull(
            SpannerDbType.ArrayOf(SpannerDbType.Date),
            new DateTime?[] {new DateTime(2017, 5, 26), null, new DateTime(2017, 5, 9)});

        [Fact]
        public Task BindDateEmptyArray() => TestBindNonNull(
            SpannerDbType.ArrayOf(SpannerDbType.Date),
            new DateTime?[] { });

        [Fact]
        public Task BindFloat64() => TestBindNonNull(SpannerDbType.Float64, 1.0, r => r.GetDouble(0));

        [Fact]
        public Task BindFloat64Array() => TestBindNonNull(
            SpannerDbType.ArrayOf(SpannerDbType.Float64),
            new double?[] {0.0, null, 1.0});

        [Fact]
        public Task BindFloat64EmptyArray() => TestBindNonNull(
            SpannerDbType.ArrayOf(SpannerDbType.Float64),
            new double[] { });

        [Fact]
        public Task BindInt64() => TestBindNonNull(SpannerDbType.Int64, 1, r => r.GetInt64(0));

        [Fact]
        public Task BindInt64Array() => TestBindNonNull(
            SpannerDbType.ArrayOf(SpannerDbType.Int64),
            new long?[] {1, null, 0});

        [Fact]
        public Task BindInt64EmptyArray() => TestBindNonNull(
            SpannerDbType.ArrayOf(SpannerDbType.Int64),
            new long[] { });

        [Fact]
        public Task BindString() => TestBindNonNull(SpannerDbType.String, "abc", r => r.GetString(0));

        [Fact]
        public Task BindStringArray() => TestBindNonNull(
            SpannerDbType.ArrayOf(SpannerDbType.String),
            new[] {"abc", null, "123"});

        [Fact]
        public Task BindStringEmptyArray() => TestBindNonNull(
            SpannerDbType.ArrayOf(SpannerDbType.String),
            new string[] { });

        [Fact]
        public Task BindTimestamp() => TestBindNonNull(
            SpannerDbType.Timestamp,
            new DateTime(2017, 5, 26, 15, 0, 0));

        [Fact]
        public Task BindTimestampArray() => TestBindNonNull(
            SpannerDbType.ArrayOf(SpannerDbType.Timestamp),
            new DateTime?[]
                {new DateTime(2017, 5, 26, 3, 15, 0), null, new DateTime(2017, 5, 9, 12, 30, 0)});

        [Fact]
        public Task BindTimestampEmptyArray() => TestBindNonNull(
            SpannerDbType.ArrayOf(SpannerDbType.Timestamp),
            new DateTime?[] { });
    }
}
