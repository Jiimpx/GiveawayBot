using System;
using System.Configuration;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.SlashCommands;
//using DSharpPlus.VoiceNext;
using Microsoft.Extensions.Logging;
using GiveawayBot.Commands;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using DSharpPlus.Interactivity.Extensions;
using GiveawayProject.Commands;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using DSharpPlus.Interactivity.Enums;

namespace GiveawayBot
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        internal static async Task MainAsync()
        {
            var discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = ConfigurationManager.AppSettings["Token"],
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All,
                MinimumLogLevel = LogLevel.Information,
                LogTimestampFormat = "dd MMM yyyy - hh:mm:ss tt",
            });

            var services = new ServiceCollection();
            services.AddSingleton<IGiveawayController, GiveawayController>();
            var serviceProvider = services.BuildServiceProvider();

            var _interactivity = discord.UseInteractivity(new InteractivityConfiguration()
            {
                PollBehaviour = PollBehaviour.DeleteEmojis,
                Timeout = TimeSpan.FromMinutes(15),
            });
            services.AddScoped(_ => _interactivity);

            var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new[] { ConfigurationManager.AppSettings["Prefix"] },
                EnableDefaultHelp = false,
                EnableDms = true,
                EnableMentionPrefix = true,
                Services = serviceProvider,
            });

            commands.RegisterCommands<GiveawayNormalCommands>();

            var slash = discord.UseSlashCommands(new SlashCommandsConfiguration()
            {
                Services = serviceProvider,
                // You can set other properties such as the default permission checks here
            });

            //slash.RegisterCommands<ApplicationCommandModule>(); //clears all global slash commands, wait for about 2 minutes.
            //slash.RegisterCommands<ApplicationCommandModule>(guildID); //clears all guild specific slash commands

            slash.RegisterCommands<GiveawaySlashCommands>();
            //https://github.com/DSharpPlus/DSharpPlus/blob/master/docs/articles/slash_commands.md
            //https://dsharpplus.github.io/DSharpPlus/articles/slash_commands.html

            Console.WriteLine($"Prefix is : {ConfigurationManager.AppSettings["Prefix"]}");
            
            await discord.ConnectAsync();
            Console.WriteLine("Connected");

            

            await Task.Delay(-1);
        }
    }
}
