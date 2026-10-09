_[English version](hardware.md)_

# Matériel et plateformes

## Plateforme

**Windows uniquement.**

SignalMe pilote le périphérique via
[Reefact.LuxaforLightingDeviceController](https://github.com/Reefact/luxafor-lighting-device-controller),
qui s'adresse à la pile HID de Windows, et surveille l'état de verrouillage de la session Windows dans
laquelle il tourne. L'outil s'installe partout où .NET tourne, et `--help` et `--version` fonctionnent
partout, mais aucun périphérique ne peut être détecté ni piloté hors de Windows.

Sur une autre plateforme, `signalme` sort sur une erreur périphérique en expliquant pourquoi.

## Périphériques

| Périphérique | État |
| --- | --- |
| Luxafor Orb | **Testé.** Le périphérique sur lequel SignalMe est développé. |
| Luxafor Flag | **Devrait fonctionner, non testé.** Mêmes identifiants et même protocole d'éclairage, six LED adressables. |
| Luxafor Mute Button, Luxafor Colorblind Flag | **Devrait fonctionner, non testé.** Les commandes d'éclairage sont les mêmes ; la disposition des LED, leur nombre et le rendu des couleurs peuvent différer. |
| Luxafor Bluetooth, Switch, Cube, Pomodoro-Timer, CO2 Monitor | **Non supportés.** Ils ne se pilotent pas par ce protocole USB HID. |

« Devrait fonctionner » signifie que le protocole correspond et que rien n'est connu comme défaillant — pas
que quelqu'un ait fait tourner SignalMe dessus. Tout retour sur un périphérique non testé est le bienvenu :
[ouvrez une issue](https://github.com/Reefact/signalme/issues).

## Plusieurs périphériques

SignalMe pilote un périphérique par exécution, et cherche les périphériques une seule fois, au démarrage.
Avec un seul périphérique branché, il est utilisé sans question. Avec plusieurs, SignalMe demande lequel :

```text
$ signalme
SignalMe 2.0.0-preview.2

2 Luxafor devices detected.

┌───┬──────────────────────────────┐
│ # │ Id                           │
├───┼──────────────────────────────┤
│ 1 │ <device-id-1>                │
│ 2 │ <device-id-2>                │
└───┴──────────────────────────────┘

Select device: 1

Identifying device #1...

Use this device? [Y/N]: n

Select device: 2

Identifying device #2...

Use this device? [Y/N]: y

Device selected: <device-id-2>
```

- `#` est un numéro que SignalMe attribue pour cette sélection seulement. `Id` est l'identifiant que la
  couche HID rapporte pour le périphérique : il distingue deux périphériques mais n'est pas fait pour
  être lu, et c'est pour cela que SignalMe vous montre le périphérique au lieu de s'en remettre à lui.
- Répondre à `Select device:` par un numéro joue une courte **vague d'identification** sur ce
  périphérique — un chenillard blanc sur ses LED, deux fois — puis l'éteint de nouveau. Ce n'est pas un
  statut : rien n'est mémorisé.
- `Use this device? [Y/N]:` accepte `y` ou `n`, en majuscules ou en minuscules. `n` revient à la
  sélection, et choisir de nouveau un périphérique rejoue la vague. `y` le garde pour toute l'exécution ;
  tous les autres sont libérés aussitôt.
- Si le périphérique échoue pendant la vague, l'erreur est affichée et la sélection est redemandée —
  choisissez un autre périphérique ou réessayez.
- Une réponse vide, non numérique ou hors plage reçoit `Invalid device number.` et la question est
  reposée ; toute autre réponse à la confirmation est redemandée elle aussi. Rien de ce que vous tapez ici
  ne peut arrêter SignalMe, sauf Ctrl+C ou la fermeture de l'entrée, qui quittent proprement l'un comme
  l'autre.

L'ordre du tableau est celui dans lequel la librairie énumère les périphériques, que SignalMe ne contrôle
pas et ne garantit pas stable — d'où la vague.

## Surveillance de la session

SignalMe suit l'état de verrouillage de la session Windows dans laquelle il a été lancé, par les
notifications de changement de session que Windows délivre à cette session. Seule cette session compte :
le verrouillage d'un autre utilisateur sur la même machine n'est pas vu, et un verrouillage n'est pas vu
non plus quand SignalMe tourne ailleurs que dans votre session interactive — lancé comme service, ou par
une tâche planifiée qui s'exécute en arrière-plan (session 0), il n'apprend jamais que vous avez
verrouillé votre écran. Lancez-le depuis un terminal dans votre propre session.

Sur une autre plateforme que Windows, la session est supposée active et ne change jamais ; c'est de toute
façon la découverte du périphérique qui arrête SignalMe là-bas.

## Limites

- **Le périphérique ne se relit pas.** Les périphériques Luxafor acceptent les commandes mais ne rendent
  pas compte de l'état de leurs LED, ce qui explique que la commande `status` rapporte ce que SignalMe a
  demandé plutôt que ce qui est allumé. Voir [Référence des commandes](commands-FR.md#status).
- **Une application à la fois.** Le périphérique est tenu pendant toute l'exécution. Si une autre
  application le détient au démarrage de SignalMe, SignalMe signale une erreur périphérique plutôt que
  d'attendre ; pendant que SignalMe tourne, cette autre application ne peut pas le piloter.
- **Pas de reconnexion.** Un périphérique débranché pendant l'exécution y met fin : SignalMe vérifie
  toutes les deux secondes que le périphérique est toujours branché, affiche
  `Luxafor device disconnected.` et sort sur une erreur périphérique, sans attendre la commande suivante.
  Un périphérique repris par une autre application, ou débranché et rebranché entre deux vérifications,
  met fin à l'exécution à l'écriture suivante : le handle que tient SignalMe ne survit pas, même à une
  absence brève. Rebranchez le périphérique et relancez SignalMe.
- **Pas de contrôle de luminosité.** Le protocole expose des couleurs, pas des niveaux de luminosité.
