using FluentAssertions;
using JaegerSamplingFilters.Filters;
using JaegerSamplingFilters.Samplers;
using JaegerSamplingFilters.Tests.Mocks;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using Xunit;

namespace JaegerSamplingFilters.Tests.Samplers
{
    public class RequestFilteringSamplerTests
    {
        #region Constructor

        [Fact]
        public void Ctor()
        {
            var unit = new RequestFilteringSampler(
                           new[] { new MockSamplingFilter() },
                           new MockSampler(),
                           Mock.Of<IHttpContextAccessor>());

            unit.Should().NotBeNull("because we passed valid arguments");

            unit.SamplingFilters
                .Should().NotBeNull()
                .And.HaveCount(1);

            unit.DefaultSampler
                .Should().BeOfType<MockSampler>(
                "because we passed a MockSampler to the constructor");

            unit.HttpContextAccessor.HttpContext
                .Should().BeNull(
                "because the mock context accessor does not contain a HttpContext");
        }

        [Fact]
        public void Ctor_EmptyFilters()
        {
            var unit = new RequestFilteringSampler(
                           new ISamplingFilter[0],
                           new MockSampler(),
                           Mock.Of<IHttpContextAccessor>());

            unit.Should().NotBeNull("because we passed valid arguments");

            unit.SamplingFilters
                .Should().NotBeNull()
                .And.BeEmpty("because we did not supply any filters to the constructor");
        }

        [Fact]
        public void Ctor_NullFilters()
        {
            var unit = new RequestFilteringSampler(
                           null,
                           new MockSampler(),
                           Mock.Of<IHttpContextAccessor>());

            unit.Should().NotBeNull("because we passed valid arguments");

            unit.SamplingFilters
                .Should().NotBeNull()
                .And.BeEmpty("because we did not supply any filters to the constructor");
        }

        [Fact]
        public void Ctor_NullSampler()
        {
            Action ctor = () => new RequestFilteringSampler(
                                   new[] { new MockSamplingFilter() },
                                   null,
                                   Mock.Of<IHttpContextAccessor>());

            ctor.Should().Throw<ArgumentNullException>()
                .WithMessage("*defaultSampler*");
        }

        [Fact]
        public void Ctor_NullContextAccessor()
        {
            Action ctor = () => new RequestFilteringSampler(
                                   new[] { new MockSamplingFilter() },
                                   new MockSampler(),
                                   null);

            ctor.Should().Throw<ArgumentNullException>()
                .WithMessage("*httpContextAccessor*");
        }

        #endregion

        #region Sample

        [Fact]
        public void Sample_NoMatchingFilters()
        {
            // Both filters will fail to match the incoming HttpRequest here, and defaultSampler should collect the trace.
            var filter1 = new MockSamplingFilter(r => r != null);
            var filter2 = new MockSamplingFilter(r => r != null);
            var defaultSampler = new MockSampler();

            var unit = new RequestFilteringSampler(new ISamplingFilter[]
            {
                filter1,
                filter2
            },
            defaultSampler,
            Mock.Of<IHttpContextAccessor>(a => a.HttpContext == Mock.Of<HttpContext>(c => c.Request == null)));

            unit.Sample("TestOperation", new Jaeger.TraceId(1, 1));

            filter1.ShouldSampleCallLogs.Should().HaveCount(1, "because we only called unit.Sample once");
            filter2.ShouldSampleCallLogs.Should().HaveCount(1, "because we only called unit.Sample once");

            filter1.ShouldSampleCallLogs[0].HttpRequest.Should().BeNull("because our HttpContextAccessor supplied a null request");
            filter1.ShouldSampleCallLogs[0].Result.Should().BeFalse("because the filter should not match a null request");

            filter2.ShouldSampleCallLogs[0].HttpRequest.Should().BeNull("because our HttpContextAccessor supplied a null request");
            filter2.ShouldSampleCallLogs[0].Result.Should().BeFalse("because the filter should not match a null request");

            defaultSampler.Samples.Should().HaveCount(1, "because the sample passed over both filters so should go to our default");
            defaultSampler.Samples[0].Operation.Should().Be("TestOperation", "because this is the sample we sent to our sampler");
        }

        [Fact]
        public void Sample_MultipleMatchingFilters()
        {
            // Both filters will match the incoming HttpRequest here, but the trace should go to only the first one
            var filter1 = new MockSamplingFilter(r => r == null);
            var filter2 = new MockSamplingFilter(r => r == null);
            var defaultSampler = new MockSampler();

            var unit = new RequestFilteringSampler(new ISamplingFilter[]
            {
                filter1,
                filter2
            },
            defaultSampler,
            Mock.Of<IHttpContextAccessor>(a => a.HttpContext == Mock.Of<HttpContext>(c => c.Request == null)));

            unit.Sample("TestOperation", new Jaeger.TraceId(1, 1));

            filter1.ShouldSampleCallLogs.Should().HaveCount(1, "because we only called unit.Sample once");
            filter2.ShouldSampleCallLogs.Should().BeEmpty(
                "because the only sample was matched by filter1, so should have never made it to a subsequent filter");

            filter1.ShouldSampleCallLogs[0].HttpRequest.Should().BeNull("because our HttpContextAccessor supplied a null request");
            filter1.ShouldSampleCallLogs[0].Result.Should().BeTrue("because the filter matches null request");
            filter1.TargetSamplerMock.Samples.Should().HaveCount(1, "because the filter matched the request, so we should have sampled it");
            filter1.TargetSamplerMock.Samples[0].Operation.Should().Be("TestOperation", "because this is the sample we sent to our sampler");

            defaultSampler.Samples.Should().BeEmpty(
                "because the only sample was matched by filter1, so should have never made it to the default sampler");
        }

        #endregion

        #region Close

        [Fact]
        public void Close_ClosesAllChildren()
        {
            var filter1 = new MockSamplingFilter();
            var filter2 = new MockSamplingFilter();
            var filter3 = new MockSamplingFilter();
            var defaultSampler = new MockSampler();

            var unit = new RequestFilteringSampler(new ISamplingFilter[]
            {
                filter1,
                filter2,
                filter3
            },
            defaultSampler,
            Mock.Of<IHttpContextAccessor>());

            unit.Close();

            filter1.IsClosed.Should().BeTrue("because closing the sampler should close all its children");
            filter2.IsClosed.Should().BeTrue("because closing the sampler should close all its children");
            filter3.IsClosed.Should().BeTrue("because closing the sampler should close all its children");
            defaultSampler.IsClosed.Should().BeTrue("because closing the sampler should close all its children");
        }

        #endregion
    }
}
