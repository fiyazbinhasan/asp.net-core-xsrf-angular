using System.Collections.Generic;
using Angular.Net.Core.XSRF.Models;

namespace Angular.Net.Core.XSRF.Repository
{
    public class TransactionRepository
    {
        private readonly List<Transaction> _transactions;
        private readonly Account _account;

        public TransactionRepository()
        {
            _transactions = new List<Transaction>();

            _account = new Account()
            {
                AccountNumber = "1234",
                CurrentBalance = 1000
            };
        }

        public IEnumerable<Transaction> GetTransactions()
        {
            return _transactions;
        }

        public void AddTransaction(Transaction transaction)
        {
            if (_account.AccountNumber == transaction.Account.AccountNumber)
            {
                if (transaction.TransactionType.Equals("DEBIT"))
                {
                    _account.CurrentBalance = _account.CurrentBalance - transaction.TransactionAmount;

                }
                else
                {
                    _account.CurrentBalance = _account.CurrentBalance + transaction.TransactionAmount;
                }

                _transactions.Add(transaction);
            }
        }
    }
}
