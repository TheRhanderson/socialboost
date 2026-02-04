![SOCIALBOOST](https://i.postimg.cc/FFjbTcgP/302407373-2623fdd1-80b3-4f50-8de8-5355094fb972.png)

<div align="center">

# SocialBoost - Potencializando Interações no ArchiSteamFarm

[![Total downloads](https://img.shields.io/github/downloads/TheRhanderson/socialboost/total.svg?label=Downloads&logo=github&cacheSeconds=600)](https://github.com/TheRhanderson/socialboost/releases)
[![Socialboost Release](https://img.shields.io/github/v/release/TheRhanderson/socialboost.svg?label=Stable&logo=github&cacheSeconds=600)](https://github.com/TheRhanderson/socialboost/releases/latest)

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

> 🛡️ Este recurso foi removido deste repositório porque não está alinhado com o propósito do plugin.

---

## 📊 Gerenciamento Automático e Inteligente

O SocialBoost gerencia suas contas de forma inteligente através de dois sistemas complementares:

### 🗄️ Banco de Dados de Rastreamento
- **Localização:** `/plugins/SocialBoost/`
- Registra todos os envios realizados por cada conta
- Evita duplicação de envios para o mesmo item
- Otimiza requisições HTTP pulando contas já utilizadas

### 🚫 Blacklist Automática por AppID
- **Localização:** `/plugins/SocialBoost/blacklist-{appid}.txt`
- Detecta automaticamente contas com VAC ban
- Bloqueia contas problemáticas por jogo específico
- Previne desperdício de requisições em contas inelegíveis
- **Totalmente automático** - sem configuração necessária

> 💡 Quando uma conta retorna erro de VAC ban (código 17), ela é automaticamente adicionada à blacklist do jogo correspondente, garantindo eficiência máxima nos próximos comandos.

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

> 🛡️ Este recurso ainda não foi importado da versão antiga do SocialBoost.

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
