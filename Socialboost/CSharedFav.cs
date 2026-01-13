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

internal static class CSharedFav {

	// Intervalo entre requisições (3 segundos = 20 req/min, abaixo do limite de 25/min)
	private static readonly TimeSpan IntervaloEntreRequisicoes = TimeSpan.FromSeconds(3);

	// Semáforo para controlar o rate limiting global
	private static readonly SemaphoreSlim RateLimitSemaphore = new(1, 1);

	/// <summary>
	/// Resultado do envio de favorito para um bot específico
	/// </summary>
	internal sealed class ResultadoEnvio {
		public required string BotName { get; init; }
		public required bool Sucesso { get; init; }
		public required string Mensagem { get; init; }
		public bool JaEnviado { get; init; }
		public bool ContaLimitada { get; init; }
		public bool BotOffline { get; init; }
	}

	/// <summary>
	/// Envia favorito de um bot específico para o sharedfile
	/// Versão interna que assume que verificação prévia já foi feita
	/// </summary>
	internal static async Task<ResultadoEnvio> EnviarFavoritoInterno(Bot bot, EAccess access, string id, string appId) {
		// Verifica permissão
		if (access < EAccess.Master) {
			return new ResultadoEnvio {
				BotName = bot.BotName,
				Sucesso = false,
				Mensagem = "Acesso negado"
			};
		}

		// Verifica se o bot está online
		if (!bot.IsConnectedAndLoggedOn) {
			return new ResultadoEnvio {
				BotName = bot.BotName,
				Sucesso = false,
				Mensagem = "Bot offline",
				BotOffline = true
			};
		}

		// Nota: Favoritos funcionam com contas limitadas, então não verificamos IsAccountLimited

		bot.ArchiLogger.LogGenericInfo($"Socialboost|SHAREDFAV => {id} (Enviando...)");

		Uri requestUrl = new(SteamCommunityURL, "/sharedfiles/favorite");
		Uri requestViewPage = new(SteamCommunityURL, $"/sharedfiles/filedetails/?id={id}");

		// Visualiza a página se necessário (para contornar proteções do Steam)
		bool? jaVisualizouParaLike = await DbHelper.VerificarEnvioItem(bot.BotName, "SHAREDLIKE", id).ConfigureAwait(false);
		if (jaVisualizouParaLike != true) {
			HtmlDocumentResponse? viewResponse = await VisualizarPagina(id, bot, requestViewPage).ConfigureAwait(false);
			if (viewResponse == null) {
				bot.ArchiLogger.LogGenericWarning($"Socialboost|SHAREDFAV => {id} (Falha ao visualizar página, tentando enviar mesmo assim...)");
			}
		}

		Dictionary<string, string> data = new(2) {
			{ "id", id },
			{ "appid", appId }
		};

		// Envia a requisição POST
		bool postSuccess = await bot.ArchiWebHandler.UrlPostWithSession(
			requestUrl,
			data: data,
			session: ESession.Lowercase,
			referer: requestViewPage
		).ConfigureAwait(false);

		if (postSuccess) {
			// Salva no banco de dados
			bool? salvou = await DbHelper.AdicionarEnvioItem(bot.BotName, "SHAREDFAV", id).ConfigureAwait(false);
			if (salvou != true) {
				bot.ArchiLogger.LogGenericWarning($"Socialboost|SHAREDFAV => {id} (Favorito enviado, mas falha ao salvar no DB)");
			}

			bot.ArchiLogger.LogGenericInfo($"Socialboost|SHAREDFAV => {id} (OK)");
			return new ResultadoEnvio {
				BotName = bot.BotName,
				Sucesso = true,
				Mensagem = "OK"
			};
		}

		bot.ArchiLogger.LogGenericError($"Socialboost|SHAREDFAV => {id} (FALHA: Erro HTTP)");
		return new ResultadoEnvio {
			BotName = bot.BotName,
			Sucesso = false,
			Mensagem = "Erro HTTP"
		};
	}

	/// <summary>
	/// Visualiza a página do sharedfile (necessário para contornar algumas proteções do Steam)
	/// </summary>
	internal static async Task<HtmlDocumentResponse?> VisualizarPagina(string id, Bot bot, Uri requestViewPage) {
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

		// Verifica quantos bots estão online (favoritos funcionam com contas limitadas)
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
		List<ResultadoEnvio> resultados = [];
		int enviosBemSucedidos = 0;
		int enviosFalhados = 0;
		bool precisaDelay = false;

		foreach (Bot bot in bots) {
			// Para se já atingiu o número desejado de ENVIOS BEM SUCEDIDOS
			if (enviosBemSucedidos >= numEnviosDesejados) {
				break;
			}

			// Pula bots offline
			if (!bot.IsConnectedAndLoggedOn) {
				continue;
			}

			// Verifica ANTES se já enviou (sem fazer requisição HTTP)
			bool? jaEnviou = await DbHelper.VerificarEnvioItem(bot.BotName, "SHAREDFAV", sharedfileId).ConfigureAwait(false);

			// Se já enviou, simplesmente pula para o próximo bot
			if (jaEnviou == true) {
				continue;
			}

			// Aplica delay APENAS se a requisição anterior foi uma requisição HTTP real
			if (precisaDelay) {
				await Task.Delay(IntervaloEntreRequisicoes).ConfigureAwait(false);
			}

			// Aplica rate limiting para requisições reais
			await RateLimitSemaphore.WaitAsync().ConfigureAwait(false);
			try {
				ResultadoEnvio resultado = await EnviarFavoritoInterno(
					bot,
					Commands.GetProxyAccess(bot, access, steamID),
					sharedfileId,
					appId
				).ConfigureAwait(false);

				// Bot ficou offline durante o processamento
				if (resultado.BotOffline) {
					continue;
				}

				
				resultados.Add(resultado);

				if (resultado.Sucesso) {
					enviosBemSucedidos++;
				} else {
					enviosFalhados++;
				}

				// Marca que a próxima iteração precisa de delay
				precisaDelay = true;

			} finally {
				RateLimitSemaphore.Release();
			}
		}

		// Monta a resposta final
		return MontarRespostaFinal(resultados, enviosBemSucedidos, enviosFalhados, numEnviosDesejados);
	}

	/// <summary>
	/// Monta a resposta final formatada com todos os resultados
	/// </summary>
	private static string MontarRespostaFinal(
		List<ResultadoEnvio> resultados,
		int sucedidos,
		int falhados,
		int desejados) {

		List<string> linhas = [];

		// Adiciona cada resultado
		foreach (ResultadoEnvio resultado in resultados) {
			string status = resultado.Sucesso ? "+" : "-";
			string detalhes = resultado.Sucesso ? "FAV" : resultado.Mensagem;
			linhas.Add($"<{resultado.BotName}> [{status}] {detalhes}");
		}

		// Adiciona resumo final
		linhas.Add("---------------------------------");
		linhas.Add($"Enviados: {sucedidos}/{desejados} | Falhas: {falhados}");

		// Aviso se não conseguiu atingir o número desejado
		if (sucedidos < desejados) {
			int faltando = desejados - sucedidos;
			linhas.Add($"Aviso: Faltaram {faltando} (já enviados ou indisponíveis)");
		}

		ASF.ArchiLogger.LogGenericInfo($"Socialboost|FAV => Concluído! Sucesso: {sucedidos}, Falhas: {falhados}");

		return string.Join(Environment.NewLine, linhas);
	}

	private static string FormatarResposta(string mensagem) => $"<Socialboost> {mensagem}";
}