using System;
using System.Threading.Tasks;
using Bot.Configurations;
using Bot.Discord.Timers;
using Bot.Interfaces.Discord.Handlers;
using Bot.Interfaces.Discord.Handlers.CommandHandlers;
using Bot.Interfaces.Discord.Services;
using Bot.LastFM.Helpers;
using Discord;
using Discord.WebSocket;
using IConnection = Bot.Interfaces.Discord.IConnection;

namespace Bot.Discord
{
    public class Connection : IConnection
    {
        private readonly DiscordShardedClient _client;
        private readonly IClientLogHandler _clientLogHandler;
        private readonly ICommandHandler _commandHandler;
        private readonly IPrefixService _prefixService;
        private readonly ApiTester _lastFMApiTester;
        private readonly DiscordBotListsUpdateTimer _botListsUpdateTimer;


        /// <summary>
        /// Creates a new <see cref="Connection"/>.
        /// </summary>
        /// <param name="client">The <see cref="DiscordShardedClient"/> that will be used.</param>
        /// <param name="clientLogHandler">The <see cref="IClientLogHandler"/> that will log all the log messages.</param>
        /// <param name="commandHandler">The <see cref="ICommandHandler"/> that will handle all the commands.</param>
        /// <param name="prefixService">The <see cref="IPrefixService"/> That will be used for the custom prefixes.</param>
        /// <param name="botListsUpdateTimer">The timer that will be used to active UpdateBotListStatsAsync().</param>
        public Connection(DiscordShardedClient client, IClientLogHandler clientLogHandler, ICommandHandler commandHandler, IPrefixService prefixService, DiscordBotListsUpdateTimer botListsUpdateTimer, ApiTester apiTester)
        {
            this._client = client;
            this._clientLogHandler = clientLogHandler;
            this._commandHandler = commandHandler;
            this._prefixService = prefixService;
            this._botListsUpdateTimer = botListsUpdateTimer;
            this._lastFMApiTester = apiTester;
        }


        /// <inheritdoc />
        public async Task ConnectAsync()
        {
            // Start the connection to discord
            await this._client.LoginAsync(TokenType.Bot, ConfigData.Data.Token).ConfigureAwait(false);
            await this._client.StartAsync().ConfigureAwait(false);

            // Initialize all the client logging
            this._clientLogHandler.Initialize();

            // Check Last.FM keys
            await this._lastFMApiTester.CheckApiKeyAsync();

            // Load all the custom prefixes
            await this._prefixService.LoadAllPrefixes().ConfigureAwait(false);

            await this._commandHandler.InitializeAsync().ConfigureAwait(false);

            // Wait the thread so the console application doesn't close.
            await Task.Delay(TimeSpan.FromDays(ConfigData.Data.RestartTime)).ConfigureAwait(false);
            await this._client.StopAsync().ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
    }
}