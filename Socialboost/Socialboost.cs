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
		ASF.ArchiLogger.LogGenericInfo($"SocialBoost 1.4.2.0 (.NET 10 ASF 3.6.1.6)");

		return Task.CompletedTask;
	}

	public async Task<string?> OnBotCommand(Bot bot, EAccess access, string message, string[] args, ulong steamID = 0) => args[0].ToUpperInvariant() switch {
		"CSHAREDLIKE" when args.Length > 2 => await CSharedLike.ExecutarComando(access, steamID, args[1], args[2]).ConfigureAwait(false),
		"CSHAREDFAV" when args.Length > 2 => await CSharedFav.ExecutarComando(access, steamID, args[1], args[2]).ConfigureAwait(false),
		"CSHAREDFILES" when args.Length > 2 => await CSharedFiles.ExecutarComando(access, steamID, args[1], args[2]).ConfigureAwait(false),     // Comandos principais de Sharedfiles (Like/Favorito Combinado)
		"CRATEREVIEW" when args.Length > 3 => await CReviews.ExecutarComando(access, steamID, args[1], args[2], args[3]).ConfigureAwait(false), // Comando de Reviews (1 = útil, 2 = engraçado, 3 = não útil)
		"CWORKSHOP" when args.Length > 3 => await CWorkshop.ExecutarComando(access, steamID, args[1], args[2], args[3]).ConfigureAwait(false),  // Comando de Workshop (1 = follow, 2 = unfollow)
		_ => null
	};
}
#pragma warning restore CA1812 // ASF uses this class during runtime
