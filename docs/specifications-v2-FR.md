# SignalMe V2 — Spécification fonctionnelle et technique

## 1. Objet

SignalMe V2 transforme SignalMe d'un outil CLI « one shot » en une application console résidente.

En V1, une commande SignalMe :

1. démarre le processus ;
2. détecte un périphérique Luxafor ;
3. applique un statut ou joue un signal ;
4. rend la main.

En V2, SignalMe :

1. démarre ;
2. découvre et sélectionne le périphérique Luxafor à utiliser ;
3. démarre un mode de fonctionnement ;
4. surveille en permanence l'état de la session Windows ;
5. pilote le Luxafor en fonction du mode actif et de l'état de la session ;
6. reste actif jusqu'à ce que l'utilisateur l'arrête avec `Ctrl+C`.

SignalMe devient ainsi un **runtime local de présence**, dont la source de statut est extensible.

---

# 2. Objectifs de la V2

La V2 doit permettre :

* de laisser SignalMe actif en permanence dans une console ;
* d'arrêter SignalMe proprement avec `Ctrl+C` ;
* de gérer automatiquement le statut `away` lorsque la session Windows est verrouillée ;
* de restaurer automatiquement le statut approprié lorsque la session est déverrouillée ;
* de proposer un mode `manual` dans lequel les changements de statut sont saisis directement dans la console ;
* de préparer l'architecture à de futurs modes tels que `teams` ;
* de détecter plusieurs périphériques Luxafor ;
* de permettre à l'utilisateur de sélectionner visuellement le Luxafor à utiliser ;
* de garantir qu'un seul composant du programme pilote effectivement le périphérique ;
* de conserver les statuts et signaux lumineux existants de SignalMe.

---

# 3. Principes structurants

## 3.1 SignalMe est désormais résident

L'exécution normale devient :

```shell
signalme
```

ou explicitement :

```shell
signalme --mode manual
```

Le processus ne se termine pas après un changement de statut.

Il reste actif jusqu'à :

```text
Ctrl+C
```

---

## 3.2 Le mode détermine la source du statut

SignalMe introduit la notion de **mode**.

Le mode indique comment le statut utilisateur est déterminé.

Exemples conceptuels :

```shell
signalme --mode manual
signalme --mode teams
```

La V2 implémente uniquement :

```text
manual
```

L'architecture doit cependant permettre d'ajouter ultérieurement d'autres modes sans modifier le cœur de SignalMe.

`teams` est donc une extension future et ne fait pas partie de l'implémentation fonctionnelle de cette V2.

---

## 3.3 Le mode manuel est le mode par défaut

Les commandes suivantes sont équivalentes :

```shell
signalme
```

```shell
signalme --mode manual
```

Un mode inconnu doit provoquer une erreur d'utilisation et l'arrêt du programme.

Exemple :

```text
> signalme --mode foo

Unknown mode: 'foo'.
Available modes: manual
```

---

# 4. Nouveau cycle de vie

Le cycle de vie nominal de SignalMe V2 est :

```text
signalme
   │
   ▼
Parsing CLI
   │
   ▼
Création du CancellationToken Ctrl+C
   │
   ▼
Découverte des Luxafor
   │
   ▼
Sélection du Luxafor
   │
   ▼
Chargement du DesiredStatus
   │
   ▼
Détermination de l'état initial de la session Windows
   │
   ▼
Démarrage du SessionMonitor
   │
   ▼
Création du mode
   │
   ▼
Démarrage du SignalMeRuntime
   │
   ▼
Boucle du mode
   │
   │
   ├── événements du mode
   ├── lock Windows
   ├── unlock Windows
   └── signaux temporaires
   │
   ▼
Ctrl+C
   │
   ▼
Arrêt propre
   │
   ▼
Extinction du Luxafor
   │
   ▼
Dispose
   │
   ▼
Exit
```

---

# 5. Découverte des périphériques Luxafor

Au démarrage, SignalMe doit découvrir **tous** les périphériques Luxafor disponibles.

