using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Web.Responses;
using System.Text.RegularExpressions;
using static ArchiSteamFarm.Steam.Integration.ArchiWebHandler;
using System.Collections.Generic;

namespace Socialboost.Helpers;

internal static partial class SessionHelper {
	public static async Task<string?> FetchSessionID(Bot bot) {
		CookieCollection cc = bot.ArchiWebHandler.WebBrowser.CookieContainer.GetCookies(SteamStoreURL);
		Cookie? sessionIdCookie = cc.Cast<Cookie>().FirstOrDefault(c => c.Name.Equals("sessionid", StringComparison.OrdinalIgnoreCase));

		return sessionIdCookie != null
			? await Task.FromResult<string?>(sessionIdCookie.Value).ConfigureAwait(false)
			: await Task.FromResult<string?>("").ConfigureAwait(false);
	}

	internal static async Task<string?> FetchReviewID(string urlReview) {

		Uri uri2 = new(urlReview);
		HtmlDocumentResponse? response = await ASF.WebBrowser!.UrlGetToHtmlDocument(uri2, referer: SteamCommunityURL).ConfigureAwait(false);

		if (response == null || response.Content?.Body == null) {
			ASF.ArchiLogger.LogGenericError("A requisição não retornou uma resposta válida.");
			return string.Empty;
		}

		string strd = response.Content.Body.InnerHtml;

		Match match = FetchReviewRegex().Match(strd);

		if (match.Success) {
			return match.Groups[1].Value;
		} else {
			ASF.ArchiLogger.LogGenericError("A requisição não retornou uma resposta válida.");
			return string.Empty;
		}
	}

	internal static async Task<string?> FetchAppIDShared(Bot firstBot, string urlReview, string id) {
		Uri uri2 = new(urlReview);

		string cookieName = $"wants_mature_content_item_{id}";
		string cookieValue = "1";

		List<KeyValuePair<string, string>> checkcontas = [new("Cookie", $"{cookieName}={cookieValue}")];

		HtmlDocumentResponse? response = await firstBot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(uri2, referer: SteamCommunityURL, headers: checkcontas).ConfigureAwait(false); // await ASF.WebBrowser!.UrlGetToHtmlDocument(uri2, referer: SteamCommunityURL).ConfigureAwait(false);

		if (response == null || response.Content?.Body == null) {
			ASF.ArchiLogger.LogGenericError("A requisição não retornou uma resposta válida.");
			return string.Empty;
		}

		string strd = response.Content.Body.InnerHtml;


		Match match = FetchAppRegex().Match(strd);

		if (match.Success) {
			return match.Groups[1].Value;
		} else {
			ASF.ArchiLogger.LogGenericError("A requisição não retornou uma resposta válida.");
			return string.Empty;
		}
	}


	internal static async Task<string?> FetchSteamID64(string urlReview) {
		Uri uri2 = new(urlReview);
		HtmlDocumentResponse? response = await ASF.WebBrowser!.UrlGetToHtmlDocument(uri2, referer: SteamCommunityURL).ConfigureAwait(false);

		if (response == null || response.Content?.Body == null) {
			ASF.ArchiLogger.LogGenericError("A requisição não retornou uma resposta válida.");
			return string.Empty;
		}

		string strd = response.Content.Body.InnerHtml;

		Match match = FetchID64Regex().Match(strd);

		if (match.Success) {
			return match.Groups[1].Value;
		} else {
			ASF.ArchiLogger.LogGenericError("A requisição não retornou uma resposta válida.");
			return string.Empty;
		}
	}

	[GeneratedRegex(@"RecordAppImpression\(\s*(\d+)\s*,\s*'[^']*'\s*\);")]
	private static partial Regex FetchAppRegex();

	[GeneratedRegex(@"""steamid"":""(\d+)""")]
	private static partial Regex FetchID64Regex();
	[GeneratedRegex(@"UserReview_Report\(\s*'(\d+)',\s*'https://steamcommunity.com',\s*function\( results \)")]
	private static partial Regex FetchReviewRegex();
}
