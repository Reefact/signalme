_[English version](commands.md)_

# Référence des commandes

Les commandes, valeurs et codes de sortie de cette page constituent le contrat public de SignalMe 1.0. Les
ajouts compatibles arriveront en versions mineures ; tout changement incompatible imposerait une nouvelle
version majeure.

```shell
signalme as <status-or-signal>
signalme status
signalme off
signalme --help
```

## `signalme as <status-or-signal>`

Définit un statut durable, ou joue un signal temporaire. L'argument est débarrassé de ses espaces et passé
en minuscules, si bien que `signalme as " BUSY "` fonctionne.

La valeur est validée avant l'ouverture du périphérique : une faute de frappe est signalée comme telle, et
les valeurs acceptées sont listées, plutôt qu'une plainte à propos d'un matériel absent.

### Statuts durables

Restent affichés jusqu'à ce que vous en changiez.

| Valeur | Alias | Couleur |
| --- | --- | --- |
| `available` | `free` | vert |
| `busy` | | jaune |
| `away` | | violet |
| `do-not-disturb` | `dnd` | rouge |

### Signaux temporaires

Jouent une animation, puis rétablissent le statut durable.

| Valeur | Se termine sur |
| --- | --- |
| `happy` | le statut durable précédent |
| `bored` | le statut durable précédent |
| `desperate` | le statut durable précédent |
| `warning` | le statut durable précédent |
| `alerting` | le statut durable précédent |
| `ready` | **`available`** |

Voir [Statuts et signaux](signals-FR.md) pour l'allure de chacun et les règles exactes de restauration.

## `signalme status`

Affiche le dernier statut durable enregistré par SignalMe.

**Cette commande n'interroge pas le périphérique Luxafor.** Les périphériques Luxafor ne savent pas rendre
compte de l'état de leurs LED ; cette commande vous dit donc ce que SignalMe a demandé en dernier, pas
nécessairement ce que les LED affichent. Les deux peuvent diverger si une autre application a piloté le
périphérique entre-temps.

Elle ne lit qu'un fichier local, elle fonctionne donc sans périphérique branché.

```shell
$ signalme status
busy

$ signalme off && signalme status
No durable status is currently set.
```

Les alias sont normalisés : après `signalme as dnd`, `status` affiche `do-not-disturb`.

## `signalme off`

Éteint toutes les LED et oublie le statut durable.

Alias : `switch-off`.

## Codes de sortie

| Code | Signification |
| ---: | --- |
| `0` | Succès |
| `1` | Erreur d'utilisation — statut ou signal inconnu |
| `2` | Erreur périphérique — aucun trouvé, ou commande refusée par le périphérique |
| `3` | Erreur inattendue |
| `4` | Interruption par l'utilisateur |

Une commande ne signale jamais un succès pour quelque chose que le périphérique a refusé. Un Ctrl+C pendant
un signal temporaire arrête l'animation, rétablit le statut durable précédent, et sort avec le code `4`.

## Exemples

```shell
# Statuts durables, avec leurs alias
signalme as available
signalme as free
signalme as busy
signalme as away
signalme as do-not-disturb
signalme as dnd

# Signaux temporaires
signalme as happy
signalme as bored
signalme as desperate
signalme as warning
signalme as alerting
signalme as ready

# Le reste
signalme status
signalme off
signalme switch-off
```

Scripter en s'appuyant sur les codes de sortie :

```shell
signalme as busy || echo "périphérique injoignable"
```
