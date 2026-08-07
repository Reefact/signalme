_[English version](hardware.md)_

# Matériel et plateformes

## Plateforme

**Windows uniquement.**

SignalMe pilote le périphérique via
[Reefact.LuxaforLightingDeviceController](https://github.com/Reefact/luxafor-lighting-device-controller),
qui s'adresse à la pile HID de Windows. L'outil s'installe partout où .NET tourne, et les commandes qui ne
touchent pas au périphérique — `--help`, `status` — fonctionnent partout, mais aucun périphérique ne peut
être détecté ni piloté hors de Windows.

Sur une autre plateforme, `signalme as busy` sort sur une erreur périphérique en expliquant pourquoi.

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

SignalMe utilise le premier périphérique Luxafor découvert par la librairie de pilotage.

La sélection explicite d'un périphérique n'est pas supportée dans SignalMe 1.0. Avec plusieurs
périphériques branchés, celui qui répond est celui que la librairie énumère en premier, et cet ordre n'est
ni contrôlé ni garanti stable par SignalMe.

## Limites

- **Le périphérique ne se relit pas.** Les périphériques Luxafor acceptent les commandes mais ne rendent
  pas compte de l'état de leurs LED, ce qui explique que `signalme status` rapporte ce que SignalMe a
  demandé en dernier plutôt que ce qui est allumé. Voir
  [Référence des commandes](commands-FR.md#signalme-status).
- **Une application à la fois.** Le périphérique est tenu le temps d'une commande. Si une autre application
  le détient, SignalMe signale une erreur périphérique plutôt que d'attendre.
- **Pas de contrôle de luminosité.** Le protocole expose des couleurs, pas des niveaux de luminosité.
