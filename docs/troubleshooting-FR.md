_[English version](troubleshooting.md)_

# Dépannage

SignalMe signale chaque échec sur stderr et sort avec un
[code distinct](commands-FR.md#codes-de-sortie). Pendant qu'il tourne, ce qu'il affiche dans la console
vous dit ce que le périphérique montre et pourquoi.

Les messages de SignalMe sont en anglais ; ils sont repris tels quels ci-dessous.

## Unknown mode

**Symptôme :** `Unknown mode: 'foo'.` suivi de `Available modes: manual`, code de sortie `1`.

**Cause :** la valeur donnée à `--mode` n'est pas un mode que SignalMe connaît. SignalMe 2.0 n'en a qu'un,
`manual`, qui est aussi celui par défaut.

**Que faire :** lancez `signalme` sans option, ou `signalme --mode manual`.

## Unknown option, Unknown command (ligne de commande)

**Symptôme :** `Unknown option: '--bogus'.` ou `Unknown command 'extra'.` (sans deux-points), suivi de
`Type 'signalme --help' for usage.`, code de sortie `1`. Un `--mode` sans valeur reçoit la même indication,
et un argument placé après `--` donne `Unexpected argument: 'extra'.`.

**Cause :** la ligne de commande n'est pas une de celles que SignalMe accepte. La seule option est
`--mode <MODE>`, avec `-m` pour forme courte ; il n'y a pas d'argument — ni de sous-commande, c'est
pourquoi un argument en trop est signalé comme une commande inconnue. Ce n'est pas le
[`Unknown command: 'buzy'.`](#unknown-command) de l'invite, qui a un deux-points, est suivi de
`Type 'help' to list available commands.` et laisse SignalMe tourner.

**Que faire :** `signalme --help` affiche l'usage. Les commandes auxquelles vous pensez peut-être — `as`,
`status`, `off` — étaient la ligne de commande de SignalMe 1.x (`signalme as busy` affiche désormais
`Unknown command 'as'.`) ; en 2.0, statuts et signaux se tapent une fois SignalMe lancé, voir
[Référence des commandes](commands-FR.md#commandes-interactives).

## No Luxafor device detected

**Symptôme :** `No Luxafor device detected.` et code de sortie `2`.

**Cause :** la librairie a énuméré les périphériques USB HID et n'a trouvé aucun Luxafor parmi eux.

Vérifiez que :

1. le périphérique est branché sur un port USB ;
2. vous êtes sous Windows — SignalMe ne peut détecter aucun périphérique sur une autre plateforme, voir
   [Matériel et plateformes](hardware-FR.md) ;
3. le périphérique fonctionne ailleurs, par exemple dans le logiciel de Luxafor — fermez ensuite ce
   logiciel, il détiendrait le périphérique.

## Could not reach a Luxafor device

**Symptôme :** `Could not reach a Luxafor device:` suivi d'un type d'exception et d'un message, code de
sortie `2`.

**Cause :** quelque chose a échoué avant que l'énumération n'aboutisse. Le message nomme la cause ; les
deux plus courantes sont une autre application qui détient le périphérique, et une exécution hors de
Windows, où la librairie HID ne peut tout simplement pas se charger.

**Que faire :** fermez l'autre application qui pilote le périphérique, puis réessayez. Le reste du message
est l'erreur sous-jacente, à citer dans une issue si ce n'est ni l'un ni l'autre.

## Invalid device number

**Symptôme :** `Invalid device number.` et de nouveau la question `Select device:`.

**Cause :** plusieurs périphériques ont été détectés et la réponse n'était pas un des numéros de la colonne
`#` — vide, pas un nombre, `0`, ou trop grand.

**Que faire :** tapez le numéro de la ligne. Rien n'est perdu : SignalMe redemande jusqu'à obtenir un
numéro valide. Voir [Matériel et plateformes](hardware-FR.md#plusieurs-périphériques) pour le dialogue
complet.

## Unknown command

**Symptôme :** `Unknown command: 'buzy'.` et `Type 'help' to list available commands.`, SignalMe continue.

**Cause :** la ligne tapée à l'invite n'est ni un statut, ni un signal, ni une commande du mode courant.
La saisie est d'abord débarrassée de ses espaces et passée en minuscules : majuscules et espaces ne sont
pas le problème.

`away` revient souvent : c'était un statut dans SignalMe 1.x, et il ne se tape plus. SignalMe l'affiche de
lui-même pendant que votre session Windows est verrouillée.

**Que faire :** `help` liste ce que le mode accepte.

## Session locked : le signal n'est pas joué

**Symptôme :** `Session locked: 'happy' is not played.` — le périphérique reste violet.

**Cause :** SignalMe considère que la session Windows dans laquelle il tourne est verrouillée, et `away` a
priorité sur les signaux. C'est attendu, et non un échec, tant que l'écran est verrouillé. Si vous tapez à
l'invite, la session depuis laquelle vous tapez n'est pas celle que SignalMe surveille, ou bien il n'a pas
vu le déverrouillage : `status` montre ce qu'il croit.

**Que faire :** rien, une fois la session déverrouillée les signaux se jouent de nouveau. Si SignalMe
continue d'annoncer `Session: locked` pendant que vous tapez, voir
[le verrouillage n'est pas remarqué](#signalme-ne-remarque-pas-le-verrouillage-ou-le-déverrouillage).

## SignalMe is off : le signal n'est pas joué

**Symptôme :** `SignalMe is off: 'happy' is not played.` — le périphérique reste éteint.

**Cause :** un signal se joue par-dessus un statut durable et le remet en place ensuite, et il n'y en a
pas : SignalMe a démarré sans rien de mémorisé, ou `off` a été tapé. Éteint a priorité sur tout, signaux
compris. Dans SignalMe 1.x un signal se jouait par-dessus un périphérique éteint ; ce n'est plus le cas.

**Que faire :** tapez d'abord un statut — `busy`, par exemple — puis le signal.

## Luxafor device disconnected

**Symptôme :** `Luxafor device disconnected.`, puis `Stopping SignalMe...` et `SignalMe stopped.`, code
de sortie `2`.

**Cause :** SignalMe vérifie toutes les deux secondes que le périphérique qu'il pilote est toujours
branché, et il ne l'était plus : débranché, derrière un hub USB ou une station d'accueil privés de
courant, ou coupé par la mise en veille du portable. SignalMe s'arrête plutôt que d'annoncer un statut
qu'aucun périphérique n'affiche, et n'écrit rien sur le périphérique en sortant, puisqu'il a disparu.

**Que faire :** rebranchez le périphérique et relancez SignalMe. Il repart du dernier statut que le
périphérique a réellement affiché.

## Le périphérique a refusé une commande, ou n'a pas pu être joint, pendant l'exécution

**Symptôme :** `The Luxafor device refused to ...` ou `The Luxafor device could not be reached: ...`, puis
`Stopping SignalMe...` et `SignalMe stopped.`, code de sortie `2`.

**Cause :** le périphérique a été trouvé au démarrage, mais une écriture ultérieure a échoué — typiquement
repris par une autre application, ou débranché trop brièvement pour que la vérification le voie partir :
le handle que tient SignalMe ne survit pas, même à une absence brève. SignalMe ne continue pas sans
périphérique à piloter, et ne prétend pas que le dernier changement a été appliqué : un statut refusé
n'est pas mémorisé.

**Que faire :** rebranchez le périphérique, fermez l'application qui l'a pris, et relancez SignalMe. Il
repart du dernier statut que le périphérique a réellement affiché.

## Le périphérique est resté allumé après l'arrêt de SignalMe

**Symptôme :** `The Luxafor device refused to turn its LEDs off.` ou
`The Luxafor device could not be turned off: ...` juste avant `SignalMe stopped.`.

**Cause :** SignalMe éteint le périphérique quand il s'arrête, pour qu'un périphérique allumé ne laisse
jamais croire qu'il surveille encore votre présence, et cette dernière commande a échoué — le périphérique
avait très probablement déjà disparu.

**Que faire :** débranchez le périphérique, ou relancez SignalMe et arrêtez-le. Le code de sortie est
celui de ce qui a arrêté SignalMe ; cet échec-là ne le change pas.

## SignalMe ne remarque pas le verrouillage, ou le déverrouillage

**Symptôme :** la session est verrouillée mais le périphérique garde la couleur de votre statut, ou
`status` annonce `Session: active` après un verrouillage.

**Cause :** SignalMe ne voit que l'état de verrouillage de la session Windows dans laquelle il tourne.
Lancé comme service, ou par une tâche planifiée qui s'exécute en arrière-plan, il tourne dans la session 0
et ne reçoit jamais le verrouillage d'un utilisateur. Sur une autre plateforme que Windows, la session est
supposée active et ne change jamais.

**Que faire :** lancez `signalme` depuis un terminal dans votre propre session interactive.

## `status` ne correspond pas aux LED

**Symptôme :** `status` annonce `Effective status: busy`, le périphérique affiche autre chose.

**Cause :** `status` rapporte ce que SignalMe a demandé. Les périphériques Luxafor ne se relisent pas ; si
une autre application a piloté le périphérique entre-temps, SignalMe n'a aucun moyen de le savoir.

**Que faire :** retapez le statut — `busy` — pour remettre le périphérique en accord.

## SignalMe démarre éteint alors qu'un statut avait été défini

**Symptôme :** `Status: off` au démarrage alors que vous attendiez le statut précédent.

**Cause :** le statut durable est mémorisé dans `%LOCALAPPDATA%\SignalMe\signalme.ini`, et ce fichier est
absent, vide, ou contient quelque chose que SignalMe 2.0 ne reconnaît pas — en particulier `away`, que
SignalMe 1.x pouvait écrire et qui veut désormais dire « pas de statut ». Chacun de ces cas est lu comme
éteint plutôt que traité comme une erreur. `off` supprime le fichier à dessein.

**Que faire :** retapez le statut. Supprimer le fichier est sans risque et fait simplement oublier
SignalMe.

## signalme : une erreur inattendue

**Symptôme :** `signalme: <Type>: <message>` et code de sortie `3`.

**Cause :** quelque chose que SignalMe n'avait pas prévu, nommé par le type de l'exception. Le
périphérique a tout de même été éteint et libéré.

**Que faire :** [ouvrez une issue](https://github.com/Reefact/signalme/issues) avec le message.

## `dotnet tool install` n'est pas reconnu

**Symptôme :** la commande d'installation échoue avant même que SignalMe n'entre en jeu.

**Cause :** `dotnet tool install` est fourni avec le SDK .NET, pas avec le runtime seul.

**Que faire :** installez le [SDK .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0).
