using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Interaction;
using ArchiSteamFarm.Web.Responses;
using Socialboost.Helpers;
using static ArchiSteamFarm.Steam.Integration.ArchiWebHandler;

namespace Socialboost;

/// <summary>
/// Classe combinada que envia Like + Favorito em um único comando
/// Otimiza a visualização da página fazendo apenas uma vez por bot
/// </summary>
internal static class CSharedFiles {

	// Intervalo entre requisições (3 segundos = 20 req/min, abaixo do limite de 25/min)
	private static readonly TimeSpan IntervaloEntreRequisicoes = TimeSpan.FromSeconds(3);

	// Semáforo para controlar o rate limiting global
	private static readonly SemaphoreSlim RateLimitSemaphore = new(1, 1);

	/// <summary>
	/// Resultado do envio combinado (like + favorito) para um bot específico
	/// </summary>
	internal sealed class ResultadoCombinado {
		public required string BotName { get; init; }
		public required bool LikeSucesso { get; init; }
		public required string LikeMensagem { get; init; }
		public required bool FavSucesso { get; init; }
		public required string FavMensagem { get; init; }
		public bool JaEnviado { get; init; }
		public bool ContaLimitada { get; init; }
		public bool BotOffline { get; init; }

		/// <summary>
		/// Retorna true se pelo menos uma ação foi bem-sucedida
		/// </summary>
		public bool AlgumSucesso => LikeSucesso || FavSucesso;

		/// <summary>
		/// Retorna true se ambas as ações foram bem-sucedidas
		/// </summary>
		public bool SucessoTotal => LikeSucesso && FavSucesso;
	}

	/// <summary>
	/// Envia like E favorito de um bot específico para o sharedfile
	/// Otimiza fazendo apenas uma visualização de página
	/// Versão interna que recebe os flags de já enviado para evitar verificações duplicadas
	/// </summary>
	internal static async Task<ResultadoCombinado> EnviarLikeEFavoritoInterno(
		Bot bot,
		EAccess access,
		string id,
		string appId,
		bool jaEnviouLike,
		bool jaEnviouFav) {

		// Verifica permissão
		if (access < EAccess.Master) {
			return new ResultadoCombinado {
				BotName = bot.BotName,
				LikeSucesso = false,
				LikeMensagem = "Acesso negado",
				FavSucesso = false,
				FavMensagem = "Acesso negado"
			};
		}

		// Verifica se o bot está online
		if (!bot.IsConnectedAndLoggedOn) {
			return new ResultadoCombinado {
				BotName = bot.BotName,
				LikeSucesso = false,
				LikeMensagem = "Bot offline",
				FavSucesso = false,
				FavMensagem = "Bot offline",
				BotOffline = true
			};
		}

		bot.ArchiLogger.LogGenericInfo($"Socialboost|SHAREDFILES => {id} (Enviando Like + Fav...)");

		Uri requestViewPage = new(SteamCommunityURL, $"/sharedfiles/filedetails/?id={id}");

		// Visualiza a página apenas UMA vez (se ainda não visualizou para nenhum dos dois)
		if (!jaEnviouLike && !jaEnviouFav) {
			HtmlDocumentResponse? viewResponse = await VisualizarPagina(id, bot, requestViewPage).ConfigureAwait(false);
			if (viewResponse == null) {
				bot.ArchiLogger.LogGenericWarning($"Socialboost|SHAREDFILES => {id} (Falha ao visualizar página, tentando enviar mesmo assim...)");
			}
		}

		// Variáveis para armazenar resultados
		bool likeSucesso = false;
		string likeMensagem = "Não enviado";
		bool favSucesso = false;
		string favMensagem = "Não enviado";
		bool contaLimitadaParaLike = false;

		// === ENVIO DO LIKE ===
		if (!jaEnviouLike) {
			// Verifica se a conta é limitada (likes não funcionam com contas limitadas)
			if (bot.IsAccountLimited) {
				likeMensagem = "Conta limitada";
				contaLimitadaParaLike = true;
			} else {
				(likeSucesso, likeMensagem) = await EnviarLikeInterno(bot, id, requestViewPage).ConfigureAwait(false);

				if (likeSucesso) {
					await DbHelper.AdicionarEnvioItem(bot.BotName, "SHAREDLIKE", id).ConfigureAwait(false);
				}
			}
		} else {
			likeMensagem = "Já enviado anteriormente";
		}

		// Aguarda um pequeno intervalo entre like e favorito do mesmo bot
		await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

		// === ENVIO DO FAVORITO ===
		if (!jaEnviouFav) {
			// Favoritos funcionam mesmo com contas limitadas
			(favSucesso, favMensagem) = await EnviarFavoritoInterno(bot, id, appId, requestViewPage).ConfigureAwait(false);

			if (favSucesso) {
				await DbHelper.AdicionarEnvioItem(bot.BotName, "SHAREDFAV", id).ConfigureAwait(false);
			}
		} else {
			favMensagem = "Já enviado anteriormente";
		}

		// Log do resultado
		string statusLike = likeSucesso ? "OK" : "FALHA";
		string statusFav = favSucesso ? "OK" : "FALHA";
		bot.ArchiLogger.LogGenericInfo($"Socialboost|SHAREDFILES => {id} (Like: {statusLike}, Fav: {statusFav})");

		return new ResultadoCombinado {
			BotName = bot.BotName,
			LikeSucesso = likeSucesso,
			LikeMensagem = likeMensagem,
			FavSucesso = favSucesso,
			FavMensagem = favMensagem,
			ContaLimitada = contaLimitadaParaLike
		};
	}

