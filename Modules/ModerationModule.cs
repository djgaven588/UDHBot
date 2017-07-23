﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace DiscordBot
{
    public class ModerationModule : ModuleBase
    {
        private readonly LoggingService _logging;

        public ModerationModule(LoggingService logging)
        {
            _logging = logging;
        }

        [Command("mute"), Summary("Mute a user for a fixed duration")]
        [Alias("shutup", "stfu")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        async Task MuteUser(IUser user, uint arg)
        {
            var u = user as IGuildUser;

            await u.AddRoleAsync(Settings.GetMutedRole(Context.Guild));
            IUserMessage reply = await ReplyAsync("User " + user + " has been muted for " + arg + " seconds.");
            await _logging.LogAction($"{Context.User.Username} has muted {u.Username} for {arg} seconds");

            await Context.Message.DeleteAsync();

            await Task.Delay((int) arg * 1000);
            await reply.DeleteAsync();
            await UnmuteUser(user);
        }

        [Command("unmute"), Summary("Unmute a muted user")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        async Task UnmuteUser(IUser user)
        {
            var u = user as IGuildUser;

            Task deleteAsync = Context.Message?.DeleteAsync();
            if (deleteAsync != null) await deleteAsync;

            await u.RemoveRoleAsync(Settings.GetMutedRole(Context.Guild));
            IUserMessage reply = await ReplyAsync("User " + user + " has been unmuted.");
            await Task.Delay(TimeSpan.FromSeconds(10d));
            await reply.DeleteAsync();
        }

        [Command("addrole"), Summary("Add a role to a user")]
        [Alias("roleadd")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        async Task AddRole(IRole role, IUser user)
        {
            if (!Settings.IsRoleAssignable(role))
            {
                await ReplyAsync("Role is not assigneable");
                return;
            }

            var u = user as IGuildUser;
            await u.AddRoleAsync(role);
            await ReplyAsync("Role " + role + " has been added to " + user);
            await _logging.LogAction($"{Context.User.Username} has added role {role} to {u.Username}");
        }

        [Command("removerole"), Summary("Remove a role from a user")]
        [Alias("roleremove")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        async Task RemoveRole(IRole role, IUser user)
        {
            if (!Settings.IsRoleAssignable(role))
            {
                await ReplyAsync("Role is not assigneable");
                return;
            }

            var u = user as IGuildUser;

            await u.RemoveRoleAsync(role);
            await ReplyAsync("Role " + role + " has been removed from " + user);
            await _logging.LogAction($"{Context.User.Username} has removed role {role} from {u.Username}");
        }

        [Command("clear"), Summary("Remove last x messages")]
        [Alias("clean", "nuke", "purge")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        async Task ClearMessages(int count)
        {
            ITextChannel channel = Context.Channel as ITextChannel;

            var messages = await channel.GetMessagesAsync(count + 1).Flatten();
            await channel.DeleteMessagesAsync(messages);

            var m = await ReplyAsync("Messages deleted.");
            await Task.Delay(5000);
            await m.DeleteAsync();
            await _logging.LogAction($"{Context.User.Username} has removed {count} messages from {Context.Channel.Name}");
        }

        [Command("kick"), Summary("Kick a user")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        async Task KickUser(IUser user)
        {
            var u = user as IGuildUser;

            await u.KickAsync();
            await _logging.LogAction($"{Context.User.Username} has kicked {u.Username}");
        }

        [Command("ban"), Summary("Ban an user")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        async Task BanUser(IUser user)
        {
            await Context.Guild.AddBanAsync(user, 7, RequestOptions.Default);
            await _logging.LogAction($"{Context.User.Username} has banned {user.Username}");
        }

        [Command("rules"), Summary("Display rules of the current channel.")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        async Task Rules(int seconds = 60)
        {
            Rules(Context.Channel, seconds);
            await Context.Message.DeleteAsync();
        }

        [Command("rules"), Summary("Display rules of the mentionned channel.")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        async Task Rules(IMessageChannel channel, int seconds = 60)
        {
            //Display rules of this channel for x seconds
            Rule rule = Settings.GetRule(channel.Id);
            IUserMessage m;
            if (rule == null)
                m = await ReplyAsync(
                    "There is no special rule for this channel.\nPlease follow global rules (you can get them by typing `!globalrules`");
            else
                m = await ReplyAsync(
                    $"{rule.header}{(rule.text.Length > 0 ? rule.text : "There is no special rule for this channel.\nPlease follow global rules (you can get them by typing `!globalrules`")}");

            IDMChannel dm = await Context.User.GetDMChannelAsync();
            if (rule == null)
                await dm.SendMessageAsync(
                    "There is no special rule for this channel.\nPlease follow global rules (you can get them by typing `!globalrules`");
            else
                await dm.SendMessageAsync(
                    $"{rule.header}{(rule.text.Length > 0 ? rule.text : "There is no special rule for this channel.\nPlease follow global rules (you can get them by typing `!globalrules`")}");

            Task deleteAsync = Context.Message?.DeleteAsync();
            if (deleteAsync != null) await deleteAsync;

            if (seconds == -1)
                return;
            await Task.Delay(seconds * 1000);
            await m.DeleteAsync();
        }

        [Command("globalrules"), Summary("Display globalrules in current channel.")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        async Task GlobalRules(int seconds = 60)
        {
            //Display rules of this channel for x seconds
            string globalRules = Settings.GetRule(0).text;
            var m = await ReplyAsync(globalRules);
            await Context.Message.DeleteAsync();

            if (seconds == -1)
                return;
            await Task.Delay(seconds * 1000);
            await m.DeleteAsync();
        }
        
        [Command("channels"), Summary("Get a description of the channels.")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        async Task ChannelsDescription(int seconds = 60)
        {
            //Display rules of this channel for x seconds
            List<string> headers = Settings.GetChannelsHeader();
            StringBuilder sb = new StringBuilder();
            foreach (var h in headers)
            sb.Append(h + "\n");
            var m = await ReplyAsync(sb.ToString());
            await Context.Message.DeleteAsync();

            if (seconds == -1)
                return;
            await Task.Delay(seconds * 1000);
            await m.DeleteAsync();
        }
    }
}