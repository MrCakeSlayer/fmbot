using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Fergun.Interactive;
using Fergun.Interactive.Extensions;
using Fergun.Interactive.Pagination;
using Fergun.Interactive.Selection;
using FMBot.Bot.Resources;

namespace FMBot.Bot.Models;

public class PagedSelectionBuilder<TOption> : BaseSelectionBuilder<PagedSelection<TOption>,
    KeyValuePair<TOption, Paginator>, PagedSelectionBuilder<TOption>>
{
    /// <summary>
    ///     Gets a dictionary of options and their paginators.
    /// </summary>
    public new IDictionary<TOption, Paginator> Options { get; set; } = new Dictionary<TOption, Paginator>();

    public override Func<KeyValuePair<TOption, Paginator>, string> StringConverter { get; set; } =
        option => option.Key?.ToString();

    public PagedSelection<TOption> Build(PageBuilder startPage)
    {
        this.SelectionPage = startPage;
        return Build();
    }

    /// <inheritdoc />
    public override PagedSelection<TOption> Build()
    {
        base.Options = this.Options;
        return new PagedSelection<TOption>(this);
    }

    public PagedSelectionBuilder<TOption> WithOptions<TPaginator>(IDictionary<TOption, TPaginator> options)
        where TPaginator : Paginator
    {
        this.Options = options as IDictionary<TOption, Paginator> ?? throw new ArgumentNullException(nameof(options));
        return this;
    }

    public PagedSelectionBuilder<TOption> AddOption(TOption option, Paginator paginator)
    {
        this.Options.Add(option, paginator);
        return this;
    }
}

public class PagedSelection<TOption> : BaseSelection<KeyValuePair<TOption, Paginator>>
{
    /// <inheritdoc />
    public PagedSelection(PagedSelectionBuilder<TOption> builder) : base(builder)
    {
        this.Options = new ReadOnlyDictionary<TOption, Paginator>(builder.Options);
        this.CurrentOption = this.Options.Keys.First();
    }

    /// <summary>
    ///     Gets a dictionary of options and their paginators.
    /// </summary>
    public new IReadOnlyDictionary<TOption, Paginator> Options { get; }

    /// <summary>
    ///     Gets the current option.
    /// </summary>
    public TOption CurrentOption { get; private set; }

    public override ComponentBuilder GetOrAddComponents(bool disableAll, ComponentBuilder builder = null)
    {
        builder ??= new ComponentBuilder();
        
        var paginator = this.Options[this.CurrentOption];

        // add paginator components to the builder
        paginator.GetOrAddComponents(disableAll, builder);

        return builder;
    }

    public override async Task<InteractiveInputResult<KeyValuePair<TOption, Paginator>>> HandleInteractionAsync(
        SocketMessageComponent input, IUserMessage message)
    {
        if (input.Message.Id != message.Id || !this.CanInteract(input.User)) return InteractiveInputStatus.Ignored;

        if (input.Data.Type == ComponentType.Button &&
            input.Data.CustomId is DiscordConstants.JumpToGuildEmote or DiscordConstants.JumpToUserEmote)
        {
            var option = input.Data.CustomId == DiscordConstants.JumpToGuildEmote ? "server" : "user";

            KeyValuePair<TOption, Paginator> selected = default;

            foreach (var value in this.Options)
            {
                var stringValue = this.StringConverter?.Invoke(value);
                if (option != stringValue)
                {
                    continue;
                }

                selected = value;
                break;
            }

            this.CurrentOption = selected.Key;
        }

        var paginator = this.Options[this.CurrentOption];

        var (emote, action) = paginator.Emotes.FirstOrDefault(x => x.Key.ToString() == input.Data.CustomId);

        if (emote is not null)
        {
            if (action == PaginatorAction.Exit) return InteractiveInputStatus.Canceled;

            await paginator.ApplyActionAsync(action).ConfigureAwait(false);
        }

        var currentPage = await paginator.GetOrLoadCurrentPageAsync().ConfigureAwait(false);

        var components = GetOrAddComponents(false);

        await input.UpdateAsync(x =>
        {
            x.Content = currentPage.Text ?? "";
            x.Embeds = currentPage.GetEmbedArray();
            x.Components = components.Build();
        }).ConfigureAwait(false);

        return InteractiveInputStatus.Ignored;
    }
}