La découverte doit être effectuée une seule fois pendant la phase de démarrage.

## 5.1 Aucun périphérique détecté

Si aucun périphérique n'est détecté :

```text
No Luxafor device detected.
Check that a Luxafor device is connected to a USB port.
```

SignalMe doit :

* afficher l'erreur ;
* ne pas démarrer le runtime ;
* terminer avec le code d'erreur correspondant à une erreur de périphérique.

---

# 6. Un seul périphérique détecté

Si exactement un périphérique est détecté :

* celui-ci est automatiquement sélectionné ;
* aucun tableau de sélection n'est affiché ;
* aucune confirmation utilisateur n'est demandée ;
* aucune wave d'identification n'est jouée ;
* SignalMe poursuit immédiatement son démarrage.

Exemple :

```text
SignalMe
Luxafor device detected.
Mode: manual

> 
```

---

# 7. Plusieurs périphériques détectés

À partir de deux périphériques détectés, SignalMe doit demander explicitement à l'utilisateur lequel utiliser.

## 7.1 Présentation

SignalMe affiche le nombre de périphériques détectés :

```text
3 Luxafor devices detected.
```

Puis un tableau :

```text
┌───┬──────────────────────────────┐
│ # │ Id                           │
├───┼──────────────────────────────┤
│ 1 │ <device-id-1>                │
│ 2 │ <device-id-2>                │
│ 3 │ <device-id-3>                │
└───┴──────────────────────────────┘
```

La colonne `#` est un numéro temporaire attribué par SignalMe pour cette sélection.

La colonne `Id` correspond à l'identifiant exposé par le périphérique ou par la librairie de contrôle Luxafor.

---

## 7.2 Sélection

SignalMe demande :

```text
Select device: _
```

La réponse attendue est le numéro `#`.

Exemple :

```text
Select device: 2
```

Les valeurs suivantes sont invalides :

* valeur vide ;
* texte non numérique ;
* `0` ;
* nombre négatif ;
* nombre supérieur au nombre de périphériques.

Une saisie invalide ne doit jamais arrêter SignalMe.

Exemple :

```text
Select device: 8
Invalid device number.

Select device: _
```

---

# 8. Identification visuelle du périphérique

Après sélection d'un numéro, SignalMe doit identifier visuellement le périphérique choisi.

Exemple :

```text
Select device: 2

Identifying device #2...
```

Le périphérique sélectionné joue une **wave courte d'identification**.

Cette animation ne constitue pas un mood SignalMe et ne modifie aucun statut utilisateur.

À la fin de la wave d'identification, le périphérique revient à l'état éteint en attendant la confirmation.

---

# 9. Confirmation du périphérique

Après la wave :

```text
Use this device? [Y/N]: _
```

SignalMe attend explicitement :

```text
Y
```

ou :

```text
N
```

La casse ne doit pas avoir d'importance.

`y`, `Y`, `n` et `N` sont donc valides.

Toute autre réponse entraîne une nouvelle demande.

## Réponse `N`

SignalMe retourne à :

```text
Select device: _
```

L'utilisateur peut sélectionner le même périphérique ou un autre.

La nouvelle sélection provoque une nouvelle wave.

## Réponse `Y`

Le périphérique devient le périphérique utilisé par SignalMe pendant toute cette exécution.

Exemple :

```text
Device selected: <device-id-2>
```

SignalMe poursuit alors son initialisation.

---

# 10. Propriété du périphérique

Une fois le périphérique sélectionné :

> le `SignalMeRuntime` devient l'unique propriétaire logique du périphérique Luxafor.

Aucun mode, monitor Windows ou parser de commande ne doit écrire directement sur le périphérique.

Tous les changements doivent passer par le coordinateur de SignalMe.

Les périphériques découverts mais non retenus doivent être libérés.

Le périphérique sélectionné reste ouvert pendant toute la durée d'exécution de SignalMe.

Il est libéré lors de l'arrêt.

---

# 11. Mode `manual`

Le mode manuel fonctionne directement dans la console de SignalMe.

Exemple :

