_[English version](signals.md)_

# Statuts et signaux

SignalMe distingue les statuts durables des signaux temporaires.

Un **statut durable** reste affiché jusqu'à ce que vous en changiez. Un **signal temporaire** joue une
animation par-dessus, puis le remet en place. `ready` est la seule exception, et elle est volontaire.

Les uns et les autres se tapent à l'invite de SignalMe ; voir la
[Référence des commandes](commands-FR.md) pour les commandes elles-mêmes. Cette page décrit ce que chacun
fait sur le périphérique, et ce qui se passe quand un signal ne va pas au bout.

## Statuts durables

| Statut | Alias | Couleur |
| --- | --- | --- |
| `available` | `free` | vert |
| `busy` | | jaune |
| `do-not-disturb` | `dnd` | rouge |
| `away` | | violet (`#9932CC`) — **automatique**, pendant que la session Windows est verrouillée |

Définir un statut allume toutes les LED dans sa couleur et l'enregistre localement, pour que SignalMe en
reparte la fois suivante et qu'un signal sache vers quoi revenir. Le statut n'est enregistré qu'une fois
le changement confirmé par le périphérique — si celui-ci refuse, SignalMe signale l'échec plutôt que de
mémoriser une couleur que les LED n'ont jamais affichée.

### Demandé et effectif

SignalMe distingue deux choses. Le **statut demandé** est ce que vous avez demandé. Le **statut effectif**
est ce que le périphérique affiche, et il se calcule dans cet ordre de priorité :

1. SignalMe est éteint — le périphérique est éteint, quoi qu'il se passe par ailleurs ;
2. la session Windows est verrouillée — le périphérique affiche `away` ;
3. un signal a été demandé — il se joue ;
4. sinon, le périphérique affiche le statut demandé.

Un verrouillage ne change donc jamais votre statut demandé : il surcharge ce qui est affiché, et la
surcharge est levée au déverrouillage. C'est pour cela que `away` ne se tape plus — c'est une surcharge
de présence, pas un choix. Un statut tapé pendant que la session est verrouillée est enregistré et
s'affichera une fois la session déverrouillée.

### L'enregistrement

L'enregistrement tient dans un petit fichier texte, sous vos données d'application locales
(`%LOCALAPPDATA%\SignalMe\signalme.ini` sous Windows). Il contient le statut demandé — jamais le `away`
produit par un verrouillage — et `off` le supprime. Il est écrit dans un fichier temporaire puis déplacé,
si bien qu'une interruption en cours d'écriture ne peut pas le corrompre. Supprimez-le et SignalMe oublie
simplement ; rien d'autre ne le lit ni ne l'écrit.

Un fichier écrit par SignalMe 1.x et qui contient `away` est lu comme « pas de statut » : SignalMe 2.0
démarre alors éteint, et le prochain statut que vous tapez remplace le fichier.

## Signaux temporaires

Un signal se joue par-dessus le statut durable que vous avez à ce moment-là. `happy` et `bored` partent
de sa couleur en fondu et y reviennent ; `desperate`, `warning` et `alerting` clignotent ou flashent, et
le statut est de nouveau affiché une fois qu'ils sont terminés. Un signal a donc besoin d'un statut
durable, et SignalMe en refuse un quand il est éteint, ou pendant que la session est verrouillée, sans
toucher au périphérique :

```text
SignalMe is off: 'happy' is not played.
Session locked: 'happy' is not played.
```

### `happy`

**Intention :** un signal léger, festif.

**Animation :** fond vers une version pastel de la couleur de votre statut, fait courir une vague
arc-en-ciel pastel sur les LED, puis revient en fondu.

**Après exécution :** le statut durable est rétabli.

### `bored`

**Intention :** l'attente, rien ne se passe.

**Animation :** dérive dans des violets profonds, une LED à la fois et dans le désordre, puis fond chaque
LED vers la couleur du statut.

**Après exécution :** le statut durable est rétabli.

### `desperate`

**Intention :** appel à l'aide, avec une pointe d'humour.

**Animation :** clignote S-O-S en blanc — trois courts, trois longs, trois courts.

**Après exécution :** le statut durable est rétabli.

### `warning`

**Intention :** quelque chose demande de l'attention.

**Animation :** alterne rouge et bleu entre les LED avant et arrière, façon gyrophare, cinq fois.

**Après exécution :** le statut durable est rétabli.

### `alerting`

**Intention :** quelque chose demande de l'attention tout de suite.

**Animation :** vingt flashs rouges rapides.

**Après exécution :** le statut durable est rétabli.

### `ready`

**Intention :** annoncer que vous êtes de nouveau disponible.

**Animation :** redescend de `do-not-disturb` vers `busy` si c'est là que vous étiez, puis clignote en
vert.

**Après exécution :** le statut durable devient `available`, et est enregistré comme tel.

Contrairement à tous les autres signaux temporaires, `ready` ne rétablit volontairement pas le statut
durable précédent après une exécution réussie. S'il ne va pas au bout, rien ne change : vous n'êtes marqué
disponible qu'une fois le signal réellement arrivé à destination.

## Ce qui se passe quand un signal se termine — ou pas

Les animations elles-mêmes ne savent rien de la restauration : c'est le runtime de SignalMe qui possède le
périphérique et décide de ce qu'il affiche une fois un signal terminé, de quelque façon qu'il se termine.

| | Résultat |
| --- | --- |
| Le signal va au bout | Le statut durable est de nouveau affiché : `Restored: busy`. Après `ready` : `Status: available`, désormais le statut durable. |
| La session Windows est verrouillée pendant le signal | L'animation s'arrête net, rien n'est rétabli, le périphérique affiche `away`. Au déverrouillage, le statut durable revient ; le signal ne reprend pas. |
| Ctrl+C pendant le signal | L'animation s'arrête, le périphérique est éteint, SignalMe sort avec le code `0`. |
| Le périphérique refuse une image, ou cesse de répondre | L'échec est signalé, SignalMe éteint le périphérique dans la mesure où il le peut encore et sort avec le code `2`. Le statut durable reste mémorisé pour la prochaine exécution. |

Un signal part toujours d'un statut durable, donc « rétabli » ne veut jamais dire « éteint » : un signal
interrompu par un verrouillage revient à votre statut au déverrouillage, et un `ready` interrompu par quoi
que ce soit vous laisse là où vous étiez.

Le runtime a une règle de plus, que le mode manuel ne peut pas atteindre puisque son invite ne revient
qu'une fois le signal terminé : une nouvelle demande qui arrive pendant qu'un signal se joue l'interrompt
— un statut s'affiche aussitôt, un autre signal remplace celui en cours, éteindre éteint le périphérique.
Un futur mode alimenté par un service de présence plutôt que par le clavier s'appuiera dessus.
