using System;

namespace AutoTrader.SpeedTrading
{
    /// <summary>
    /// Kern der Speed-Idee: entscheidet rein rechnerisch, wie viele freie
    /// Reittiere die Party halten sollte, um die Kartengeschwindigkeit zu
    /// maximieren -- ohne in den Herd-Malus zu laufen.
    ///
    /// Zugrundeliegende Vanilla-Mechanik (DefaultPartySpeedCalculatingModel):
    ///  - Jeder Fusssoldat, der ein freies Reittier aufsitzen kann, gibt einen
    ///    Geschwindigkeitsbonus. Optimal ist also ~1 freies Reittier je Fusssoldat.
    ///  - Reittiere/Packtiere oberhalb einer Schwelle (~ MemberCount * 1.05)
    ///    erzeugen einen zunehmenden Herd-Malus.
    /// Quelle der Kennzahlen: Party-Speed-Analysen der Community; die exakten
    /// Faktoren gehoeren gegen die dekompilierte v1.4.7 verifiziert (siehe NOTES.md).
    /// </summary>
    public sealed class PartySpeedAdvisor
    {
        /// <summary>Sicherheitsabstand unter der Herd-Schwelle (Anzahl Tiere).</summary>
        private const int HerdSafetyMargin = 2;

        /// <summary>Multiplikator fuer die Herd-Schwelle relativ zur Party-Groesse.</summary>
        private readonly float _herdThresholdFactor;

        public PartySpeedAdvisor(float herdThresholdFactor = 1.05f)
        {
            _herdThresholdFactor = herdThresholdFactor;
        }

        /// <summary>
        /// Anzahl freier Reittiere, unterhalb derer kein Herd-Malus entsteht.
        /// </summary>
        public int HerdThreshold(in PartySnapshot p)
        {
            return (int)Math.Floor(p.MemberCount * _herdThresholdFactor);
        }

        /// <summary>
        /// Ideale Anzahl freier Reittiere: genug um alle Fusssoldaten aufsitzen
        /// zu lassen, aber sicher unterhalb der Herd-Schwelle.
        /// </summary>
        public int TargetSpareMounts(in PartySnapshot p)
        {
            int safeCap = Math.Max(0, HerdThreshold(in p) - HerdSafetyMargin);
            int desiredForMounting = p.FootTroopCount;
            return Math.Min(desiredForMounting, safeCap);
        }

        /// <summary>Leitet aus dem Schnappschuss eine Kauf-/Verkaufsempfehlung ab.</summary>
        public MountRecommendation Recommend(in PartySnapshot p)
        {
            int target = TargetSpareMounts(in p);
            int threshold = HerdThreshold(in p);

            // Zu viele Tiere -> Herd-Malus: Ueberschuss ueber die Schwelle abstossen.
            if (p.SpareMountCount > threshold)
            {
                int sell = p.SpareMountCount - target;
                return new MountRecommendation(0, sell,
                    $"Herd-Malus: {p.SpareMountCount} Reittiere > Schwelle {threshold}, {sell} verkaufen (Ziel {target}).");
            }

            // Zu wenige Tiere, um alle Fusssoldaten zu beritten -> nachkaufen.
            if (p.SpareMountCount < target)
            {
                int buy = target - p.SpareMountCount;
                return new MountRecommendation(buy, 0,
                    $"Speed-Bonus: {buy} Reittiere kaufen (habe {p.SpareMountCount}, Ziel {target} fuer {p.FootTroopCount} Fusssoldaten).");
            }

            return new MountRecommendation(0, 0, "Reittier-Bestand bereits im Speed-Optimum.");
        }
    }
}

