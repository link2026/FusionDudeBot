using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using static SysBot.Pokemon.TradeSettings.TradeSettingsCategory;

namespace SysBot.Pokemon.Discord;

[Summary("Queues new Link Code trades")]
public class TradeModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

    private static readonly char[] separator = [' '];

    private static readonly char[] separatorArray = [' '];

    private static readonly char[] separatorArray0 = [' '];

    [Command("fixOT")]
    [Alias("fix", "f")]
    [Summary("Fixes OT and Nickname of a Pokémon you show via Link Trade if an advert is detected.")]
    [RequireQueueRole(nameof(DiscordManager.RolesFixOT))]
    public async Task FixAdOT()
    {
        var userID = Context.User.Id;

        // Check if the user is already in the queue
        if (Info.IsUserInQueue(userID))
        {
            _ = ReplyAndDeleteAsync("You already have an existing trade in the queue. Please wait until it is processed.", 2);
            return;
        }

        var code = Info.GetRandomTradeCode(userID, Context.Channel, Context.User);
        var trainerName = Context.User.Username;
        var lgcode = Info.GetRandomLGTradeCode();
        var sig = Context.User.GetFavor();

        await QueueHelper<T>.AddToQueueAsync(
            Context,
            code,
            trainerName,
            sig,
            new T(),
            PokeRoutineType.FixOT,
            PokeTradeType.FixOT,
            Context.User,
            false,
            1,
            1,
            false,
            false,
            lgcode,     // Assuming this is a valid List<Pictocodes>
            false       // Boolean as expected in the 15th argument
        ).ConfigureAwait(false);

        if (Context.Message is IUserMessage userMessage)
        {
            _ = DeleteMessagesAfterDelayAsync(userMessage, null, 2);
        }
    }

    [Command("fixOT")]
    [Alias("fix", "f")]
    [Summary("Fixes OT and Nickname of a Pokémon you show via Link Trade if an advert is detected.")]
    [RequireQueueRole(nameof(DiscordManager.RolesFixOT))]
    public async Task FixAdOT([Summary("Trade Code")] int code)
    {
        var userID = Context.User.Id;

        // Check if the user is already in the queue
        if (Info.IsUserInQueue(userID))
        {
            _ = ReplyAndDeleteAsync("You already have an existing trade in the queue. Please wait until it is processed.", 2);
            return;
        }

        var trainerName = Context.User.Username;
        var sig = Context.User.GetFavor();
        var lgcode = Info.GetRandomLGTradeCode();

        await QueueHelper<T>.AddToQueueAsync(
            Context,
            code,
            trainerName,
            sig,
            new T(),
            PokeRoutineType.FixOT,
            PokeTradeType.FixOT,
            Context.User,
            false,
            1,
            1,
            false,
            false,
            lgcode,     // Assuming this is a valid List<Pictocodes>
            false       // Boolean as expected in the 15th argument
        ).ConfigureAwait(false);

        if (Context.Message is IUserMessage userMessage)
        {
            _ = DeleteMessagesAfterDelayAsync(userMessage, null, 2);
        }
    }

    [Command("fixOTList")]
    [Alias("fl", "fq")]
    [Summary("Prints the users in the FixOT queue.")]
    [RequireSudo]
    public async Task GetFixListAsync()
    {
        string msg = Info.GetTradeList(PokeRoutineType.FixOT);
        var embed = new EmbedBuilder();
        embed.AddField(x =>
        {
            x.Name = "Pending Trades";
            x.Value = msg;
            x.IsInline = false;
        });
        await ReplyAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
    }

    [Command("dittoTrade")]
    [Alias("dt", "ditto")]
    [Summary("Makes the bot trade you a Ditto with a requested stat spread and language.")]
    public async Task DittoTrade([Summary("A combination of \"ATK/SPA/SPE\" or \"6IV\"")] string keyword, [Summary("Language")] string language, [Summary("Nature")] string nature)
    {
        var userID = Context.User.Id;

        // Check if the user is already in the queue
        if (Info.IsUserInQueue(userID))
        {
            _ = ReplyAndDeleteAsync("You already have an existing trade in the queue. Please wait until it is processed.", 2);
            return;
        }

        var code = Info.GetRandomTradeCode(userID, Context.Channel, Context.User);
        await DittoTrade(code, keyword, language, nature).ConfigureAwait(false);
        if (Context.Message is IUserMessage userMessage)
        {
            _ = DeleteMessagesAfterDelayAsync(userMessage, null, 2);
        }
    }

    [Command("dittoTrade")]
    [Alias("dt", "ditto")]
    [Summary("Makes the bot trade you a Ditto with a requested stat spread and language.")]
    public async Task DittoTrade([Summary("Trade Code")] int code, [Summary("A combination of \"ATK/SPA/SPE\" or \"6IV\"")] string keyword, [Summary("Language")] string language, [Summary("Nature")] string nature)
    {
        var userID = Context.User.Id;

        // Check if the user is already in the queue
        if (Info.IsUserInQueue(userID))
        {
            _ = ReplyAndDeleteAsync("You already have an existing trade in the queue. Please wait until it is processed.", 2);
            return;
        }

        keyword = keyword.ToLower().Trim();

        if (Enum.TryParse(language, true, out LanguageID lang))
        {
            language = lang.ToString();
        }
        else
        {
            _ = ReplyAndDeleteAsync($"Couldn't recognize language: {language}.", 2, Context.Message);
            return;
        }

        nature = nature.Trim()[..1].ToUpper() + nature.Trim()[1..].ToLower();
        var set = new ShowdownSet($"{keyword}(Ditto)\nLanguage: {language}\nNature: {nature}");
        var template = AutoLegalityWrapper.GetTemplate(set);
        var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
        var pkm = sav.GetLegal(template, out var result);
        AbstractTrade<T>.DittoTrade((T)pkm);
        var la = new LegalityAnalysis(pkm);

        if (pkm is not T pk || !la.Valid)
        {
            var reason = result == "Timeout" ? "That set took too long to generate." : "I wasn't able to create something from that.";
            var imsg = $"Oops! {reason} Here's my best attempt for that Ditto!";
            await Context.Channel.SendPKMAsync(pkm, imsg).ConfigureAwait(false);
            return;
        }

        pk.ResetPartyStats();

        // Ad Name Check
        if (Info.Hub.Config.Trade.TradeConfiguration.EnableSpamCheck)
        {
            if (AbstractTrade<T>.HasAdName(pk, out string ad))
            {
                await ReplyAndDeleteAsync("Detected Admon URL in the Pokémon's nickname or OT, which is not allowed.", 5);
                return;
            }
        }

        var sig = Context.User.GetFavor();
        await QueueHelper<T>.AddToQueueAsync(Context, code, Context.User.Username, sig, pk, PokeRoutineType.LinkTrade, PokeTradeType.Specific).ConfigureAwait(false);
        if (Context.Message is IUserMessage userMessage)
        {
            _ = DeleteMessagesAfterDelayAsync(userMessage, null, 2);
        }
    }

    [Command("itemTrade")]
    [Alias("it", "item")]
    [Summary("Makes the bot trade you a Pokémon holding the requested item, or Ditto if stat spread keyword is provided.")]
    public async Task ItemTrade([Remainder] string item)
    {
        // Check if the user is already in the queue
        var userID = Context.User.Id;
        if (Info.IsUserInQueue(userID))
        {
            await ReplyAsync("You already have an existing trade in the queue. Please wait until it is processed.").ConfigureAwait(false);
            return;
        }
        var code = Info.GetRandomTradeCode(userID, Context.Channel, Context.User);
        await ItemTrade(code, item).ConfigureAwait(false);
    }

    [Command("itemTrade")]
    [Alias("it", "item")]
    [Summary("Makes the bot trade you a Pokémon holding the requested item.")]
    public async Task ItemTrade([Summary("Trade Code")] int code, [Remainder] string item)
    {
        var userID = Context.User.Id;

        // Check if the user is already in the queue
        if (Info.IsUserInQueue(userID))
        {
            _ = ReplyAndDeleteAsync("You already have an existing trade in the queue. Please wait until it is processed.", 2);
            return;
        }

        Species species = Info.Hub.Config.Trade.TradeConfiguration.ItemTradeSpecies == Species.None ? Species.Diglett : Info.Hub.Config.Trade.TradeConfiguration.ItemTradeSpecies;
        var set = new ShowdownSet($"{SpeciesName.GetSpeciesNameGeneration((ushort)species, 2, 8)} @ {item.Trim()}");
        var template = AutoLegalityWrapper.GetTemplate(set);
        var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
        var pkm = sav.GetLegal(template, out var result);
        pkm = EntityConverter.ConvertToType(pkm, typeof(T), out _) ?? pkm;

        if (pkm.HeldItem == 0)
        {
            _ = ReplyAndDeleteAsync($"{Context.User.Username}, the item you entered wasn't recognized.", 2, Context.Message);
            return;
        }

        var la = new LegalityAnalysis(pkm);
        if (pkm is not T pk || !la.Valid)
        {
            var reason = result == "Timeout" ? "That set took too long to generate." : "I wasn't able to create something from that.";
            var imsg = $"Oops! {reason} Here's my best attempt for that {species}!";
            await Context.Channel.SendPKMAsync(pkm, imsg).ConfigureAwait(false);
            return;
        }

        pk.ResetPartyStats();
        var sig = Context.User.GetFavor();
        await QueueHelper<T>.AddToQueueAsync(Context, code, Context.User.Username, sig, pk, PokeRoutineType.LinkTrade, PokeTradeType.Specific).ConfigureAwait(false);

        if (Context.Message is IUserMessage userMessage)
        {
            _ = DeleteMessagesAfterDelayAsync(userMessage, null, 2);
        }
    }

    [Command("tradeList")]
    [Alias("tl")]
    [Summary("Prints the users in the trade queues.")]
    [RequireSudo]
    public async Task GetTradeListAsync()
    {
        string msg = Info.GetTradeList(PokeRoutineType.LinkTrade);
        var embed = new EmbedBuilder();
        embed.AddField(x =>
        {
            x.Name = "Pending Trades";
            x.Value = msg;
            x.IsInline = false;
        });
        await ReplyAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
    }

    [Command("egg")]
    [Alias("Egg")]
    [Summary("Trades an egg generated from the provided Pokémon name.")]
    public async Task TradeEgg([Remainder] string egg)
    {
        var userID = Context.User.Id;
        var code = Info.GetRandomTradeCode(userID, Context.Channel, Context.User);
        await TradeEggAsync(code, egg).ConfigureAwait(false);
    }

    [Command("egg")]
    [Alias("Egg")]
    [Summary("Trades an egg generated from the provided Pokémon name.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task TradeEggAsync([Summary("Trade Code")] int code, [Summary("Showdown Set")][Remainder] string content)
    {
        var userID = Context.User.Id;

        // Check if the user is already in the queue
        if (Info.IsUserInQueue(userID))
        {
            _ = ReplyAndDeleteAsync("You already have an existing trade in the queue. Please wait until it is processed.", 2);
            return;
        }
        content = ReusableActions.StripCodeBlock(content);
        var set = new ShowdownSet(content);
        var template = AutoLegalityWrapper.GetTemplate(set);
        _ = Task.Run(async () =>
        {
            try
            {
                var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
                var pkm = sav.GetLegal(template, out var result);
                pkm = EntityConverter.ConvertToType(pkm, typeof(T), out _) ?? pkm;

                if (pkm is not T pk)
                {
                    _ = ReplyAndDeleteAsync("I wasn't able to create an egg for that.", 2, Context.Message);
                    return;
                }

                // Use the EggTrade method without setting the nickname
                pk.IsNicknamed = false; // Make sure we don't set a nickname
                AbstractTrade<T>.EggTrade(pk, template);

                var sig = Context.User.GetFavor();
                await AddTradeToQueueAsync(code, Context.User.Username, pk, sig, Context.User).ConfigureAwait(false);

                _ = DeleteMessagesAfterDelayAsync(null, Context.Message, 2);
            }
            catch (Exception ex)
            {
                LogUtil.LogSafe(ex, nameof(TradeModule<T>));
                _ = ReplyAndDeleteAsync("An error occurred while processing the request.", 2, Context.Message);
            }
            if (Context.Message is IUserMessage userMessage)
            {
                _ = DeleteMessagesAfterDelayAsync(userMessage, null, 2);
            }
        });

        // Return immediately to avoid blocking
        await Task.CompletedTask;
    }

    [Command("hidetrade")]
    [Alias("ht")]
    [Summary("Makes the bot trade you a Pokémon converted from the provided Showdown Set without showing the trade embed details.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public Task HideTradeAsync([Summary("Showdown Set")][Remainder] string content)
    {
        var userID = Context.User.Id;
        var code = Info.GetRandomTradeCode(userID, Context.Channel, Context.User);
        return HideTradeAsync(code, content);
    }

    [Command("hidetrade")]
    [Alias("ht")]
    [Summary("Makes the bot trade you a Pokémon converted from the provided Showdown Set without showing the trade embed details.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task HideTradeAsync([Summary("Trade Code")] int code, [Summary("Showdown Set")][Remainder] string content)
    {
        List<Pictocodes>? lgcode = null;
        var userID = Context.User.Id;

        // Check if the user is already in the queue
        if (Info.IsUserInQueue(userID))
        {
            _ = ReplyAndDeleteAsync("You already have an existing trade in the queue. Please wait until it is processed.", 2);
            return;
        }
        var ignoreAutoOT = content.Contains("OT:") || content.Contains("TID:") || content.Contains("SID:");
        content = ReusableActions.StripCodeBlock(content);
        content = AbstractTrade<T>.ConvertBalls(content);

        // Check if the showdown set contains "Egg"
        bool isEgg = AbstractTrade<T>.IsEggCheck(content);

        var set = new ShowdownSet(content);
        var template = AutoLegalityWrapper.GetTemplate(set);

        if (set.InvalidLines.Count != 0)
        {
            var msg = $"Unable to parse Showdown Set:\n{string.Join("\n", set.InvalidLines)}";
            _ = ReplyAndDeleteAsync(msg, 2, Context.Message);
            return;
        }
        _ = Task.Run(async () =>
        {
            try
            {
                var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
                var pkm = sav.GetLegal(template, out var result);
                var la = new LegalityAnalysis(pkm);
                var spec = GameInfo.Strings.Species[template.Species];

                if (isEgg && pkm is T eggPk)
                {
                    eggPk.IsNicknamed = false; // Make sure we don't set a nickname
                    AbstractTrade<T>.EggTrade(eggPk, template);
                    pkm = eggPk; // Update the pkm reference
                    la = new LegalityAnalysis(pkm); // Re-analyze legality
                }
                else
                {
                    if (SysCord<T>.Runner.Config.Trade.TradeConfiguration.SuggestRelearnMoves)
                    {
                        switch (pkm)
                        {
                            case PK9 pk9:
                                pk9.SetRecordFlagsAll();
                                break;

                            case PK8 pk8:
                                pk8.SetRecordFlagsAll();
                                break;

                            case PB8 pb8:
                                pb8.SetRecordFlagsAll();
                                break;

                            case PB7 pb7:
                            case PA8 pa8:
                                break;
                        }
                    }

                    pkm.HeldItem = pkm switch
                    {
                        PA8 => (int)HeldItem.None,
                        _ when pkm.HeldItem == 0 && !pkm.IsEgg => (int)SysCord<T>.Runner.Config.Trade.TradeConfiguration.DefaultHeldItem,
                        _ => pkm.HeldItem
                    };

                    if (pkm is PB7)
                    {
                        lgcode = TradeModule<T>.GenerateRandomPictocodes(3);
                        if (pkm.Species == (int)Species.Mew && pkm.IsShiny)
                        {
                            await ReplyAsync("Mew can **not** be Shiny in LGPE. PoGo Mew does not transfer and Pokeball Plus Mew is shiny locked.");
                            return;
                        }
                    }
                }
                bool setEdited = false;
                if (pkm is not T pk || !la.Valid || !string.IsNullOrEmpty(set.Form.ToString()))
                {
                    // Perform auto correct if it's on and send that shit through again
                    if (SysCord<T>.Runner.Config.Trade.AutoCorrectConfig.EnableAutoCorrect && !la.Valid)
                    {
                        var (finalShowdownSet, correctionMessages) = await AutoCorrectShowdown<T>.PerformAutoCorrect(content, pkm, la);
                        set = new ShowdownSet(finalShowdownSet);
                        template = AutoLegalityWrapper.GetTemplate(set);
                        pkm = sav.GetLegal(template, out result);
                        la = new LegalityAnalysis(pkm);
                        setEdited = true;
                        if (correctionMessages.Count > 0 && la.Valid)
                        {
                            var userName = Context.User.Mention;
                            var changesEmbed = new EmbedBuilder()
                                .WithTitle("Showdown Set Corrections")
                                .WithColor(Color.Orange)
                                .WithDescription(string.Join("\n", correctionMessages))
                                .AddField("Corrected Showdown Set:", $"```{finalShowdownSet}```")
                                .Build();
                            var correctionMessage = await ReplyAsync($"{userName}, here are the corrections we made to your Showdown set:", embed: changesEmbed).ConfigureAwait(false);
                            _ = DeleteMessagesAfterDelayAsync(correctionMessage, Context.Message, 30);
                        }
                    }

                    if (pkm is not T correctedPk || !la.Valid)
                    {
                        var reason = result == "Timeout" ? $"That {spec} set took too long to generate." :
                                     result == "VersionMismatch" ? "Request refused: PKHeX and Auto-Legality Mod version mismatch." :
                                     $"I wasn't able to create a {spec} from that set.";

                        var embedBuilder = new EmbedBuilder()
                            .WithTitle("Trade Creation Failed.")
                            .WithColor(Color.Red)
                            .AddField("Status", $"Failed to create {spec}.")
                            .AddField("Reason", reason);

                        if (result == "Failed")
                        {
                            var legalizationHint = AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm);
                            if (legalizationHint.Contains("Requested shiny value (ShinyType."))
                            {
                                legalizationHint = $"{spec} **cannot** be shiny. Please try again.";
                            }

                            if (!string.IsNullOrEmpty(legalizationHint))
                            {
                                embedBuilder.AddField("Hint", legalizationHint);
                            }
                        }

                        string userMention = Context.User.Mention;
                        string messageContent = $"{userMention}, here's the report for your request:";
                        var message = await Context.Channel.SendMessageAsync(text: messageContent, embed: embedBuilder.Build()).ConfigureAwait(false);
                        _ = DeleteMessagesAfterDelayAsync(message, Context.Message, 30);
                        return;
                    }
                    pk = correctedPk;
                }
                // Set correct MetDate for Mightiest Mark
                AbstractTrade<T>.CheckAndSetUnrivaledDate(pk);
                if (pk.WasEgg)
                    pk.EggMetDate = pk.MetDate;
                pk.ResetPartyStats();

                // Ad Name Check
                if (Info.Hub.Config.Trade.TradeConfiguration.EnableSpamCheck)
                {
                    if (AbstractTrade<T>.HasAdName(pk, out string ad))
                    {
                        await ReplyAndDeleteAsync("Detected Admon URL in the Pokémon's nickname or OT, which is not allowed. You also suck for trying.", 5);
                        return;
                    }
                }

                var sig = Context.User.GetFavor();
                await AddTradeToQueueAsync(code, Context.User.Username, pk, sig, Context.User, isBatchTrade: false, batchTradeNumber: 1, totalBatchTrades: 1, true, false, lgcode: lgcode, ignoreAutoOT: ignoreAutoOT, setEdited: setEdited).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogUtil.LogSafe(ex, nameof(TradeModule<T>));
                var msg = $"Oops! An unexpected problem happened with this Showdown Set:\n```{string.Join("\n", set.GetSetLines())}```";

                _ = ReplyAndDeleteAsync(msg, 2, Context.Message);
            }
            if (Context.Message is IUserMessage userMessage)
            {
                _ = DeleteMessagesAfterDelayAsync(userMessage, null, 0);
            }
        });

        // Return immediately to avoid blocking
        await Task.CompletedTask;
    }

    [Command("hidetrade")]
    [Alias("ht")]
    [Summary("Makes the bot trade you the provided Pokémon file without showing the trade embed details.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public Task HideTradeAsyncAttach(
            [Summary("Trade Code")] int code,
            [Summary("Ignore AutoOT")] bool ignoreAutoOT = false)
    {
        var sig = Context.User.GetFavor();
        return HideTradeAsyncAttach(code, sig, Context.User, ignoreAutoOT: ignoreAutoOT);
    }

    [Command("hidetrade")]
    [Alias("ht")]
    [Summary("Makes the bot trade you the attached file without showing the trade embed details.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    private async Task HideTradeAsyncAttach([Summary("Ignore AutoOT")] bool ignoreAutoOT = false)
    {
        var userID = Context.User.Id;
        var code = Info.GetRandomTradeCode(userID, Context.Channel, Context.User);
        var sig = Context.User.GetFavor();
        await HideTradeAsyncAttach(code, sig, Context.User, ignoreAutoOT: ignoreAutoOT).ConfigureAwait(false);
        if (Context.Message is IUserMessage userMessage)
        {
            _ = DeleteMessagesAfterDelayAsync(userMessage, null, 2);
        }
    }

    [Command("trade")]
    [Alias("t")]
    [Summary("Makes the bot trade you a Pokémon converted from the provided Showdown Set.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public Task TradeAsync([Summary("Showdown Set")][Remainder] string content)
    {
        var userID = Context.User.Id;
        var code = Info.GetRandomTradeCode(userID, Context.Channel, Context.User);
        return TradeAsync(code, content);
    }

    [Command("trade")]
    [Alias("t")]
    [Summary("Makes the bot trade you a Pokémon converted from the provided Showdown Set.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task TradeAsync([Summary("Trade Code")] int code, [Summary("Showdown Set")][Remainder] string content)
    {
        List<Pictocodes>? lgcode = null;

        // Check if the user is already in the queue
        var userID = Context.User.Id;
        if (Info.IsUserInQueue(userID))
        {
            _ = ReplyAndDeleteAsync("You already have an existing trade in the queue. Please wait until it is processed.", 2, null);
            return;
        }

        var ignoreAutoOT = content.Contains("OT:") || content.Contains("TID:") || content.Contains("SID:");
        content = ReusableActions.StripCodeBlock(content);
        content = AbstractTrade<T>.ConvertBalls(content);

        // Check if the showdown set contains "Egg"
        bool isEgg = AbstractTrade<T>.IsEggCheck(content);

        var set = new ShowdownSet(content);
        var template = AutoLegalityWrapper.GetTemplate(set);

        if (set.InvalidLines.Count != 0)
        {
            var msg = $"Unable to parse Showdown Set:\n{string.Join("\n", set.InvalidLines)}";
            _ = ReplyAndDeleteAsync(msg, 2, Context.Message);
            return;
        }
        _ = Task.Run(async () =>
        {
            try
            {
                var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
                var pkm = sav.GetLegal(template, out var result);
                var la = new LegalityAnalysis(pkm);
                var spec = GameInfo.Strings.Species[template.Species];

                if (isEgg && pkm is T eggPk)
                {
                    eggPk.IsNicknamed = false; // Make sure we don't set a nickname
                    AbstractTrade<T>.EggTrade(eggPk, template);
                    pkm = eggPk; // Update the pkm reference
                    la = new LegalityAnalysis(pkm); // Re-analyze legality
                }
                else
                {
                    if (SysCord<T>.Runner.Config.Trade.TradeConfiguration.SuggestRelearnMoves)
                    {
                        switch (pkm)
                        {
                            case PK9 pk9:
                                pk9.SetRecordFlagsAll();
                                break;
                            case PK8 pk8:
                                pk8.SetRecordFlagsAll();
                                break;
                            case PB8 pb8:
                                pb8.SetRecordFlagsAll();
                                break;
                            case PB7 pb7:
                            case PA8 pa8:
                                break;
                        }
                    }

                    pkm.HeldItem = pkm switch
                    {
                        PA8 => (int)HeldItem.None,
                        _ when pkm.HeldItem == 0 && !pkm.IsEgg => (int)SysCord<T>.Runner.Config.Trade.TradeConfiguration.DefaultHeldItem,
                        _ => pkm.HeldItem
                    };

                    if (pkm is PB7)
                    {
                        lgcode = TradeModule<T>.GenerateRandomPictocodes(3);
                        if (pkm.Species == (int)Species.Mew && pkm.IsShiny)
                        {
                            await ReplyAsync("Mew can **not** be Shiny in LGPE. PoGo Mew does not transfer and Pokeball Plus Mew is shiny locked.");
                            return;
                        }
                    }
                }

                bool setEdited = false;
                if (pkm is not T pk || !la.Valid || !string.IsNullOrEmpty(set.Form.ToString()))
                {
                    // Perform auto correct if it's on and send that shit through again
                    if (SysCord<T>.Runner.Config.Trade.AutoCorrectConfig.EnableAutoCorrect && !la.Valid)
                    {
                        var (finalShowdownSet, correctionMessages) = await AutoCorrectShowdown<T>.PerformAutoCorrect(content, pkm, la);
                        set = new ShowdownSet(finalShowdownSet);
                        template = AutoLegalityWrapper.GetTemplate(set);
                        pkm = sav.GetLegal(template, out result);
                        la = new LegalityAnalysis(pkm);
                        setEdited = true;
                        if (correctionMessages.Count > 0 && la.Valid)
                        {
                            var userName = Context.User.Mention;
                            var changesEmbed = new EmbedBuilder()
                                .WithTitle("Showdown Set Corrections")
                                .WithColor(Color.Orange)
                                .WithDescription(string.Join("\n", correctionMessages))
                                .AddField("Corrected Showdown Set:", $"```{finalShowdownSet}```")
                                .Build();
                            var correctionMessage = await ReplyAsync($"{userName}, here are the corrections we made to your Showdown set:", embed: changesEmbed).ConfigureAwait(false);
                            _ = DeleteMessagesAfterDelayAsync(correctionMessage, Context.Message, 30);
                        }
                    }

                    if (pkm is not T correctedPk || !la.Valid)
                    {
                        var reason = result == "Timeout" ? $"That {spec} set took too long to generate." :
                                     result == "VersionMismatch" ? "Request refused: PKHeX and Auto-Legality Mod version mismatch." :
                                     $"I wasn't able to create a {spec} from that set.";

                        var embedBuilder = new EmbedBuilder()
                            .WithTitle("Trade Creation Failed.")
                            .WithColor(Color.Red)
                            .AddField("Status", $"Failed to create {spec}.")
                            .AddField("Reason", reason);

                        if (result == "Failed")
                        {
                            var legalizationHint = AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm);
                            if (legalizationHint.Contains("Requested shiny value (ShinyType."))
                            {
                                legalizationHint = $"{spec} **cannot** be shiny. Please try again.";
                            }

                            if (!string.IsNullOrEmpty(legalizationHint))
                            {
                                embedBuilder.AddField("Hint", legalizationHint);
                            }
                        }

                        string userMention = Context.User.Mention;
                        string messageContent = $"{userMention}, here's the report for your request:";
                        var message = await Context.Channel.SendMessageAsync(text: messageContent, embed: embedBuilder.Build()).ConfigureAwait(false);
                        _ = DeleteMessagesAfterDelayAsync(message, Context.Message, 30);
                        return;
                    }
                    pk = correctedPk;
                }
                // Set correct MetDate for Mightiest Mark
                AbstractTrade<T>.CheckAndSetUnrivaledDate(pk);
                if (pk.WasEgg)
                    pk.EggMetDate = pk.MetDate;
                pk.ResetPartyStats();

                // Ad Name Check
                if (Info.Hub.Config.Trade.TradeConfiguration.EnableSpamCheck)
                {
                    if (AbstractTrade<T>.HasAdName(pk, out string ad))
                    {
                        await ReplyAndDeleteAsync("Detected Admon URL in the Pokémon's nickname or OT, which is not allowed. You also suck for trying.", 5);
                        return;
                    }
                }

                var sig = Context.User.GetFavor();
                await AddTradeToQueueAsync(code, Context.User.Username, pk, sig, Context.User, isBatchTrade: false, batchTradeNumber: 1, totalBatchTrades: 1, lgcode: lgcode, ignoreAutoOT: ignoreAutoOT, setEdited: setEdited).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogUtil.LogSafe(ex, nameof(TradeModule<T>));
                var msg = $"Oops! An unexpected problem happened with this Showdown Set:\n```{string.Join("\n", set.GetSetLines())}```";

                _ = ReplyAndDeleteAsync(msg, 2, null);
            }
            if (Context.Message is IUserMessage userMessage)
            {
                _ = DeleteMessagesAfterDelayAsync(userMessage, null, 2);
            }
        });

        // Return immediately to avoid blocking
        await Task.CompletedTask;
    }

    [Command("trade")]
    [Alias("t")]
    [Summary("Makes the bot trade you the provided Pokémon file.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public Task TradeAsyncAttach(
    [Summary("Trade Code")] int code,
    [Summary("Ignore AutoOT")] bool ignoreAutoOT = false)
    {
        var sig = Context.User.GetFavor();
        return TradeAsyncAttachInternal(code, sig, Context.User, ignoreAutoOT: ignoreAutoOT);
    }

    [Command("trade")]
    [Alias("t")]
    [Summary("Makes the bot trade you the attached file.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task TradeAsyncAttach([Summary("Ignore AutoOT")] bool ignoreAutoOT = false)
    {
        var userID = Context.User.Id;
        var code = Info.GetRandomTradeCode(userID, Context.Channel, Context.User);
        var sig = Context.User.GetFavor();
        await Task.Run(async () =>
        {
            await TradeAsyncAttachInternal(code, sig, Context.User, ignoreAutoOT).ConfigureAwait(false);
        }).ConfigureAwait(false);
        if (Context.Message is IUserMessage userMessage)
        {
            _ = DeleteMessagesAfterDelayAsync(userMessage, null, 2);
        }
    }

    [Command("batchTrade")]
    [Alias("bt")]
    [Summary("Makes the bot trade multiple Pokémon from the provided list, up to a maximum of 3 trades.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task BatchTradeAsync([Summary("List of Showdown Sets separated by '---'")][Remainder] string content)
    {
        // First, check if batch trades are allowed
        if (!SysCord<T>.Runner.Config.Trade.TradeConfiguration.AllowBatchTrades)
        {
            _ = ReplyAndDeleteAsync("Batch trades are currently disabled.", 2);
            return;
        }

        // Check if the user is already in the queue
        var userID = Context.User.Id;
        if (Info.IsUserInQueue(userID))
        {
            _ = ReplyAndDeleteAsync("You already have an existing trade in the queue. Please wait until it is processed.", 2);
            return;
        }

        content = ReusableActions.StripCodeBlock(content);
        var trades = TradeModule<T>.ParseBatchTradeContent(content);
        var maxTradesAllowed = SysCord<T>.Runner.Config.Trade.TradeConfiguration.MaxPkmsPerTrade;

        // Check if batch mode is allowed and if the number of trades exceeds the limit
        if (maxTradesAllowed < 1 || trades.Count > maxTradesAllowed)
        {
            _ = ReplyAndDeleteAsync($"You can only process up to {maxTradesAllowed} trades at a time. Please reduce the number of trades in your batch.", 5, Context.Message);
            _ = DeleteMessagesAfterDelayAsync(null, Context.Message, 2);
            return;
        }

        var batchTradeCode = Info.GetRandomTradeCode(userID, Context.Channel, Context.User);

        // Execute the trades in strict order of request, with enforced sequential delays
        for (int i = 0; i < trades.Count; i++)
        {
            var trade = trades[i];
            int batchTradeNumber = i + 1;

            // Sequential execution of each trade, ensuring the next trade does not start until the previous one completes
            await ProcessSingleTradeAsync(trade, batchTradeCode, true, batchTradeNumber, trades.Count)
                .ConfigureAwait(false); // Ensures trade order by awaiting in sequence

            // Log to confirm trade order and add delay to control pacing
            Console.WriteLine($"Completed batch trade #{batchTradeNumber}: {trade}");

            // Add a delay of 3/4 of a second before processing the next batch trade number
            if (i < trades.Count - 1)
            {
                await Task.Delay(750).ConfigureAwait(false); // 750 milliseconds = 0.75 seconds
            }
        }

        // Final cleanup
        if (Context.Message is IUserMessage userMessage)
        {
            _ = DeleteMessagesAfterDelayAsync(userMessage, null, 2);
        }
    }



    private static List<string> ParseBatchTradeContent(string content)
    {
        var delimiters = new[] { "---", "—-", "\n\n" }; // Includes both three hyphens and an em dash followed by a hyphen for phone users, and just a normal space in between.
        return content.Split(delimiters, StringSplitOptions.RemoveEmptyEntries)
                            .Select(trade => trade.Trim())
                            .ToList();
    }

    [Command("batchtradezip")]
    [Alias("btz")]
    [Summary("Makes the bot trade multiple Pokémon from the provided .zip file, up to a maximum of 6 trades.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task BatchTradeZipAsync()
    {
        // Check if batch trades are allowed
        if (!SysCord<T>.Runner.Config.Trade.TradeConfiguration.AllowBatchTrades)
        {
            _ = ReplyAndDeleteAsync("Batch trades are currently disabled.", 2);
            return;
        }

        // Check if the user is already in the queue
        var userID = Context.User.Id;
        if (Info.IsUserInQueue(userID))
        {
            _ = ReplyAndDeleteAsync("You already have an existing trade in the queue. Please wait until it is processed.", 2);
            return;
        }

        var attachment = Context.Message.Attachments.FirstOrDefault();
        if (attachment == default)
        {
            _ = ReplyAndDeleteAsync("No attachment provided!", 2);
            return;
        }

        if (!attachment.Filename.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            _ = ReplyAndDeleteAsync("Invalid file format. Please provide a .zip file.", 2);
            return;
        }

        var zipBytes = await new HttpClient().GetByteArrayAsync(attachment.Url);
        await using var zipStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        var entries = archive.Entries.ToList();

        const int maxTradesAllowed = 6;

        // Check trade limit
        if (entries.Count > maxTradesAllowed)
        {
            _ = ReplyAndDeleteAsync($"You can only process up to {maxTradesAllowed} trades at a time. Please reduce the number of Pokémon in your .zip file.", 5, Context.Message);
            return;
        }

        var batchTradeCode = Info.GetRandomTradeCode(userID, Context.Channel, Context.User);

        // Sequentially process each Pokémon entry in the .zip file
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            int batchTradeNumber = i + 1;

            // Extract and process each Pokémon in strict order
            await using var entryStream = entry.Open();
            var pkBytes = await TradeModule<T>.ReadAllBytesAsync(entryStream).ConfigureAwait(false);
            var pk = EntityFormat.GetFromBytes(pkBytes);

            if (pk is T)
            {
                // Execute the trade and ensure sequential order
                await ProcessSingleTradeAsync((T)pk, batchTradeCode, true, batchTradeNumber, entries.Count)
                    .ConfigureAwait(false);

                // Log trade completion and apply delay for sequential processing
                Console.WriteLine($"Completed batch trade #{batchTradeNumber}: {entry.FullName}");

                // Delay for order control
                if (i < entries.Count - 1)
                {
                    await Task.Delay(750).ConfigureAwait(false);
                }
            }
        }

        // Final cleanup
        if (Context.Message is IUserMessage userMessage)
        {
            _ = DeleteMessagesAfterDelayAsync(userMessage, null, 2);
        }
    }



    private static async Task<byte[]> ReadAllBytesAsync(Stream stream)
    {
        await using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
        return memoryStream.ToArray();
    }

    private async Task ProcessSingleTradeAsync(T pk, int batchTradeCode, bool isBatchTrade, int batchTradeNumber, int totalBatchTrades)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var la = new LegalityAnalysis(pk);
                var spec = GameInfo.Strings.Species[pk.Species];

                if (!la.Valid)
                {
                    await ReplyAsync($"The {spec} in the provided file is not legal.").ConfigureAwait(false);
                    return;
                }
                // Set correct MetDate for Mightiest Mark
                AbstractTrade<T>.CheckAndSetUnrivaledDate(pk);
                pk.ResetPartyStats();

                var userID = Context.User.Id;
                var code = Info.GetRandomTradeCode(userID, Context.Channel, Context.User);
                var lgcode = Info.GetRandomLGTradeCode();

                // Ad Name Check
                if (Info.Hub.Config.Trade.TradeConfiguration.EnableSpamCheck)
                {
                    if (AbstractTrade<T>.HasAdName(pk, out string ad))
                    {
                        await ReplyAndDeleteAsync("Detected Admon URL in the Pokémon's nickname or OT, which is not allowed. You also suck for trying.", 5);
                        return;
                    }
                }

                // Add the trade to the queue
                var sig = Context.User.GetFavor();
                await AddTradeToQueueAsync(batchTradeCode, Context.User.Username, pk, sig, Context.User, isBatchTrade, batchTradeNumber, totalBatchTrades, lgcode: lgcode, tradeType: PokeTradeType.Batch).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogUtil.LogSafe(ex, nameof(TradeModule<T>));
            }
        });

        // Return immediately to avoid blocking
        await Task.CompletedTask;
    }

    private async Task ProcessSingleTradeAsync(string tradeContent, int batchTradeCode, bool isBatchTrade, int batchTradeNumber, int totalBatchTrades)
    {
        tradeContent = ReusableActions.StripCodeBlock(tradeContent);
        var set = new ShowdownSet(tradeContent);
        var ignoreAutoOT = tradeContent.Contains("OT:") || tradeContent.Contains("TID:") || tradeContent.Contains("SID:");
        var template = AutoLegalityWrapper.GetTemplate(set);

        if (set.InvalidLines.Count != 0)
        {
            var msg = $"Unable to parse Showdown Set:\n{string.Join("\n", set.InvalidLines)}";
            await ReplyAsync(msg).ConfigureAwait(false);
            return;
        }
        _ = Task.Run(async () =>
        {
            try
            {
                var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
                var pkm = sav.GetLegal(template, out var result);
                var la = new LegalityAnalysis(pkm);
                var spec = GameInfo.Strings.Species[template.Species];
                bool setEdited = false;
                if (pkm is not T pk || !la.Valid || !string.IsNullOrEmpty(set.Form.ToString()))
                {
                    // Perform auto correct if it's on and send that shit through again
                    if (SysCord<T>.Runner.Config.Trade.AutoCorrectConfig.EnableAutoCorrect && !la.Valid)
                    {
                        var (finalShowdownSet, correctionMessages) = await AutoCorrectShowdown<T>.PerformAutoCorrect(tradeContent, pkm, la);
                        set = new ShowdownSet(finalShowdownSet);
                        template = AutoLegalityWrapper.GetTemplate(set);
                        pkm = sav.GetLegal(template, out result);
                        la = new LegalityAnalysis(pkm);
                        setEdited = true;
                        if (correctionMessages.Count > 0 && la.Valid)
                        {
                            var userName = Context.User.Mention;
                            var changesEmbed = new EmbedBuilder()
                                .WithTitle("Showdown Set Corrections")
                                .WithColor(Color.Orange)
                                .WithDescription(string.Join("\n", correctionMessages))
                                .AddField("Corrected Showdown Set:", $"```{finalShowdownSet}```")
                                .Build();
                            var correctionMessage = await ReplyAsync($"{userName}, here are the corrections we made to your Showdown set:", embed: changesEmbed).ConfigureAwait(false);
                            _ = DeleteMessagesAfterDelayAsync(correctionMessage, Context.Message, 30);
                        }
                    }

                    if (pkm is not T correctedPk || !la.Valid)
                    {
                        var reason = result == "Timeout" ? $"That {spec} set took too long to generate." :
                                     result == "VersionMismatch" ? "Request refused: PKHeX and Auto-Legality Mod version mismatch." :
                                     $"I wasn't able to create a {spec} from that set.";

                        var embedBuilder = new EmbedBuilder()
                            .WithTitle("Trade Creation Failed.")
                            .WithColor(Color.Red)
                            .AddField("Status", $"Failed to create {spec}.")
                            .AddField("Reason", reason);

                        if (result == "Failed")
                        {
                            var legalizationHint = AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm);
                            if (legalizationHint.Contains("Requested shiny value (ShinyType."))
                            {
                                legalizationHint = $"{spec} **cannot** be shiny. Please try again.";
                            }

                            if (!string.IsNullOrEmpty(legalizationHint))
                            {
                                embedBuilder.AddField("Hint", legalizationHint);
                            }
                        }

                        string userMention = Context.User.Mention;
                        string messageContent = $"{userMention}, here's the report for your request:";
                        var message = await Context.Channel.SendMessageAsync(text: messageContent, embed: embedBuilder.Build()).ConfigureAwait(false);
                        _ = DeleteMessagesAfterDelayAsync(message, Context.Message, 30);
                        return;
                    }

                    pk = correctedPk;
                }

                if (SysCord<T>.Runner.Config.Trade.TradeConfiguration.SuggestRelearnMoves)
                {
                    if (pkm is PK9 pk9)
                    {
                        pk9.SetRecordFlagsAll();
                    }
                    else if (pkm is PK8 pk8)
                    {
                        pk8.SetRecordFlagsAll();
                    }
                    else if (pkm is PB8 pb8)
                    {
                        pb8.SetRecordFlagsAll();
                    }
                    else if (pkm is PB7 pb7)
                    {
                        // not applicable for PB7 (LGPE)
                    }
                    else if (pkm is PA8 pa8)
                    {
                        // not applicable for PA8 (Legends: Arceus)
                    }
                }

                if (pkm is PA8)
                {
                    pkm.HeldItem = (int)HeldItem.None; // Set to None for "Legends: Arceus" Pokémon
                }
                else if (pkm.HeldItem == 0 && !pkm.IsEgg)
                {
                    pkm.HeldItem = (int)SysCord<T>.Runner.Config.Trade.TradeConfiguration.DefaultHeldItem;
                }

                if (pkm is PB7)
                {
                    if (pkm.Species == (int)Species.Mew)
                    {
                        if (pkm.IsShiny)
                        {
                            await ReplyAsync("Mew cannot be Shiny in LGPE. POGO Mew does not transfer. Poké Ball Plus Mew is shiny-locked.");
                            return;
                        }
                    }
                }
                // Set correct MetDate for Mightiest Mark
                AbstractTrade<T>.CheckAndSetUnrivaledDate(pkm);
                if (pkm.WasEgg)
                    pkm.EggMetDate = pkm.MetDate;
                pkm.ResetPartyStats();

                var userID = Context.User.Id;
                var code = Info.GetRandomTradeCode(userID, Context.Channel, Context.User);
                var lgcode = Info.GetRandomLGTradeCode();
                if (pkm is PB7)
                {
                    lgcode = TradeModule<T>.GenerateRandomPictocodes(3);
                }

                // Ad Name Check
                if (Info.Hub.Config.Trade.TradeConfiguration.EnableSpamCheck)
                {
                    if (AbstractTrade<T>.HasAdName(pk, out string ad))
                    {
                        await ReplyAndDeleteAsync("Detected Admon URL in the Pokémon's nickname or OT, which is not allowed. You also suck for trying.", 5);
                        return;
                    }
                }

                var sig = Context.User.GetFavor();
                await AddTradeToQueueAsync(batchTradeCode, Context.User.Username, pk, sig, Context.User, isBatchTrade, batchTradeNumber, totalBatchTrades, lgcode: lgcode, tradeType: PokeTradeType.Batch, ignoreAutoOT: ignoreAutoOT, setEdited: setEdited).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogUtil.LogSafe(ex, nameof(TradeModule<T>));
            }
        });

        // Return immediately to avoid blocking
        await Task.CompletedTask;
    }

    [Command("listevents")]
    [Alias("le")]
    [Summary("Lists available event files, filtered by a specific letter or substring, and sends the list via DM.")]
    public async Task ListEventsAsync([Remainder] string args = "")
    {
        const int itemsPerPage = 20; // Number of items per page
        var eventsFolderPath = SysCord<T>.Runner.Config.Trade.RequestFolderSettings.EventsFolder;
        var botPrefix = SysCord<T>.Runner.Config.Discord.CommandPrefix;

        // Check if the events folder path is not set or empty
        if (string.IsNullOrEmpty(eventsFolderPath))
        {
            _ = ReplyAndDeleteAsync("This bot does not have this feature set up.", 2, Context.Message);
            return;
        }

        // Parsing the arguments to separate filter and page number
        string filter = "";
        int page = 1;
        var parts = args.Split(separator, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length > 0)
        {
            // Check if the last part is a number (page number)
            if (int.TryParse(parts.Last(), out int parsedPage))
            {
                page = parsedPage;
                filter = string.Join(" ", parts.Take(parts.Length - 1));
            }
            else
            {
                filter = string.Join(" ", parts);
            }
        }

        var allEventFiles = Directory.GetFiles(eventsFolderPath)
                                     .Select(Path.GetFileNameWithoutExtension)
                                     .OrderBy(file => file)
                                     .ToList();

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var filteredEventFiles = allEventFiles
                                 .Where(file => string.IsNullOrWhiteSpace(filter) || file.Contains(filter, StringComparison.OrdinalIgnoreCase))
                                 .ToList();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        IUserMessage replyMessage;

        // Check if there are no files matching the filter
        if (!filteredEventFiles.Any())
        {
            replyMessage = await ReplyAsync($"No events found matching the filter '{filter}'.");
            _ = DeleteMessagesAfterDelayAsync(replyMessage, Context.Message, 10);
        }
        else
        {
            var pageCount = (int)Math.Ceiling(filteredEventFiles.Count / (double)itemsPerPage);
            page = Math.Clamp(page, 1, pageCount); // Ensure page number is within valid range

            var pageItems = filteredEventFiles.Skip((page - 1) * itemsPerPage).Take(itemsPerPage);

            var embed = new EmbedBuilder()
                .WithTitle($"Available Events - Filter: '{filter}'")
                .WithDescription($"Page {page} of {pageCount}")
                .WithColor(Color.Blue);

            foreach (var item in pageItems)
            {
                var index = allEventFiles.IndexOf(item) + 1; // Get the index from the original list
                embed.AddField($"{index}. {item}", $"Use `{botPrefix}er {index}` to request this event.");
            }

            if (Context.User is IUser user)
            {
                try
                {
                    var dmChannel = await user.CreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(embed: embed.Build());
                    replyMessage = await ReplyAsync($"{Context.User.Mention}, I've sent you a DM with the list of events.");
                }
                catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.Forbidden)
                {
                    // This exception is thrown when the bot cannot send DMs to the user
                    replyMessage = await ReplyAsync($"{Context.User.Mention}, I'm unable to send you a DM. Please check your **Server Privacy Settings**.");
                }
            }
            else
            {
                replyMessage = await ReplyAsync("**Error**: Unable to send a DM. Please check your **Server Privacy Settings**.");
            }

            _ = DeleteMessagesAfterDelayAsync(replyMessage, Context.Message, 10);
        }
    }

    [Command("eventrequest")]
    [Alias("er")]
    [Summary("Downloads event attachments from the specified EventsFolder and adds to trade queue.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task EventRequestAsync(int index)
    {
        var userID = Context.User.Id;

        // Check if the user is already in the queue
        if (Info.IsUserInQueue(userID))
        {
            _ = ReplyAndDeleteAsync("You already have an existing trade in the queue. Please wait until it is processed.", 2, Context.Message);
            return;
        }

        try
        {
            var eventsFolderPath = SysCord<T>.Runner.Config.Trade.RequestFolderSettings.EventsFolder;
            var eventFiles = Directory.GetFiles(eventsFolderPath)
                .Select(Path.GetFileName)
                .OrderBy(x => x)
                .ToList();

            // Check if the events folder path is not set or empty
            if (string.IsNullOrEmpty(eventsFolderPath))
            {
                _ = ReplyAndDeleteAsync("This bot does not have this feature set up.", 2, Context.Message);
                return;
            }

            if (index < 1 || index > eventFiles.Count)
            {
                _ = ReplyAndDeleteAsync("Invalid event index. Please use a valid event number from the `.le` command.", 2, Context.Message);
                return;
            }

            var selectedFile = eventFiles[index - 1]; // Adjust for zero-based indexing
#pragma warning disable CS8604 // Possible null reference argument.
            var fileData = await File.ReadAllBytesAsync(Path.Combine(eventsFolderPath, selectedFile));
#pragma warning restore CS8604 // Possible null reference argument.
            var download = new Download<PKM>
            {
                Data = EntityFormat.GetFromBytes(fileData),
                Success = true
            };

            var pk = GetRequest(download);
            if (pk == null)
            {
                _ = ReplyAndDeleteAsync("Failed to convert event file to the required PKM type.", 2, Context.Message);
                return;
            }

            var code = Info.GetRandomTradeCode(userID, Context.Channel, Context.User);
            var lgcode = Info.GetRandomLGTradeCode();
            var sig = Context.User.GetFavor();

            await ReplyAsync("Event request added to queue.").ConfigureAwait(false);
            await AddTradeToQueueAsync(code, Context.User.Username, pk, sig, Context.User, lgcode: lgcode).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _ = ReplyAndDeleteAsync($"An error occurred: {ex.Message}", 2, Context.Message);
        }
        finally
        {
            if (Context.Message is IUserMessage userMessage)
            {
                _ = DeleteMessagesAfterDelayAsync(userMessage, null, 2);
            }
        }
    }

    [Command("battlereadylist")]
    [Alias("brl")]
    [Summary("Lists available battle-ready files, filtered by a specific letter or substring, and sends the list via DM.")]
    public async Task BattleReadyListAsync([Remainder] string args = "")
    {
        const int itemsPerPage = 20; // Number of items per page
        var battleReadyFolderPath = SysCord<T>.Runner.Config.Trade.RequestFolderSettings.BattleReadyPKMFolder;
        var botPrefix = SysCord<T>.Runner.Config.Discord.CommandPrefix;

        // Check if the battleready folder path is not set or empty
        if (string.IsNullOrEmpty(battleReadyFolderPath))
        {
            _ = ReplyAndDeleteAsync("This bot does not have this feature set up.", 2, Context.Message);
            return;
        }

        // Parsing the arguments to separate filter and page number
        string filter = "";
        int page = 1;
        var parts = args.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length > 0)
        {
            // Check if the last part is a number (page number)
            if (int.TryParse(parts.Last(), out int parsedPage))
            {
                page = parsedPage;
                filter = string.Join(" ", parts.Take(parts.Length - 1));
            }
            else
            {
                filter = string.Join(" ", parts);
            }
        }

        var allBattleReadyFiles = Directory.GetFiles(battleReadyFolderPath)
                                           .Select(Path.GetFileNameWithoutExtension)
                                           .OrderBy(file => file)
                                           .ToList();

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var filteredBattleReadyFiles = allBattleReadyFiles
                                       .Where(file => string.IsNullOrWhiteSpace(filter) || file.Contains(filter, StringComparison.OrdinalIgnoreCase))
                                       .ToList();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        IUserMessage replyMessage;

        // Check if there are no files matching the filter
        if (!filteredBattleReadyFiles.Any())
        {
            replyMessage = await ReplyAsync($"No battle-ready files found matching the filter '{filter}'.");
            _ = DeleteMessagesAfterDelayAsync(replyMessage, Context.Message, 10);
        }
        else
        {
            var pageCount = (int)Math.Ceiling(filteredBattleReadyFiles.Count / (double)itemsPerPage);
            page = Math.Clamp(page, 1, pageCount); // Ensure page number is within valid range

            var pageItems = filteredBattleReadyFiles.Skip((page - 1) * itemsPerPage).Take(itemsPerPage);

            var embed = new EmbedBuilder()
                .WithTitle($"Available Battle-Ready Files - Filter: '{filter}'")
                .WithDescription($"Page {page} of {pageCount}")
                .WithColor(Color.Blue);

            foreach (var item in pageItems)
            {
                var index = allBattleReadyFiles.IndexOf(item) + 1; // Get the index from the original list
                embed.AddField($"{index}. {item}", $"Use `{botPrefix}brr {index}` to request this battle-ready file.");
            }

            if (Context.User is IUser user)
            {
                try
                {
                    var dmChannel = await user.CreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(embed: embed.Build());
                    replyMessage = await ReplyAsync($"{Context.User.Mention}, I've sent you a DM with the list of battle-ready files.");
                }
                catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.Forbidden)
                {
                    // This exception is thrown when the bot cannot send DMs to the user
                    replyMessage = await ReplyAsync($"{Context.User.Mention}, I'm unable to send you a DM. Please check your **Server Privacy Settings**.");
                }
            }
            else
            {
                replyMessage = await ReplyAsync("**Error**: Unable to send a DM. Please check your **Server Privacy Settings**.");
            }

            _ = DeleteMessagesAfterDelayAsync(replyMessage, Context.Message, 10);
        }
    }

    [Command("battlereadyrequest")]
    [Alias("brr", "br")]
    [Summary("Downloads battle-ready attachments from the specified folder and adds to trade queue.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task BattleReadyRequestAsync(int index)
    {
        var userID = Context.User.Id;

        // Check if the user is already in the queue
        if (Info.IsUserInQueue(userID))
        {
            _ = ReplyAndDeleteAsync("You already have an existing trade in the queue. Please wait until it is processed.", 2, Context.Message);
            return;
        }

        try
        {
            var battleReadyFolderPath = SysCord<T>.Runner.Config.Trade.RequestFolderSettings.BattleReadyPKMFolder;
            var battleReadyFiles = Directory.GetFiles(battleReadyFolderPath)
                .Select(Path.GetFileName)
                .OrderBy(x => x)
                .ToList();

            // Check if the battleready folder path is not set or empty
            if (string.IsNullOrEmpty(battleReadyFolderPath))
            {
                _ = ReplyAndDeleteAsync("This bot does not have this feature set up.", 2, Context.Message);
                return;
            }

            if (index < 1 || index > battleReadyFiles.Count)
            {
                _ = ReplyAndDeleteAsync("Invalid battle-ready file index. Please use a valid file number from the `.blr` command.", 2, Context.Message);
                return;
            }

            var selectedFile = battleReadyFiles[index - 1];
#pragma warning disable CS8604 // Possible null reference argument.
            var fileData = await File.ReadAllBytesAsync(Path.Combine(battleReadyFolderPath, selectedFile));
#pragma warning restore CS8604 // Possible null reference argument.
            var download = new Download<PKM>
            {
                Data = EntityFormat.GetFromBytes(fileData),
                Success = true
            };

            var pk = GetRequest(download);
            if (pk == null)
            {
                _ = ReplyAndDeleteAsync("Failed to convert battle-ready file to the required PKM type.", 2, Context.Message);
                return;
            }

            var code = Info.GetRandomTradeCode(userID, Context.Channel, Context.User);
            var lgcode = Info.GetRandomLGTradeCode();
            var sig = Context.User.GetFavor();

            await ReplyAsync("Battle-ready request added to queue.").ConfigureAwait(false);
            await AddTradeToQueueAsync(code, Context.User.Username, pk, sig, Context.User, lgcode: lgcode).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _ = ReplyAndDeleteAsync($"An error occurred: {ex.Message}", 2, Context.Message);
        }
        finally
        {
            if (Context.Message is IUserMessage userMessage)
            {
                _ = DeleteMessagesAfterDelayAsync(userMessage, null, 2);
            }
        }
    }

    [Command("tradeUser")]
    [Alias("tu", "tradeOther")]
    [Summary("Makes the bot trade the mentioned user the attached file.")]
    [RequireSudo]
    public async Task TradeAsyncAttachUser([Summary("Trade Code")] int code, [Remainder] string _)
    {
        if (Context.Message.MentionedUsers.Count > 1)
        {
            await ReplyAsync("Too many mentions. Queue one user at a time.").ConfigureAwait(false);
            return;
        }

        if (Context.Message.MentionedUsers.Count == 0)
        {
            await ReplyAsync("A user must be mentioned in order to do this.").ConfigureAwait(false);
            return;
        }

        var usr = Context.Message.MentionedUsers.ElementAt(0);
        var sig = usr.GetFavor();
        await Task.Run(async () =>
        {
            await TradeAsyncAttachInternal(code, sig, usr).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    [Command("tradeUser")]
    [Alias("tu", "tradeOther")]
    [Summary("Makes the bot trade the mentioned user the attached file.")]
    [RequireSudo]
    public Task TradeAsyncAttachUser([Remainder] string _)
    {
        var userID = Context.User.Id;
        var code = Info.GetRandomTradeCode(userID, Context.Channel, Context.User);
        return TradeAsyncAttachUser(code, _);
    }

    private async Task TradeAsyncAttachInternal(int code, RequestSignificance sig, SocketUser usr, bool ignoreAutoOT = false)
    {
        var attachment = Context.Message.Attachments.FirstOrDefault();
        if (attachment == default)
        {
            await ReplyAsync("No attachment provided!").ConfigureAwait(false);
            return;
        }
        var att = await NetUtil.DownloadPKMAsync(attachment).ConfigureAwait(false);
        var pk = GetRequest(att);
        if (pk == null)
        {
            await ReplyAsync("Attachment provided is not compatible with this module!").ConfigureAwait(false);
            return;
        }
        await AddTradeToQueueAsync(code, usr.Username, pk, sig, usr, ignoreAutoOT: ignoreAutoOT).ConfigureAwait(false);
    }

    private async Task HideTradeAsyncAttach(int code, RequestSignificance sig, SocketUser usr, bool ignoreAutoOT = false)
    {
        var attachment = Context.Message.Attachments.FirstOrDefault();
        if (attachment == default)
        {
            await ReplyAsync("No attachment provided!").ConfigureAwait(false);
            return;
        }

        var att = await NetUtil.DownloadPKMAsync(attachment).ConfigureAwait(false);
        var pk = GetRequest(att);
        if (pk == null)
        {
            await ReplyAsync("Attachment provided is not compatible with this module!").ConfigureAwait(false);
            return;
        }
        await AddTradeToQueueAsync(code, usr.Username, pk, sig, usr, isHiddenTrade: true, ignoreAutoOT: ignoreAutoOT).ConfigureAwait(false);
    }

    private static T? GetRequest(Download<PKM> dl)
    {
        if (!dl.Success)
            return null;
        return dl.Data switch
        {
            null => null,
            T pk => pk,
            _ => EntityConverter.ConvertToType(dl.Data, typeof(T), out _) as T,
        };
    }

    private async Task AddTradeToQueueAsync(int code, string trainerName, T? pk, RequestSignificance sig, SocketUser usr, bool isBatchTrade = false, int batchTradeNumber = 1, int totalBatchTrades = 1, bool isHiddenTrade = false, bool isMysteryMon = false, bool isMysteryEgg = false, List<Pictocodes>? lgcode = null, PokeTradeType tradeType = PokeTradeType.Specific, bool ignoreAutoOT = false, bool setEdited = false)
    {
        lgcode ??= TradeModule<T>.GenerateRandomPictocodes(3);
        if (pk is not null && !pk.CanBeTraded())
        {
            var reply = await ReplyAsync("Provided Pokémon content is blocked from trading!").ConfigureAwait(false);
            await Task.Delay(6000).ConfigureAwait(false); // Delay for 6 seconds
            await reply.DeleteAsync().ConfigureAwait(false);
            return;
        }
        var la = new LegalityAnalysis(pk!);
        if (!la.Valid)
        {
            string responseMessage;
            if (pk?.IsEgg == true)
            {
                string speciesName = SpeciesName.GetSpeciesName(pk.Species, (int)LanguageID.English);
                responseMessage = $"Invalid Showdown Set for the {speciesName} egg. Please review your information and try again.\n```\n{la.Report()}\n```";
            }
            else
            {
                string speciesName = SpeciesName.GetSpeciesName(pk!.Species, (int)LanguageID.English);
                responseMessage = $"{speciesName} attachment is not legal, and cannot be traded!\n\nLegality Report:\n```\n{la.Report()}\n```";
            }
            var reply = await ReplyAsync(responseMessage).ConfigureAwait(false);
            await Task.Delay(6000);
            await reply.DeleteAsync().ConfigureAwait(false);
            return;
        }
        bool isNonNative = false;
        // Check if the Pokémon's context is different or if it's from Pokémon GO (GO Pokémon are non-native)
        if (la.EncounterOriginal.Context != pk?.Context || pk?.GO == true)
        {
            isNonNative = true;
        }
        // Prevent trade if non-native and bot settings disallow non-native Pokémon
        if (Info.Hub.Config.Legality.DisallowNonNatives && (la.EncounterOriginal.Context != pk?.Context || pk?.GO == true))
        {
            // Allow the owner to prevent trading entities that require a HOME Tracker even if the file has one already.
            string speciesName = SpeciesName.GetSpeciesName(pk!.Species, (int)LanguageID.English);
            await ReplyAsync($"This **{speciesName}** is not native to this game, and cannot be traded! Trade with the correct bot, then trade to HOME.").ConfigureAwait(false);
            return;
        }
        // Prevent trade if the Pokémon has a HOME tracker and bot settings disallow tracked Pokémon
        if (Info.Hub.Config.Legality.DisallowTracked && pk is IHomeTrack { HasTracker: true })
        {
            string speciesName = SpeciesName.GetSpeciesName(pk.Species, (int)LanguageID.English);
            await ReplyAsync($"This {speciesName} file is tracked by HOME, and cannot be traded!").ConfigureAwait(false);
            return;
        }
        // handle past gen file requests
        // thanks manu https://github.com/Manu098vm/SysBot.NET/commit/d8c4b65b94f0300096704390cce998940413cc0d
        if (!la.Valid && la.Results.Any(m => m.Identifier is CheckIdentifier.Memory))
        {
            var clone = (T)pk!.Clone();
            clone.HandlingTrainerName = pk.OriginalTrainerName;
            clone.HandlingTrainerGender = pk.OriginalTrainerGender;
            if (clone is PK8 or PA8 or PB8 or PK9)
                ((dynamic)clone).HandlingTrainerLanguage = (byte)pk.Language;
            clone.CurrentHandler = 1;
            la = new LegalityAnalysis(clone);
            if (la.Valid) pk = clone;
        }
        await QueueHelper<T>.AddToQueueAsync(
            Context,
            code,
            trainerName,
            sig,
            pk!,
            PokeRoutineType.LinkTrade,
            tradeType,
            usr,
            isBatchTrade: isBatchTrade,
            batchTradeNumber: batchTradeNumber,
            totalBatchTrades: totalBatchTrades,
            isHiddenTrade: isHiddenTrade,
            isMysteryEgg: isMysteryEgg,
            lgcode: lgcode,
            ignoreAutoOT: ignoreAutoOT,
            setEdited: setEdited,
            isNonNative: isNonNative
        ).ConfigureAwait(false);
    }

    public static List<Pictocodes> GenerateRandomPictocodes(int count)
    {
        Random rnd = new();
        List<Pictocodes> randomPictocodes = [];
        Array pictocodeValues = Enum.GetValues(typeof(Pictocodes));

        for (int i = 0; i < count; i++)
        {
#pragma warning disable CS8605 // Unboxing a possibly null value.
            Pictocodes randomPictocode = (Pictocodes)pictocodeValues.GetValue(rnd.Next(pictocodeValues.Length));
#pragma warning restore CS8605 // Unboxing a possibly null value.
            randomPictocodes.Add(randomPictocode);
        }

        return randomPictocodes;
    }

    private async Task ReplyAndDeleteAsync(string message, int delaySeconds, IMessage? messageToDelete = null)
    {
        try
        {
            var sentMessage = await ReplyAsync(message).ConfigureAwait(false);
            _ = DeleteMessagesAfterDelayAsync(sentMessage, messageToDelete, delaySeconds);
        }
        catch (Exception ex)
        {
            LogUtil.LogSafe(ex, nameof(TradeModule<T>));
        }
    }

    private async Task DeleteMessagesAfterDelayAsync(IMessage? sentMessage, IMessage? messageToDelete, int delaySeconds)
    {
        try
        {
            await Task.Delay(delaySeconds * 1000);

            if (sentMessage != null)
            {
                try
                {
                    await sentMessage.DeleteAsync();
                }
                catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.UnknownMessage)
                {
                    // Ignore Unknown Message exception
                }
            }

            if (messageToDelete != null)
            {
                try
                {
                    await messageToDelete.DeleteAsync();
                }
                catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.UnknownMessage)
                {
                    // Ignore Unknown Message exception
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.LogSafe(ex, nameof(TradeModule<T>));
        }
    }

    [Command("homeready")]
    [Alias("hr")]
    [Summary("Displays instructions on how to use the HOME-Ready module.")]
    private async Task HomeReadyInstructionsAsync()
    {
        var embed0 = new EmbedBuilder()
            .WithTitle("-------HOME-READY MODULE INSTRUCTIONS-------");

        embed0.WithImageUrl("https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/homereadybreak.png");
        var message0 = await ReplyAsync(embed: embed0.Build());


        var embed1 = new EmbedBuilder()
            .AddField("GET LIST: `hrl <Pokemon>`",
                      "- This will search for any Pokemon in the entire module.\n" +
                      "**Example:** `hrl Mewtwo`\n");

        embed1.WithImageUrl("https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/homereadybreak.png");
        var message1 = await ReplyAsync(embed: embed1.Build());


        var embed2 = new EmbedBuilder()
            .AddField("CHANGE PAGES: `hrl <page>`",
                      "- This will change the page you're viewing, with or without additional variables.\n" +
                      "**Example:** `hrl 5 Charmander`\n");

        embed2.WithImageUrl("https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/homereadybreak.png");
        var message2 = await ReplyAsync(embed: embed2.Build());

        var embed3 = new EmbedBuilder()
            .AddField("TRADING FILES: `hrr <number>`",
                      "- This will trade you the Pokemon through the bot via the designated number.\n" +
                      "**Example:** `hrr 682`\n");

        embed3.WithImageUrl("https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/homereadybreak.png");
        var message3 = await ReplyAsync(embed: embed3.Build());

        _ = Task.Run(async () =>
        {
            await Task.Delay(90_000);
            await message0.DeleteAsync();
            await message1.DeleteAsync();
            await message2.DeleteAsync();
        });
    }

    [Command("homereadyrequest")]
    [Alias("hrr")]
    [Summary("Downloads HOME-ready attachments from the specified folder and adds it to the trade queue.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    private async Task HOMEReadyRequestAsync(int index)
    {
        // Check if the user is already in the queue
        var userID = Context.User.Id;
        if (Info.IsUserInQueue(userID))
        {
            await ReplyAsync("You're already in a queue. Finish with your current queue before attempting to join another.").ConfigureAwait(false);
            return;
        }
        try
        {
            var homeReadyFolderPath = SysCord<T>.Runner.Config.Trade.RequestFolderSettings.HOMEReadyPKMFolder;
            var botPrefix = SysCord<T>.Runner.Config.Discord.CommandPrefix;
            var homeReadyFiles = Directory.GetFiles(homeReadyFolderPath)
                                            .Select(Path.GetFileName)
                                            .OrderBy(x => x)
                                            .ToList();

            // Check if the HOME-ready folder path is not set or empty
            if (string.IsNullOrEmpty(homeReadyFolderPath))
            {
                await ReplyAsync("This bot does not have this feature set up.");
                return;
            }

            if (index < 1 || index > homeReadyFiles.Count)
            {
                await ReplyAsync("Your selection was invalid. Please use a valid file number.").ConfigureAwait(false);
                return;
            }

            var selectedFile = homeReadyFiles[index - 1];
            var fileData = await File.ReadAllBytesAsync(Path.Combine(homeReadyFolderPath, selectedFile));

            var download = new Download<PKM>
            {
                Data = EntityFormat.GetFromBytes(fileData),
                Success = true
            };

            var pk = GetRequest(download);
            if (pk == null)
            {
                await ReplyAsync("Failed to convert the legal HOME-ready file to the required PKM type.").ConfigureAwait(false);
                return;
            }

            var code = Info.GetRandomTradeCode(userID, Context.Channel, Context.User);
            var lgcode = Info.GetRandomLGTradeCode();
            var sig = Context.User.GetFavor();
            var tradeMessage = await Context.Channel.SendMessageAsync($"HOME-Ready request added to queue.");
            await AddTradeToQueueAsync(code, Context.User.Username, pk, sig, Context.User, lgcode: lgcode).ConfigureAwait(false);

        }
        catch (Exception ex)
        {
            await ReplyAsync($"**Error:** {ex.Message}").ConfigureAwait(false);
        }
        finally
        {
            if (Context.Message is IUserMessage userMessage)
            {
                await userMessage.DeleteAsync().ConfigureAwait(false);
            }
        }

    }

    [Command("homereadylist")]
    [Alias("hrl")]
    [Summary("Lists available HOME-ready files, filtered by a specific letter or substring, then sends the list to the channel.")]
    private async Task HOMEListAsync([Remainder] string args = "")
    {
        const int itemsPerPage = 10; // Number of items per page
        var homeReadyFolderPath = SysCord<T>.Runner.Config.Trade.RequestFolderSettings.HOMEReadyPKMFolder;
        var botPrefix = SysCord<T>.Runner.Config.Discord.CommandPrefix;

        // Check if the homeready folder path is not set or empty
        if (string.IsNullOrEmpty(homeReadyFolderPath))
        {
            await ReplyAsync("This bot does not have this feature set up.");
            return;
        }

        // Parsing the arguments to separate filter and page number
        string filter = "";
        int page = 1; // Declare and initialize the page variable
        var parts = args.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length > 0)
        {
            // Check if the last part is a number (page number)
            if (int.TryParse(parts.Last(), out int parsedPage))
            {
                page = parsedPage;
                filter = string.Join(" ", parts.Take(parts.Length - 1));
            }
            else
            {
                filter = string.Join(" ", parts);
            }
        }

        var allHOMEReadyFiles = Directory.GetFiles(homeReadyFolderPath)
                                           .Select(Path.GetFileName)
                                           .OrderBy(file => file)
                                           .ToList();

        var filteredHOMEReadyFiles = allHOMEReadyFiles
                                       .Where(file => string.IsNullOrWhiteSpace(filter) || file.Contains(filter, StringComparison.OrdinalIgnoreCase))
                                       .ToList();

        IUserMessage replyMessage;

        // Check if there are no files matching the filter
        if (!filteredHOMEReadyFiles.Any())
        {
            replyMessage = await ReplyAsync($"No HOME-ready files found matching the filter '{filter}'.");
        }
        else
        {
            var pageCount = (int)Math.Ceiling(filteredHOMEReadyFiles.Count / (double)itemsPerPage);
            page = Math.Clamp(page, 1, pageCount); // Ensure page number is within valid range

            var pageItems = filteredHOMEReadyFiles.Skip((page - 1) * itemsPerPage).Take(itemsPerPage);

            var embed = new EmbedBuilder()
                .WithTitle($"Available HOME-Ready Files - Filter: '{filter}'")
                .WithDescription($"Page {page} of {pageCount}")
                .WithColor(Color.Blue);

            foreach (var item in pageItems)

            {
                var index = allHOMEReadyFiles.IndexOf(item) + 1; // Get the index from the original list
                embed.AddField($"{index}. {item}", $"Use `{botPrefix}hrr {index}` to trade this legal HOME-ready file.");
            }

            // Send confirmation message in the same channel
            replyMessage = await ReplyAsync($"Use `{botPrefix}hrl <page>` to change the page you are viewing.\n**Current Support:** USUM/LGPE/POGO/BDSP/SWSH/PLA -> SV.");
            var message = await ReplyAsync(embed: embed.Build());

            // Delay for 10 seconds
            await Task.Delay(20_000);

            // Delete the message
            await message.DeleteAsync();

        }
    }
}