	/// <summary>
	/// Envia like E favorito de um bot específico para o sharedfile (versão pública que faz verificação)
	/// </summary>
	internal static async Task<ResultadoCombinado> EnviarLikeEFavorito(Bot bot, EAccess access, string id, string appId) {
		// Verifica se já foi enviado anteriormente
		bool? jaEnviouLike = await DbHelper.VerificarEnvioItem(bot.BotName, "SHAREDLIKE", id).ConfigureAwait(false);
		bool? jaEnviouFav = await DbHelper.VerificarEnvioItem(bot.BotName, "SHAREDFAV", id).ConfigureAwait(false);

		// Se já enviou ambos, retorna como já enviado
		if (jaEnviouLike == true && jaEnviouFav == true) {
			return new ResultadoCombinado {
				BotName = bot.BotName,
				LikeSucesso = false,
				LikeMensagem = "Já enviado",
				FavSucesso = false,
				FavMensagem = "Já enviado",
				JaEnviado = true
			};
		}

		return await EnviarLikeEFavoritoInterno(bot, access, id, appId, jaEnviouLike == true, jaEnviouFav == true).ConfigureAwait(false);
	}

	/// <summary>
	/// Envia o like internamente e retorna o resultado
	/// Usa UrlPostWithSession (mesmo método do código original que funciona)
	/// </summary>
	private static async Task<(bool sucesso, string mensagem)> EnviarLikeInterno(Bot bot, string id, Uri referer) {
		Uri requestUrl = new(SteamCommunityURL, "/sharedfiles/voteup");

		Dictionary<string, string> data = new(1) {
			{ "id", id }
		};

		// Usa o mesmo método do código original que funciona
		bool postSuccess = await bot.ArchiWebHandler.UrlPostWithSession(
			requestUrl,
			data: data,
			session: ESession.Lowercase,
			referer: referer
		).ConfigureAwait(false);

		if (postSuccess) {
			return (true, "OK");
		}

		return (false, "Erro HTTP");
	}

	/// <summary>
	/// Envia o favorito internamente e retorna o resultado
	/// </summary>
	private static async Task<(bool sucesso, string mensagem)> EnviarFavoritoInterno(Bot bot, string id, string appId, Uri referer) {
		Uri requestUrl = new(SteamCommunityURL, "/sharedfiles/favorite");

		Dictionary<string, string> data = new(2) {
			{ "id", id },
			{ "appid", appId }
		};

		bool postSuccess = await bot.ArchiWebHandler.UrlPostWithSession(
			requestUrl,
			data: data,
			session: ESession.Lowercase,
			referer: referer
		).ConfigureAwait(false);

		if (postSuccess) {
			return (true, "OK");
		}

		return (false, "Erro HTTP");
	}

