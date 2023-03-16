using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.AspNetCore.Mvc;
using DSharpPlus.Interactivity;
using System.Linq;
using System.Threading.Channels;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using GiveawayProject.Commands;

namespace GiveawayProject.Commands
{
    //NOTE: Normal Commands can't have
    public class GiveawayNormalCommands : BaseCommandModule
    {
        IGiveawayController _giveawayController;

        public GiveawayNormalCommands(IGiveawayController giveawayController)
        {
            _giveawayController = giveawayController;
        }

        [Command("test")]
        public async Task test(CommandContext ctx)
        {
            await ctx.RespondAsync($"test number: {_giveawayController.interfaceCheck()}");
        }

        [Command("ping")]
        public async Task Ping(CommandContext ctx)
        {
            await ctx.RespondAsync("Pong!"); //online check
        }

        [Command("CreateGiveaway")]
        [Description("Giveaway Creation")]
        public async Task CreateGiveaway(CommandContext ctx)
        {
            DiscordChannel channel = getChannelResponse(ctx).Result;
            if (channel == null)
                return; //cancelled/timed out.

            timeUnit time = getTimeResponse(ctx, channel).Result;
            if (time.getTimeNumber() == 0)
                return; //cancelled/timed out.

            int numberOfWinners = getWinnersResponse(ctx, time).Result;
            if (time.getTimeNumber() == 0)
                return; //cancelled/timed out.

            string item = getItemResponse(ctx, numberOfWinners).Result;
            if (time.getTimeNumber() == 0)
                return; //cancelled/timed out.

            Giveaway giveaway = _giveawayController.CreateGiveaway(ctx.Client, ctx.Member, item, numberOfWinners, time, channel);
            DiscordMessage message = await sendMessage(ctx.Channel, $"Giveway created,\nTo confirm before giveaway starts:\nItem: {item}, numberOfWinners: {numberOfWinners}, Channel: {channel}, Time duration: {time.getTimeNumber()} {time.getTimeUnit()}\n\n Type `/StartMessage (Message ID)` to start Giveaway.");
            _giveawayController.AttachMessage(message, giveaway);
        }

        public async Task<DiscordChannel> getChannelResponse(CommandContext ctx)
        {
            var response = QuestionResponse(ctx, ":tada: Welcome to the giveaway creation!\n" +
                "What channel will the giveaway be in?\n" +
                "Type `cancel` to stop the process\n\n" +
                "`Type in the channel name`").Result;

            DiscordChannel channel = null;
            while (channel == null)
            {
                if (!response.TimedOut) // this triggers if you didn't reach the timeout delay configured in the Interactions options, when the "next message" you're waiting for is read.
                {
                    if (response.Result.Content.ToLower() == "cancel")
                    {
                        await ctx.RespondAsync($"Cancelling giveaway process!");
                        return null;
                    }

                    // assuming you have the response string stored in a variable called "response"
                    var regex = new Regex(@"<#(\d+)>"); // match a pattern of "<#channel_id>"
                    var match = regex.Match(response.Result.Content);
                    if (match.Success)
                    {
                        var channelId = ulong.Parse(match.Groups[1].Value);
                        channel = ctx.Guild.GetChannel(channelId);
                        return channel;
                    }
                }
                else // timed out
                {
                    await ctx.RespondAsync($"Timed out giveaway process!");
                    return null; // exit loop.
                }

                response = QuestionResponse(ctx, "It seems you didn't input a correct channel string (<#ChannelID>)\n" +
                    "Type `cancel` to stop the process\n\n" +
                    "`Type in the channel name`").Result;

                channel = null;
            }

            return null;
        }

        public async Task<timeUnit> getTimeResponse(CommandContext ctx, DiscordChannel channel)
        {
            var response = QuestionResponse(ctx, $":tada: Giveaway channel set to {channel.Mention}!\n" +
                "Next, how long will the giveaway last ?\n" +
                "Type `cancel` to stop the process\n\n" +
                "`Type in duration in seconds.`\n" +
                "`For minutes add M at the end, hours a H, or days a D`").Result;

            timeUnit _timeUnit = null;
            while (_timeUnit == null)
            {
                if (!response.TimedOut) // this triggers if you didn't reach the timeout delay configured in the Interactions options, when the "next message" you're waiting for is read.
                {
                    if (response.Result.Content.ToLower() == "cancel")
                    {
                        await ctx.RespondAsync($"Cancelling giveaway process!");
                        return null;
                    }

                    try
                    {
                        _timeUnit = await ConvertDurationToSeconds(response.Result.Content);
                        return _timeUnit;
                    }
                    catch (Exception e) { Console.WriteLine($"Exception got: {e}"); };
                }
                else // timed out
                {
                    await ctx.RespondAsync($"Timed out giveaway process!");
                    return null; // exit loop.
                }

                response = QuestionResponse(ctx, "It seems you didn't input a correct time option (e.g. 1000, 1M, 2H, 3D)\n" +
                    "Type `cancel` to stop the process\n\n" +
                    "`Type in the time option name`").Result;

                _timeUnit = null;
            }


            return new timeUnit(timeOptions.minutes, 0); //shouldn't reach this line.
        }

