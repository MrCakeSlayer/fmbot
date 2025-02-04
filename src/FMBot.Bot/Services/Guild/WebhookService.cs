using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Webhook;
using Discord.WebSocket;
using FMBot.Bot.Extensions;
using FMBot.Domain.Models;
using FMBot.Persistence.Domain.Models;
using FMBot.Persistence.EntityFrameWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

namespace FMBot.Bot.Services.Guild
{
    public class WebhookService
    {
        private readonly IDbContextFactory<FMBotDbContext> _contextFactory;
        private readonly string _avatarImagePath;
        private readonly BotSettings _botSettings;
        private readonly GuildService _guildService;

        public WebhookService(IDbContextFactory<FMBotDbContext> contextFactory, IOptions<BotSettings> botSettings, GuildService guildService)
        {
            this._contextFactory = contextFactory;
            this._guildService = guildService;
            this._botSettings = botSettings.Value;

            this._avatarImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "default-avatar.png");

            if (!File.Exists(this._avatarImagePath))
            {
                Log.Information("Downloading avatar...");
                var wc = new System.Net.WebClient();
                wc.DownloadFile("https://fmbot.xyz/img/bot/avatar.png", this._avatarImagePath);
            }
        }

        public async Task<Webhook> CreateWebhook(ICommandContext context, int guildId)
        {
            await using var fs = File.OpenRead(this._avatarImagePath);

            var socketWebChannel = context.Channel as SocketTextChannel;

            if (socketWebChannel == null)
            {
                return null;
            }

            var botType = context.GetBotType();

            var botTypeName = botType == BotType.Production ? "" : botType == BotType.Develop ? " develop" : " local";
            var newWebhook = await socketWebChannel.CreateWebhookAsync($".fmbot{botTypeName} featured", fs,
                new RequestOptions { AuditLogReason = "Created webhook for .fmbot featured feed." });

            await using var db = this._contextFactory.CreateDbContext();
            var webhook = new Webhook
            {
                GuildId = guildId,
                BotType = botType,
                Created = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                DiscordWebhookId = newWebhook.Id,
                Token = newWebhook.Token
            };

            await db.Webhooks.AddAsync(webhook);
            await db.SaveChangesAsync();

            Log.Information("Created webhook for guild {guildId}", guildId);

            return webhook;
        }

        public async Task<bool> TestWebhook(Webhook webhook, string text)
        {
            try
            {
                var webhookClient = new DiscordWebhookClient(webhook.DiscordWebhookId, webhook.Token);

                await webhookClient.SendMessageAsync(text);

                return true;
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Could not find"))
                {
                    await using var db = this._contextFactory.CreateDbContext();

                    db.Webhooks.Remove(webhook);
                    await db.SaveChangesAsync();

                    Log.Information("Removed webhook from database for guild {guildId}", webhook.GuildId);
                }
                else
                {
                    Log.Error(e, "Unknown error while testing webhook for {guildId}", webhook.GuildId);
                }

                return false;
            }
        }

        public async Task SendFeaturedWebhooks(BotType botType, FeaturedLog featured)
        {
            var embed = new EmbedBuilder();
            embed.WithThumbnailUrl(featured.ImageUrl);
            embed.AddField("Featured:", featured.Description);

            await using var db = await this._contextFactory.CreateDbContextAsync();
            var webhooks = await db.Webhooks
                .AsQueryable()
                .ToListAsync();

            foreach (var webhook in webhooks)
            {
                await SendWebhookEmbed(webhook, embed, featured.UserId);
            }
        }

        public static async Task SendFeaturedPreview(FeaturedLog featured, string webhook)
        {
            var embed = new EmbedBuilder();
            embed.WithImageUrl(featured.ImageUrl);
            embed.AddField("Featured:", featured.Description);

            var dateValue = ((DateTimeOffset)featured.DateTime).ToUnixTimeSeconds();
            embed.AddField("Time", $"<t:{dateValue}:F>");
            embed.AddField("Resetting", $"`.resetfeatured {featured.FeaturedLogId}`");

            embed.WithFooter(featured.ImageUrl);

            var webhookClient = new DiscordWebhookClient(webhook);
            await webhookClient.SendMessageAsync(embeds: new[] { embed.Build() });
        }

        public async Task PostFeatured(FeaturedLog featuredLog, DiscordShardedClient client)
        {
            var builder = new EmbedBuilder();
            builder.WithThumbnailUrl(featuredLog.ImageUrl);
            builder.AddField("Featured:", featuredLog.Description);

            if (this._botSettings.Bot.BaseServerId != 0 && this._botSettings.Bot.FeaturedChannelId != 0)
            {
                var guild = client.GetGuild(this._botSettings.Bot.BaseServerId);
                var channel = guild?.GetTextChannel(this._botSettings.Bot.FeaturedChannelId);

                if (channel != null)
                {
                    if (featuredLog.UserId.HasValue)
                    {
                        await using var db = await this._contextFactory.CreateDbContextAsync();
                        var dbGuild = await db.Guilds
                            .AsQueryable()
                            .Include(i => i.GuildUsers)
                            .ThenInclude(i => i.User)
                            .FirstOrDefaultAsync(f => f.DiscordGuildId == this._botSettings.Bot.BaseServerId);

                        if (dbGuild?.GuildUsers != null && dbGuild.GuildUsers.Any())
                        {
                            var guildUser = dbGuild.GuildUsers.FirstOrDefault(f => f.UserId == featuredLog.UserId);

                            if (guildUser != null)
                            {
                                var localFeaturedMsg = await channel.SendMessageAsync(
                                    $"🥳 Congratulations <@{guildUser.User.DiscordUserId}>! You've just been picked as the featured user for the next hour.",
                                    false,
                                    builder.Build());

                                if(localFeaturedMsg != null)
                                {
                                    await this._guildService.AddReactionsAsync(localFeaturedMsg, guild, true);
                                }

                                return;
                            }
                        }
                    }

                    var message = await channel.SendMessageAsync("", false, builder.Build());

                    if (message != null)
                    {
                        await this._guildService.AddReactionsAsync(message, guild);
                    }
                }
            }
            else
            {
                Log.Warning("Featured channel not set, not sending featured message");
            }
        }

        private async Task SendWebhookEmbed(Webhook webhook, EmbedBuilder embed, int? featuredUserId)
        {
            try
            {
                var webhookClient = new DiscordWebhookClient(webhook.DiscordWebhookId, webhook.Token);

                if (featuredUserId.HasValue)
                {
                    await using var db = await this._contextFactory.CreateDbContextAsync();
                    var guild = await db.Guilds
                        .AsQueryable()
                        .Include(i => i.GuildUsers)
                        .FirstOrDefaultAsync(f => f.GuildId == webhook.GuildId);

                    if (guild?.GuildUsers != null && guild.GuildUsers.Any())
                    {
                        var guildUser = guild.GuildUsers.FirstOrDefault(f => f.UserId == featuredUserId);

                        if (guildUser != null)
                        {
                            embed.WithFooter($"🥳 Congratulations! This user is in your server under the name {guildUser.UserName}.");
                        }
                    }
                }

                await webhookClient.SendMessageAsync(embeds: new[] { embed.Build() });
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Could not find"))
                {
                    await using var db = await this._contextFactory.CreateDbContextAsync();

                    db.Webhooks.Remove(webhook);
                    await db.SaveChangesAsync();

                    Log.Information("Removed webhook from database for guild {guildId}", webhook.GuildId);
                }
                else
                {
                    Log.Error(e, "Unknown error while testing webhook for {guildId}", webhook.GuildId);
                }
            }
        }
    }
}
