using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FIG.Assessment;

/// <summary>
/// In this example, the goal of this GetPeopleInfo method is to fetch a list of Person IDs from our database (not implemented / not part of this example),
/// and for each Person ID we need to hit some external API for information about the person. Then we would like to return a dictionary of the results to the caller.
/// We want to perform this work in parallel to speed it up, but we don't want to hit the API too frequently, so we are trying to limit to 5 requests at a time at most
/// by using 5 background worker threads.
/// Feel free to suggest changes to any part of this example.
/// In addition to finding issues and/or ways of improving this method, what is a name for this sort of queueing pattern?
///Provider Consumer pattern is what this looks like, but could also be one part of a saga pattern that builds out a 'person' object from various sources
/// </summary>
public class Example1
{
    private readonly IConfiguration _config;
    private static HttpClient _client;
    private readonly ILogger<Example1> _logger;
    public Example1(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<Example1> logger)
    {
        _config = config;
        _logger = logger;
        _client = httpClientFactory.CreateClient();
        ArgumentNullException.ThrowIfNull(_client);
        
        string baseUrl = _config.GetValue<string>("PersonClient:BaseUrl");
        ArgumentNullException.ThrowIfNull(baseUrl);
        _client.BaseAddress = new Uri(baseUrl);
    } 
    public async Task<Dictionary<int, int>> GetPeopleInfo()
    {
        int consumerCount = _config.GetValue<int>("ConsumerCount", 5);
        
        // initialize empty queue, and empty result set
        var personIdQueue = new ConcurrentQueue<int>();
        var results = new ConcurrentDictionary<int, int>();

        _logger.LogInformation($"starting {nameof(GetPeopleInfo)} provider task..");
        Task provider = Task.Run(() => CollectPersonIds(personIdQueue));
        
        
        _logger.LogInformation($"starting {consumerCount} {nameof(GatherNumericInfo)} task");
        List<Task> consumers = Enumerable.Range(0,consumerCount)
            .Select(i =>
            {
                _logger.LogInformation($"starting {nameof(GatherNumericInfo)} {i+1}");
                return Task.Run(() => GatherNumericInfo(personIdQueue, results, "age"));
            }).ToList();
        
        await Task.WhenAll(consumers);
        _logger.LogInformation($"consumer tasks complete with {results.Count} unique records");
        
        return new(results);
    }

    private Task CollectPersonIds(ConcurrentQueue<int> personIdQueue)
    {
        // dummy implementation, would be pulling from a database
        for (var i = 1; i < 100; i++)
        {
            if (i % 10 == 0) Thread.Sleep(TimeSpan.FromMilliseconds(50)); // artificial delay every now and then
            personIdQueue.Enqueue(i);
        }
        return Task.CompletedTask;
    }

    private async Task GatherNumericInfo(ConcurrentQueue<int> personIdQueue, ConcurrentDictionary<int,int> results, string fieldName)
    {
        // pull IDs off the queue until it is empty
        while (personIdQueue.TryDequeue(out var id))
        {
            using (var response = await _client.GetAsync($"people/{id}/{fieldName}"))
            {
                string json = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"{fieldName} found for personId {id} : {json}");
                if (!results.TryAdd(id, int.Parse(json)))
                {
                    _logger.LogError($"PersonId ({id}) already exists in dictionary.");
                }
            }
        }
    }
}
