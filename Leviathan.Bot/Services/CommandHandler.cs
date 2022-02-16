using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Serilog;

namespace Leviathan.Bot.Services
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _commands;
        private readonly IServiceProvider _services;

        public CommandHandler(DiscordSocketClient client, InteractionService commands, IServiceProvider services)
        {
            _client = client;
            _commands = commands;
            _services = services;
        }

        public async Task InitializeAsync()
        {
            var currentAssembly = Assembly.GetEntryAssembly();
            
            if (currentAssembly!.GetName().Name == "Leviathan.Bot")
            {
                await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            }
            else
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                                        .SingleOrDefault(assembly => assembly.GetName().Name == "Leviathan.Bot");
                
                await _commands.AddModulesAsync(assembly, _services);
            }

            _client.InteractionCreated += HandleInteraction;
        }

        private async Task HandleInteraction(SocketInteraction arg)
        {
            try
            {
                var ctx = new SocketInteractionContext(_client, arg);
                await _commands.ExecuteCommandAsync(ctx, _services);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "HandleInteraction Exception");

                if (arg.Type == InteractionType.ApplicationCommand) 
                    await arg.GetOriginalResponseAsync().ContinueWith(async msg => await msg.Result.DeleteAsync());
            }
        }
    }
}