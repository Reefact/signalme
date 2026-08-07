_[English version](signals.md)_

# Statuts et signaux

SignalMe distingue les statuts durables des signaux temporaires.

Un **statut durable** reste affiché jusqu'à ce que vous en changiez. Un **signal temporaire** joue une
animation puis remet votre statut durable en place. `ready` est la seule exception, et elle est
volontaire.

## Statuts durables

| Statut | Alias | Couleur |
| --- | --- | --- |
| `available` | `free` | vert |
| `busy` | | jaune |
| `away` | | violet (`#9932CC`) |
| `do-not-disturb` | `dnd` | rouge |

Définir un statut allume toutes les LED dans sa couleur et l'enregistre localement, pour qu'un signal
ultérieur sache vers quoi revenir. Le statut n'est enregistré qu'une fois le changement confirmé par le
périphérique — si celui-ci refuse, SignalMe échoue plutôt que de mémoriser une couleur que les LED n'ont
jamais affichée.

L'enregistrement tient dans un petit fichier texte, sous vos données d'application locales
(`%LOCALAPPDATA%\SignalMe\signalme.ini` sous Windows). Il est écrit dans un fichier temporaire puis
déplacé, si bien qu'une interruption en cours d'écriture ne peut pas le corrompre. Supprimez-le et
SignalMe oublie simplement ; rien d'autre ne le lit ni ne l'écrit.

## Signaux temporaires

### `happy`

**Intention :** un signal léger, festif.

**Animation :** fond vers une version pastel de la couleur de votre statut courant, fait courir une vague
arc-en-ciel pastel sur les LED, puis revient en fondu.

**Après exécution :** le statut durable précédent est rétabli.

### `bored`

**Intention :** l'attente, rien ne se passe.

**Animation :** dérive dans des violets profonds, une LED à la fois et dans le désordre, puis fond chaque
LED vers la couleur du statut.

**Après exécution :** le statut durable précédent est rétabli.

### `desperate`

**Intention :** appel à l'aide, avec une pointe d'humour.

**Animation :** clignote S-O-S en blanc — trois courts, trois longs, trois courts.

**Après exécution :** le statut durable précédent est rétabli.

### `warning`

**Intention :** quelque chose demande de l'attention.

**Animation :** alterne rouge et bleu entre les LED avant et arrière, façon gyrophare, cinq fois.

**Après exécution :** le statut durable précédent est rétabli.

### `alerting`

**Intention :** quelque chose demande de l'attention tout de suite.

**Animation :** vingt flashs rouges rapides.

**Après exécution :** le statut durable précédent est rétabli.

### `ready`

**Intention :** annoncer que vous êtes de nouveau disponible.

**Animation :** redescend de `do-not-disturb` vers `busy` si c'est là que vous étiez, puis clignote en
vert.

**Après exécution :** le statut durable devient `available`.

Contrairement à tous les autres signaux temporaires, `ready` ne rétablit volontairement pas le statut
durable précédent après une exécution réussie. S'il ne va pas au bout, il retombe sur le statut d'où vous
êtes parti — vous n'êtes marqué disponible qu'une fois le signal réellement arrivé à destination.

## Règles de restauration

Ce qu'il advient de votre statut durable dans chaque cas :

| | Résultat |
| --- | --- |
| Le signal réussit | Statut rétabli. Sortie `0`. |
| Le signal échoue | Statut rétabli, puis l'échec d'origine est signalé. |
| Ctrl+C | L'animation s'arrête, le statut est rétabli. Sortie `4`. |
| La restauration échoue après un signal réussi | La commande échoue sur une erreur périphérique. Laisser les LED sur une couleur d'animation n'est pas un succès. |
| Le signal et la restauration échouent tous les deux | L'échec du signal reste l'erreur remontée ; l'échec de restauration est signalé à part sur stderr et ne le masque jamais. |

S'il n'y avait aucun statut durable au départ, « rétabli » veut dire que les LED sont éteintes.