        public async Task<int> getWinnersResponse(CommandContext ctx, timeUnit _timeUnit)
        {
            var response = QuestionResponse(ctx, $":tada: Giveaway set to end in {_timeUnit.getTimeNumber()}{char.ToUpper(_timeUnit.getTimeUnit().ToString()[0])}!\n" +
                $"Next, how many winners should there be?\n\n" +
                $"Type 'cancel' to stop the process\n" +
                $"Type in the amount of winners").Result;

            int _numberOfWinners = 0;
            while (_numberOfWinners == 0)
            {
                if (!response.TimedOut) // this triggers if you didn't reach the timeout delay configured in the Interactions options, when the "next message" you're waiting for is read.
                {
                    if (response.Result.Content.ToLower() == "cancel")
                    {
                        await ctx.RespondAsync($"Cancelling giveaway process!");
                        return 0;
                    }

                    try
                    {
                        _numberOfWinners = int.Parse(response.Result.Content);
                        return _numberOfWinners;
                    }
                    catch (Exception e) { Console.WriteLine($"Exception got: {e}"); };
                }
                else // timed out
                {
                    await ctx.RespondAsync($"Timed out giveaway process!");
                    return 0; // exit loop.
                }

                response = QuestionResponse(ctx, "It seems you didn't input a correct number of winners option\n" +
                    "Type `cancel` to stop the process\n\n" +
                    "`Type in the time option name`").Result;

                _timeUnit = null;
            }


            return 0; //shouldn't reach this line.
        }

        public async Task<string> getItemResponse(CommandContext ctx, int numberOfWinners)
        {
            var response = QuestionResponse(ctx, $":tada: Giveaway set to have 10 winners\n" +
                $"Next, what are you giving away ?\n\n" +
                $"Type `cancel` to stop the process\n" +
                $"`Type in the prize!`").Result;

            string _item = null;
            while (_item == null)
            {
                if (!response.TimedOut) // this triggers if you didn't reach the timeout delay configured in the Interactions options, when the "next message" you're waiting for is read.
                {
                    if (response.Result.Content.ToLower() == "cancel")
                    {
                        await ctx.RespondAsync($"Cancelling giveaway process!");
                        return null;
                    }

                    try
                    {
                        _item = response.Result.Content;
                        return _item;
                    }
                    catch (Exception e) { Console.WriteLine($"Exception got: {e}"); };
                }
                else // timed out
                {
                    await ctx.RespondAsync($"Timed out giveaway process!");
                    return null; // exit loop.
                }

                response = QuestionResponse(ctx, "It seems you didn't input a item\n" +
                    "Type `cancel` to stop the process\n\n" +
                    "`Type in the prize!`").Result;

                _item = null;
            }

            return null; //shouldn't reach this line.
        }

        public async Task<InteractivityResult<DiscordMessage>> QuestionResponse(CommandContext ctx, string message)
        {
            var msg = await ctx.RespondAsync(message);

            var result = await ctx.Message.GetNextMessageAsync(); //awaiting channel input from user.

            return result;
        }

        public async Task<timeUnit> ConvertDurationToSeconds(string durationString)
        {
            // Parse the duration string to extract the numeric value and unit
            var match = Regex.Match(durationString, @"^(\d+)([dhms])?$");
            if (!match.Success)
            {
                throw new ArgumentException("Invalid duration string.");
            }
            var value = int.Parse(match.Groups[1].Value);
            var unit = match.Groups[2].Value;

            timeOptions timeOption;
            // Convert the value to seconds based on the unit
            switch (unit.ToLower())
            {
                case "d":
                    timeOption = timeOptions.days;
                    break;
                case "h":
                    timeOption = timeOptions.hours;
                    break;
                case "m":
                    timeOption = timeOptions.minutes;
                    break;
                default:
                    throw new ArgumentException("Invalid duration unit.");
            }

            return new timeUnit(timeOption, value);
        }

        [Command("GetNextMessage")]
        [Description("GetNextMessage")]
        public async Task GetNextMessage(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("Listening :eyes:");

            var result = await ctx.Message.GetNextMessageAsync(); //ctx is the CommandContext

            if (!result.TimedOut) //this triggers if you didn't reach the timeout delay configured in the Interactions options, when the "next message" you're waiting for is read.
            {
                //Here, you can do what you want. Check the content of the message (result.Result.Content), or whatever. This is usually where the bot will do something with the answer it was waiting.
                await ctx.Channel.SendMessageAsync("You said :" + result.Result.Content); // for example.
            }
        }

        [Command("RerollGiveaway")]
        [Description("Reroll Giveaway")]
        [RequirePermissions(Permissions.Administrator)]
        public async Task RerollGiveaway(CommandContext ctx,
        [Description("Message ID of the Giveaway")] string giveawayID)
        {
            await ctx.RespondAsync("Rerolling giveaway!");
            await _giveawayController.RerollGiveaway(ctx.Channel, giveawayID);
        }

        [Command("GetGiveawayList")]
        [Description("Gets List of Giveaway")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task GetGiveawayList(CommandContext ctx)
        {
            await ctx.RespondAsync(_giveawayController.GetGiveawayList().Result);
        }

        [Command("StartGiveaway")]
        [Description("Starts a giveaway")]
        [RequirePermissions(Permissions.Administrator)]
        public async Task StartGiveaway(CommandContext ctx,
        [Description("Message ID of the Giveaway")] string giveawayID)
        {
            await ctx.RespondAsync(":tada: Starting Giveaway :tada:");
            await _giveawayController.StartGiveaway(ctx.Channel, giveawayID);
        }

        // Sends a message to the user who triggered the interaction and returns the resulting DiscordInteractionResponseBuilder object
        public async Task<DiscordMessage> sendMessage(DiscordChannel channel, string message) //built so it is easier to send messages.
        {
            var msg = new DiscordMessageBuilder()
                    .WithContent(message)
                    .SendAsync(channel);
            return msg.Result;
        }
    }
}