```text
> signalme --mode manual

SignalMe
Mode: manual

> busy
Status: busy

> available
Status: available

> _
```

SignalMe attend continuellement une nouvelle saisie.

Conceptuellement :

```text
while SignalMe is running
    wait for console input
    interpret input
    send intent to SignalMeRuntime
```

Une ligne vide est ignorée.

Une commande inconnue affiche une erreur sans terminer SignalMe.

---

# 12. Statuts durables du mode manuel

Le mode manuel doit conserver les statuts durables déjà supportés par SignalMe :

```text
available
busy
do-not-disturb
```

Les alias existants doivent rester disponibles :

```text
free → available
dnd  → do-not-disturb
```

`away` a désormais une responsabilité particulière : il est utilisé automatiquement par SignalMe lorsque la session Windows est verrouillée.

Le mode manuel n'a donc pas besoin de demander à l'utilisateur de passer lui-même en `away` pour représenter le verrouillage du poste.

---

# 13. Signaux temporaires

Les signaux temporaires existants restent supportés :

```text
happy
bored
desperate
warning
alerting
ready
```

Exemple :

```text
> busy
Status: busy

> happy
Playing: happy
...
Restored: busy
```

Le principe reste :

```text
DesiredStatus
      ↓
temporary signal
      ↓
DesiredStatus restored
```

Le signal `ready` conserve sa sémantique particulière :

```text
ready
    ↓
animation
    ↓
DesiredStatus = available
```

Après un `ready` réussi, `available` devient donc le nouveau statut durable.

---

# 14. Commande `status`

Dans le runtime interactif :

```text
> status
```

doit permettre de connaître au minimum :

* le mode actif ;
* le statut demandé ;
* le statut effectivement affiché ;
* l'état de la session Windows.

Exemple :

```text
Mode: manual
Desired status: busy
Effective status: away
Session: locked
```

Après déverrouillage :

```text
Mode: manual
Desired status: busy
Effective status: busy
Session: active
```

---

# 15. Commande `off`

La commande :

```text
> off
```

doit :

1. annuler un éventuel signal temporaire ;
2. éteindre le périphérique ;
3. supprimer le statut durable mémorisé ;
4. placer SignalMe dans l'état `Off`.

Exemple :

```text
> off
Status: off
```

`off` signifie explicitement que l'utilisateur ne veut plus que SignalMe affiche un état de présence.

---

# 16. Commande `help`

Le runtime doit proposer :

```text
> help
```

qui affiche les commandes disponibles dans le mode courant.

Pour `manual` :

```text
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

Le contenu exact pourra être formaté avec Spectre.Console.

---

# 17. Modèle d'état

La V2 doit impérativement distinguer :

## DesiredStatus

Le statut durable voulu par le mode.

Exemples :

```text
Available
Busy
DoNotDisturb
```

ou aucun statut lorsque SignalMe est `Off`.

## SessionState

État local de la session Windows :

```text
Active
Locked
```

## EffectiveStatus

État réellement représenté par le Luxafor.

Il est calculé par SignalMe.

---

# 18. Règle fondamentale Desired / Effective

Exemple :

```text
DesiredStatus = Busy
SessionState  = Active

→ EffectiveStatus = Busy
```

Lorsque Windows est verrouillé :

```text
DesiredStatus = Busy
SessionState  = Locked

→ EffectiveStatus = Away
```

`DesiredStatus` ne change pas.

Lorsque Windows est déverrouillé :

```text
DesiredStatus = Busy
SessionState  = Active

→ EffectiveStatus = Busy
```

Il n'est donc pas nécessaire de remplacer temporairement le statut durable par `away`.

`away` est un **override de présence**.

---

# 19. Surveillance de la session Windows

SignalMe V2 reste une application Windows.

Un composant dédié doit surveiller la session Windows dans laquelle SignalMe a été lancé.

Conceptuellement :

```text
ISessionMonitor

    SessionLocked
    SessionUnlocked
