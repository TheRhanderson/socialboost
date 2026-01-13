using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Interaction;
using Socialboost.Helpers;
using static ArchiSteamFarm.Steam.Integration.ArchiWebHandler;

namespace Socialboost;

internal static class CReviews {

	// Intervalo entre requisições (3 segundos = 20 req/min, abaixo do limite de 25/min)
	private static readonly TimeSpan IntervaloEntreRequisicoes = TimeSpan.FromSeconds(3);

	// Semáforo para controlar o rate limiting global
	private static readonly SemaphoreSlim RateLimitSemaphore = new(1, 1);

	/// <summary>
	/// Tipos de avaliação de review
	/// </summary>
	internal enum TipoAvaliacao {
		Util = 1,
		Engracado = 2,
		NaoUtil = 3
	}

	/// <summary>
	/// Resultado do envio de avaliação para um bot específico
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
	/// Obtém o nome amigável do tipo de avaliação
	/// </summary>
	private static string ObterNomeTipo(TipoAvaliacao tipo) => tipo switch {
		TipoAvaliacao.Util => "ÚTIL",
		TipoAvaliacao.Engracado => "ENGRAÇADO",
		TipoAvaliacao.NaoUtil => "NÃO ÚTIL",
		_ => "DESCONHECIDO"
	};

	/// <summary>
	/// Envia avaliação de review de um bot específico
	/// </summary>
	internal static async Task<ResultadoEnvio> EnviarAvaliacaoInterno(Bot bot, EAccess access, string urlReview, string idReview, TipoAvaliacao tipo) {
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

		// Verifica se a conta é limitada
		if (bot.IsAccountLimited) {
			return new ResultadoEnvio {
				BotName = bot.BotName,
				Sucesso = false,
				Mensagem = "Conta limitada",
				ContaLimitada = true
			};
		}

		string nomeTipo = ObterNomeTipo(tipo);
		bot.ArchiLogger.LogGenericInfo($"Socialboost|REVIEWS|{nomeTipo} => {idReview} (Enviando...)");

		// Define URLs
		Uri requestViewPage = new(urlReview);
		Uri requestUrl;
		Dictionary<string, string> data;

		// Configura requisição baseado no tipo
		switch (tipo) {
			case TipoAvaliacao.Util:
				requestUrl = new(SteamCommunityURL, $"/userreviews/rate/{idReview}");
				data = new(1) { { "rateup", "true" } };
				break;

			case TipoAvaliacao.Engracado:
				requestUrl = new(SteamCommunityURL, $"/userreviews/votetag/{idReview}");
				data = new(2) { { "rateup", "true" }, { "tagid", "1" } };
				break;

			case TipoAvaliacao.NaoUtil:
				requestUrl = new(SteamCommunityURL, $"/userreviews/rate/{idReview}");
				data = new(1) { { "rateup", "false" } };
				break;

			default:
				return new ResultadoEnvio {
					BotName = bot.BotName,
					Sucesso = false,
					Mensagem = "Tipo inválido"
				};
		}

		// Envia a requisição POST
		bool postSuccess = await bot.ArchiWebHandler.UrlPostWithSession(
			requestUrl,
			data: data,
			session: ESession.Lowercase,
			referer: requestViewPage
		).ConfigureAwait(false);

		if (postSuccess) {
			// Para "Não Útil", removemos o registro anterior (desfaz voto útil)
			// Para outros tipos, adicionamos o registro
			if (tipo == TipoAvaliacao.NaoUtil) {
				await DbHelper.RemoverItem(bot.BotName, "REVIEWS", idReview).ConfigureAwait(false);
			} else {
				bool? salvou = await DbHelper.AdicionarEnvioItem(bot.BotName, "REVIEWS", idReview).ConfigureAwait(false);
				if (salvou != true) {
					bot.ArchiLogger.LogGenericWarning($"Socialboost|REVIEWS => {idReview} (Enviado, mas falha ao salvar no DB)");
				}
			}

			bot.ArchiLogger.LogGenericInfo($"Socialboost|REVIEWS|{nomeTipo} => {idReview} (OK)");
			return new ResultadoEnvio {
				BotName = bot.BotName,
				Sucesso = true,
				Mensagem = "OK"
			};
		}

		bot.ArchiLogger.LogGenericError($"Socialboost|REVIEWS|{nomeTipo} => {idReview} (FALHA: Erro HTTP)");
		return new ResultadoEnvio {
			BotName = bot.BotName,
			Sucesso = false,
			Mensagem = "Erro HTTP"
		};
	}

