using FluentAssertions;
using Jaeger.Samplers;
using JaegerSamplingFilters.Filters;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using Xunit;

namespace JaegerSamplingFilters.Tests.Filters
{
    public class RegexSamplingFilterTests
    {
        #region Constructor

        [Fact]
        public void Ctor_ValidRegex()
            => new RegexSamplingFilter("AnyValidRegex", true)
            .Should()
            .NotBeNull("because we passed a valid regular expression");

        [Fact]
        public void Ctor_ValidRegexAndTargetSampler()
            => new RegexSamplingFilter("AnyValidRegex", Mock.Of<ISampler>())
            .Should()
            .NotBeNull("because we passed a valid regular expression with a Sampler instance");

        [Fact]
        public void Ctor_InvalidRegex()
        {
            Action ctor = () => new RegexSamplingFilter("[", true);

            ctor.Should()
                .Throw<ArgumentException>(
                "because we supplied an invalid regular expression");
        }

        [Fact]
        public void Ctor_NullSampler()
        {
            Action ctor = () => new RegexSamplingFilter("AnyValidRegex", null);

            ctor.Should()
                .Throw<ArgumentNullException>(
                "because we supplied a null target sampler");
        }

        #endregion

        #region ShouldSample

        [Fact]
        public void ShouldSample_NullRequest()
        {
            var unit = new RegexSamplingFilter("Anything", true);

            unit.Invoking(u => u.ShouldSample(null))
                .Should()
                .Throw<ArgumentNullException>(
                "beacuse we supplied a null HttpRequest");
        }

        [Theory]
        [InlineData("/v1/hello", true)]
        [InlineData("/v1/nope", false)]
        [InlineData("/hello", true)]
        [InlineData("/hel/lo", false)]
        public void ShouldSample_RegexMatches(string path, bool expected)
        {
            var unit = new RegexSamplingFilter("hello", true);
            var request = Mock.Of<HttpRequest>(r => r.Path == path);

            unit.ShouldSample(request).Should().Be(expected);
        }

        #endregion
    }
}
