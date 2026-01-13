using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Socialboost.Helpers;

internal static class DbHelper {

	// Crie uma instância reutilizável de JsonSerializerOptions
	public static JsonSerializerOptions JsonSerializerOptions = new() {
		WriteIndented = true,
		TypeInfoResolver = default
	};


	public static async Task<bool> VerificarEnvioItem(string botName, string boostType, string idToCheck) {
		try {
			// Defina o caminho da pasta baseada no boostType
			string directoryPath = Path.Combine("plugins/SocialBoost", boostType.ToUpperInvariant());

			// Verifique se a pasta existe, caso contrário, crie-a
			if (!Directory.Exists(directoryPath)) {
				_ = Directory.CreateDirectory(directoryPath);
			}

			// Defina o caminho do arquivo baseado no idToCheck
			string filePath = Path.Combine(directoryPath, $"{idToCheck}.txt");

			// Verifique se o arquivo existe
			if (!File.Exists(filePath)) {
				// Tente criar um arquivo vazio
				try {
					using FileStream fs = File.Create(filePath);
				} catch (Exception ex) {
					Console.WriteLine($"Erro ao criar o arquivo: {ex.Message}");
					return false;
				}
			}

			string[] lines = await File.ReadAllLinesAsync(filePath).ConfigureAwait(false);

			foreach (string line in lines) {
				if (line == botName) {
					return true;
				}
			}

			return false;

		} catch (UnauthorizedAccessException uex) {
			Console.WriteLine($"Acesso não autorizado: {uex.Message}");
			return false;
		} catch (Exception ex) {
			Console.WriteLine($"Erro: {ex.Message}");
			return false;
		}
	}



	public static async Task<bool> AdicionarEnvioItem(string botName, string boostType, string idToCheck) {

		string directoryPath = Path.Combine("plugins/SocialBoost", boostType.ToUpperInvariant());

		if (!Directory.Exists(directoryPath)) {
			_ = Directory.CreateDirectory(directoryPath);
		}

		string filePath = Path.Combine(directoryPath, $"{idToCheck}.txt");

		// Abre o arquivo no modo de acréscimo e escreve a nova linha
		await File.AppendAllTextAsync(filePath, botName + Environment.NewLine).ConfigureAwait(false);

		return true; // Bot adicionado com sucesso, retorna true
	}



	public static async Task<bool> RemoverItem(string botName, string boostType, string idToRemove) {

		string directoryPath = Path.Combine("plugins/SocialBoost", boostType);

		if (!Directory.Exists(directoryPath)) {
			return false;
		}

		string filePath = Path.Combine(directoryPath, $"{idToRemove}.txt");

		if (!File.Exists(filePath)) {
			return false;
		}

		List<string> lines = new(await File.ReadAllLinesAsync(filePath).ConfigureAwait(false));

		if (lines.Remove(botName)) {
			using StreamWriter writer = new(filePath, false);
			foreach (string line in lines) {
				await writer.WriteLineAsync(line).ConfigureAwait(false);
			}
			return true;
		}

		return false;
	}

	// Agora pode ser passada como parâmetro normalmente:
	public static List<string> GetReviewList(string boostType, BotData botData) =>
		boostType.ToUpperInvariant() switch {
			"REVIEWS" => botData.Reviews,
			"SHAREDLIKE" => botData.SharedLike,
			"SHAREDFAV" => botData.SharedFav,
			"WORKSHOP" => botData.Workshop,
			"REPORTABUSE" => botData.ReportAbuse,
			_ => throw new ArgumentException("Tipo de revisão inválido"),
		};


	// Classe normal pública
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes")]

	public sealed class BotData {
		public List<string> Reviews { get; set; } = [];
		public List<string> SharedLike { get; set; } = [];
		public List<string> SharedFav { get; set; } = [];
		public List<string> Workshop { get; set; } = [];

		// NOVA: Lista para armazenar denúncias enviadas
		public List<string> ReportAbuse { get; set; } = [];
	}




}
