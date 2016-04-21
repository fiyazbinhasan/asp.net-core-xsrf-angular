namespace Angular.Net.Core.XSRF.Models
{
    public class Transaction
    {
        public decimal TransactionAmount { get; set; }
        public string TransactionType { get; set; }
        public Account Account { get; set; }
    }
}
