![SOCIALBOOST](https://github.com/TheRhanderson/socialboost-asf/assets/24517851/2623fdd1-80b3-4f50-8de8-5355094fb972)

<div align="center">

# SocialBoost - Potencializando Interações no ArchiSteamFarm

![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/TheRhanderson/socialboost/total)
[![GitHub Release](https://img.shields.io/github/v/release/TheRhanderson/socialboost?logo=github)](https://github.com/TheRhanderson/socialboost/releases)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**SocialBoost** é um plugin complementar para ArchiSteamFarm, projetado para melhorar as interações na plataforma Steam. Este plugin oferece recursos para potencializar o número de curtidas e favoritos em imagens, guias e vários tipos de conteúdo. Também permite avaliações de jogos do usuário (Útil/Engraçado) e permite seguir Workshop de jogadores, com mais recursos em breve.

[Recursos](#-recursos) • [Instalação](#-como-instalar) • [Gerenciamento Automático](#-gerenciamento-automático) • [Privacidade](#-privacidade-e-transparência)

</div>

---

## 🚀 Recursos

### 📁 Sharedfiles

Potencialize curtidas e favoritos em itens do Workshop da Steam, capturas de tela, guias e outros conteúdos compartilhados.

| Comando | Descrição |
|---------|-------------|
| `CSHAREDLIKE [Id] [Quantidade]` | Envia curtidas para um sharedfile |
| `CSHAREDFAV [Id] [Quantidade]` | Envia favoritos para um sharedfile |
| `CSHAREDFILES [Id] [Quantidade]` | Envia **tanto** curtidas quanto favoritos para um sharedfile |

> 💡 **Dica:** O `Id` é o número no final da URL do sharedfile.

**Exemplo:**
```
CSHAREDFILES 3142209500 10
```
Isso envia 10 curtidas e 10 favoritos para o sharedfile com ID `3142209500`.

---

### ⭐ Análise de Jogo feito por usuários

Potencialize as análises de jogos feitas por alguém

| Comando | Descrição |
|---------|-------------|
| `CRATEREVIEW [URL da Avaliação] [Tipo] [Quantidade]` | Envia uma recomendação para uma avaliação de jogo |

**Tipos disponíveis:**

| Tipo | Ação |
|:----:|--------|
| `1` | 👍 Útil |
| `2` | 😂 Engraçado |
| `3` | 👎 Não Útil |

**Exemplo:**
```
CRATEREVIEW https://steamcommunity.com/id/username/recommended/730 1 10
```
Isso marca a avaliação como **Útil** usando 10 contas bot.

---

### 🔧 Steam Workshop

Siga ou deixe de seguir o Workshop de um perfil da Steam.

| Comando | Descrição |
|---------|-------------|
| `CWORKSHOP [URL do Perfil] [Tipo] [Quantidade]` | Siga/deixe de seguir o Workshop de um perfil |

**Tipos disponíveis:**

| Tipo | Ação |
|:----:|--------|
| `1` | ➕ Seguir |
| `2` | ➖ Deixar de Seguir |

> ✅ **Observação:** Contas limitadas são compatíveis com este recurso.

**Exemplo:**
```
CWORKSHOP https://steamcommunity.com/id/username 1 15
```
Isso segue o Workshop do perfil usando 15 contas bot.

---

### 🚨 Denunciar Abuso

Denuncie perfis da Steam por violações diversas.

| Comando | Descrição |
|---------|-------------|
| `CREPORTABUSE [Tipo] [URL do Perfil] [Motivo] [Quantidade]` | Envia denúncias de abuso para um perfil da Steam |

**Tipos disponíveis:**

| Tipo | Violação |
|:----:|-----------|
| `3` | 🎭 Tentativa de fraude |
| `14` | 🔓 Conta comprometida |
| `18` | 📦 Roubo de item |
| `20` | 🖼️ Avatar inadequado |
| `21` | ✏️ Nome de perfil inadequado |

> ⚠️ **Importante:** Use `+` em vez de espaços no campo de motivo.

**Exemplo:**
```
CREPORTABUSE 14 https://steamcommunity.com/profiles/76561198000000000 Conta+foi+comprometida 5
```
Isso envia 5 denúncias de abuso com o motivo "Conta foi comprometida".

> 🛡️ Este recurso foi removido deste repositório porque não está alinhado com o proósito do plugin.

---

## 📊 Gerenciamento Automático

O SocialBoost inclui um sistema inteligente de gerenciamento de contas através de um banco de dados local localizado na pasta `/plugins`.

**Recursos:**
- 🗄️ Rastreia contas usadas para envios específicos
- 🔄 Evita reutilização de contas para o mesmo envio
- 📈 Verifica bots disponíveis antes de enviar

### Verificar Bots Disponíveis

Use `CHECKBOOST` para ver quantos bots ainda podem enviar para um alvo específico:

```
CHECKBOOST [Tipo] [Id]
```

**Tipos suportados:**

| Tipo | Entrada Esperada |
|------|----------------|
| `sharedlike` | ID do sharedfile (da URL) |
| `sharedfav` | ID do sharedfile (da URL) |
| `workshop` | URL do Perfil da Steam |
| `reviews` | URL da Avaliação |

**Exemplo:**
```
CHECKBOOST sharedlike 3142209500
```

---

## 📥 Como Instalar

1. Certifique-se de estar usando a **versão genérica do ASF 6.3.1.6 (recomendado)**
2. Visite a página [**Releases**](https://github.com/TheRhanderson/socialboost/releases)
3. Baixe a versão mais recente disponível
4. Extraia o conteúdo para a pasta `/plugins` da sua instalação do ASF
5. Reinicie o ASF
6. **Aproveite!** 🎉

---

## 🔒 Privacidade e Transparência

**O SocialBoost não coleta nenhum dado do usuário.** Sua privacidade é totalmente respeitada.

- ❌ Sem nomes de conta
- ❌ Sem endereços IP
- ❌ Sem dados pessoais
- ❌ Sem rastreamento de uso
- ❌ Sem conexões externas

> 🛡️ Tudo funciona localmente na sua máquina. O plugin opera inteiramente usando apenas chamadas oficiais do Steam API.

---

<div align="center">

**Feito com ❤️ por [@TheRhanderson](https://github.com/TheRhanderson)**

⭐ Dê uma estrela neste repositório se achar útil!

</div>