```

L'implémentation Windows doit surveiller uniquement la session interactive concernée par l'instance de SignalMe.

Elle ne doit pas réagir au verrouillage ou déverrouillage d'une autre session utilisateur présente sur la machine.

L'implémentation peut reposer sur les notifications de session Windows/WTS adaptées à la session courante.

---

# 20. Verrouillage Windows

Lorsqu'un événement de verrouillage est reçu :

```text
SessionState = Locked
```

SignalMe doit immédiatement recalculer l'état effectif.

Si SignalMe est actif avec un statut :

```text
EffectiveStatus = Away
```

Exemple :

```text
DesiredStatus = Available

Windows session locked.

EffectiveStatus = Away
```

---

# 21. Déverrouillage Windows

Lorsqu'un événement de déverrouillage est reçu :

```text
SessionState = Active
```

SignalMe recalcule immédiatement l'état effectif à partir du statut durable courant.

Exemple :

```text
DesiredStatus = Busy
EffectiveStatus = Away

Windows session unlocked.

EffectiveStatus = Busy
```

---

# 22. Changement de statut pendant `away`

L'architecture doit autoriser un mode à changer son `DesiredStatus` pendant que la session est verrouillée.

Exemple conceptuel pour un futur mode automatisé :

```text
SessionState  = Locked
DesiredStatus = Busy
Effective     = Away

source → Available

DesiredStatus = Available
Effective     = Away
```

Le Luxafor reste en `away`.

Au déverrouillage :

```text
Effective = Available
```

Cette règle est essentielle pour permettre de futurs modes tels que Teams.

---

# 23. Priorités des états

Le calcul du rendu doit suivre une politique explicite.

## 23.1 Off

Lorsque SignalMe est explicitement `Off` :

```text
EffectiveStatus = Off
```

Le périphérique reste éteint.

## 23.2 Session verrouillée

Sinon, si :

```text
SessionState = Locked
```

alors :

```text
EffectiveStatus = Away
```

## 23.3 Signal temporaire

Sinon, un signal temporaire explicitement demandé peut être joué.

## 23.4 Statut durable

Sinon :

```text
EffectiveStatus = DesiredStatus
```

La priorité peut donc être résumée ainsi :

```text
Off
 │
 ├── oui ───────────────► OFF
 │
 └── non
       │
       ▼
Session locked ?
       │
       ├── oui ─────────► AWAY
       │
       └── non
             │
             ▼
Temporary signal ?
             │
             ├── oui ───► SIGNAL
             │
             └── non ───► DesiredStatus
```

---

# 24. Verrouillage pendant un signal temporaire

Un verrouillage Windows a priorité sur un mood.

Exemple :

```text
DesiredStatus = Busy

> happy

HAPPY animation running...

Windows locked
```

SignalMe doit :

1. interrompre proprement le signal temporaire ;
2. ne pas effectuer sa restauration normale ;
3. appliquer immédiatement `away`.

Résultat :

```text
DesiredStatus   = Busy
SessionState    = Locked
EffectiveStatus = Away
```

Au déverrouillage :

```text
EffectiveStatus = Busy
```

Le mood interrompu ne reprend pas.

---

# 25. Source de statut

Le runtime ne doit pas connaître les détails du mode actif.

Un mode produit des **intentions métier**.

Exemples :

```text
SetDesiredStatus(Busy)
PlaySignal(Happy)
TurnOff
```

Le runtime décide ensuite comment ces intentions influencent le périphérique en fonction de l'état global.

---

# 26. Contrat des modes

L'architecture doit introduire une abstraction équivalente à :

```csharp
public interface ISignalMeMode
{
    Task RunAsync(
        ISignalMeContext context,
        CancellationToken cancellationToken);
}
```

Le nom exact des types pourra évoluer, mais la séparation de responsabilité est obligatoire.

Le mode :

* collecte les informations permettant de déterminer le statut ;
* produit des intentions SignalMe ;
* ne connaît pas le Luxafor ;
* ne connaît pas les couleurs RGB ;
* ne pilote pas directement les LEDs ;
* ne gère pas la logique `away` liée à Windows.

---

# 27. `ManualMode`

`ManualMode` implémente `ISignalMeMode`.

Sa responsabilité est :

```text
Console
   ↓
