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

internal static class CWorkshop {

	// Intervalo entre requisições (1 segundo para workshop)
	private static readonly TimeSpan IntervaloEntreRequisicoes = TimeSpan.FromSeconds(1);

	// Semáforo para controlar o rate limiting global
	private static readonly SemaphoreSlim RateLimitSemaphore = new(1, 1);

	/// <summary>
	/// Tipos de ação no workshop
	/// </summary>
	internal enum ETipoAcao {
		Follow = 1,
		Unfollow = 2
	}

	/// <summary>
	/// Resultado do envio para um bot específico
	/// </summary>
	internal sealed class ResultadoEnvio {
		public required string BotName { get; init; }
		public required bool Sucesso { get; init; }
		public required string Mensagem { get; init; }
		public bool JaEnviado { get; init; }
		public bool BotOffline { get; init; }
	}

	/// <summary>
	/// Obtém o nome amigável do tipo de ação
	/// </summary>
	private static string ObterNomeTipo(ETipoAcao tipo) => tipo switch {
		ETipoAcao.Follow => "FOLLOW",
		ETipoAcao.Unfollow => "UNFOLLOW",
		_ => "DESCONHECIDO"
	};

	/// <summary>
	/// Executa follow/unfollow no workshop de um usuário
	/// </summary>
	internal static async Task<ResultadoEnvio> ExecutarAcaoInterno(Bot bot, EAccess access, string urlPerfil, string steamIdAlvo, ETipoAcao tipo) {
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

		string nomeTipo = ObterNomeTipo(tipo);
		bot.ArchiLogger.LogGenericInfo($"Socialboost|WORKSHOP|{nomeTipo} => {steamIdAlvo} (Enviando...)");

		// Define URLs baseado na ação
		Uri requestUrl = tipo == ETipoAcao.Follow
			? new($"{urlPerfil}/followuser")
			: new($"{urlPerfil}/unfollowuser");

		Uri requestViewPage = new($"{urlPerfil}/myworkshopfiles/");

		Dictionary<string, string> data = new(1) {
			{ "steamid", steamIdAlvo }
		};

		// Envia a requisição POST
		bool postSuccess = await bot.ArchiWebHandler.UrlPostWithSession(
			requestUrl,
			data: data,
			session: ESession.Lowercase,
			referer: requestViewPage
		).ConfigureAwait(false);

		if (postSuccess) {
			// Para Unfollow, removemos o registro anterior
			// Para Follow, adicionamos o registro
			if (tipo == ETipoAcao.Unfollow) {
				await DbHelper.RemoverItem(bot.BotName, "WORKSHOP", steamIdAlvo).ConfigureAwait(false);
			} else {
				bool? salvou = await DbHelper.AdicionarEnvioItem(bot.BotName, "WORKSHOP", steamIdAlvo).ConfigureAwait(false);
				if (salvou != true) {
					bot.ArchiLogger.LogGenericWarning($"Socialboost|WORKSHOP => {steamIdAlvo} (Enviado, mas falha ao salvar no DB)");
				}
			}

			bot.ArchiLogger.LogGenericInfo($"Socialboost|WORKSHOP|{nomeTipo} => {steamIdAlvo} (OK)");
			return new ResultadoEnvio {
				BotName = bot.BotName,
				Sucesso = true,
				Mensagem = "OK"
			};
		}

		bot.ArchiLogger.LogGenericError($"Socialboost|WORKSHOP|{nomeTipo} => {steamIdAlvo} (FALHA: Erro HTTP)");
		return new ResultadoEnvio {
			BotName = bot.BotName,
			Sucesso = false,
			Mensagem = "Erro HTTP"
		};
	}

	/// <summary>
	/// Comando principal que processa múltiplos bots com rate limiting
	/// </summary>
	public static async Task<string?> ExecutarComando(EAccess access, ulong steamID, string urlPerfil, string tipoAcao, string quantidadeDesejada) {
		// Validação dos parâmetros
		if (string.IsNullOrEmpty(urlPerfil) || string.IsNullOrEmpty(tipoAcao) || string.IsNullOrEmpty(quantidadeDesejada)) {
			ASF.ArchiLogger.LogNullError(null, nameof(urlPerfil) + " || " + nameof(tipoAcao) + " || " + nameof(quantidadeDesejada));
			return null;
		}

		// Valida tipo de ação (1 = follow, 2 = unfollow)
		if (!int.TryParse(tipoAcao, out int tipoInt) || tipoInt < 1 || tipoInt > 2) {
			return access >= EAccess.Owner ? FormatarResposta("Tipo inválido. Use: 1 (follow), 2 (unfollow)") : null;
		}

		ETipoAcao tipo = (ETipoAcao) tipoInt;

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

		// Obtém o SteamID64 do perfil alvo
		string? steamIdAlvo = await SessionHelper.FetchSteamID64(urlPerfil).ConfigureAwait(false);

		if (string.IsNullOrEmpty(steamIdAlvo)) {
			return access >= EAccess.Owner
				? FormatarResposta("Erro ao obter SteamID64 do perfil")
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
			bool? jaEnviou = await DbHelper.VerificarEnvioItem(bot.BotName, "WORKSHOP", steamIdAlvo).ConfigureAwait(false);

			// Lógica especial para Unfollow:
			// - Se já seguiu antes (jaEnviou == true), permite enviar "unfollow"
			// - Se não seguiu antes (jaEnviou == false), não faz sentido enviar "unfollow"
			if (tipo == ETipoAcao.Unfollow) {
				if (jaEnviou != true) {
					// Não seguiu antes, não pode dar unfollow - apenas pula
					continue;
				}
			} else {
				// Para Follow, se já enviou, pula para o próximo bot
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
				ResultadoEnvio resultado = await ExecutarAcaoInterno(
					bot,
					Commands.GetProxyAccess(bot, access, steamID),
					urlPerfil,
					steamIdAlvo,
					tipo
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
		ETipoAcao tipo) {

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
			string motivo = tipo == ETipoAcao.Unfollow
				? "bots não seguiam antes"
				: "bots já seguiam ou indisponíveis";
			linhas.Add($"Aviso: Faltaram {faltando} ({motivo})");
		}

		ASF.ArchiLogger.LogGenericInfo($"Socialboost|WORKSHOP|{nomeTipo} => Concluído! Sucesso: {sucedidos}, Falhas: {falhados}");

		return string.Join(Environment.NewLine, linhas);
	}

	private static string FormatarResposta(string mensagem) => $"<Socialboost> {mensagem}";
}
