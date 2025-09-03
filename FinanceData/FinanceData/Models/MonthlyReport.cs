using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceData.Models
{
    public class MonthlyReport
    {
        /// <summary>
        /// La dernière entrée de solde enregistrée pour ce mois.
        /// </summary>
        public BalanceEntry LastEntryOfMonth { get; set; }

        /// <summary>
        /// L'évolution de la valeur par rapport au dernier solde du mois précédent.
        /// </summary>
        public decimal EvolutionValue { get; set; }

        /// <summary>
        /// L'évolution en pourcentage par rapport au dernier solde du mois précédent.
        /// </summary>
        public decimal EvolutionPercentage { get; set; }

        // Un constructeur pour s'assurer que LastEntryOfMonth n'est jamais nul.
        public MonthlyReport(BalanceEntry lastEntry)
        {
            LastEntryOfMonth = lastEntry;
        }
    }
}
