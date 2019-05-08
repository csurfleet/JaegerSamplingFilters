using Jaeger.Samplers;
using Microsoft.AspNetCore.Http;
using System;
using System.Text.RegularExpressions;

namespace JaegerSamplingFilters.Filters
{
    /// <summary>A sampling filter which will match <see cref="HttpRequest.Path"/> against a supplied Regex.</summary>
    public class RegexSamplingFilter : ISamplingFilter
    {
        /// <summary>A sampling filter which will match <see cref="HttpRequest.Path"/> against a supplied Regex.</summary>
        /// <param name="regex">The regex to match <see cref="HttpRequest.Path"/> against.</param>
        /// <param name="targetSampler">The sampler to pass matching spans to.</param>
        public RegexSamplingFilter(string regex, ISampler targetSampler)
        {
            Regex = new Regex(regex);
            TargetSampler = targetSampler ?? throw new ArgumentNullException(nameof(targetSampler));
        }

        /// <summary>A sampling filter which will match <see cref="HttpRequest.Path"/> against a supplied Regex.</summary>
        /// <param name="regex">The regex to match <see cref="HttpRequest.Path"/> against.</param>
        /// <param name="sample">Should matched spans be sampled?</param>
        public RegexSamplingFilter(string regex, bool sample)
            : this(regex, new ConstSampler(sample)) { }

        /// <summary>The sampler to pass matching spans to.</summary>
        public ISampler TargetSampler { get; }

        /// <summary>The regex to match <see cref="HttpRequest.Path"/> against.</summary>
        public Regex Regex { get; }

        /// <summary>Should the current supplied <see cref="HttpRequest"/> be sampled?</summary>
        /// <param name="request">The current HTTP Request.</param>
        public bool ShouldSample(HttpRequest request)
            => (request != default)
                ? Regex.IsMatch(request.Path)
                : throw new ArgumentNullException(nameof(request));
    }
}