ReadLine
   ↓
Parse
   ↓
SignalMe intent
```

Exemple :

```text
busy
```

devient :

```text
SetDesiredStatus(Busy)
```

et :

```text
happy
```

devient :

```text
PlaySignal(Happy)
```

---

# 28. Extension future `TeamsMode`

La V2 ne doit pas implémenter Teams.

Elle doit seulement rendre possible une extension future :

```shell
signalme --mode teams
```

Un futur `TeamsMode` pourrait produire :

```text
Teams available
      ↓
SetDesiredStatus(Available)

Teams busy
      ↓
SetDesiredStatus(Busy)

Teams DND
      ↓
SetDesiredStatus(DoNotDisturb)
```

La logique Windows resterait inchangée :

```text
Teams mode
     │
     ▼
DesiredStatus
     │
     ▼
StatusCoordinator ◄──── Windows session
     │
     ▼
EffectiveStatus
     │
     ▼
Luxafor
```

Ainsi, l'ajout de Teams ne doit nécessiter aucune modification de la politique de statut du Luxafor.

---

# 29. SignalMeRuntime

Le cœur de la V2 doit être représenté par un composant de durée de vie longue, par exemple :

```text
SignalMeRuntime
```

Il doit être responsable :

* du cycle de vie général ;
* du périphérique sélectionné ;
* du coordinateur d'état ;
* du mode actif ;
* du monitoring de session ;
* de la cancellation globale.

Conceptuellement :

```csharp
await runtime.RunAsync(cancellationToken);
```

Cet appel ne retourne normalement qu'à l'arrêt de SignalMe.

---

# 30. StatusCoordinator

Un composant central, par exemple :

```text
StatusCoordinator
```

doit être l'unique autorité décidant de ce que le Luxafor doit afficher.

Il reçoit :

```text
Mode
 ├── SetDesiredStatus
 ├── PlaySignal
 └── TurnOff

WindowsSessionMonitor
 ├── Locked
 └── Unlocked

Runtime
 └── Shutdown
```

et produit :

```text
Luxafor output
```

---

# 31. Sérialisation des événements

Deux sources différentes peuvent produire des événements presque simultanément.

Exemple :

```text
ManualMode → Busy
Windows    → Locked
```

Elles ne doivent jamais écrire simultanément sur le périphérique.

Toutes les opérations impactant le Luxafor doivent être sérialisées.

Le mécanisme concret peut utiliser :

* une queue ;
* un `Channel<T>` ;
* une synchronisation équivalente.

La solution ne doit pas reposer sur plusieurs composants appelant directement le device avec des `lock` dispersés.

L'ordre de traitement doit être déterministe.

---

# 32. Cancellation

Un `CancellationTokenSource` global doit être créé au démarrage.

`Ctrl+C` doit provoquer :

```text
cancellation.Cancel()
```

Le token doit couvrir **tout le cycle de vie**, y compris :

* sélection du périphérique ;
* confirmation Y/N ;
* mode manuel ;
* attente de saisie ;
* animations ;
* monitoring ;
* runtime.

---

# 33. Ctrl+C

`Ctrl+C` est le mécanisme normal d'arrêt de SignalMe.

Exemple :

```text
> busy
Status: busy

> ^C

