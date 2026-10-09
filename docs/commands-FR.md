_[English version](commands.md)_

# Référence des commandes

Les options, commandes interactives, messages et codes de sortie de cette page constituent le contrat
public de SignalMe 2.0. Les ajouts compatibles arriveront en versions mineures ; tout changement
incompatible imposerait une nouvelle version majeure.

SignalMe a deux niveaux de commandes. La **ligne de commande** le lance :

```shell
signalme [--mode <MODE>]
signalme --help       # ou -h
signalme --version    # ou -v
```

Une fois qu'il tourne, les **commandes interactives** se tapent à son invite :

```text
> busy
> happy
> status
> off
> help
```

## Ligne de commande

### `--mode <MODE>`, `-m <MODE>`

Le mode décide d'où vient le statut. SignalMe 2.0 n'en connaît qu'un, `manual`, dans lequel vous tapez
le statut vous-même ; c'est le mode par défaut, si bien que `signalme` et `signalme --mode manual` sont
une seule et même chose. Le nom est débarrassé de ses espaces et comparé sans tenir compte de la casse.

Un mode inconnu est une erreur d'utilisation, signalée avant qu'aucun périphérique ne soit touché :

```text
$ signalme --mode foo
Unknown mode: 'foo'.
Available modes: manual
```

### `--help` (`-h`), `--version` (`-v`)

`--help` affiche l'usage, `--version` affiche la version ; `-h` et `-v` en sont les formes courtes. Ni
l'un ni l'autre n'ouvre de périphérique : les deux fonctionnent sur n'importe quelle machine, Windows ou
non.

### Tout le reste

Une option inconnue, un argument que SignalMe n'attend pas, ou un `--mode` sans valeur est une erreur
d'utilisation, code de sortie `1` :

```text
$ signalme --bogus
Unknown option: '--bogus'.
Type 'signalme --help' for usage.

$ signalme extra
Unknown command 'extra'.
Type 'signalme --help' for usage.
```

SignalMe n'a pas de sous-commandes : sur la ligne de commande, `Unknown command` désigne un argument qu'il
n'attendait pas (un argument placé après `--` est signalé par `Unexpected argument: 'extra'.`). Ce n'est
pas le `Unknown command: '…'.` de l'invite, qui concerne une ligne tapée une fois SignalMe lancé.

## Démarrage

```text
$ signalme
SignalMe 2.0.0-preview.1
Luxafor device detected.
Mode: manual
Status: busy
Commands: help
Press Ctrl+C to stop.

> 
```

Dans l'ordre :