	/// <summary>
	/// Visualiza a página do sharedfile (necessário para contornar algumas proteções do Steam)
	/// </summary>
	private static async Task<HtmlDocumentResponse?> VisualizarPagina(string id, Bot bot, Uri requestViewPage) {
		string cookieName = $"wants_mature_content_item_{id}";
		string cookieValue = "1";

		List<KeyValuePair<string, string>> headers = [
			new("Cookie", $"{cookieName}={cookieValue}")
		];

		return await bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(
			requestViewPage,
			headers: headers,
			referer: SteamCommunityURL
		).ConfigureAwait(false);
	}

	/// <summary>
	/// Comando principal que processa múltiplos bots com rate limiting
	/// </summary>
	public static async Task<string?> ExecutarComando(EAccess access, ulong steamID, string sharedfileId, string quantidadeDesejada) {
		// Validação dos parâmetros
		if (string.IsNullOrEmpty(sharedfileId) || string.IsNullOrEmpty(quantidadeDesejada)) {
			ASF.ArchiLogger.LogNullError(null, nameof(sharedfileId) + " || " + nameof(quantidadeDesejada));
			return null;
		}

		if (!int.TryParse(quantidadeDesejada, out int numEnviosDesejados) || numEnviosDesejados <= 0) {
			return access >= EAccess.Owner ? FormatarResposta("Quantidade inválida. Use um número positivo.") : null;
		}

		// Obtém todos os bots
		HashSet<Bot>? bots = Bot.GetBots("ASF");
		if (bots == null || bots.Count == 0) {
			return access >= EAccess.Owner ? FormatarResposta(Strings.BotNotFound) : null;
		}

		// Verifica quantos bots estão online
		int botsOnline = 0;
		foreach (Bot bot in bots) {
			if (bot.IsConnectedAndLoggedOn) {
				botsOnline++;
			}
		}

		if (botsOnline < numEnviosDesejados) {
			return access >= EAccess.Owner
				? FormatarResposta($"Bots online disponíveis: {botsOnline}. Necessário: {numEnviosDesejados}")
				: null;
		}

		// Obtém o AppID do sharedfile usando o primeiro bot disponível
		Bot firstBot = bots.First(b => b.IsConnectedAndLoggedOn);
		string? appId = await SessionHelper.FetchAppIDShared(
			firstBot,
			$"https://steamcommunity.com/sharedfiles/filedetails/?id={sharedfileId}",
			sharedfileId
		).ConfigureAwait(false);

		if (string.IsNullOrEmpty(appId)) {
			return access >= EAccess.Owner
				? FormatarResposta($"Erro ao obter AppID do sharedfile {sharedfileId}")
				: null;
		}

		// Listas para armazenar resultados
		List<ResultadoCombinado> resultados = [];
		int botsConsiderados = 0;  // Conta todos os bots válidos (online), incluindo os que já enviaram
		int likesEnviados = 0;
		int favsEnviados = 0;
		int likesFalhados = 0;
		int favsFalhados = 0;
		int jaEnviadosAnteriormente = 0;
		bool precisaDelay = false;  // Controla se precisa aplicar delay antes da próxima requisição

		foreach (Bot bot in bots) {
			// Para se já atingiu o número desejado de bots considerados
			if (likesEnviados >= numEnviosDesejados && favsEnviados >= numEnviosDesejados) {
				break;
			}

			// Pula bots offline (não conta como considerado)
			if (!bot.IsConnectedAndLoggedOn) {
				continue;
			}

			// Verifica ANTES se já enviou (sem fazer requisição HTTP)
			bool? jaEnviouLike = await DbHelper.VerificarEnvioItem(bot.BotName, "SHAREDLIKE", sharedfileId).ConfigureAwait(false);
			bool? jaEnviouFav = await DbHelper.VerificarEnvioItem(bot.BotName, "SHAREDFAV", sharedfileId).ConfigureAwait(false);

			// Se já enviou ambos, registra e pula sem delay
			if (jaEnviouLike == true && jaEnviouFav == true) {
				jaEnviadosAnteriormente++;
				resultados.Add(new ResultadoCombinado {
					BotName = bot.BotName,
					LikeSucesso = false,
					LikeMensagem = "Já enviado",
					FavSucesso = false,
					FavMensagem = "Já enviado",
					JaEnviado = true
				});
				continue;  // Não precisa de delay, não fez requisição HTTP
			}

			// Aplica delay APENAS se a requisição anterior foi uma requisição HTTP real
			if (precisaDelay) {
				await Task.Delay(IntervaloEntreRequisicoes).ConfigureAwait(false);
			}

			// Aplica rate limiting para requisições reais
			await RateLimitSemaphore.WaitAsync().ConfigureAwait(false);
			try {
				ResultadoCombinado resultado = await EnviarLikeEFavoritoInterno(
					bot,
					Commands.GetProxyAccess(bot, access, steamID),
					sharedfileId,
					appId,
					jaEnviouLike == true,
					jaEnviouFav == true
				).ConfigureAwait(false);

				// Bot offline durante o processamento (raro, mas possível)
				if (resultado.BotOffline) {
					continue;
				}

				// Conta o bot como considerado
				botsConsiderados++;
				resultados.Add(resultado);

				if (resultado.LikeSucesso) {
					likesEnviados++;
				} else if (resultado.LikeMensagem != "Já enviado anteriormente" && !resultado.ContaLimitada) {
					likesFalhados++;
				}

				if (resultado.FavSucesso) {
					favsEnviados++;
				} else if (resultado.FavMensagem != "Já enviado anteriormente") {
					favsFalhados++;
				}

				// Marca que a próxima iteração precisa de delay (pois fizemos requisição HTTP)
				precisaDelay = true;

			} finally {
				RateLimitSemaphore.Release();
			}
		}

		// Monta a resposta final
		return MontarRespostaFinal(resultados, likesEnviados, favsEnviados, likesFalhados, favsFalhados, numEnviosDesejados, jaEnviadosAnteriormente);
	}