Stopping SignalMe...
SignalMe stopped.
```

L'arrêt doit être propre.

SignalMe doit :

1. empêcher l'arrêt brutal immédiat de la console ;
2. déclencher la cancellation globale ;
3. arrêter le mode ;
4. arrêter le monitoring Windows ;
5. interrompre une animation éventuelle ;
6. éteindre le périphérique ;
7. libérer le périphérique ;
8. terminer le processus.

Le Luxafor ne doit pas rester sur un statut qui laisserait penser que SignalMe continue à surveiller la présence après l'arrêt de l'application.

---

# 34. Persistance du statut durable

La persistance actuelle du statut durable peut être conservée, mais sa signification devient précisément :

> dernier `DesiredStatus` durable choisi avec succès.

Elle ne représente jamais l'état effectif temporaire.

Exemple :

```text
DesiredStatus   = Busy
EffectiveStatus = Away
```

Le stockage contient :

```text
busy
```

et jamais :

```text
away
```

lorsque `away` provient du verrouillage Windows.

---

# 35. Démarrage avec un statut mémorisé

Après sélection du périphérique :

* SignalMe lit le dernier `DesiredStatus` mémorisé ;
* si un statut existe, il devient le `DesiredStatus` initial ;
* si aucun statut n'existe, SignalMe démarre en `Off`.

L'état de session Windows doit ensuite être pris en compte avant d'afficher quoi que ce soit.

Exemple :

```text
Stored DesiredStatus = Busy
Session = Active

→ Busy
```

Si la session est verrouillée :

```text
Stored DesiredStatus = Busy
Session = Locked

→ Away
```

---

# 36. Persistance et erreurs périphériques

Un changement de statut ne doit être persisté qu'après que le périphérique a accepté le changement requis.

Le stockage ne doit pas annoncer un statut que le Luxafor n'a jamais réussi à afficher.

Cette garantie déjà présente dans SignalMe doit être conservée.

---

# 37. Architecture cible

Une organisation possible est :

```text
SignalMe/
│
├── Commands/
│   ├── SignalMeCommandApp.cs
│   └── RunCommand.cs
│
├── Modes/
│   ├── ISignalMeMode.cs
│   ├── SignalMeModeFactory.cs
│   └── Manual/
│       └── ManualMode.cs
│
├── Runtime/
│   ├── SignalMeRuntime.cs
│   ├── SignalMeState.cs
│   ├── StatusCoordinator.cs
│   └── SignalMeIntent.cs
│
├── Sessions/
│   ├── ISessionMonitor.cs
│   └── WindowsSessionMonitor.cs
│
├── Devices/
│   ├── LuxaforDeviceDiscovery.cs
│   └── LuxaforDeviceSelector.cs
│
├── MoodPatterns/
│   └── ...
│
├── Services/
│   └── ...
│
├── Infrastructure/
│   └── ...
│
└── Program.cs
```

Cette arborescence est indicative.

La séparation des responsabilités est normative ; les noms et dossiers exacts peuvent être adaptés.

---

# 38. CLI Spectre.Console

Spectre.Console.Cli peut être conservé pour le parsing initial.

Le programme doit avoir une commande par défaut permettant :

```shell
signalme
```

et :

```shell
signalme --mode manual
```

Le parsing initial ne doit pas être confondu avec les commandes interactives tapées une fois SignalMe démarré.

Il existe donc deux niveaux distincts :

```text
Shell
│
└── signalme --mode manual
       │
       ▼
SignalMe runtime
       │
       ├── busy
       ├── available
       ├── happy
       ├── status
       └── ...
```

---

# 39. Rupture avec le modèle V1

La V2 est une évolution majeure et peut assumer une rupture de comportement.

Le modèle :

```shell
signalme as busy
```

puis fin du processus n'est plus le modèle principal de SignalMe.

Le changement de statut se fait désormais dans l'instance résidente :

```text
> signalme

> busy
> happy
> available
```

Les anciennes abstractions de commande devront être refactorées pour que leur logique métier puisse être utilisée par le runtime plutôt que liée au cycle de vie d'un processus one-shot.

---

# 40. Gestion des erreurs

## Erreur de CLI

Exemples :

* mode inconnu ;
* option invalide.

Résultat :

```text
UsageError
```

SignalMe ne démarre pas.

## Aucun device

```text
DeviceError
```

SignalMe ne démarre pas.

## Erreur d'accès Luxafor

Exemples :

* périphérique déjà utilisé ;
* périphérique débranché ;
* erreur HID.

SignalMe affiche une erreur explicite.

Une erreur critique rendant impossible le pilotage du périphérique provoque l'arrêt du runtime.

## Commande interactive inconnue

Exemple :

```text
> buzy
Unknown command: 'buzy'.
Type 'help' to list available commands.
```

Le runtime continue.

---

# 41. Déconnexion du Luxafor pendant l'exécution

Si le périphérique sélectionné devient inaccessible pendant l'exécution :

* l'erreur doit être affichée ;
* SignalMe ne doit pas prétendre avoir appliqué le changement demandé ;
* le runtime doit s'arrêter proprement si le périphérique ne peut plus être piloté.

La reconnexion ou la redécouverte dynamique de périphériques ne fait pas partie de cette V2.

---

# 42. Interface console — exemple nominal

```text
> signalme

