using System;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;
using JetBrains.Annotations;


namespace Socialboost;

#pragma warning disable CA1812 // ASF uses this class during runtime
[UsedImplicitly]
internal sealed class Socialboost : IGitHubPluginUpdates, IBotCommand2, IPlugin {
	public string Name => nameof(Socialboost);
	public string RepositoryName => "socialboost";
	public Version Version => typeof(Socialboost).Assembly.GetName().Version ?? throw new InvalidOperationException(nameof(Version));

	public Task OnLoaded() {
		ASF.ArchiLogger.LogGenericInfo($"Hello  {Name}!");

		return Task.CompletedTask;
	}

	public async Task<string?> OnBotCommand(Bot bot, EAccess access, string message, string[] args, ulong steamID = 0) => args[0].ToUpperInvariant() switch {
		// Comandos principais de Sharedfiles (Like, Favorito e Combinado)
		"CSHAREDLIKE" when args.Length > 2 => await CSharedLike.ExecutarComando(access, steamID, args[1], args[2]).ConfigureAwait(false),
		"CSHAREDFAV" when args.Length > 2 => await CSharedFav.ExecutarComando(access, steamID, args[1], args[2]).ConfigureAwait(false),
		"CSHAREDFILES" when args.Length > 2 => await CSharedFiles.ExecutarComando(access, steamID, args[1], args[2]).ConfigureAwait(false),
		"CRATEREVIEW" when args.Length > 3 => await CReviews.ExecutarComando(access, steamID, args[1], args[2], args[3]).ConfigureAwait(false),
		_ => null
	};
}
#pragma warning restore CA1812 // ASF uses this class during runtime
