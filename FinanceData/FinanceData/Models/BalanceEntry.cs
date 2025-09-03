using SQLite;

namespace FinanceData.Models
{
    public class BalanceEntry
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public decimal Value { get; set; }

        public DateTime Date { get; set; }

        [Indexed]
        public int AccountId { get; set; }
    }
}
