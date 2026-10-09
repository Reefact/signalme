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

## Comment ça s'assemble

SignalMe 2.0 est un processus résident construit autour d'une règle : **un seul composant écrit sur le
périphérique.**

- Un **mode** (`Modes/`) est une source d'intentions — définir un statut durable, jouer un signal,
  éteindre. Le mode manuel les lit dans la console ; un futur mode pourrait les tirer d'un service de
  présence. Un mode ne connaît ni le périphérique ni les couleurs.
- Le **moniteur de session** (`Sessions/`) rapporte le verrouillage et le déverrouillage de la session
  Windows, depuis le thread sur lequel Windows les délivre.
- Le **coordinateur de statut** (`Runtime/StatusCoordinator.cs`) reçoit les deux par un seul canal et les
  traite un par un. Il calcule le statut effectif (éteint, puis `away` pendant le verrouillage, puis un
  signal, puis le statut demandé), écrit sur le périphérique, persiste le statut demandé une fois que le
  périphérique a obéi, et affiche ce qui a changé. Les animations sont des tâches filles de sa boucle,
  lancées et attendues par elle, si bien qu'une image ne peut jamais arriver après l'écriture d'un
  statut.
- Le **runtime** (`Runtime/SignalMeRuntime.cs`) porte le cycle de vie : il démarre le moniteur, affiche le
  statut initial avant que le mode ne démarre, fait tourner le coordinateur et le mode côte à côte, et
  éteint puis libère le périphérique quoi qu'il soit arrivé.
- La **ligne de commande** (`Commands/`) analyse `--mode`, découvre et sélectionne le périphérique, puis
  le confie au runtime et traduit son issue en code de sortie.

## Tests

La suite tourne en quelques secondes et ne demande aucun matériel. Tout ce que le vrai programme touche
est derrière un point d'injection que les tests remplacent :

- **`FakeLuxaforDevice`** implémente `ILuxaforDevice`, l'interface que la librairie de pilotage expose
  déjà : aucune surcouche n'a été nécessaire. Il enregistre les commandes qu'il accepte et peut se voir
  demander de les refuser ou de lever une exception, ce qui couvre les chemins d'échec du périphérique ;
  son `Path` est modifiable, pour que les tests de sélection distinguent deux périphériques, tout comme
  `IsConnected`, pour qu'un test puisse le débrancher.
- **`IConsole`** / **`FakeConsole`** : chaque interaction avec la console passe par `IConsole`. Le faux
  répond aux lectures à partir d'un script, puis annonce la fin de l'entrée — ou laisse une lecture en
  attente jusqu'à l'annulation du jeton, comme un utilisateur qui n'appuie jamais sur Entrée avant
  Ctrl+C. Il capture la sortie, les invites et les erreurs, et tient une transcription des lectures et
  des écritures dans l'ordre où elles ont eu lieu : c'est ainsi qu'un test vérifie que le statut initial
  a été affiché avant la première invite du mode.
- **`ISessionMonitor`** / **`FakeSessionMonitor`** : le test déclenche un verrouillage ou un
  déverrouillage à la main, depuis n'importe quel thread, et peut faire en sorte que `Start()` bascule
  l'état, le déclenche, ou lève une exception — les trois choses que fait le vrai moniteur quand il
  comble l'écart entre sa construction et son abonnement.
- **`IDeviceConnectionMonitor`** / **`FakeDeviceConnectionMonitor`** : le test annonce le périphérique
  débranché à la main, au moment qu'il choisit. Le vrai moniteur est testé à part, sur une vérification
  que le test contrôle et un intervalle de quelques millisecondes.
- **`ILuxaforDeviceDiscovery`** / **`FakeDiscovery`**, qui renvoie une liste de faux ou lève une
  exception. Les tests de la ligne de commande font tourner la vraie ligne de commande, de `--help` au
  code de sortie, à travers un sac de faux `SignalMeServices`.
- **`IDelay`** est le point d'injection par lequel les animations attendent. En production l'attente est
  réelle ; `InstantDelay` revient aussitôt, et `ControllableDelay` retient une attente choisie, pour qu'un
  test obtienne une animation réellement en cours quand un verrouillage, une autre intention ou l'arrêt
  arrive. C'est ce qui rend les règles d'interruption testables.
