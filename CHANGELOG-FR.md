_[English version](CHANGELOG.md)_

# Journal des modifications

Tous les changements notables de ce projet sont consignés dans ce fichier.

Le format s'appuie sur [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) et ce projet suit le
[versionnage sémantique](https://semver.org/spec/v2.0.0.html).

> `CHANGELOG.md` reste la version de référence : les notes de version du package y renvoient et
> l'outillage attend les intitulés Keep a Changelog en anglais. Cette page en est la traduction.

## [Non publié]

### Ajouté

- **La validation du package vérifie désormais l'icône par rapport à la limite de 1 Mo de nuget.org.** La
  contrainte était écrite dans le guide de développement, mais rien ne l'appliquait, et nuget.org ne la
  fait respecter qu'au push du package — une fois le tag posé et le job de release déjà lancé. C'est
  maintenant un échec de build, où une icône trop lourde ne coûte rien d'autre qu'un fichier plus léger.
- **Des tests pour les scripts de build**, en Pester, sous `build/tests`, lancés par
  `build/Test-BuildScripts.ps1` dans les deux workflows. `Validate-Package.ps1` est ce qui sépare un
  package cassé d'une version publiée immuable, et il ne mérite cette place que s'il échoue vraiment : la
  suite lui soumet des packages synthétiques faux d'exactement une façon — une icône d'exactement 1 Mo,
  une assembly manquante, une commande renommée, un fichier source qui a fui — et vérifie qu'il refuse
  chacun d'eux.

## [1.0.2] - 2026-08-07

Encore une version qui n'apporte qu'une icône, pour la même raison que la précédente : un package NuGet
publié est immuable, une nouvelle icône exige donc une version à elle. Même outil, mêmes commandes, même
comportement.

### Modifié

- **Une nouvelle icône de package** — une sphère lumineuse, ce que SignalMe allume vraiment. Elle remplace
  le phare de la 1.0.1, qui venait de Flaticon sous une licence exigeant que le design soit crédité partout
  où il apparaît. Une icône de package ne porte aucune mention de ce genre : c'était donc au README de le
  faire pour elle. L'icône appartient désormais au projet et ne dépend des conditions de personne, d'où la
  disparition de la section d'attribution dans les deux READMEs — elle n'existait que pour porter ce
  crédit.

### Supprimé

- **`build/make-icon.py`**, qui dessinait le phare à partir de primitives vectorielles. L'icône est
  maintenant une image versionnée et non plus générée : un générateur qui produit l'ancien design ne
  ferait qu'induire en erreur. Les contraintes qu'une icône de remplacement doit respecter sont écrites
  dans le guide de développement à la place.

## [1.0.1] - 2026-08-07

Une version qui n'apporte qu'une icône. Un package NuGet est immuable : l'icône ne pouvait pas rejoindre la
1.0.0 déjà publiée, et exigeait une version à elle. Même outil, mêmes commandes, même comportement.

### Ajouté

- **Une icône de package** — un phare — pour que SignalMe soit identifiable dans une liste NuGet au lieu
  d'afficher le placeholder par défaut. Le build échoue désormais si l'icône n'arrive pas dans le package,
  comme il le faisait déjà pour le README. Le design est crédité dans le README, comme sa licence l'exige.

## [1.0.0] - 2026-08-07

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

[Non publié]: https://github.com/Reefact/signalme/compare/v1.0.2...HEAD
[1.0.2]: https://github.com/Reefact/signalme/compare/v1.0.1...v1.0.2
[1.0.1]: https://github.com/Reefact/signalme/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/Reefact/signalme/releases/tag/v1.0.0
