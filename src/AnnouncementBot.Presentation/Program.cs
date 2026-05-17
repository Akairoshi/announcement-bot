using AnnouncementBot.Application;
using AnnouncementBot.Infrastructure;
using AnnouncementBot.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) => {
        
        var botToken = context.Configuration
        .GetSection(BotConfiguration.SectionName)
        .Get<BotConfiguration>()!
        .Token;

        services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));

        services
        .AddApplication()
        .AddInfrastructure(context.Configuration);

    })
    .Build();

await host.RunAsync();