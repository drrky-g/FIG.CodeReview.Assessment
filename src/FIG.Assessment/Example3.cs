using FIG.Assessment.Interfaces;
using FIG.Assessment.Models.Database;
using FIG.Assessment.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FIG.Assessment;

/// <summary>
/// In this example, we are writing a service that will run (potentially as a windows service or elsewhere) and once a day will run a report on all new
/// users who were created in our system within the last 24 hours, as well as all users who deactivated their account in the last 24 hours. We will then
/// email this report to the executives so they can monitor how our user base is growing.
/// </summary>
public class Example3
{
    public static void Main(string[] args)
    {
        Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddDbContext<UserContext>(options =>
                {
                    options.UseSqlServer("dummy-connection-string");
                });
                services.AddSingleton<IUserReportService, UserRepository>();
                services.AddHostedService<DailyReportService>();
            })
            .Build()
            .Run();
    }
}

public class DailyReportService : BackgroundService
{
    private readonly IUserReportService _userReportService;
    public DailyReportService(IUserReportService userReportService, IConfiguration config) => 
        _userReportService = userReportService;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // when the service starts up, start by looking back at the last 24 hours
        var startingFrom = DateTime.Now.AddDays(-1);

        while (!stoppingToken.IsCancellationRequested)
        {
            // run both queries in parallel to save time
            var newUsersTask = this._userReportService.GetNewUsersAsync(startingFrom);
            var deactivatedUsersTask = this._userReportService.GetDeactivatedUsersAsync(startingFrom);
            Task.WhenAll(newUsersTask, deactivatedUsersTask).ContinueWith(async (work) =>
            {
                if (work.IsCompletedSuccessfully)
                {
                    await SendUserReportAsync(work.Result[0], work.Result[1]);
                }
                else
                {
                    //logging/alerts with exception from work (would need to inject interfaces that implimenting this
                    var e = work.Exception;
                }
            }, stoppingToken)
                .Unwrap();

            // send report to execs
            await this.SendUserReportAsync(newUsersTask.Result, deactivatedUsersTask.Result);

            // save the current time, wait 24hr, and run the report again - using the new cutoff date
            startingFrom = DateTime.Now;
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private Task SendUserReportAsync(IEnumerable<UserDb> newUsers, IEnumerable<UserDb> deactivatedUsers)
    {
        // not part of this example
        return Task.CompletedTask;
    }
}