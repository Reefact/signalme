_[English version](development.md)_

# Développement

## Prérequis

La version du SDK .NET épinglée dans [`global.json`](../global.json).

SignalMe compile en traitant les warnings comme des erreurs, et ce qui constitue un diagnostic est décidé
par les analyseurs et le compilateur du SDK. Un SDK non épinglé, c'est un build vert sur une machine et
rouge sur une autre — ce qui est exactement arrivé ici une fois, un poste en `10.0.110` face à un agent en
`10.0.302`. Le SDK est donc épinglé à une feature band, les correctifs évoluent à l'intérieur, et la CI
l'installe via `global-json-file` plutôt qu'en résolvant une version flottante.

Si `dotnet` signale un SDK manquant, installez celui que réclame `global.json` plutôt que de modifier le
fichier. Le faire évoluer est un choix délibéré : une nouvelle feature band peut faire apparaître de
nouveaux warnings, qui sont ici des erreurs.

Le package d'analyseurs est épinglé lui aussi, dans [`Directory.Build.props`](../Directory.Build.props),
avec les réglages de langage et de qualité partagés par les deux projets.

## Build

```shell
dotnet restore
dotnet build -c Release
dotnet test -c Release
dotnet pack -c Release -o artifacts
```

## Tests

La suite tourne en moins d'une seconde et ne demande aucun matériel.

- **`FakeLuxaforDevice`** implémente `ILuxaforDevice`, l'interface que la librairie de pilotage expose
  déjà : aucune surcouche n'a été nécessaire pour rendre SignalMe testable. Il enregistre les commandes
  qu'il accepte et peut se voir demander de les refuser ou de lever une exception, ce qui couvre les
  chemins d'échec du périphérique.
- **`IDelay`** est le point d'injection par lequel les animations attendent. En production l'attente est
  réelle ; dans les tests elle est immédiate. C'est ce qui garde la suite rapide, et c'est aussi ce qui
  rend l'annulation testable — un test annule à une attente choisie.
- **`UserCurrentStatus`** prend son répertoire en argument, si bien que les tests utilisent un dossier
  temporaire plutôt que le vrai profil utilisateur.

Les tests vérifient le contrat, pas les images : aucun ne fige une frame d'animation précise.

La parallélisation des tests est désactivée pour une seule raison — plusieurs tests redirigent
`Console.Error` pour vérifier ce que SignalMe rapporte, ce qui est un état global au processus.

### Les scripts de build

`build/Validate-Package.ps1` garde une porte à sens unique — une version publiée sur nuget.org est
immuable — il a donc sa propre suite de tests, en Pester, sous `build/tests`.

```shell
./build/Test-BuildScripts.ps1            # -Detailed détaille chaque test
```

Elle demande Pester 5 (`Install-Module Pester -MinimumVersion 5.0.0 -Scope CurrentUser
-SkipPublisherCheck`) ; l'image Windows de la CI l'embarque déjà. `PackageFixture.psm1` fabrique des
`.nupkg` synthétiques au lieu de lancer `dotnet pack`, parce qu'un build sain ne sait pas exprimer ce dont
la suite parle : des packages faux d'exactement une façon. Chaque test nomme son défaut — une icône
d'exactement 1 Mo, une assembly manquante, une commande renommée — et vérifie que le validateur le refuse.

## Installation locale de l'outil

```shell
dotnet pack -c Release -o artifacts
dotnet tool install --global SignalMe --add-source ./artifacts --version 1.0.2
```

Utilisez `--tool-path ./tmp-tool` au lieu de `--global` pour l'essayer sans toucher à vos outils globaux.

## Ressources graphiques

`assets/icon.png` est l'icône du package : une sphère lumineuse, 512 × 512, fond transparent. Elle est
versionnée telle qu'elle a été produite — rien dans le build ne la dérive ni ne la réécrit, donc changer
l'icône revient à remplacer ce fichier.

Une seule contrainte ferme pour un remplacement : **rester sous 1 Mo.** nuget.org refuse une icône plus
lourde, et il le fait au moment de la publication, bien après que la CI soit passée au vert.
`build/Validate-Package.ps1` mesure l'icône empaquetée par rapport à cette limite : une icône de
remplacement trop lourde fait échouer le build plutôt que la release.

## CI

`.github/workflows/ci.yml`, sur les pull requests et les push sur `main`, sur un agent Windows — la
plateforme à laquelle SignalMe est destiné. Aucun périphérique physique n'est jamais nécessaire.

```text
restore → contrôles de style et d'analyseurs → build -warnaserror → tests → pack
        → tests des scripts de build → validation du contenu du package
        → installation de l'outil et exécution → publication du package
```

`dotnet format whitespace` en est volontairement exclu : ce code aligne les affectations consécutives, ce
que le style par défaut normalise. `dotnet format style` et `dotnet format analyzers` sont tous deux
appliqués.

Les étapes d'empaquetage comptent plus qu'il n'y paraît. `build/Validate-Package.ps1` lit le `.nupkg` et
vérifie qu'il s'agit bien d'un package d'outil installable — le marqueur `DotnetTool`, le nom de la
commande, les assemblies attendues, le README, une icône sous la limite de 1 Mo de nuget.org, aucun
fichier source parasite. `build/Test-ToolInstall.ps1` l'installe ensuite pour de vrai et exécute les
commandes qui fonctionnent sans périphérique, y compris le chemin « aucun périphérique ». L'étape qui
les précède fait tourner les tests du validateur : un validateur qui ne refuserait plus rien est repéré
avant de laisser passer un package cassé.

## Publier une release

Les releases sont publiées par `.github/workflows/release.yml`, déclenché par un tag de version.

```shell
# 1. définir <Version> dans SignalMe/SignalMe.csproj, par exemple 1.0.0
# 2. mettre à jour CHANGELOG.md, tout merger dans main
git tag v1.0.0
git push origin v1.0.0
```

Le job refuse de publier quand le tag ne correspond pas à la version du projet : un tag mal saisi ne peut
donc pas publier la mauvaise. Il exécute ensuite les tests, la validation du package et une véritable
installation de l'outil avant de pousser quoi que ce soit.

### Configuration initiale

La publication utilise le **trusted publishing** de nuget.org (OIDC) : aucune clé d'API à longue durée
n'est stockée dans le dépôt. Le job échange un jeton OIDC GitHub contre une clé éphémère au moment du push.
Il faut, une fois pour toutes :

- Sur nuget.org, une trusted publishing policy pour ce dépôt :

  | Champ | Valeur |
  | --- | --- |
  | Repository owner | `Reefact` |
  | Repository | `signalme` |
  | Workflow file | `release.yml` — le nom de fichier seul, **pas** `.github/workflows/release.yml` |
  | Environment | `nuget` (facultatif, correspond au job) |

  Une policy peut appartenir à un utilisateur ou à une organisation. Il n'y a rien à configurer pour le
  tag : nuget.org filtre sur ces champs-là, et restreindre le workflow aux tags `v*` est le rôle de son
  propre déclencheur.

- Un secret ou une variable de dépôt `NUGET_USER`, contenant le compte nuget.org — utilisateur ou
  organisation — propriétaire de la policy. C'est un nom de compte, pas un identifiant secret.

- Un environnement `nuget` dans les paramètres du dépôt. Y ajouter un reviewer obligatoire rend chaque
  release soumise à approbation manuelle.