SignalMe 2.x
Mode: manual
Luxafor device detected.

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

Windows session locked.
Effective status: away

Windows session unlocked.
Effective status: available

> busy
Status: busy

> ^C
Stopping SignalMe...
SignalMe stopped.
```

---

# 43. Interface console — exemple multi-device

```text
> signalme

SignalMe 2.x

2 Luxafor devices detected.

┌───┬──────────────────────────────┐
│ # │ Id                           │
├───┼──────────────────────────────┤
│ 1 │ <device-id-1>                │
│ 2 │ <device-id-2>                │
└───┴──────────────────────────────┘

Select device: 1

Identifying device #1...

Use this device? [Y/N]: N

Select device: 2

Identifying device #2...

Use this device? [Y/N]: Y

Device selected: <device-id-2>
Mode: manual

> _
```

---

# 44. Tests unitaires obligatoires

La refonte doit conserver une forte couverture de tests.

## Device discovery

Tester :

* zéro périphérique ;
* un périphérique ;
* plusieurs périphériques ;
* erreur pendant la découverte.

## Device selector

Tester :

* sélection d'un device parmi plusieurs ;
* valeur hors plage ;
* valeur non numérique ;
* confirmation `Y` ;
* confirmation `N` ;
* nouvelle sélection après `N` ;
* cancellation pendant la sélection ;
* libération des périphériques non sélectionnés.

## StatusCoordinator

Tester au minimum :

```text
Available + Active → Available
Busy + Active      → Busy
DND + Active       → DND

Available + Locked → Away
Busy + Locked      → Away
DND + Locked       → Away

Busy
→ Lock
→ Away
→ Unlock
→ Busy
```

## Persistance

Tester :

```text
Busy
→ Lock
→ stored = Busy

Away automatique
→ stored reste Busy
```

## Off

Tester :

```text
Busy
→ Off
→ LEDs off
→ DesiredStatus null
→ persisted status removed
```

## Moods

Tester :

```text
Busy
→ Happy
→ Busy
```

et :

```text
Busy
→ Happy
→ Lock pendant Happy
→ Happy cancelled
→ Away
→ Unlock
→ Busy
```

## Ready

Tester :

```text
Busy
→ Ready
→ Available
→ DesiredStatus = Available
→ persisted = Available
```

## Cancellation

Tester :

* `Ctrl+C` pendant l'attente console ;
* `Ctrl+C` pendant une animation ;
* `Ctrl+C` pendant la sélection de device ;
* `Ctrl+C` pendant la confirmation Y/N ;
* arrêt propre du runtime ;
* extinction du périphérique ;
* `Dispose`.

---

# 45. Tests du mode manuel

Tester :

```text
busy
available
free
do-not-disturb
dnd
happy
bored
desperate
warning
alerting
ready
status
off
help
```

Tester également :

* commande inconnue ;
* espaces avant/après ;
* casse différente ;
* ligne vide.

Exemple :

```text
 BUSY