- **`IMoodPattern`** : une animation est une fonction pure du périphérique, du délai et du statut d'où
  elle part. Elle ne persiste rien et ne rétablit rien, si bien que les règles du coordinateur se testent
  sans se soucier des images qu'un motif envoie.
- **`UserCurrentStatus`** prend son répertoire en argument, si bien que les tests utilisent un dossier
  temporaire plutôt que le vrai profil utilisateur.

Les tests vérifient le contrat, pas les images : aucun ne fige une frame d'animation précise. Les messages
que SignalMe affiche font partie de ce contrat et sont vérifiés à la lettre.

Les assertions sont écrites avec [NFluent](https://www.n-fluent.net/), comme dans la librairie Luxafor :
`Check.That(actual).IsEqualTo(expected)`, `Check.ThatCode(...).Throws<T>()`. xUnit fait tourner les
tests, son `Assert` n'est pas utilisé. Un cas reste hors de `Check.ThatCode` : NFluent attend le code
asynchrone, et une tâche annulée qu'on attend ainsi rapporte une `TaskCanceledException` générique au lieu
de l'exception qui porte la raison. Les quelques tests qui vérifient cette raison la capturent avec
`Record.ExceptionAsync` de xUnit et la vérifient avec NFluent.

La parallélisation des tests est désactivée pour une seule raison — quelques tests redirigent la console
du processus pour vérifier ce que SignalMe rapporte, ce qui est un état global au processus.

### Ce que les tests ne peuvent pas couvrir

`WindowsSessionMonitor` s'abonne aux notifications de changement de session de Windows, qu'aucun test ne
sait déclencher. Il n'a pas de test unitaire ; le coordinateur et le runtime sont testés contre le faux
moniteur, il ne reste donc à vérifier à la main que ceci : le vrai délivre bien. Après avoir installé un
build (ci-dessous), périphérique branché :

1. lancez `signalme`, tapez `busy` : le périphérique est jaune ;
2. verrouillez la session (Win+L) : le périphérique passe au violet ;
3. déverrouillez-la : le périphérique est de nouveau jaune, et la console affiche
   `Windows session locked.` / `Effective status: away`, puis `Windows session unlocked.` /
   `Effective status: busy` ;
4. `status` annonce `Session: active`.

À faire une fois par release, et après tout changement du moniteur ou du package
`Microsoft.Win32.SystemEvents` sur lequel il repose.

`LuxaforDeviceConnectionMonitor` est dans la même situation : il est testé sur une vérification que le
test contrôle, et seul un vrai périphérique dit si `ILuxaforDevice.IsConnected` suit un vrai
débranchement. Périphérique branché :

1. lancez `signalme`, tapez `busy` : le périphérique est jaune ;
2. débranchez le périphérique : en deux secondes au plus, sans rien taper, la console affiche
   `Luxafor device disconnected.`, puis `Stopping SignalMe...` et `SignalMe stopped.`, sans erreur
   d'extinction ;
3. le code de sortie est `2` (`echo $LASTEXITCODE` dans PowerShell).

Une fois par release également, et après tout changement du moniteur ou de la librairie Luxafor.

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
dotnet tool install --global SignalMe --add-source ./artifacts --version 2.0.0-preview.2
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
fichier source parasite. `build/Test-ToolInstall.ps1` l'installe ensuite pour de vrai et exécute ce qui
fonctionne sans périphérique : `--help` doit mentionner `--mode`, `--version` doit afficher la version, un
mode inconnu et une option inconnue doivent être des erreurs d'utilisation (code `1`, le premier citant
`manual`), et `signalme` sans argument doit sortir en `2` avec un message — l'agent n'a ni périphérique ni
entrée interactive, et SignalMe doit le dire plutôt que d'attendre une ligne qui ne viendra jamais.
L'étape qui les précède fait tourner les tests du validateur : un validateur qui ne refuserait plus rien
est repéré avant de laisser passer un package cassé.

## Publier une release

Les releases sont publiées par `.github/workflows/release.yml`, déclenché par un tag de version.

```shell
# 1. définir <Version> dans SignalMe/SignalMe.csproj, par exemple 2.0.0
# 2. mettre à jour CHANGELOG.md, tout merger dans main
git tag v2.0.0
git push origin v2.0.0
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
