_[English version](https://github.com/Reefact/signalme/blob/main/README.md)_

# SignalMe

[![CI](https://github.com/Reefact/signalme/actions/workflows/ci.yml/badge.svg)](https://github.com/Reefact/signalme/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/SignalMe.svg)](https://www.nuget.org/packages/SignalMe)
[![Licence](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](https://github.com/Reefact/signalme/blob/main/LICENSE)

SignalMe est un petit compagnon en ligne de commande pour les périphériques Luxafor. Affichez votre
disponibilité, déclenchez des signaux lumineux expressifs et automatisez votre statut de travail sans
interface graphique.

![Le statut busy, le signal happy, et le retour automatique à busy](https://raw.githubusercontent.com/Reefact/signalme/main/assets/demo.gif)

<sub>Rendu à partir de la séquence de LED que le code produit réellement, ralenti pour être lisible. Ce
n'est pas une captation d'un périphérique.</sub>

```shell
signalme as busy      # jaune fixe, et ça reste
signalme as happy     # signal arc-en-ciel, puis retour à busy tout seul
signalme status       # busy
signalme off          # extinction
```

## Pourquoi SignalMe ?

- **Sans interface graphique.** Une commande, depuis n'importe quel shell, script ou automatisation.
- **Expressif.** Des statuts de disponibilité durables, plus des signaux lumineux temporaires.
- **Restauration sûre.** Les signaux rétablissent votre statut durable même en cas d'échec ou
  d'interruption ; `ready` se termine volontairement sur `available`.

## Installation

SignalMe cible .NET 10 et fonctionne sous Windows. L'installer comme outil .NET global nécessite le
[SDK .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0) : `dotnet tool install` est fourni avec le
SDK, pas avec le runtime seul.

```shell
dotnet tool install --global SignalMe
```

Utilisez ensuite `signalme` depuis n'importe quel shell. `dotnet tool update --global SignalMe` pour mettre
à jour, `dotnet tool uninstall --global SignalMe` pour désinstaller.

## Démarrage rapide

**Un statut durable reste affiché jusqu'à ce que vous en changiez. Un signal temporaire joue une animation
puis rétablit automatiquement votre statut durable précédent.** `ready` fait exception : il se termine sur
`available`.

```shell
signalme as <status-or-signal>
signalme status                  # ce que SignalMe a défini en dernier
signalme off                     # alias : switch-off
```

### Statuts durables

| Statut | Alias | Couleur |
| --- | --- | --- |
| `available` | `free` | vert |
| `busy` | | jaune |
| `away` | | violet |
| `do-not-disturb` | `dnd` | rouge |

### Signaux temporaires

| Signal | Intention |
| --- | --- |
| `happy` | arc-en-ciel festif |
| `bored` | dérive lente dans les violets |
| `desperate` | S-O-S |
| `warning` | alternance façon gyrophare |
| `alerting` | alerte rouge rapide |
| `ready` | annonce la disponibilité, et se termine sur `available` |

SignalMe mémorise votre statut durable dans vos données d'application locales et le rétablit après un
signal. Il ne signale jamais un succès pour une commande que le périphérique a refusée, et chaque échec a
son propre [code de sortie](https://github.com/Reefact/signalme/blob/main/docs/commands-FR.md#codes-de-sortie).

## Documentation

- [Référence des commandes](https://github.com/Reefact/signalme/blob/main/docs/commands-FR.md) — chaque commande, valeur et code de sortie
- [Statuts et signaux](https://github.com/Reefact/signalme/blob/main/docs/signals-FR.md) — ce que fait chaque signal, et les règles exactes de restauration
- [Matériel et plateformes](https://github.com/Reefact/signalme/blob/main/docs/hardware-FR.md) — périphériques testés, Windows uniquement, plusieurs périphériques
- [Dépannage](https://github.com/Reefact/signalme/blob/main/docs/troubleshooting-FR.md) — ce que signifie chaque erreur et quoi faire
- [Développement et releases](https://github.com/Reefact/signalme/blob/main/docs/development-FR.md) — build, tests, CI, publication

## Licence

[Apache-2.0](https://github.com/Reefact/signalme/blob/main/LICENSE)

## Crédits

Icône du phare : [Maison lumineuse icônes créées par VectorPortal](https://www.flaticon.com/fr/icones-gratuites/maison-lumineuse)
— Flaticon.