```

doit être traité comme :

```text
busy
```

---

# 46. Testabilité

Aucune logique métier ne doit dépendre directement de :

```text
Console.ReadLine()
Console.WriteLine()
Luxafor.GetDevices()
Windows WTS
```

sans abstraction testable lorsque cette dépendance rend le comportement difficile à tester.

Le code métier doit pouvoir être testé avec :

* faux device Luxafor ;
* faux session monitor ;
* fausse entrée console ;
* faux store ;
* cancellation contrôlée.

Les tests ne doivent pas nécessiter un vrai Luxafor branché.

---

# 47. Critères d'acceptation fonctionnels

La V2 est considérée fonctionnellement terminée lorsque tous les scénarios suivants fonctionnent.

## AC-01 — lancement

```shell
signalme
```

démarre SignalMe en mode manuel et reste actif.

## AC-02 — mode explicite

```shell
signalme --mode manual
```

produit le même comportement.

## AC-03 — zéro Luxafor

Aucun Luxafor :

```text
erreur → exit
```

## AC-04 — un Luxafor

Un seul Luxafor :

```text
sélection automatique → runtime
```

## AC-05 — plusieurs Luxafor

Plusieurs Luxafor :

```text
table
→ choix #
→ wave
→ confirmation
→ runtime
```

## AC-06 — refus du device

```text
N
→ nouvelle sélection
```

## AC-07 — statut manuel

```text
> busy
```

affiche le statut `busy` et SignalMe reste actif.

## AC-08 — verrouillage

Avec :

```text
DesiredStatus = Busy
```

un verrouillage Windows donne :

```text
EffectiveStatus = Away
```

sans modifier `DesiredStatus`.

## AC-09 — déverrouillage

Après déverrouillage :

```text
EffectiveStatus = Busy
```

## AC-10 — mood

```text
Busy
→ Happy
→ Busy
```

## AC-11 — lock pendant mood

```text
Busy
→ Happy
→ Windows Lock
→ Away
→ Windows Unlock
→ Busy
```

## AC-12 — status

`status` distingue correctement :

```text
DesiredStatus
EffectiveStatus
SessionState
Mode
```

## AC-13 — off

```text
off
```

éteint le Luxafor et supprime le statut durable.

## AC-14 — Ctrl+C

`Ctrl+C` :

```text
arrête le runtime
→ éteint le Luxafor
→ dispose les ressources
→ rend la main
```

## AC-15 — pas d'accès concurrent au device

Il ne doit jamais exister deux opérations concurrentes écrivant sur le même Luxafor.

---

# 48. Hors périmètre V2

Ne font pas partie de cette version :

* intégration Microsoft Teams ;
* intégration Outlook ;
* intégration Slack ;
* agrégation de plusieurs providers ;
* mode `auto` ;
* exécution en Windows Service ;
* tray icon ;
* GUI ;
* pilotage distant de SignalMe ;
* Named Pipes ;
* API HTTP locale ;
* redécouverte dynamique après débranchement ;
* pilotage simultané de plusieurs Luxafor.

La V2 doit cependant éviter les choix architecturaux qui empêcheraient l'ajout ultérieur de nouveaux modes.

---

# 49. Résumé architectural

Le modèle cible est :

```text
                         ┌─────────────────┐
                         │   ManualMode    │
                         │                 │
                         │ console input   │
                         └────────┬────────┘
                                  │
                                  │ intents
                                  ▼
                        ┌──────────────────┐
                        │                  │
Windows Session ───────►│ StatusCoordinator│
Lock / Unlock           │                  │
                        └────────┬─────────┘
                                 │
                                 │ effective output
                                 ▼
                        ┌──────────────────┐
                        │     Luxafor      │
                        └──────────────────┘
```

Le mode fournit l'intention utilisateur.

Windows fournit le contexte de présence locale.

Le `StatusCoordinator` décide.

Le Luxafor affiche.

---

# 50. Vision finale de la V2

SignalMe V2 n'est plus :

> une commande qui change l'état d'un Luxafor.

SignalMe V2 devient :

> **une application locale résidente qui maintient en temps réel un signal de présence sur un périphérique Luxafor, à partir d'un mode de détermination du statut et du contexte de la session Windows.**

La première implémentation est volontairement simple :

```text
mode = manual
+
Windows lock/unlock
+
Luxafor
```

mais le découpage doit permettre demain :

```text
mode = teams
+
Windows lock/unlock
+
Luxafor
```

sans modifier le moteur central de gestion de présence.