	/// <summary>
	/// Comando principal que processa múltiplos bots com rate limiting
	/// </summary>
	public static async Task<string?> ExecutarComando(EAccess access, ulong steamID, string urlReview, string tipoAcao, string quantidadeDesejada) {
		// Validação dos parâmetros
		if (string.IsNullOrEmpty(urlReview) || string.IsNullOrEmpty(tipoAcao) || string.IsNullOrEmpty(quantidadeDesejada)) {
			ASF.ArchiLogger.LogNullError(null, nameof(urlReview) + " || " + nameof(tipoAcao) + " || " + nameof(quantidadeDesejada));
			return null;
		}

		// Valida tipo de ação (1 = útil, 2 = engraçado, 3 = não útil)
		if (!int.TryParse(tipoAcao, out int tipoInt) || tipoInt < 1 || tipoInt > 3) {
			return access >= EAccess.Owner ? FormatarResposta("Tipo inválido. Use: 1 (útil), 2 (engraçado), 3 (não útil)") : null;
		}

		TipoAvaliacao tipo = (TipoAvaliacao)tipoInt;

		if (!int.TryParse(quantidadeDesejada, out int numEnviosDesejados) || numEnviosDesejados <= 0) {
			return access >= EAccess.Owner ? FormatarResposta("Quantidade inválida. Use um número positivo.") : null;
		}

		// Obtém todos os bots
		HashSet<Bot>? bots = Bot.GetBots("ASF");
		if (bots == null || bots.Count == 0) {
			return access >= EAccess.Owner ? FormatarResposta(Strings.BotNotFound) : null;
		}

		// Verifica quantos bots estão online e disponíveis (não limitados)
		int botsOnline = 0;
		foreach (Bot bot in bots) {
			if (bot.IsConnectedAndLoggedOn && !bot.IsAccountLimited) {
				botsOnline++;
			}
		}

		if (botsOnline < numEnviosDesejados) {
			return access >= EAccess.Owner
				? FormatarResposta($"Bots online disponíveis: {botsOnline}. Necessário: {numEnviosDesejados}")
				: null;
		}

		// Obtém o ID da review a partir da URL
		string? idReview = await SessionHelper.FetchReviewID(urlReview).ConfigureAwait(false);

		if (string.IsNullOrEmpty(idReview)) {
			return access >= EAccess.Owner
				? FormatarResposta("Erro ao obter ID da review a partir da URL")
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

			// Pula bots offline ou com conta limitada
			if (!bot.IsConnectedAndLoggedOn || bot.IsAccountLimited) {
				continue;
			}

			// Verifica ANTES se já enviou (sem fazer requisição HTTP)
			bool? jaEnviou = await DbHelper.VerificarEnvioItem(bot.BotName, "REVIEWS", idReview).ConfigureAwait(false);

			// Lógica especial para "Não Útil":
			// - Se já votou antes (jaEnviou == true), permite enviar "não útil" (desfaz o voto)
			// - Se não votou antes (jaEnviou == false), não faz sentido enviar "não útil"
			if (tipo == TipoAvaliacao.NaoUtil) {
				if (jaEnviou != true) {
					// Não votou antes, não pode desfazer voto - apenas pula para o próximo bot
					continue;
				}
			} else {
				// Para "Útil" e "Engraçado", se já enviou, pula para o próximo bot
				if (jaEnviou == true) {
					continue;
				}
			}

			// Aplica delay APENAS se a requisição anterior foi uma requisição HTTP real
			if (precisaDelay) {
				await Task.Delay(IntervaloEntreRequisicoes).ConfigureAwait(false);
			}

			// Aplica rate limiting para requisições reais
			await RateLimitSemaphore.WaitAsync().ConfigureAwait(false);
			try {
				ResultadoEnvio resultado = await EnviarAvaliacaoInterno(
					bot,
					Commands.GetProxyAccess(bot, access, steamID),
					urlReview,
					idReview,
					tipo
				).ConfigureAwait(false);

				// Bot ficou offline durante o processamento
				if (resultado.BotOffline || resultado.ContaLimitada) {
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
		return MontarRespostaFinal(resultados, enviosBemSucedidos, enviosFalhados, numEnviosDesejados, tipo);
	}

	/// <summary>
	/// Monta a resposta final formatada com todos os resultados
	/// </summary>
	private static string MontarRespostaFinal(
		List<ResultadoEnvio> resultados,
		int sucedidos,
		int falhados,
		int desejados,
		TipoAvaliacao tipo) {

		List<string> linhas = [];
		string nomeTipo = ObterNomeTipo(tipo);

		// Adiciona cada resultado
		foreach (ResultadoEnvio resultado in resultados) {
			string status = resultado.Sucesso ? "+" : "-";
			string detalhes = resultado.Sucesso ? nomeTipo : resultado.Mensagem;
			linhas.Add($"<{resultado.BotName}> [{status}] {detalhes}");
		}

		// Adiciona resumo final
		linhas.Add("---------------------------------");
		linhas.Add($"Enviados: {sucedidos}/{desejados} | Falhas: {falhados}");

		// Aviso se não conseguiu atingir o número desejado
		if (sucedidos < desejados) {
			int faltando = desejados - sucedidos;
			string motivo = tipo == TipoAvaliacao.NaoUtil 
				? "bots não votaram antes" 
				: "bots já votaram ou indisponíveis";
			linhas.Add($"Aviso: Faltaram {faltando} ({motivo})");
		}

		ASF.ArchiLogger.LogGenericInfo($"Socialboost|REVIEWS|{nomeTipo} => Concluído! Sucesso: {sucedidos}, Falhas: {falhados}");

		return string.Join(Environment.NewLine, linhas);
	}

	private static string FormatarResposta(string mensagem) => $"<Socialboost> {mensagem}";
}