	/// <summary>
	/// Monta a resposta final formatada com todos os resultados
	/// </summary>
	private static string MontarRespostaFinal(
		List<ResultadoCombinado> resultados,
		int likesOk,
		int favsOk,
		int likesFail,
		int favsFail,
		int desejados,
		int jaEnviados) {

		List<string> linhas = [];

		// Adiciona cada resultado
		foreach (ResultadoCombinado resultado in resultados) {
			if (resultado.JaEnviado) {
				linhas.Add($"<{resultado.BotName}> [=] Já enviado anteriormente");
			} else {
				string likeStatus = resultado.LikeSucesso ? "+" : "-";
				string favStatus = resultado.FavSucesso ? "+" : "-";

				string likeDetalhe = resultado.LikeSucesso ? "LIKE" : resultado.LikeMensagem;
				string favDetalhe = resultado.FavSucesso ? "FAV" : resultado.FavMensagem;

				linhas.Add($"<{resultado.BotName}> [{likeStatus}] {likeDetalhe} | [{favStatus}] {favDetalhe}");
			}
		}

		// Adiciona resumo final
		linhas.Add("---------------------------------");
		linhas.Add($"Likes: {likesOk}/{desejados} (Falhas: {likesFail})");
		linhas.Add($"Favs:  {favsOk}/{desejados} (Falhas: {favsFail})");

		if (jaEnviados > 0) {
			linhas.Add($"Ignorados (já enviados): {jaEnviados}");
		}

		ASF.ArchiLogger.LogGenericInfo($"Socialboost|SHAREDFILES => Concluido! Likes: {likesOk}, Favs: {favsOk}, Ignorados: {jaEnviados}");

		return string.Join(Environment.NewLine, linhas);
	}

	private static string FormatarResposta(string mensagem) => $"<Socialboost> {mensagem}";
}
