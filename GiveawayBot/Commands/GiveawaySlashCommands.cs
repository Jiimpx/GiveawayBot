using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus;
using GiveawayProject.Commands;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace GiveawayBot.Commands
{
    // The lifespan of the slash commands, set to Singleton
    [SlashModuleLifespan(SlashModuleLifespan.Singleton)]
    public class GiveawaySlashCommands : ApplicationCommandModule
    {
        IGiveawayController _giveawayController;
        public GiveawaySlashCommands(IGiveawayController giveawayController)
        {
            _giveawayController = giveawayController;
        }

        // The slash command for pinging the bot
        [SlashCommand("ping", "testing connection")]
        public async Task Ping(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync("Pong!");
        }

        [SlashCommand("Test", "Test Controller Interface")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task Test(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync($"test number: {_giveawayController.interfaceCheck()}");
        }

        [SlashCommand("CreateGiveaway", "Giveaway Creation")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task CreateGiveaway(InteractionContext ctx,
            [Option("item", "Name of item in Giveaway")] string item,
            [Option("winners", "How many winners")] long numberOfWinners,
            [Option("duration", "How many minutes, hours, days? (1,2,3,etc)")] long duration,
            [Option("timeOption", "Minutes, Hours or Days")] timeOptions timeOption,
            [Option("channel", "Channel where the Giveaway is hosted")] DiscordChannel channel)
        {
            // Send a response to the user indicating the Giveaway is being created
            await ctx.CreateResponseAsync("Attempting to create giveaway.");
            timeUnit _timeUnit = new timeUnit(timeOption, (int)duration); // create time unit.

            Giveaway giveaway = _giveawayController.CreateGiveaway(ctx.Client, ctx.Member, item, numberOfWinners, _timeUnit, channel);
            DiscordMessage message = await sendMessage(ctx, $"Giveway created,\nTo confirm before giveaway starts:\nItem: {item}, numberOfWinners: {numberOfWinners}, Channel: {channel}, Time duration: {_timeUnit.getTimeNumber()} {_timeUnit.getTimeUnit()}\n\n Type `/StartMessage (Message ID)` to start Giveaway.");
            _giveawayController.AttachMessage(message, giveaway);
        }

        [SlashCommand("CreateGiveawayProcess", "Giveaway Creation")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task CreateGiveaway(InteractionContext ctx)
        {
            // Send a response to the user indicating the Giveaway is being created
            await ctx.CreateResponseAsync("Attempting to create giveaway.");

            Giveaway giveaway = _giveawayController.CreateGiveaway(ctx);
            //get confirmation screen from Giveaway or GiveawayController.
            DiscordMessage message = await sendMessage(ctx, $"Giveway created,\nTo confirm before giveaway starts:\nItem: {giveaway._item}, numberOfWinners: {giveaway._numberOfWinners}, Channel: {giveaway._channel}, Time duration: {giveaway._timerDuration}\n\n Type `/StartMessage (Message ID)` to start Giveaway.");
            _giveawayController.AttachMessage(message, giveaway);
        }

        [SlashCommand("RerollGiveaway", "Reroll Giveaway")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task RerollGiveaway(InteractionContext ctx,
        [Option("giveawayID", "Message ID of the Giveaway")] string giveawayID)
        {
            await ctx.CreateResponseAsync("Rerolling giveaway!");
            await _giveawayController.RerollGiveaway(ctx.Channel, giveawayID);
        }

        [SlashCommand("GetGiveawayList", "Gets List of Giveaway")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task GetGiveawayList(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(_giveawayController.GetGiveawayList().Result);
        }

        [SlashCommand("StartGiveaway", "Starts a giveaway")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task StartGiveaway(InteractionContext ctx,
        [Option("giveawayID", "Message ID of the Giveaway")] string giveawayID)
        {
            await ctx.CreateResponseAsync(":tada: Starting Giveaway :tada:");
            await _giveawayController.StartGiveaway(ctx.Channel, giveawayID);
        }

        public async Task<DiscordMessage> sendMessage(InteractionContext ctx, string message)
        {
            // sends a message to the channel where the slash command was invoked
            var msg = new DiscordMessageBuilder()
                    .WithContent(message)
                    .SendAsync(ctx.Channel);
            return msg.Result;
        }

        // Sends a private message to the user who triggered the interaction and returns the resulting DiscordInteractionResponseBuilder object
        public async Task<DiscordInteractionResponseBuilder> sendPrivateMessage(InteractionContext ctx, string message)
        {
            // Creates a new DiscordInteractionResponseBuilder object with the provided message content
            var msg = new DiscordInteractionResponseBuilder()
                    .WithContent(message);

            // Sets the message to ephemeral, meaning only the user who triggered the interaction can see it
            msg.AsEphemeral(true);

            // Sends the response to the user
            await ctx.CreateResponseAsync(msg);

            // Returns the response message object
            return msg;
        }
    }
}