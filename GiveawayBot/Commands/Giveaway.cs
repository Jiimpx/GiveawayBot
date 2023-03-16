using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.CommandsNext;
using DSharpPlus;
using GiveawayBot.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using DSharpPlus.CommandsNext;
using GiveawayProject.Commands;

namespace GiveawayProject.Commands
{
    public class Giveaway
    {
        GiveawayController _giveawayController;

        // class variables
        GiveawayController _parent;  // object instance of a parent class
        List<DiscordUser> _participants = new List<DiscordUser>();  // a list of Discord users participating in the Giveaway
        public string _item { get; private set; } // the item being given away
        public DiscordMember _host { get; private set; } // the Discord user hosting the Giveaway
        public DiscordChannel _channel { get; private set; } // the channel in which the Giveaway takes place
        public DiscordMessage _message { get; set; }  // the Giveaway message sent to the channel
        System.Timers.Timer _timer;  // timer used to manage the Giveaway
        public long _timerDuration { get; private set; }  // the duration of the Giveaway in milliseconds
        public int _numberOfWinners { get; private set; }  // the number of winners to be chosen
        List<DiscordUser> _winners;  // a list of the winners of the Giveaway
        Random _rnd;  // random number generator
        long _unixEndTime;  // the end time of the Giveaway in Unix time
        DiscordClient _client;  // the Discord client instance
        System.Timers.Timer _deathTimer;  // timer used to manage the death of the Giveaway
        public bool started = false; // start flag

        // constructor
        public Giveaway(DiscordClient client, GiveawayController parent, DiscordMember host, string item, long numberOfWinners, DiscordChannel channel, long durationInSeconds, GiveawayController giveawayController)
        {
            _giveawayController = giveawayController;
            _client = client;
            _parent = parent;
            _item = item;
            _host = host;
            _numberOfWinners = (int)numberOfWinners;
            _channel = channel;
            _timerDuration = durationInSeconds * 1000;  // converts the duration to milliseconds
            _rnd = new Random();  // initializes the random number generator
            _winners = new List<DiscordUser>();  // initializes the list of winners

            _timer = new System.Timers.Timer(_timerDuration);  // initializes the timer with the duration
            _timer.Elapsed += giveawayGetReactions;  // assigns a method to the Elapsed event of the timer
            _timer.AutoReset = false;  // disables the automatic resetting of the timer
        }

        // method to start the Giveaway
        public async Task startGiveaway()
        {
            if (started) //makes sure we don't start a giveaway twice
                return;

            // Calculate the end time of the Giveaway and convert it to Unix time
            var endTime = DateTimeOffset.UtcNow.Add(TimeSpan.FromMilliseconds(_timerDuration));
            _unixEndTime = endTime.ToUnixTimeSeconds();

            // Send the Giveaway message with information about the prize, end time, host, and number of entries and winners
            _message = await sendEmbed(_channel, DiscordColor.Blue, _item, $"Ends: <t:{_unixEndTime}:R> (<t:{_unixEndTime}:f>)\n" +
                                                                                    $"Hosted by: {_host.Mention}\n" +
                                                                                    $"Winners: {_numberOfWinners}");

            // Add the emote to the message
            await _message.CreateReactionAsync(_giveawayController.emoji);

            // Start timer
            _timer.Enabled = true;

            //set started to true.
            started = true;
        }

        async void giveawayGetReactions(object Source, ElapsedEventArgs e)
        {
            // gets all people who reacted, including the bot
            var reactionList = await _message.GetReactionsAsync(_giveawayController.emoji);

            // convert the reaction list to an editable list of DiscordUser objects
            _participants = new List<DiscordUser>(reactionList);

            // remove the bot from the list of participants (since the bot set up the reaction)
            _participants.Remove(_client.CurrentUser);

            // call the giveawayResults method to determine the winners and send the results
            await giveawayResults();
        }

        public async Task rerollGiveaway()
        {
            // clear the list of winners to rerun the Giveaway
            _winners.Clear();

            // call the giveawayResults method to determine the new winners
            await giveawayResults();
        }

        async Task giveawayResults()
        {
            // randomly select winners from the list of participants until the desired number of winners is reached
            while (_winners.Count < _numberOfWinners && _participants.Count > 0)
            {
                int i = _rnd.Next(_participants.Count);
                _winners.Add(_participants[i]);
                _participants.RemoveAt(i);
            }
            // create a string with the mentions of the winners
            string output = "";
            foreach (DiscordUser winner in _winners)
                output += winner.Mention + ", ";

            // remove the trailing comma and space from the output string
            output = output.TrimEnd(", ".ToCharArray());

            // update the Giveaway message with the results
            await editEmbed(_message, DiscordColor.Red, _item, $"Ended <t:{_unixEndTime}:R> (<t:{_unixEndTime}:f>)\n" +
                                                                                    $"Hosted by: {_host.Mention}\n" +
                                                                                    $"Winners: {output}");

            // start or reset the death timer
            await resetDeathTimer();

            // returns text announcing winners.
            await sendMessage(_channel, $"Congratulations {output}! You have won **{_item}**!");
        }

        async Task resetDeathTimer()
        {
            if (_deathTimer == null)
            {
                // If the death timer has not been created yet, create it and start it.
                // This timer will run for a week before firing, and will only fire once.
                _deathTimer = new System.Timers.Timer(7 * 24 * 60 * 60 * 1000);
                _deathTimer.Elapsed += killGiveaway;
                _deathTimer.AutoReset = false;
                _deathTimer.Start();
            }
            else
            {
                // If the death timer already exists, stop and restart it.
                // This is done to reset the timer whenever the Giveaway is interacted with.
                _deathTimer.Stop();
                _deathTimer.Start();
            }
        }

        async void killGiveaway(object Source, ElapsedEventArgs e)
        {
            // When the death timer fires, call the parent object's killGiveaway method, passing in this Giveaway object.
            Console.WriteLine($"Killed the Giveaway for {_item}");
            _parent.killGiveaway(this);
        }

        public async Task<DiscordMessage> sendMessage(DiscordChannel channel, string message)
        {
            // sends a message to the channel where the slash command was invoked
            var msg = new DiscordMessageBuilder()
                    .WithContent(message)
                    .SendAsync(channel);
            return msg.Result;
        }

        public async Task<DiscordMessage> editMessage(DiscordMessage message, string newContent)
        {
            // edits a message with new content
            await message.ModifyAsync(new DiscordMessageBuilder()
                .WithContent(newContent));
            return message;
        }

        async Task<DiscordMessage> sendEmbed(DiscordChannel channel, DiscordColor embedColor, string title, string embedMessage)
        {
            DiscordEmbed builder;

            // Create a new embed builder and set its properties
            builder = new DiscordEmbedBuilder()
            {
                Color = embedColor,
                Title = title,
                Description = embedMessage
            }.Build();

            // Send the embed to the specified channel and return the resulting message
            DiscordMessage message = await channel.SendMessageAsync(builder);
            return message;
        }

        async Task<DiscordMessage> editEmbed(DiscordMessage message, DiscordColor embedColor, string title, string embedMessage)
        {
            DiscordEmbed builder;

            // Create a new embed builder and set its properties
            builder = new DiscordEmbedBuilder()
            {
                Color = embedColor,
                Title = title,
                Description = embedMessage
            }.Build();

            // Modify the existing message with the new embed and return it
            await message.ModifyAsync(new DiscordMessageBuilder()
                .WithEmbed(builder));
            return message;
        }
    }
}
