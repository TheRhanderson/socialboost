using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Web.Responses;
using static ArchiSteamFarm.Steam.Integration.ArchiWebHandler;

namespace Socialboost.Helpers;

internal static partial class AppIdHelper {
	private static readonly Regex AppIdRegex = DataAppId();

	/// <summary>
	/// Obtém o AppID de um sharedfile através da página
	/// </summary>
	internal static async Task<string?> ObterAppIdDoSharedfile(Bot bot, string sharedfileId) {
		if (string.IsNullOrEmpty(sharedfileId)) {
			ASF.ArchiLogger.LogNullError(nameof(sharedfileId));
			return null;
		}

		try {
			Uri requestViewPage = new(SteamCommunityURL, $"/sharedfiles/filedetails/?id={sharedfileId}");

			string cookieName = $"wants_mature_content_item_{sharedfileId}";
			string cookieValue = "1";

			System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>> headers = [
				new("Cookie", $"{cookieName}={cookieValue}")
			];

			HtmlDocumentResponse? response = await bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(
				requestViewPage,
				headers: headers,
				referer: SteamCommunityURL
			).ConfigureAwait(false);

			if (response?.Content == null) {
				ASF.ArchiLogger.LogGenericWarning($"Socialboost|APPID => Falha ao obter página do sharedfile {sharedfileId}");
				return null;
			}

			// Obtém o HTML como string
			string htmlContent = response.Content.Source.Text;

			// Extrai o AppID usando regex
			Match match = AppIdRegex.Match(htmlContent);
			if (match.Success && match.Groups.Count > 1) {
				string appId = match.Groups[1].Value;
				ASF.ArchiLogger.LogGenericInfo($"Socialboost|APPID => AppID {appId} detectado para sharedfile {sharedfileId}");
				return appId;
			}

			ASF.ArchiLogger.LogGenericWarning($"Socialboost|APPID => Não foi possível extrair AppID do sharedfile {sharedfileId}");
			return null;

		} catch (Exception ex) {
			ASF.ArchiLogger.LogGenericException(ex);
			return null;
		}
	}

	[GeneratedRegex(@"data-appid=""(\d+)""", RegexOptions.IgnoreCase | RegexOptions.Compiled, "pt-BR")]
	private static partial Regex DataAppId();
}
