using Jaeger.Samplers;
using Microsoft.AspNetCore.Http;

namespace JaegerSamplingFilters.Filters
{
    /// <summary>
    /// A sampling filter can make a decision on weather to sample a span, based on the <see cref="HttpRequest"/>
    /// currently being processed. Either use one of the pre-created ones, or design your own.
    /// </summary>
    public interface ISamplingFilter
    {
        /// <summary>This is the sampler which will be used to process your span when the filter is matched.</summary>
        ISampler TargetSampler { get; }

        /// <summary>Should the current supplied <see cref="HttpRequest"/> be sampled?</summary>
        /// <param name="request">The current HTTP Request.</param>
        bool ShouldSample(HttpRequest request);
    }
}
