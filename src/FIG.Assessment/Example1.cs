using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;

namespace FIG.Assessment;

/// <summary>
/// In this example, the goal of this GetPeopleInfo method is to fetch a list of Person IDs from our database (not implemented / not part of this example),
/// and for each Person ID we need to hit some external API for information about the person. Then we would like to return a dictionary of the results to the caller.
/// We want to perform this work in parallel to speed it up, but we don't want to hit the API too frequently, so we are trying to limit to 5 requests at a time at most
/// by using 5 background worker threads.
/// Feel free to suggest changes to any part of this example.
/// In addition to finding issues and/or ways of improving this method, what is a name for this sort of queueing pattern?
///Provider Consumer pattern is what this looks like, but could also be one part of a saga pattern
/// </summary>
public class Example1
{
    private readonly IConfiguration _config;
    public Example1(IConfiguration config) => _config = config;
    public async Task<Dictionary<int, int>> GetPeopleInfo()
    {
        int consumerCount = _config.GetValue<int>("ConsumerCount", 5);
        
        // initialize empty queue, and empty result set
        var personIdQueue = new ConcurrentQueue<int>();
        var results = new Dictionary<int, int>();
        
        Task provider = Task.Run(() => CollectPersonIds(personIdQueue));
        
        List<Task> consumers = Enumerable.Range(0,consumerCount)
            .Select(_ => Task.Run(() => GatherNumericInfo(personIdQueue, results, "age"))).ToList();
        
        await Task.WhenAll(consumers);
        return results;
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

    private async Task GatherNumericInfo(ConcurrentQueue<int> personIdQueue, Dictionary<int,int> results, string fieldName)
    {
        // pull IDs off the queue until it is empty
        while (personIdQueue.TryDequeue(out var id))
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://some.example.api/people/{id}/{fieldName}");
            var response = await client.SendAsync(request);
            var age = int.Parse(response.Content.ReadAsStringAsync().Result);
            if (!results.TryAdd(id, age))
            {
                //error handling?
            }
        }
    }
}
