using DSharpPlus.Entities;
using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GiveawayProject.Commands;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using System.Text.RegularExpressions;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;

// This class acts like a brain of the program. It reduces duplication of code, and handles the Giveaway objects. Both the Slash commands and Normal commands pass their commands to this Controller.
namespace GiveawayProject.Commands
{
    public class GiveawayController : IGiveawayController
    {
        // celebation emoji
        public DiscordEmoji emoji { get; set; }

        // Used for random number to test interface.
        private long _testnumber = 0;

        // List of giveaways, initialized as an empty list
        private List<Giveaway> giveawayList = new List<Giveaway>();

        public long interfaceCheck()
        {
            if (_testnumber == 0)
            {
                // Create a new instance of the Random class.
                Random random = new Random();

                // Generate two random integers between 0 and int.MaxValue (inclusive), and combine them to create a random long value.
                _testnumber = ((long)random.Next(int.MaxValue) << 32) | random.Next(int.MaxValue);
            }

            return _testnumber;
        }

        // Creating a Giveaway
        public Giveaway CreateGiveaway(DiscordClient client, DiscordMember member, string item, long numberOfWinners, timeUnit _timeUnit, DiscordChannel channel)
        {
            //checking if emoji is created, if not, get it and store it
            if (emoji == null)
                emoji = DiscordEmoji.FromName(client, ":tada:");

            // Get time duration from timeUnit
            long duration = _timeUnit.getDuration();

            // Create a new Giveaway object
            Giveaway giveaway = new Giveaway(client, this, member, item, numberOfWinners, channel, duration, this);

            // Add the Giveaway object to the list of giveaways
            giveawayList.Add(giveaway);

            //returns the Giveaway Object for confirmation.
            return giveaway;
        }
        public Giveaway CreateGiveaway(object ctx)
        {
            DiscordChannel channel = null;
            timeUnit time = null;
            int numberOfWinners = 0;
            string item = null;
            DiscordClient client = null;
            DiscordMember member = null;

            if (ctx is CommandContext)
            {
                CommandContext commandctx = (CommandContext)ctx;
                DiscordChannel sendChannel = commandctx.Channel;
                channel = getChannelResponse(commandctx, sendChannel).Result;
                if (channel == null)
                    return null; //cancelled/timed out.

                time = getTimeResponse(commandctx, channel, sendChannel).Result;
                if (time.getTimeNumber() == 0)
                    return null; //cancelled/timed out.

                numberOfWinners = getWinnersResponse(commandctx, time, sendChannel).Result;
                if (time.getTimeNumber() == 0)
                    return null; //cancelled/timed out.

                item = getItemResponse(ctx, numberOfWinners, sendChannel).Result;
                if (time.getTimeNumber() == 0)
                    return null; //cancelled/timed out.

                client = commandctx.Client;
                member = commandctx.Member;
            }
            else if (ctx is InteractionContext)
            {
                InteractionContext interactionctx = (InteractionContext)ctx;
                DiscordChannel sendChannel = interactionctx.Channel;
                channel = getChannelResponse(interactionctx, sendChannel).Result;
                if (channel == null)
                    return null; //cancelled/timed out.

                time = getTimeResponse(interactionctx, channel, sendChannel).Result;
                if (time.getTimeNumber() == 0)
                    return null; //cancelled/timed out.

                numberOfWinners = getWinnersResponse(interactionctx, time, sendChannel).Result;
                if (time.getTimeNumber() == 0)
                    return null; //cancelled/timed out.

                item = getItemResponse(ctx, numberOfWinners, sendChannel).Result;
                if (time.getTimeNumber() == 0)
                    return null; //cancelled/timed out.

                client = interactionctx.Client;
                member = interactionctx.Member;
            }

            if (emoji == null)
                emoji = DiscordEmoji.FromName(client, ":tada:");

            Giveaway giveaway = CreateGiveaway(client, member, item, numberOfWinners, time, channel);
            DiscordMessage message = sendMessage(channel, $"Giveway created,\nTo confirm before giveaway starts:\nItem: {item}, numberOfWinners: {numberOfWinners}, Channel: {channel}, Time duration: {time.getTimeNumber()} {time.getTimeUnit()}\n\n Type `/StartMessage (Message ID)` to start Giveaway.").Result;
            AttachMessage(message, giveaway);
            return giveaway;
        }