1. **Le périphérique.** SignalMe cherche tous les Luxafor une seule fois, au démarrage. S'il y en a
   exactement un, il est utilisé et `Luxafor device detected.` s'affiche. S'il y en a plusieurs,
   SignalMe les liste et demande lequel utiliser, en identifiant chaque candidat par une courte vague
   lumineuse — voir [Matériel et plateformes](hardware-FR.md#plusieurs-périphériques). S'il n'y en a
   aucun, SignalMe sort sur une erreur périphérique.
2. **Le mode**, `Mode: manual`.
3. **Le statut initial.** SignalMe repart du statut durable mémorisé lors de la dernière exécution, et
   l'affiche sur le périphérique avant que l'invite n'apparaisse ; sans rien de mémorisé, il affiche
   `Status: off` et le périphérique reste éteint. Si la session Windows est déjà verrouillée, le
   périphérique affiche `away` à la place et une seconde ligne le dit : `Effective status: away`.
4. **L'invite.** `Commands: help` et `Press Ctrl+C to stop.` viennent du mode manuel, puis `> ` vous
   attend.

## Commandes interactives

La saisie est débarrassée de ses espaces et passée en minuscules, si bien que ` BUSY ` veut dire `busy`.
Une ligne vide est ignorée. Ce que SignalMe ne reconnaît pas est signalé, et SignalMe continue :

```text
> buzy
Unknown command: 'buzy'.
Type 'help' to list available commands.
```

Chaque commande est menée à son terme avant l'invite suivante : un signal, par exemple, se joue jusqu'au
bout (ou jusqu'à ce que quelque chose l'interrompe) avant que `> ` ne revienne.

### Statuts durables

Restent affichés jusqu'à ce que vous en changiez.

| Commande | Alias | Couleur |
| --- | --- | --- |
| `available` | `free` | vert |
| `busy` | | jaune |
| `do-not-disturb` | `dnd` | rouge |

```text
> busy
Status: busy
```

Le statut n'est mémorisé qu'une fois que le périphérique l'a affiché. Tapé pendant que la session est
verrouillée, il est mémorisé tout de même, mais le périphérique continue d'afficher `away` jusqu'au
déverrouillage, et SignalMe le dit :

```text
> busy
Status: busy
Effective status: away
```

### `away`

`away` n'est plus une commande. C'est la **surcharge de présence** que SignalMe applique de lui-même :
pendant que votre session Windows est verrouillée, le périphérique est violet quel que soit votre statut,
et quand vous la déverrouillez, votre statut revient exactement tel qu'il était. Taper `away` est une
commande inconnue.

### Signaux temporaires

Jouent une animation par-dessus votre statut durable, puis le remettent en place.

| Commande | Se termine sur |
| --- | --- |
| `happy` | votre statut durable |
| `bored` | votre statut durable |
| `desperate` | votre statut durable |
| `warning` | votre statut durable |
| `alerting` | votre statut durable |
| `ready` | **`available`** |

```text
> happy
Playing: happy
Restored: busy

> ready
Playing: ready
Status: available
```

Un signal a besoin d'un statut durable par-dessus lequel se jouer. Il est refusé, sans rien changer sur le
périphérique, quand SignalMe est éteint :

```text
> happy
SignalMe is off: 'happy' is not played.
```

et quand la session est verrouillée :

```text
> happy
Session locked: 'happy' is not played.
```

Un signal en cours est interrompu par un verrouillage (le périphérique passe en `away`, rien n'est
rétabli, le signal ne reprend pas au déverrouillage) et par Ctrl+C. L'invite ne revient qu'une fois le
signal terminé : ce que vous tapez entre-temps est appliqué ensuite. Voir
[Statuts et signaux](signals-FR.md) pour l'allure de chaque signal et les règles exactes.

### `status`

Affiche où en sont les choses :

```text
> status
Mode: manual
Desired status: busy
Effective status: away
Session: locked
```

- **Desired status** est le statut durable que vous avez demandé (`none` quand SignalMe est éteint).
- **Effective status** est ce que le périphérique affiche : `off`, `away`, `available`, `busy` ou
  `do-not-disturb`.
- **Session** vaut `active` ou `locked`.

C'est l'état tel que SignalMe le connaît. Les périphériques Luxafor ne savent pas rendre compte de leurs
LED : si une autre application a piloté le périphérique entre-temps, SignalMe n'a aucun moyen de le
savoir.

### `off`

Éteint toutes les LED et oublie le statut durable :

```text
> off
Status: off
```

Tant que vous n'avez pas retapé un statut, les signaux sont refusés et un verrouillage de session ne
change rien : éteint a priorité sur tout le reste.

### `help`

Liste les commandes du mode courant :

```text
> help
Statuses:
  available
  free
  busy
  do-not-disturb
  dnd

Signals:
  happy
  bored
  desperate
  warning
  alerting
  ready

Commands:
  status
  off
  help

Press Ctrl+C to stop SignalMe.
```

## Verrouillage et déverrouillage de la session

SignalMe surveille la session Windows dans laquelle il tourne. Un verrouillage ou un déverrouillage est
appliqué aussitôt et annoncé dans la console, même pendant que l'invite attend — la notification prend
ses propres lignes et l'invite est réaffichée après elle :

```text
> 
Windows session locked.
Effective status: away
> 
Windows session unlocked.
Effective status: busy
> 
```

SignalMe éteint, le périphérique reste éteint à travers un verrouillage et la notification dit
`Effective status: off`. Seule la session dans laquelle SignalMe tourne est surveillée ; voir
[Matériel et plateformes](hardware-FR.md#surveillance-de-la-session).

## Arrêt

Ctrl+C est la façon normale de s'arrêter. SignalMe interrompt un signal éventuel, éteint le périphérique,
le libère et sort avec le code `0` :

```text
> ^C
Stopping SignalMe...
SignalMe stopped.
```

Le périphérique est éteint à dessein : un périphérique encore allumé après l'arrêt de SignalMe laisserait
croire qu'il surveille toujours votre présence. Votre statut durable reste mémorisé pour la prochaine
exécution.

Fermer l'entrée (fin de fichier sur la console) arrête SignalMe de la même façon. Un Ctrl+C, ou la
fermeture de l'entrée, pendant la sélection du périphérique affiche `SignalMe stopped.` et sort avec le
code `0` lui aussi.

## Codes de sortie

| Code | Signification |
| ---: | --- |
| `0` | SignalMe a tourné et s'est arrêté proprement : Ctrl+C, ou entrée fermée |
| `1` | Erreur d'utilisation — mode inconnu, option inconnue, valeur d'option manquante, argument inattendu |
| `2` | Erreur périphérique — aucun trouvé, découverte en échec, ou périphérique débranché ou ayant refusé une commande pendant l'exécution |
| `3` | Erreur inattendue |

Un périphérique qui cesse de répondre pendant que SignalMe tourne met fin à l'exécution : l'échec est
signalé, le périphérique est éteint dans la mesure où il le peut encore, et SignalMe sort avec le code `2`
plutôt que de prétendre que le dernier changement a été appliqué.

Un périphérique débranché est remarqué en deux secondes au plus, sans attendre la commande suivante :
SignalMe vérifie qu'il est toujours branché, affiche `Luxafor device disconnected.` sur stderr et sort
avec le code `2`. Rien n'est écrit sur un périphérique disparu, si bien qu'aucune erreur d'extinction ne
suit.

Scripter en s'appuyant sur les codes de sortie :

```shell
signalme || echo "SignalMe ne s'est pas arrêté proprement"
```

## Exemple de session

```text
$ signalme
SignalMe 2.0.0-preview.1
Luxafor device detected.
Mode: manual
Status: busy
Commands: help
Press Ctrl+C to stop.

> available
Status: available

> happy
Playing: happy
Restored: available

> status
Mode: manual
Desired status: available
Effective status: available
Session: active

> 
Windows session locked.
Effective status: away
> 
Windows session unlocked.
Effective status: available
> dnd
Status: do-not-disturb

> off
Status: off

> happy
SignalMe is off: 'happy' is not played.

> ^C
Stopping SignalMe...
SignalMe stopped.
```
