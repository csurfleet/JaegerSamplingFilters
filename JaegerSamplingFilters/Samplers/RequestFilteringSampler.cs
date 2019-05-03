using Jaeger;
using Jaeger.Samplers;
using Jaeger.Util;
using JaegerSamplingFilters.Filters;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JaegerSamplingFilters.Samplers
{
    /// <summary>Sampler able to make decisions about sampling a trace based on the HttpRequest it is running within.</summary>
    public class RequestFilteringSampler : ValueObject, ISampler
    {
        private readonly object _lock = new object();

        /// <summary>
        /// A collection of sampling filters. These will be used to match requests. Order is important - the first filter which
        /// matches a request will be activated and any following it be ignored.
        /// </summary>
        internal List<ISamplingFilter> SamplingFilters { get; }

        /// <summary>The sampler to use when none of the items in <see name="SamplingFilters"/> matches.</summary>
        internal ISampler DefaultSampler { get; private set; }

        /// <summary>Gives access to the current <see cref="HttpContext"/>.</summary>
        internal IHttpContextAccessor HttpContextAccessor { get; }

        /// <summary>Sampler able to make decisions about sampling a trace based on the HttpRequest it is running within.</summary>
        /// <param name="sampleFilters">
        /// A collection of sampling filters. These will be used to match requests. Order is important - the first filter which
        /// matches a request will be activated and any following it be ignored.
        /// </param>
        /// <param name="httpContextAccessor">Gives access to the current <see cref="HttpContext"/>.</param>
        public RequestFilteringSampler(IEnumerable<ISamplingFilter> sampleFilters,
            IHttpContextAccessor httpContextAccessor)
            : this(
                sampleFilters.ToList(),
                new ConstSampler(true),
                httpContextAccessor)
        {
            Update();
        }

        /// <summary>Sampler able to make decisions about sampling a trace based on the HttpRequest it is running within.</summary>
        /// <param name="sampleFilters">
        /// A collection of sampling filters. These will be used to match requests. Order is important - the first filter which
        /// matches a request will be activated and any following it be ignored.
        /// </param>
        /// <param name="defaultSampler">The sampler to use when none of the items in <paramref name="sampleFilters"/> matches.</param>
        /// <param name="httpContextAccessor">Gives access to the current <see cref="HttpContext"/>.</param>
        public RequestFilteringSampler(
            List<ISamplingFilter> sampleFilters,
            ISampler defaultSampler,
            IHttpContextAccessor httpContextAccessor)
        {
            SamplingFilters = sampleFilters;
            DefaultSampler = defaultSampler ?? throw new ArgumentNullException(nameof(defaultSampler));
            HttpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// Updates the <see cref="GuaranteedThroughputSampler"/> for each operation.
        /// </summary>
        /// <returns><c>true</c>, if any samplers were updated.</returns>
        public bool Update()
        {
            lock (_lock)
            {
                var isUpdated = false;

                var defaultSampler = new ConstSampler(true);

                if (!defaultSampler.Equals(DefaultSampler))
                {
                    DefaultSampler.Close();
                    DefaultSampler = defaultSampler;
                    isUpdated = true;
                }

                return isUpdated;
            }
        }

        /// <summary>Makes a decision on tracing a span.</summary>
        /// <param name="operation">The operation being executed. (This is not just the HTTP Method)</param>
        /// <param name="id">The ID of the span.</param>
        public SamplingStatus Sample(string operation, TraceId id)
        {
            lock (_lock)
            {
                var context = HttpContextAccessor.HttpContext;

                return SamplingFilters.Find(f => f.ShouldSample(context.Request))?.TargetSampler?.Sample(operation, id) ??
                    DefaultSampler.Sample(operation, id);
            }
        }

        /// <summary>Gets a string representation of the Sampler.</summary>
        public override string ToString() => nameof(RequestFilteringSampler);

        /// <summary>Closes the sampler, and any sub-samplers.</summary>
        public void Close()
        {
            lock (_lock)
            {
                DefaultSampler.Close();
                foreach (var sampler in SamplingFilters.Select(f => f.TargetSampler))
                {
                    sampler.Close();
                }
            }
        }

        /// <summary>Gets the value of the sampler and all sub-samplers.</summary>
        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return DefaultSampler;
            foreach (var sampler in SamplingFilters)
            {
                yield return sampler;
            }
        }
    }
}
