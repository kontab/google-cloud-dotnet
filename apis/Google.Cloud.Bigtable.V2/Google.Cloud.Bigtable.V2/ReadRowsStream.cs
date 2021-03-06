﻿// Copyright 2018 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Google.Api.Gax.Grpc;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Google.Cloud.Bigtable.V2
{
    /// <summary>
    /// The stream of <see cref="Row"/> instances returned from the server.
    /// </summary>
    public class ReadRowsStream : IAsyncEnumerable<Row>
    {
        private readonly CallSettings _callSettings;
        private readonly BigtableClientImpl _client;
        private int _enumeratorCount;
        private readonly ReadRowsRequest _originalRequest;
        private readonly RetrySettings _retrySettings;
        private readonly BigtableServiceApiClient.ReadRowsStream _underlyingStream;

        internal ReadRowsStream(
            BigtableClientImpl client,
            ReadRowsRequest originalRequest,
            CallSettings callSettings,
            RetrySettings retrySettings,
            BigtableServiceApiClient.ReadRowsStream underlyingStream)
        {
            _client = client;
            _originalRequest = originalRequest;
            _callSettings = callSettings;
            _retrySettings = retrySettings;
            _underlyingStream = underlyingStream;
        }

        /// <summary>
        /// The underlying gRPC duplex streaming call.
        /// </summary>
        public AsyncServerStreamingCall<ReadRowsResponse> GrpcCall => _underlyingStream.GrpcCall;

        /// <inheritdoc/>
        public IAsyncEnumerator<Row> GetEnumerator()
        {
            if (Interlocked.CompareExchange(ref _enumeratorCount, 1, 0) == 1)
            {
                throw new InvalidOperationException($"The {nameof(ReadRowsStream)} can only be iterated once");
            }

            return new RowAsyncEnumerator(_client, _originalRequest, _callSettings, _retrySettings, _underlyingStream);
        }
    }
}
