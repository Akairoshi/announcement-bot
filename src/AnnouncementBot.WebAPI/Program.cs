using AnnouncementBot.Application;
using AnnouncementBot.Infrastructure;
using AnnouncementBot.Infrastructure.Configuration;
using AnnouncementBot.WebApi.Telegram.FSM;
using AnnouncementBot.WebApi.Middlewares;
using AnnouncementBot.WebApi.Telegram;
using AnnouncementBot.WebApi.Telegram.Callbacks;
using AnnouncementBot.WebApi.Telegram.Callbacks.Interfaces;
using AnnouncementBot.WebApi.Telegram.Commands;
using AnnouncementBot.WebApi.Telegram.Commands.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Локальный кэш (необходим для временного хранения 6-значных кодов Auth-by-Code)
builder.Services.AddMemoryCache();

var botToken = builder.Configuration
    .GetSection(BotConfiguration.SectionName)
    .Get<BotConfiguration>()!
    .Token;

// Регистрация сервисов ядра Telegram-боссинга (Singleton)
builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
builder.Services.AddSingleton<IUpdateHandler, UpdateHandler>();
builder.Services.AddSingleton<ConversationStateStorage>();

builder.Services.AddHostedService<TelegramBotWorker>();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);


builder.Services.AddScoped<IBotMiddleware, ExceptionHandlingMiddleware>();
builder.Services.AddScoped<IBotMiddleware, UserRegistrationMiddleware>();
builder.Services.AddScoped<IBotMiddleware, AuthorizationMiddleware>();


builder.Services.AddScoped<IBotCommand, StartCommand>();
builder.Services.AddScoped<IBotCommand, CancelCommand>();
builder.Services.AddScoped<IBotCommand, ProfileCommand>();
builder.Services.AddScoped<IBotCommand, SubscribeCommand>();
builder.Services.AddScoped<IBotCommand, ListAnnouncementCommand>();
builder.Services.AddScoped<IBotCommand, AdminRequestCommand>();

builder.Services.AddScoped<IBotCommand, MakeAnnouncementCommand>();
builder.Services.AddScoped<IBotCommand, ListTemplateCommand>();
builder.Services.AddScoped<IBotCommand, AddTemplateCommand>();
builder.Services.AddScoped<IBotCommand, UpdateTemplateCommand>();
builder.Services.AddScoped<IBotCommand, RemoveTemplateCommand>();

builder.Services.AddScoped<IBotCommand, AddCategoryCommand>();
builder.Services.AddScoped<IBotCommand, UpdateCategoryCommand>();
builder.Services.AddScoped<IBotCommand, RemoveCategoryCommand>();
builder.Services.AddScoped<IBotCommand, ListCategoryCommand>();
builder.Services.AddScoped<IBotCommand, ListAdminCommand>();
builder.Services.AddScoped<IBotCommand, RemoveAdminCommand>();
builder.Services.AddScoped<IBotCommand, ListAdminRequestsCommand>();

builder.Services.AddScoped<ICallbackHandler, MakeAnnouncementCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, SubscribeCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, AdminRequestReviewCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, CategoryCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, AdminRemoveCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, TemplateCallbackHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options.WithTitle("AnnouncementBot API")
               .WithTheme(ScalarTheme.DeepSpace);
    });
}

app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

app.UseHttpsRedirection();

// В будущем сюда добавятся:
// app.UseAuthentication();
// app.UseAuthorization();

app.MapControllers();

await app.RunAsync();