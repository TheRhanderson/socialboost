using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;

namespace Socialboost.Helpers;

internal static class BlacklistHelper {
	// Usa o diretório de configuração do ASF + plugins/SocialBoost
	private static readonly string BlacklistDirectory = Path.Combine(
		Directory.GetParent(ArchiSteamFarm.SharedInfo.ConfigDirectory)?.FullName ?? Directory.GetCurrentDirectory(),
		"plugins",
		"SocialBoost"
	);

	private static readonly SemaphoreSlim FileLock = new(1, 1);

	/// <summary>
	/// Adiciona um bot à blacklist de um AppID específico
	/// </summary>
	internal static async Task<bool> AdicionarBotNaBlacklist(string botName, string appId) {
		if (string.IsNullOrEmpty(botName) || string.IsNullOrEmpty(appId)) {
			ASF.ArchiLogger.LogNullError(nameof(botName) + " || " + nameof(appId));
			return false;
		}

		await FileLock.WaitAsync().ConfigureAwait(false);
		try {
			// Garante que o diretório existe (cria se necessário)
			Directory.CreateDirectory(BlacklistDirectory);

			ASF.ArchiLogger.LogGenericDebug($"Socialboost|BLACKLIST => Diretório: {BlacklistDirectory}");

			string blacklistPath = Path.Combine(BlacklistDirectory, $"blacklist-{appId}.txt");

			// Lê a blacklist atual (se existir)
			HashSet<string> botsBlacklisted = new(StringComparer.OrdinalIgnoreCase);
			if (File.Exists(blacklistPath)) {
				string[] lines = await File.ReadAllLinesAsync(blacklistPath).ConfigureAwait(false);
				foreach (string line in lines) {
					string trimmed = line.Trim();
					if (!string.IsNullOrEmpty(trimmed)) {
						botsBlacklisted.Add(trimmed);
					}
				}
			}

			// Se o bot já está na blacklist, não faz nada
			if (botsBlacklisted.Contains(botName)) {
				ASF.ArchiLogger.LogGenericWarning($"Socialboost|BLACKLIST => {botName} já está na blacklist do AppID {appId}");
				return true;
			}

			// Adiciona o bot
			botsBlacklisted.Add(botName);

			// Salva de volta no arquivo
			await File.WriteAllLinesAsync(blacklistPath, botsBlacklisted.OrderBy(b => b)).ConfigureAwait(false);

			ASF.ArchiLogger.LogGenericInfo($"Socialboost|BLACKLIST => {botName} adicionado à blacklist do AppID {appId}");
			return true;

		} catch (Exception ex) {
			ASF.ArchiLogger.LogGenericException(ex);
			return false;
		} finally {
			FileLock.Release();
		}
	}

	/// <summary>
	/// Verifica se um bot está na blacklist de um AppID específico
	/// </summary>
	internal static async Task<bool> BotEstaNaBlacklist(string botName, string appId) {
		if (string.IsNullOrEmpty(botName) || string.IsNullOrEmpty(appId)) {
			return false;
		}

		await FileLock.WaitAsync().ConfigureAwait(false);
		try {
			string blacklistPath = Path.Combine(BlacklistDirectory, $"blacklist-{appId}.txt");

			if (!File.Exists(blacklistPath)) {
				return false;
			}

			string[] lines = await File.ReadAllLinesAsync(blacklistPath).ConfigureAwait(false);
			foreach (string line in lines) {
				string trimmed = line.Trim();
				if (trimmed.Equals(botName, StringComparison.OrdinalIgnoreCase)) {
					return true;
				}
			}

			return false;

		} catch (Exception ex) {
			ASF.ArchiLogger.LogGenericException(ex);
			return false;
		} finally {
			FileLock.Release();
		}
	}

	/// <summary>
	/// Obtém todos os bots que estão na blacklist de um AppID específico
	/// </summary>
	internal static async Task<HashSet<string>> ObterBotsNaBlacklist(string appId) {
		if (string.IsNullOrEmpty(appId)) {
			return [];
		}

		await FileLock.WaitAsync().ConfigureAwait(false);
		try {
			string blacklistPath = Path.Combine(BlacklistDirectory, $"blacklist-{appId}.txt");

			if (!File.Exists(blacklistPath)) {
				return [];
			}

			string[] lines = await File.ReadAllLinesAsync(blacklistPath).ConfigureAwait(false);
			HashSet<string> bots = new(StringComparer.OrdinalIgnoreCase);

			foreach (string line in lines) {
				string trimmed = line.Trim();
				if (!string.IsNullOrEmpty(trimmed)) {
					bots.Add(trimmed);
				}
			}

			return bots;

		} catch (Exception ex) {
			ASF.ArchiLogger.LogGenericException(ex);
			return [];
		} finally {
			FileLock.Release();
		}
	}

	/// <summary>
	/// Remove um bot da blacklist de um AppID específico
	/// </summary>
	internal static async Task<bool> RemoverBotDaBlacklist(string botName, string appId) {
		if (string.IsNullOrEmpty(botName) || string.IsNullOrEmpty(appId)) {
			ASF.ArchiLogger.LogNullError(nameof(botName) + " || " + nameof(appId));
			return false;
		}

		await FileLock.WaitAsync().ConfigureAwait(false);
		try {
			string blacklistPath = Path.Combine(BlacklistDirectory, $"blacklist-{appId}.txt");

			if (!File.Exists(blacklistPath)) {
				ASF.ArchiLogger.LogGenericWarning($"Socialboost|BLACKLIST => Arquivo de blacklist do AppID {appId} não existe");
				return false;
			}

			// Lê a blacklist atual
			HashSet<string> botsBlacklisted = new(StringComparer.OrdinalIgnoreCase);
			string[] lines = await File.ReadAllLinesAsync(blacklistPath).ConfigureAwait(false);
			foreach (string line in lines) {
				string trimmed = line.Trim();
				if (!string.IsNullOrEmpty(trimmed)) {
					botsBlacklisted.Add(trimmed);
				}
			}

			// Remove o bot
			bool removed = botsBlacklisted.Remove(botName);

			if (!removed) {
				ASF.ArchiLogger.LogGenericWarning($"Socialboost|BLACKLIST => {botName} não estava na blacklist do AppID {appId}");
				return false;
			}

			// Salva de volta no arquivo
			if (botsBlacklisted.Count == 0) {
				// Se ficou vazio, deleta o arquivo
				File.Delete(blacklistPath);
				ASF.ArchiLogger.LogGenericInfo($"Socialboost|BLACKLIST => Blacklist do AppID {appId} vazia, arquivo removido");
			} else {
				await File.WriteAllLinesAsync(blacklistPath, botsBlacklisted.OrderBy(b => b)).ConfigureAwait(false);
			}

			ASF.ArchiLogger.LogGenericInfo($"Socialboost|BLACKLIST => {botName} removido da blacklist do AppID {appId}");
			return true;

		} catch (Exception ex) {
			ASF.ArchiLogger.LogGenericException(ex);
			return false;
		} finally {
			FileLock.Release();
		}
	}
}
