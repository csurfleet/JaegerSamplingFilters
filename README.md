# JaegerSamplingFilters

A collection of useful Jaeger filters and samplers which will allow you to easily filter your requests
based on the incoming HttpRequest object.

## Huh?

Normally in Jaeger you have the ability to use various OpenTracing Samplers to decide when to trace a span.
This might be a probability measure (trace 60% of spans), or a simple on/off. There is also an AdaptiveSampler which lets
you make a decision based on the method of the call. This often is inconsistent, and only allows filtering on the one variable.
We wanted something better.

The sampling filters here will allow you to filter traces based on anything you can find in the incoming HttpRequest object.
Want to trace based on the existance of a header? We got you. Don't want to trace web requests for HTTP requests to any route under/v2
but only between 2 and 4PM on a Tuesday - you're probably doing it wrong, but you have the power. Most of you, however, probably just want
to filter based on the incoming route. ;)

## Show Me The $%^£ Code!

Fine, so lets assume you have Jaeger setup to trace everything. Your setup in the Startup class will look a lot like:

```c#
services.AddSingleton<ITracer>(new Tracer.Builder(serviceName)
	.WithReporter(myReporter)
	  .WithSampler(new ConstSampler(true))
    .Build());
```

Now, you find that you need to expose a /metrics route for your health service to query. Great! But after you release, you realise
that this is being called **every damn second**, and filling up your Jaeger list. (This never happened to me, stop looking at me like that!)

Lets filter out the metrics endpoint then:

```c#
services.AddSingleton<ITracer>(sp => new Tracer.Builder(serviceName)
	.WithReporter(myReporter)
	  .WithSampler(new RequestFilteringSampler(new ISamplingFilter[]
	{
		new RegexSamplingFilter("/metrics", new ConstSampler(false))
	},
	defaultSampler: new ConstSampler(true)
	httpContextAccessor: sp.GetRequiredService<IHttpContextAccessor>()))
    .Build());
```

OK cool! So if we take this apart a little, we have a RequestFilteringSampler with:
- A RegexSamplingFilter which will match incoming '/metrics' requests, and send them to a ConstSampler(false). I.E. it won't trace them;
- A default sampler to use for everything not matched by the RegexSamplingFilter - in this case a ConstSampler(true);
- A context accessor, so that we can inspect the incoming request.

We can actually write this much more compactly using the defaults available:

```c#
services.AddSingleton<ITracer>(sp => new Tracer.Builder(serviceName)
	.WithReporter(myReporter)
	  .WithSampler(new RequestFilteringSampler(new ISamplingFilter[]
	{
		new RegexSamplingFilter("/metrics", false)
	},
	sp.GetRequiredService<IHttpContextAccessor>()))
    .Build());
```