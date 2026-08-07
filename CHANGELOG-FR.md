_[English version](CHANGELOG.md)_

# Journal des modifications

Tous les changements notables de ce projet sont consignés dans ce fichier.

Le format s'appuie sur [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) et ce projet suit le
[versionnage sémantique](https://semver.org/spec/v2.0.0.html).

> `CHANGELOG.md` reste la version de référence : les notes de version du package y renvoient et
> l'outillage attend les intitulés Keep a Changelog en anglais. Cette page en est la traduction.

## [Non publié]

## [1.0.1]

### Ajouté

- **Une icône de package** — un phare — pour que SignalMe soit identifiable dans une liste NuGet au lieu
  d'afficher le placeholder par défaut. Le build échoue désormais si l'icône n'arrive pas dans le package,
  comme il le faisait déjà pour le README. Le design est crédité dans le README, comme sa licence l'exige.

Rien d'autre n'a changé : même outil, mêmes commandes, même comportement. Un package NuGet est immuable, on
ne peut donc pas ajouter d'icône à une version déjà publiée — d'où une version corrective.

## [1.0.0]

Première version publique stable de SignalMe.

1.0.0 signifie que le contrat public de la ligne de commande — commandes, valeurs acceptées, alias et codes
de sortie — est désormais considéré comme stable. Les ajouts compatibles arriveront en versions mineures ;
tout changement incompatible imposerait une nouvelle version majeure. Ce n'est pas une affirmation que
l'outil a cessé d'évoluer.

### Ajouté

- **Installable comme outil .NET global** : `dotnet tool install --global SignalMe`, puis `signalme`.
  Cible .NET 10, fonctionne sous Windows.
- **Statuts de disponibilité durables** : `available` (alias `free`), `busy`, `away` et `do-not-disturb`
  (alias `dnd`). Ils restent affichés jusqu'à ce que vous en changiez, et sont mémorisés localement.
- **Signaux lumineux temporaires** : `happy`, `bored`, `desperate`, `warning`, `alerting` et `ready`.
  Chacun joue une animation puis rétablit le statut durable — y compris quand l'animation échoue, et quand
  elle est interrompue. `ready` fait exception : il annonce la disponibilité et se termine sur
  `available`.
- **`signalme status`**, qui affiche le dernier statut durable défini par SignalMe. La commande n'ouvre pas
  le périphérique — les périphériques Luxafor ne savent pas rendre compte de l'état de leurs LED — elle
  rapporte donc ce qui a été demandé, et fonctionne sans périphérique branché.
- **`signalme off`**, qui éteint toutes les LED et oublie le statut durable. `switch-off` en est un alias.
- **Ctrl+C** arrête un signal, rétablit le statut durable précédent, et sort proprement.
- **Codes de sortie stables** : `0` succès, `1` erreur d'utilisation, `2` erreur périphérique, `3` erreur
  inattendue, `4` interruption.
- **Documentation** : une référence des commandes, la sémantique de chaque statut et de chaque signal, le
  matériel testé et non testé, un guide de dépannage, et un guide de développement et de publication.

### Notes

- Une commande ne signale jamais un succès pour quelque chose que le périphérique a refusé. Chaque appel au
  périphérique est vérifié, et un refus fait échouer la commande avec un message nommant l'opération.
- Le statut durable est stocké dans `%LOCALAPPDATA%\SignalMe\signalme.ini`, écrit via un fichier
  temporaire pour qu'une interruption ne puisse pas le laisser à moitié écrit. Un fichier absent ou
  illisible est lu comme « pas de statut ».
- SignalMe utilise le premier périphérique Luxafor découvert par la librairie de pilotage. La sélection
  explicite d'un périphérique n'est pas supportée en 1.0.
- Seul le Luxafor Orb a été testé. Voir [Matériel et plateformes](docs/hardware-FR.md) pour ce qui devrait
  fonctionner et ce qui n'est pas supporté.

[Non publié]: https://github.com/Reefact/signalme/compare/v1.0.1...HEAD
[1.0.1]: https://github.com/Reefact/signalme/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/Reefact/signalme/releases/tag/v1.0.0
