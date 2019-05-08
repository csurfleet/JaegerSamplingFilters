using Jaeger.Samplers;
using JaegerSamplingFilters.Filters;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace JaegerSamplingFilters.Tests.Mocks
{
    /// <summary>
    /// A mock <see cref="ISamplingFilter"/>. You can check the calls made against it through the <see cref="ShouldSampleCallLogs"/>.
    /// </summary>
    public class MockSamplingFilter : ISamplingFilter
    {
        /// <summary>
        /// A mock <see cref="ISamplingFilter"/>. You can check the calls made against it through the <see cref="ShouldSampleCallLogs"/>.
        /// </summary>
        public MockSamplingFilter() => TargetSampler = new MockSampler();

        /// <summary>
        /// A mock <see cref="ISamplingFilter"/>. You can check the calls made against it through the <see cref="ShouldSampleCallLogs"/>.
        /// </summary>
        /// <param name="samplingFilter">The filter to use when <see cref="ShouldSample(HttpRequest)"/> is called.</param>
        public MockSamplingFilter(Func<HttpRequest, bool> samplingFilter)
        {
            TargetSampler = new MockSampler();
            SamplingFilter = samplingFilter;
        }

        public ISampler TargetSampler { get; }

        /// <summary>
        /// Test helper. Provides access to the <see cref="MockSampler"/> behind <see cref="TargetSampler"/>, or throws if the type is invalid.
        /// </summary>
        public MockSampler TargetSamplerMock => (TargetSampler is MockSampler)
            ? (MockSampler)TargetSampler
            : throw new Exception($"Cannot access mock features on TargetSampler - type was {TargetSampler.GetType().Name}");

        /// <summary>Test helper. The filter to use when <see cref="ShouldSample(HttpRequest)"/> is called.</summary>
        public Func<HttpRequest, bool> SamplingFilter { get; }

        /// <summary>Test helper. Has <see cref="Close"/>() been called on <see cref="TargetSampler"/>?</summary>
        public bool IsClosed
            => (TargetSampler is MockSampler)
                ? ((MockSampler)TargetSampler).IsClosed
                : throw new Exception($"Can only determine the closed state of MockSamplers, a {TargetSampler.GetType().Name} was found");

        public bool ShouldSample(HttpRequest request)
        {
            var result = SamplingFilter?.Invoke(request) ?? true;
            ShouldSampleCallLogs.Add(new ShouldSampleCallLog(request, result));
            return result;
        }

        /// <summary>Test helper. A log of calls made to <see cref="ShouldSample(HttpRequest)"/>.</summary>
        public List<ShouldSampleCallLog> ShouldSampleCallLogs { get; } = new List<ShouldSampleCallLog>();
    }

    /// <summary>A log of a call to <see cref="MockSamplingFilter.ShouldSample(HttpRequest)"/>.</summary>
    public readonly struct ShouldSampleCallLog
    {
        public ShouldSampleCallLog(HttpRequest request, bool result)
        {
            HttpRequest = request;
            Result = result;
            CalledAt = DateTimeOffset.Now;
        }

        public HttpRequest HttpRequest { get; }

        public bool Result { get; }

        public DateTimeOffset CalledAt { get; }
    }
}