        public async Task<DiscordChannel> getChannelResponse(object ctx, DiscordChannel sendChannel)
        {
            InteractivityResult<DiscordMessage> response = new InteractivityResult<DiscordMessage>();
            if (ctx is CommandContext)
            {
                response = QuestionResponse((CommandContext)ctx, ":tada: Welcome to the giveaway creation!\n" +
                "What channel will the giveaway be in?\n" +
                "Type `cancel` to stop the process\n\n" +
                "`Type in the channel name`").Result;
            }
            else if (ctx is InteractionContext)
            {
                response = QuestionResponse((InteractionContext)ctx, ":tada: Welcome to the giveaway creation!\n" +
                "What channel will the giveaway be in?\n" +
                "Type `cancel` to stop the process\n\n" +
                "`Type in the channel name`").Result;
            }

            DiscordChannel channel = null;
            while (channel == null)
            {
                if (!response.TimedOut) // this triggers if you didn't reach the timeout delay configured in the Interactions options, when the "next message" you're waiting for is read.
                {
                    if (response.Result.Content.ToLower() == "cancel")
                    {
                        await sendMessage(sendChannel, $"Cancelling giveaway process!");
                        return null;
                    }

                    // assuming you have the response string stored in a variable called "response"
                    var regex = new Regex(@"<#(\d+)>"); // match a pattern of "<#channel_id>"
                    var match = regex.Match(response.Result.Content);
                    if (match.Success)
                    {
                        var channelId = ulong.Parse(match.Groups[1].Value);
                        if (ctx is CommandContext)
                        {
                            CommandContext commandContext = (CommandContext)ctx;
                            channel = commandContext.Guild.GetChannel(channelId);
                        }
                        else
                        {
                            InteractionContext interactionContext = (InteractionContext)ctx;
                            channel = interactionContext.Guild.GetChannel(channelId);
                        }
                        return channel;
                    }
                }
                else // timed out
                {
                    await sendMessage(sendChannel, $"Timed out giveaway process!");
                    return null; // exit loop.
                }

                if (ctx is CommandContext)
                {
                    response = QuestionResponse((CommandContext)ctx, "It seems you didn't input a correct channel string (<#ChannelID>)\n" +
                    "Type `cancel` to stop the process\n\n" +
                    "`Type in the channel name`").Result;
                }
                else if (ctx is InteractionContext)
                {
                    response = QuestionResponse((InteractionContext)ctx, "It seems you didn't input a correct channel string (<#ChannelID>)\n" +
                    "Type `cancel` to stop the process\n\n" +
                    "`Type in the channel name`").Result;
                }

                channel = null;
            }

            return null;
        }

