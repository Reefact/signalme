_[English version](https://github.com/Reefact/signalme/blob/main/README.md)_

# SignalMe

[![CI](https://github.com/Reefact/signalme/actions/workflows/ci.yml/badge.svg)](https://github.com/Reefact/signalme/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/SignalMe.svg)](https://www.nuget.org/packages/SignalMe)
[![Licence](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](https://github.com/Reefact/signalme/blob/main/LICENSE)

SignalMe est un petit runtime de présence pour les périphériques Luxafor. Il tourne dans votre terminal,
affiche votre disponibilité sur le périphérique, joue des signaux lumineux expressifs et passe de lui-même
en `away` pendant que votre session Windows est verrouillée — sans interface graphique, sans icône dans la
zone de notification.

![Le statut busy, le signal happy, et le retour automatique à busy](https://raw.githubusercontent.com/Reefact/signalme/main/assets/demo.gif)

<sub>Rendu à partir de la séquence de LED que le code produit réellement, ralenti pour être lisible. Ce
n'est pas une captation d'un périphérique.</sub>

```text
$ signalme
SignalMe 2.0.0
Luxafor device detected.
Mode: manual
Status: off
Commands: help
Press Ctrl+C to stop.

> busy
Status: busy

> happy
Playing: happy
Restored: busy

> 
Windows session locked.
Effective status: away
> 
Windows session unlocked.
Effective status: busy
> ^C
Stopping SignalMe...
SignalMe stopped.
```

## Pourquoi SignalMe ?

- **Sans interface graphique.** Un processus dans un terminal, piloté par de courtes commandes tapées à
  son invite, arrêté par Ctrl+C.
- **Attentif à votre présence.** Verrouillez votre session Windows et le périphérique passe au violet ;
  déverrouillez-la et votre statut revient, intact. Vous n'avez plus jamais à passer en `away` vous-même.
- **Expressif.** Des statuts de disponibilité durables, plus des signaux lumineux temporaires qui
  reviennent d'eux-mêmes à votre statut ; `ready` se termine volontairement sur `available`.
- **Honnête.** Un seul composant pilote le périphérique, chaque commande envoyée est vérifiée, et
  SignalMe n'annonce jamais un statut que les LED n'ont pas affiché. Le périphérique est éteint quand
  SignalMe s'arrête.

## Installation

SignalMe cible .NET 10 et fonctionne sous Windows. L'installer comme outil .NET global nécessite le
[SDK .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0) : `dotnet tool install` est fourni avec le
SDK, pas avec le runtime seul.

```shell
dotnet tool install --global SignalMe --prerelease
```

SignalMe 2.0 est en préversion : sans `--prerelease`, `dotnet tool install` choisit la dernière version
stable, qui est encore l'outil en ligne de commande 1.x. Utilisez ensuite `signalme` depuis n'importe quel
shell. `dotnet tool update --global SignalMe --prerelease` pour mettre à jour,
`dotnet tool uninstall --global SignalMe` pour désinstaller.

## Démarrage rapide

Lancez SignalMe et laissez-le tourner :

```shell
signalme                   # équivaut à : signalme --mode manual
```

Il trouve votre Luxafor, affiche le statut mémorisé lors de la dernière exécution (ou reste éteint), et
attend à une invite `> `. Tapez un statut ou un signal, `status` pour savoir où en sont les choses, `off`
pour éteindre le périphérique, `help` pour la liste. Ctrl+C éteint le périphérique et quitte.

**Un statut durable reste affiché jusqu'à ce que vous en changiez. Un signal temporaire joue une animation
puis remet votre statut durable en place.** `ready` fait exception : il se termine sur `available`.

### Statuts durables

| Commande | Alias | Couleur |
| --- | --- | --- |
| `available` | `free` | vert |
| `busy` | | jaune |
| `do-not-disturb` | `dnd` | rouge |

`away` (violet) ne se tape plus : SignalMe l'affiche de lui-même pendant que votre session Windows est
verrouillée, et revient à votre statut quand vous la déverrouillez.

### Signaux temporaires

| Commande | Intention |
| --- | --- |
| `happy` | arc-en-ciel festif |
| `bored` | dérive lente dans les violets |
| `desperate` | S-O-S |
| `warning` | alternance façon gyrophare |
| `alerting` | alerte rouge rapide |
| `ready` | annonce la disponibilité, et se termine sur `available` |

Un signal se joue par-dessus votre statut durable, il lui en faut donc un : SignalMe éteint, ou session
verrouillée, un signal est refusé avec un message. Un verrouillage pendant un signal l'arrête net.

SignalMe mémorise votre statut durable dans vos données d'application locales et en repart la fois
suivante. Il ne signale jamais un succès pour une commande que le périphérique a refusée, et chaque échec
a son propre [code de sortie](https://github.com/Reefact/signalme/blob/main/docs/commands-FR.md#codes-de-sortie).

## Documentation

- [Référence des commandes](https://github.com/Reefact/signalme/blob/main/docs/commands-FR.md) — la ligne de commande, chaque commande interactive, et les codes de sortie
- [Statuts et signaux](https://github.com/Reefact/signalme/blob/main/docs/signals-FR.md) — ce que fait chaque signal, et ce qui se passe quand il est interrompu
- [Matériel et plateformes](https://github.com/Reefact/signalme/blob/main/docs/hardware-FR.md) — périphériques testés, Windows uniquement, plusieurs périphériques, surveillance de la session
- [Dépannage](https://github.com/Reefact/signalme/blob/main/docs/troubleshooting-FR.md) — ce que signifie chaque message et quoi faire
- [Développement et releases](https://github.com/Reefact/signalme/blob/main/docs/development-FR.md) — build, tests, CI, publication

## Licence

[Apache-2.0](https://github.com/Reefact/signalme/blob/main/LICENSE)
