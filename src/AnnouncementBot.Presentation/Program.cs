using AnnouncementBot.Application;
using AnnouncementBot.Infrastructure;
using AnnouncementBot.Infrastructure.Configuration;
using AnnouncementBot.Presentation.Telegram.FSM;
using AnnouncementBot.Presentation.Middlewares;
using AnnouncementBot.Presentation.Telegram;
using AnnouncementBot.Presentation.Telegram.Callbacks;
using AnnouncementBot.Presentation.Telegram.Callbacks.Interfaces;
using AnnouncementBot.Presentation.Telegram.Commands;
using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Microsoft.Extensions.Configuration;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var botToken = context.Configuration
            .GetSection(BotConfiguration.SectionName)
            .Get<BotConfiguration>()!
            .Token;

        services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
        services.AddSingleton<IUpdateHandler, UpdateHandler>();
        services.AddSingleton<ConversationStateStorage>();

        services.AddHostedService<TelegramBotWorker>();

        services
            .AddApplication()
            .AddInfrastructure(context.Configuration);

        // Middleware pipeline: ExceptionHandling → UserRegistration → Authorization
        services.AddScoped<IBotMiddleware, ExceptionHandlingMiddleware>();
        services.AddScoped<IBotMiddleware, UserRegistrationMiddleware>();
        services.AddScoped<IBotMiddleware, AuthorizationMiddleware>();

        // Команды — все роли
        services.AddScoped<IBotCommand, StartCommand>();
        services.AddScoped<IBotCommand, CancelCommand>();
        services.AddScoped<IBotCommand, ProfileCommand>();
        services.AddScoped<IBotCommand, SubscribeCommand>();
        services.AddScoped<IBotCommand, ListAnnouncementCommand>();
        services.AddScoped<IBotCommand, AdminRequestCommand>();

        // Команды — Admin + SuperAdmin
        services.AddScoped<IBotCommand, MakeAnnouncementCommand>();
        services.AddScoped<IBotCommand, ListTemplateCommand>();
        services.AddScoped<IBotCommand, AddTemplateCommand>();
        services.AddScoped<IBotCommand, UpdateTemplateCommand>();
        services.AddScoped<IBotCommand, RemoveTemplateCommand>();

        // Команды — SuperAdmin
        services.AddScoped<IBotCommand, AddCategoryCommand>();
        services.AddScoped<IBotCommand, UpdateCategoryCommand>();
        services.AddScoped<IBotCommand, RemoveCategoryCommand>();
        services.AddScoped<IBotCommand, ListCategoryCommand>();
        services.AddScoped<IBotCommand, ListAdminCommand>();
        services.AddScoped<IBotCommand, RemoveAdminCommand>();
        services.AddScoped<IBotCommand, ListAdminRequestsCommand>();

        // Callback handlers
        services.AddScoped<ICallbackHandler, MakeAnnouncementCallbackHandler>();
        services.AddScoped<ICallbackHandler, SubscribeCallbackHandler>();
        services.AddScoped<ICallbackHandler, AdminRequestReviewCallbackHandler>();
        services.AddScoped<ICallbackHandler, CategoryCallbackHandler>();
        services.AddScoped<ICallbackHandler, AdminRemoveCallbackHandler>();
        services.AddScoped<ICallbackHandler, TemplateCallbackHandler>();
    })
    .Build();

await host.RunAsync();