        public async Task<timeUnit> getTimeResponse(object ctx, DiscordChannel channel, DiscordChannel sendChannel)
        {
            InteractivityResult<DiscordMessage> response = new InteractivityResult<DiscordMessage>();
            if (ctx is CommandContext)
            {
                response = QuestionResponse((CommandContext)ctx, $":tada: Giveaway channel set to {channel.Mention}!\n" +
                "Next, how long will the giveaway last ?\n" +
                "Type `cancel` to stop the process\n\n" +
                "`Type in duration in seconds.`\n" +
                "`For minutes add M at the end, hours a H, or days a D`").Result;
            }
            else if (ctx is InteractionContext)
            {
                response = QuestionResponse((InteractionContext)ctx, $":tada: Giveaway channel set to {channel.Mention}!\n" +
                "Next, how long will the giveaway last ?\n" +
                "Type `cancel` to stop the process\n\n" +
                "`Type in duration in seconds.`\n" +
                "`For minutes add M at the end, hours a H, or days a D`").Result;
            }

            timeUnit _timeUnit = null;
            while (_timeUnit == null)
            {
                if (!response.TimedOut) // this triggers if you didn't reach the timeout delay configured in the Interactions options, when the "next message" you're waiting for is read.
                {
                    if (response.Result.Content.ToLower() == "cancel")
                    {
                        await sendMessage(sendChannel, $"Cancelling giveaway process!");
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
                    await sendMessage(sendChannel, $"Timed out giveaway process!");
                    return null; // exit loop.
                }

                if (ctx is CommandContext)
                {
                    response = QuestionResponse((CommandContext)ctx, "It seems you didn't input a correct time option (e.g. 1000, 1M, 2H, 3D)\n" +
                    "Type `cancel` to stop the process\n\n" +
                    "`Type in the time option name`").Result;
                }
                else if (ctx is InteractionContext)
                {
                    response = QuestionResponse((InteractionContext)ctx, "It seems you didn't input a correct time option (e.g. 1000, 1M, 2H, 3D)\n" +
                    "Type `cancel` to stop the process\n\n" +
                    "`Type in the time option name`").Result;
                }

                _timeUnit = null;
            }


            return new timeUnit(timeOptions.minutes, 0); //shouldn't reach this line.
        }

        public async Task<int> getWinnersResponse(object ctx, timeUnit _timeUnit, DiscordChannel sendChannel)
        {
            InteractivityResult<DiscordMessage> response = new InteractivityResult<DiscordMessage>();
            if (ctx is CommandContext)
            {
                response = QuestionResponse((CommandContext)ctx, $":tada: Giveaway set to end in {_timeUnit.getTimeNumber()}{char.ToUpper(_timeUnit.getTimeUnit().ToString()[0])}!\n" +
                $"Next, how many winners should there be?\n\n" +
                $"Type 'cancel' to stop the process\n" +
                $"Type in the amount of winners").Result;
            }
            else if (ctx is InteractionContext)
            {
                response = QuestionResponse((InteractionContext)ctx, $":tada: Giveaway set to end in {_timeUnit.getTimeNumber()}{char.ToUpper(_timeUnit.getTimeUnit().ToString()[0])}!\n" +
                $"Next, how many winners should there be?\n\n" +
                $"Type 'cancel' to stop the process\n" +
                $"Type in the amount of winners").Result;
            }

            int _numberOfWinners = 0;
            while (_numberOfWinners == 0)
            {
                if (!response.TimedOut) // this triggers if you didn't reach the timeout delay configured in the Interactions options, when the "next message" you're waiting for is read.
                {
                    if (response.Result.Content.ToLower() == "cancel")
                    {
                        await sendMessage(sendChannel, $"Cancelling giveaway process!");
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
                    await sendMessage(sendChannel, $"Timed out giveaway process!");
                    return 0; // exit loop.
                }

                if (ctx is CommandContext)
                {
                    response = QuestionResponse((CommandContext)ctx, "It seems you didn't input a correct number of winners option\n" +
                    "Type `cancel` to stop the process\n\n" +
                    "`Type in the time option name`").Result;
                }
                else if (ctx is InteractionContext)
                {
                    response = QuestionResponse((InteractionContext)ctx, "It seems you didn't input a correct number of winners option\n" +
                    "Type `cancel` to stop the process\n\n" +
                    "`Type in the time option name`").Result;
                }

                _timeUnit = null;
            }


            return 0; //shouldn't reach this line.
        }

        public async Task<string> getItemResponse(object ctx, int numberOfWinners, DiscordChannel sendChannel)
        {
            InteractivityResult<DiscordMessage> response = new InteractivityResult<DiscordMessage>();
            if (ctx is CommandContext)
            {
                response = QuestionResponse((CommandContext)ctx, $":tada: Giveaway set to have {numberOfWinners} winners\n" +
                    $"Next, what are you giving away ?\n\n" +
                    $"Type `cancel` to stop the process\n" +
                    $"`Type in the prize!`").Result;
            }
            else if (ctx is InteractionContext)
            {
                response = QuestionResponse((InteractionContext)ctx, $":tada: Giveaway set to have 10 winners\n" +
                    $"Next, what are you giving away ?\n\n" +
                    $"Type `cancel` to stop the process\n" +
                    $"`Type in the prize!`").Result;
            }

            string _item = null;
            while (_item == null)
            {
                if (!response.TimedOut) // this triggers if you didn't reach the timeout delay configured in the Interactions options, when the "next message" you're waiting for is read.
                {
                    if (response.Result.Content.ToLower() == "cancel")
                    {
                        await sendMessage(sendChannel, $"Cancelling giveaway process!");
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
                    await sendMessage(sendChannel, $"Timed out giveaway process!");
                    return null; // exit loop.
                }

                if (ctx is CommandContext)
                {
                    response = QuestionResponse((CommandContext)ctx, "It seems you didn't input a item\n" +
                    "Type `cancel` to stop the process\n\n" +
                    "`Type in the prize!`").Result;
                }
                else if (ctx is InteractionContext)
                {
                    response = QuestionResponse((InteractionContext)ctx, "It seems you didn't input a item\n" +
                    "Type `cancel` to stop the process\n\n" +
                    "`Type in the prize!`").Result;
                }
                _item = null;
            }

            return null; //shouldn't reach this line.
        }

        public async Task<InteractivityResult<DiscordMessage>> QuestionResponse(CommandContext ctx, string message)
        {
            var msg = await sendMessage(ctx.Channel, message);

            var result = await ctx.Channel.GetNextMessageAsync(); //awaiting channel input from user.

            return result;
        }

        public async Task<InteractivityResult<DiscordMessage>> QuestionResponse(InteractionContext ctx, string message)
        {
            var msg = await sendMessage(ctx.Channel, message);

            var result = await ctx.Channel.GetNextMessageAsync();

            return result;
        }

        public async Task<timeUnit> ConvertDurationToSeconds(string durationString)
        {
            durationString = durationString.ToLower();
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

        public void AttachMessage(DiscordMessage message, Giveaway giveaway)
        {
            giveaway._message = message;
        }

        public async Task StartGiveaway(DiscordChannel channel, string giveawayID)
        {
            Giveaway startGiveaway = await getGiveawayFromMessageID(channel, giveawayID);

            if (startGiveaway == null)
            {
                return;
            }

            if (startGiveaway.started)
            {
                await sendMessage(channel, "Already started this giveaway!");
                return;
            }

            await startGiveaway.startGiveaway();
        }

        public async Task RerollGiveaway(DiscordChannel channel, string giveawayID)
        {
            Giveaway rerollGiveaway = await getGiveawayFromMessageID(channel, giveawayID);

            if (rerollGiveaway == null)
            {
                return;
            }

            // Otherwise, reroll the Giveaway
            await rerollGiveaway.rerollGiveaway();
        }

        public async Task<Giveaway> getGiveawayFromMessageID(DiscordChannel channel, string giveawayID)
        {
            // Parse the message Id from string to ulong
            if (!ulong.TryParse(giveawayID, out ulong giveawayMessageId))
            {
                await sendMessage(channel, "This is not a valid message ID, couldn't be parsed as ulong, please make sure it is correct!");
                return null;
            }

            // Search for the Giveaway with the same message Id
            Giveaway giveaway = null;
            foreach (Giveaway potentialGiveaway in giveawayList)
                if (potentialGiveaway._message.Id == giveawayMessageId)
                    giveaway = potentialGiveaway;

            // If the Giveaway was not found, send a private message to the user
            if (giveaway == null)
            {
                await sendMessage(channel, "I could not find a Giveaway for that Message ID, please make sure it is correct!");
                return null;
            }

            return giveaway;
        }

        public async Task<string> GetGiveawayList()
        {
            // If there is atleast one Giveaway in giveawayList
            if (giveawayList.Count == 0)
                return "There is no giveaways";

            /// Create a string with the list of giveaways
            string output = "";
            foreach (Giveaway Giveaway in giveawayList)
                output += $"Giveaway:{Giveaway._item}, ";
            // Send the list to the user
            return output;
        }

        public void killGiveaway(Giveaway giveawayToKill)
        {
            // Remove the given Giveaway from the list of active giveaways
            giveawayList.Remove(giveawayToKill);
        }

        public async Task<DiscordMessage> sendMessage(DiscordChannel channel, string message)
        {
            // sends a message to the channel where the slash command was invoked
            var msg = new DiscordMessageBuilder()
                    .WithContent(message)
                    .SendAsync(channel);
            return msg.Result;
        }
    }

    public enum timeOptions { minutes, hours, days };

    public interface IGiveawayController
    {
        long interfaceCheck();
        Giveaway CreateGiveaway(DiscordClient client, DiscordMember member, string item, long numberOfWinners, timeUnit _timeUnit, DiscordChannel channel);
        Giveaway CreateGiveaway(object ctx);
        Task StartGiveaway(DiscordChannel channel, string giveawayID);
        Task RerollGiveaway(DiscordChannel channel, string giveawayID);
        Task<string> GetGiveawayList();
        void AttachMessage(DiscordMessage messag, Giveaway giveaway);
        
        DiscordEmoji emoji { get; set; }
    }
}
