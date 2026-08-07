_[English version](troubleshooting.md)_

# Dépannage

SignalMe signale chaque échec sur stderr et sort avec un
[code distinct](commands-FR.md#codes-de-sortie). Si une commande n'a rien dit et est sortie en `0`, c'est
qu'elle a fonctionné.

Les messages de SignalMe sont en anglais ; ils sont repris tels quels ci-dessous.

## No Luxafor device detected

**Symptôme :** `No Luxafor device detected.` et code de sortie `2`.

**Cause :** la librairie a énuméré les périphériques USB HID et n'a trouvé aucun Luxafor parmi eux.

Vérifiez que :

1. le périphérique est branché sur un port USB ;
2. vous êtes sous Windows — SignalMe ne peut détecter aucun périphérique sur une autre plateforme, voir
   [Matériel et plateformes](hardware-FR.md) ;
3. le périphérique fonctionne ailleurs, par exemple dans le logiciel de Luxafor.

## Could not reach a Luxafor device

**Symptôme :** `Could not reach a Luxafor device:` suivi d'un type d'exception et d'un message, code de
sortie `2`.

**Cause :** quelque chose a échoué avant que l'énumération n'aboutisse. Le message nomme la cause ; les
deux plus courantes sont une autre application qui détient le périphérique, et une exécution hors de
Windows, où la librairie HID ne peut tout simplement pas se charger.

**Que faire :** fermez l'autre application qui pilote le périphérique, puis réessayez. Le reste du message
est l'erreur sous-jacente, à citer dans une issue si ce n'est ni l'un ni l'autre.

## The Luxafor device refused to ...

**Symptôme :** `The Luxafor device refused to ...` et code de sortie `2`.

**Cause :** le périphérique a été trouvé mais a rejeté une écriture — typiquement débranché en cours de
commande, ou repris par une autre application pendant l'exécution de SignalMe.

**Que faire :** relancez la commande. Si un signal temporaire a été interrompu de cette façon, votre statut
durable a été rétabli au préalable : rien ne reste appliqué à moitié.

## `signalme status` ne correspond pas aux LED

**Symptôme :** `status` annonce `busy`, le périphérique affiche autre chose.

**Cause :** c'est attendu. `status` rapporte ce que SignalMe a demandé en dernier. Les périphériques
Luxafor ne se relisent pas ; si une autre application a piloté le périphérique ensuite, SignalMe n'a aucun
moyen de le savoir.

**Que faire :** redéfinissez le statut — `signalme as busy` — pour remettre le périphérique en accord avec
ce que SignalMe mémorise.

## Un signal a été interrompu

**Symptôme :** `Interrupted. The previous status was restored.` et code de sortie `4`.

**Cause :** vous avez fait Ctrl+C pendant un signal temporaire.

**Que faire :** rien. L'animation s'est arrêtée et votre statut durable a été remis en place.

## Le fichier de statut durable est absent ou illisible

**Symptôme :** `signalme status` affiche `No durable status is currently set.` alors que vous attendiez un
statut.

**Cause :** le fichier est absent, vide, ou contient quelque chose que SignalMe ne reconnaît pas. Chacun de
ces cas est lu comme « pas de statut » plutôt que traité comme une erreur.

**Que faire :** redéfinissez le statut. Le fichier se trouve dans `%LOCALAPPDATA%\SignalMe\signalme.ini` ;
le supprimer est sans risque et fait simplement oublier SignalMe.

## `dotnet tool install` n'est pas reconnu

**Symptôme :** la commande d'installation échoue avant même que SignalMe n'entre en jeu.

**Cause :** `dotnet tool install` est fourni avec le SDK .NET, pas avec le runtime seul.

**Que faire :** installez le [SDK .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0).
