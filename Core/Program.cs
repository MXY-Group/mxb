using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Sentry;

namespace core
{
    class Program
    {
        static void Main(string[] args)
        {
            DotNetEnv.Env.TraversePath().Load();
            using (SentrySdk.Init(DotNetEnv.Env.GetString("sentry")))
            {
                new Program().Run().GetAwaiter().GetResult();
            }
        }

        private static DiscordSocketClient _client; 
        private CommandService _commands;
        private IServiceProvider _services;
        
        private async Task Run()
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();

            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .BuildServiceProvider();

            string botToken = DotNetEnv.Env.GetString("token");

            _client.Log += Log;

            await RegisterCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, botToken);

            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private async Task RegisterCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        }

        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            string messageLower = arg.Content.ToLower();
            var message = arg as SocketUserMessage;
            if (message is null || message.Author.IsBot) return;
            int argumentPos = 0;
            if (message.HasStringPrefix("rd!", ref argumentPos) || message.HasMentionPrefix(_client.CurrentUser, ref argumentPos))
            {
                var context = new SocketCommandContext(_client, message);
                var result = await _commands.ExecuteAsync(context, argumentPos, _services);
                if (!result.IsSuccess)
                {
                    Console.WriteLine(result.ErrorReason);
                    await message.Channel.SendMessageAsync(result.ErrorReason);
                }
            }
        }
    }
}