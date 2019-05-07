using Jaeger;
using Jaeger.Samplers;
using Moq;
using System;
using System.Collections.Generic;

namespace JaegerSamplingFilters.Tests.Mocks
{
    /// <summary>
    /// A mock <see cref="ISampler"/> which will let you easily query the sampled items through <see cref="Samples"/>.
    /// </summary>
    public class MockSampler : ISampler
    {
        public void Close() => IsClosed = true;

        /// <summary>Test helper. Has <see cref="Close"/>() been called on this sampler?.</summary>
        public bool IsClosed { get; private set; }

        public SamplingStatus Sample(string operation, TraceId id)
        {
            Samples.Add(new MockedSample(operation, id));
            return new SamplingStatus(true, Mock.Of<IReadOnlyDictionary<string, object>>());
        }

        /// <summary>Test helper. The samples collected by this sampler.</summary>
        public List<MockedSample> Samples { get; } = new List<MockedSample>();
    }

    /// <summary>A sample stored by a <see cref="MockSampler"/>.</summary>
    public readonly struct MockedSample
    {
        public MockedSample(string operation, TraceId id)
        {
            Operation = operation;
            Id = id;
            SampledAt = DateTimeOffset.Now;
        }

        public string Operation { get; }
        public TraceId Id { get; }
        public DateTimeOffset SampledAt { get; }
    }
}
