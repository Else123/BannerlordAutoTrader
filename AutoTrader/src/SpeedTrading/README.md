# Speed-Aware Mount Trading (Feature-Branch `feature/speed-aware-mounts`)

Erweiterung des AutoTraders: Reittiere so handeln, dass die **Party-Karten-
geschwindigkeit maximiert** wird — statt der bisherigen groben "halte Tiere
unter Party-Groesse"-Regel.

## Ausgangslage im Upstream-Code

Eskalior hat Reittier-Handel angefangen und wieder stillgelegt:

- `AutoTraderLogic.PerformAutoTrade` Zeile 68: `//BuyHorses();` (auskommentiert)
- `AutoTraderLogic` Zeilen ~249-295: `BuyHorses()` / `BuyHorseFilter()` (komplett auskommentiert)
- `AutoTraderSpecialRules.CheckBuyHorsesRules` (Zeile 31): kauft nur Packtiere,
  Regel `NumPartyMembers > NumLivestockAnimals`, mit `// TODO: Add max herding setting`
- `AutoTraderLogic.CanSell` (Zeile ~477): schuetzt Packtiere pauschal vor Verkauf

### Was fehlt (= unser Mehrwert)

1. **Speed-Optimum**: genug freie Reittiere, um Fusssoldaten aufsitzen zu lassen
   (Vanilla-Speed-Bonus je berittenem Fusssoldaten).
2. **Herd-Schwelle**: Tiere oberhalb ~`MemberCount * 1.05` erzeugen Malus -> nicht
   ueberkaufen, Ueberschuss abstossen (genau Eskaliors offenes TODO).
3. **Reit- vs. Packtier**: `NumberOfLivestockAnimals` = Vieh (Kuh/Schaf), NICHT Pferde.
   Reitpferde muessen separat gezaehlt werden.
4. **Verkauf von Herd-Ueberschuss** statt pauschalem Packtier-Schutz.
5. **Kein unbegrenztes Anhaeufen**: aktuell koennen Reit-/Packtiere endlos
   dazukommen -> Party wird immer langsamer. Das Speed-Optimum MUSS immer als
   Obergrenze wirken.

## Sonderfall: Schiffskapazitaet (Warsails-Flottenmodus)

Wenn der Spieler `UseMaxFleetCapacityValue` aktiviert (nur Schiffskapazitaet zaehlt,
`WarsailsHelper.GetFleetCargoCapacity` / `GetFleetTotalWeightCarried`), dann:

- Die **Mount-Obergrenze an die Schiffskapazitaet koppeln**, sodass geladene Tiere
  und Fracht grob im Gleichgewicht bleiben (nicht die Kartengeschwindigkeit der
  Land-Party optimieren, waehrend die Fracht auf Schiffen liegt).
- Das **Speed-Optimum bleibt trotzdem aktiv** als Obergrenze: keine endlose
  Mount-Akkumulation, auch nicht im Flottenmodus.
- Praktisch: In `PartySnapshot`/`PartySpeedAdvisor` einen Kapazitaetsbezug ergaenzen
  (Ziel = min(Fusssoldaten, Herd-Schwelle, kapazitaetsbasierte Obergrenze)) und im
  Flottenmodus die kapazitaetsbasierte Obergrenze aus der Schiffskapazitaet ableiten.

## Entscheidungslogik (bereits vorbereitet, engine-frei, testbar)

- `PartySnapshot.cs` — Datenschnappschuss (Members, Fusssoldaten, freie Reittiere, Gewicht, Kapazitaet)
- `MountRecommendation.cs` — Ergebnis: BuyCount / SellCount / Begruendung
- `PartySpeedAdvisor.cs` — Kernheuristik: Zielbestand = min(Fusssoldaten, Herd-Schwelle - Marge)

Diese Klassen sind noch NICHT im `AutoTrader.csproj` (bewusst, bis verdrahtet).

## Integrationsschritte (wenn Toolchain steht)

1. **`ILogicConnector` + `AutoTraderLogicConnector` erweitern** um die fehlenden Primitive:
   - `int GetNumFootTroops()` -> aus `MemberRoster` (nicht-berittene Formationsklassen)
   - `int GetNumSpareRidingMounts()` -> Inventar-Pferde mit `HorseComponent.IsPackAnimal == false`
   (Gewicht/Kapazitaet/PartyMembers gibt es schon.)
2. **`AutoTraderSpecialRules.CheckBuyHorsesRules`** durch `PartySpeedAdvisor`-Aufruf ersetzen
   (Snapshot bauen -> `Recommend` -> BuyCount > 0 ?).
3. **Verkaufspfad** in `CanSell` fuer Herd-Ueberschuss ergaenzen (SellCount > 0).
4. **`BuyHorses()` reaktivieren** (Eskaliors Geruest) ODER in den bestehenden
   `CanBuy`-Horse-Zweig falten — eine Variante waehlen, nicht beide.
5. **Config** in `AutoTraderConfig` + MCM-GUI: Feature-Toggle, Herd-Schwellen-%,
   Mindest-Reserve an Reittieren.
6. **Verifikation**: Build gruen, im Spiel Stadt betreten -> Reittier-Kauf/-Verkauf
   bewegt Party-Speed messbar Richtung Optimum.

## Verifikations-Hinweis (API bereits belegt)

Aus dem Upstream-Connector bestaetigt (kein Rateanteil mehr):
- Handel laeuft ueber `TransferCommand.Transfer(...)` + `InventoryLogic`
- Packtier-Erkennung: `item.HorseComponent.IsPackAnimal`
- Party-Groesse: `PartyBase.MainParty.NumberOfAllMembers`
- Viehzahl: `ItemRoster.NumberOfLivestockAnimals`
- Gewicht/Kapazitaet: mit Warsails-Flotten-Sonderfall (`WarsailsHelper`)
