using FIG.Assessment.Interfaces;
using FIG.Assessment.Models.Database;
using FIG.Assessment.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
            .ConfigureLogging((context, logging) =>
            {
                //add logging via abstraction/package (like SeriLog, NLog, etc.)
            })
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

public interface IAlertService
{
    void SendAlert(string toAddress, string subject, Exception exception);
}

//Instead of creating a full blown hosted bg service for this I would probably just write it as a task and schedule it on a machine
//or add it to a large task scheduler/manager that may be part of domain (esp in context of reporting, pushing it to db layer wouldnt be bad idea)
public class DailyReportService : BackgroundService
{
    private readonly IUserReportService _userReportService;
    private readonly ILogger<DailyReportService> _logger;
    private readonly IAlertService _alertService;
    private readonly string _toAddress;
    public DailyReportService(IUserReportService userReportService, ILogger<DailyReportService> logger, IAlertService alertService, IConfiguration config)
    {
        _userReportService = userReportService;
        _logger = logger;
        _alertService = alertService;
        _toAddress = config.GetValue<string>("ToAddress", "");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // when the service starts up, start by looking back at the last 24 hours
        var startingFrom = DateTime.UtcNow.AddDays(-1);

        while (!stoppingToken.IsCancellationRequested)
        {
            
            _logger.LogInformation("Starting daily report task");
            // run both queries in parallel to save time
            var newUsersTask = this._userReportService.GetNewUsersAsync(startingFrom);
            var deactivatedUsersTask = this._userReportService.GetDeactivatedUsersAsync(startingFrom);
            _logger.LogInformation($"initializing ");
            Task.WhenAll(newUsersTask, deactivatedUsersTask).ContinueWith(async (work) =>
            {
                if (work.IsCompletedSuccessfully)
                {
                    _logger.LogInformation("queries completed.");
                    _logger.LogInformation($"new users : {work.Result[0].Count()}, deactivated users: {work.Result[1].Count()}");
                    await SendUserReportAsync(work.Result[0], work.Result[1]);
                    _logger.LogInformation("user report sent.");
                }
                else
                {
                    _logger.LogInformation($"error running queries for report service. Exception: {work.Exception?.Message}");
                    if(!string.IsNullOrEmpty(_toAddress))
                        _alertService.SendAlert(_toAddress, $"{nameof(DailyReportService)} failure", work.Exception);
                        
                }
            }, stoppingToken)
                .Unwrap();

            // send report to execs
            await this.SendUserReportAsync(newUsersTask.Result, deactivatedUsersTask.Result);

            // save the current time, wait 24hr, and run the report again - using the new cutoff date
            startingFrom = DateTime.UtcNow;
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private Task SendUserReportAsync(IEnumerable<UserDb> newUsers, IEnumerable<UserDb> deactivatedUsers)
    {
        // not part of this example
        return Task.CompletedTask;
    }
}