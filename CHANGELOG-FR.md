_[English version](CHANGELOG.md)_

# Journal des modifications

Tous les changements notables de ce projet sont consignés dans ce fichier.

Le format s'appuie sur [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) et ce projet suit le
[versionnage sémantique](https://semver.org/spec/v2.0.0.html).

> `CHANGELOG.md` reste la version de référence : les notes de version du package y renvoient et
> l'outillage attend les intitulés Keep a Changelog en anglais. Cette page en est la traduction.

## [Non publié]

La prochaine version est une nouvelle version majeure. SignalMe cesse d'être une commande « one shot »
pour devenir un processus résident : il tourne dans votre terminal, vous tapez statuts et signaux à son
invite, et il passe de lui-même le périphérique en `away` pendant que votre session Windows est
verrouillée. La ligne de commande de la 1.x n'existe plus, et c'est à cela que sert la version majeure.

### Ajouté

- **`away` automatique au verrouillage de la session.** SignalMe surveille la session Windows dans
  laquelle il tourne : un verrouillage affiche `away` sur le périphérique, un déverrouillage ramène votre
  statut, et le statut que vous avez demandé n'est modifié ni par l'un ni par l'autre. Un verrouillage
  interrompt un signal en cours, qui ne reprend pas. Seule la session dans laquelle SignalMe tourne est
  surveillée : lancé comme service ou dans une session d'arrière-plan, il ne voit aucun verrouillage.
- **La sélection du périphérique.** Avec plusieurs Luxafor branchés, SignalMe les liste, joue une courte
  vague blanche sur celui que vous désignez pour que vous voyiez lequel c'est, et demande confirmation
  avant de l'utiliser. Les périphériques non retenus sont libérés. Avec un seul périphérique, rien n'est
  demandé, comme avant.
- **Le mode `manual`**, celui par défaut et le seul pour l'instant, dans lequel le statut se tape à
  l'invite de SignalMe : les statuts et signaux de la 1.x, plus `status` (mode, statut demandé, statut
  effectif, état de la session), `off` et `help`. Une saisie inconnue est signalée et SignalMe continue.
  `--mode <MODE>` nomme le mode, pour que d'autres sources de statut puissent s'ajouter plus tard sans
  toucher au cœur.
- **`--version`**, qui affiche la version et quitte, comme `--help` sans toucher à un périphérique.
- **Un arrêt propre à la fin de l'entrée.** Fermer l'entrée de la console arrête SignalMe comme Ctrl+C :
  périphérique éteint, code de sortie `0`.
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

### Modifié

- **SignalMe est résident.** `signalme` le lance et il reste jusqu'à Ctrl+C : il trouve le périphérique
  une fois, affiche le statut mémorisé lors de la dernière exécution, puis applique ce que vous tapez et
  ce que fait la session. Il éteint le périphérique quand il s'arrête, pour qu'un périphérique allumé ne
  laisse jamais croire qu'il surveille encore votre présence. Un seul composant écrit sur le
  périphérique, toujours ; un signal, un changement de statut et un verrouillage ne peuvent plus se faire
  concurrence.
- **La ligne de commande est incompatible avec la 1.x.** `signalme [--mode <MODE>]`, `--help` et
  `--version` sont tout ce qu'elle accepte ; une option inconnue, un argument inattendu ou une valeur
  d'option manquante est une erreur d'utilisation. Les statuts et les signaux sont désormais des
  commandes interactives.
- **Un signal a besoin d'un statut durable.** SignalMe éteint, un signal est refusé avec
  `SignalMe is off: '<signal>' is not played.` plutôt que joué par-dessus un périphérique éteint, et
  `ready` ne peut plus transformer « pas de statut » en `available`. Éteint a priorité sur tout, le
  verrouillage vient ensuite : un signal est refusé aussi pendant que la session est verrouillée.
- **Un signal interrompu n'est pas rétabli par le signal.** La restauration appartient au runtime : ce qui
  interrompt un signal — un verrouillage, Ctrl+C, une nouvelle demande du mode — décide de ce que le
  périphérique affiche ensuite. `ready` se termine toujours sur `available`, et toujours seulement quand
  il va au bout.
- **Codes de sortie.** Ctrl+C sort désormais en `0` : arrêter SignalMe est la façon normale de s'en
  servir, pas une interruption. `1` couvre toute erreur de ligne de commande (mode inconnu compris), `2`
  tout échec du périphérique, avant ou pendant l'exécution, et `3` l'inattendu.
- **Un échec du périphérique pendant l'exécution arrête SignalMe**, avec le code de sortie `2`, après
  avoir éteint le périphérique dans la mesure où il le peut encore. Un statut que le périphérique a refusé
  n'est pas mémorisé.
- **Le fichier de statut est lu avec plus de soin.** Il contient toujours le dernier statut durable que le
  périphérique a affiché, jamais le `away` produit par un verrouillage. Un fichier laissé par la 1.x et
  qui dit `away` est lu comme « pas de statut ».
- **La description du package** décrit désormais un runtime de présence résident.

### Supprimé

- **Les commandes `as`, `status` et `off`** de la ligne de commande, et l'alias `switch-off`. Leur rôle
  se joue à l'invite de SignalMe : un statut ou un signal par son nom, `status` et `off`.
- **Le code de sortie `4`** (interruption par l'utilisateur). Ctrl+C sort en `0`.
- **`away` comme statut que l'on définit.** C'est la surcharge de présence que SignalMe applique pendant
  que la session est verrouillée ; tapé à l'invite, c'est une commande inconnue.

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
