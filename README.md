![SOCIALBOOST](https://github.com/TheRhanderson/socialboost-asf/assets/24517851/2623fdd1-80b3-4f50-8de8-5355094fb972)

<div align="center">

# SocialBoost - Boosting Interactions on ArchiSteamFarm

![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/TheRhanderson/socialboost/total)
[![GitHub Release](https://img.shields.io/github/v/release/TheRhanderson/socialboost?logo=github)](https://github.com/TheRhanderson/socialboost/releases)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**SocialBoost** is a complementary plugin for ArchiSteamFarm, designed to enhance interactions on the Steam platform. This plugin provides features to boost the number of likes and favorites on images, guides, and various content types. It also enables user game reviews (Helpful/Funny) and allows following players' Workshop, with more features to be added soon.

[Features](#-features) • [Installation](#-how-to-install) • [Auto Management](#-auto-management) • [Privacy](#-privacy-and-transparency)

</div>

---

## 🚀 Features

### 📁 Sharedfiles

Boost likes and favorites on Steam Workshop items, screenshots, guides, and other shared content.

| Command | Description |
|---------|-------------|
| `CSHAREDLIKE [Id] [Amount]` | Sends likes to a sharedfile |
| `CSHAREDFAV [Id] [Amount]` | Sends favorites to a sharedfile |
| `CSHAREDFILES [Id] [Amount]` | Sends **both** likes and favorites to a sharedfile |

> 💡 **Tip:** The `Id` is the number at the end of the sharedfile URL.

**Example:**
```
CSHAREDFILES 3142209500 10
```
This sends 10 likes and 10 favorites to the sharedfile with ID `3142209500`.

---

### ⭐ Game Reviews

Boost helpfulness ratings on Steam game reviews.

| Command | Description |
|---------|-------------|
| `CRATEREVIEW [Review Url] [Type] [Amount]` | Sends a recommendation for a game review |

**Types available:**

| Type | Action |
|:----:|--------|
| `1` | 👍 Helpful |
| `2` | 😂 Funny |
| `3` | 👎 Not Helpful |

**Example:**
```
CRATEREVIEW https://steamcommunity.com/id/username/recommended/730 1 10
```
This marks the review as **Helpful** using 10 bot accounts.

---

### 🔧 Steam Workshop

Follow or unfollow a Steam profile's Workshop.

| Command | Description |
|---------|-------------|
| `CWORKSHOP [Profile Url] [Type] [Amount]` | Follow/unfollow a profile's Workshop |

**Types available:**

| Type | Action |
|:----:|--------|
| `1` | ➕ Follow |
| `2` | ➖ Unfollow |

> ✅ **Note:** Limited accounts are compatible with this feature.

**Example:**
```
CWORKSHOP https://steamcommunity.com/id/username 1 15
```
This follows the profile's Workshop using 15 bot accounts.

---

### 🚨 Report Abuse

Report Steam profiles for various violations.

| Command | Description |
|---------|-------------|
| `CREPORTABUSE [Type] [Profile Url] [Reason] [Amount]` | Sends abuse reports to a Steam profile |

**Types available:**

| Type | Violation |
|:----:|-----------|
| `3` | 🎭 Fraud attempt |
| `14` | 🔓 Compromised account |
| `18` | 📦 Item theft |
| `20` | 🖼️ Inappropriate avatar |
| `21` | ✏️ Inappropriate profile name |

> ⚠️ **Important:** Use `+` instead of spaces in the reason field.

**Example:**
```
CREPORTABUSE 14 https://steamcommunity.com/profiles/76561198000000000 Account+was+compromised 5
```
This sends 5 abuse reports with the reason "Account was compromised".

> 🛡️ This feature was removed from this repository.

---

## 📊 Auto Management

SocialBoost includes an intelligent account management system through a local database located in the `/plugins` folder.

**Features:**
- 🗄️ Tracks accounts used for specific submissions
- 🔄 Prevents reuse of accounts for the same submission
- 📈 Check available bots before sending

### Check Available Bots

Use `CHECKBOOST` to see how many bots can still submit for a given target:

```
CHECKBOOST [Type] [Id]
```

**Supported types:**

| Type | Expected Input |
|------|----------------|
| `sharedlike` | Sharedfile ID (from URL) |
| `sharedfav` | Sharedfile ID (from URL) |
| `workshop` | Steam Profile URL |
| `reviews` | Review URL |

**Example:**
```
CHECKBOOST sharedlike 3142209500
```

---

## 📥 How to Install

1. Make sure you are using the **generic version of ASF 6.3.1.6 (recommended)**
2. Visit the [**Releases**](https://github.com/TheRhanderson/socialboost/releases) page
3. Download the latest available version
4. Extract the contents into the `/plugins` folder of your ASF installation
5. Restart ASF
6. **Enjoy!** 🎉

---

## 🔒 Privacy and Transparency

**SocialBoost does not collect any user data.** Your privacy is fully respected.

- ❌ No account names
- ❌ No IP addresses
- ❌ No personal data
- ❌ No usage tracking
- ❌ No external connections

> 🛡️ Everything runs locally on your machine. The plugin operates entirely offline with no telemetry or analytics of any kind.

---

<div align="center">

**Made with ❤️ by [@TheRhanderson](https://github.com/TheRhanderson)**

⭐ Star this repository if you find it useful!

</div>
