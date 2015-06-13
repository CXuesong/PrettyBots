using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrettyBots.Strategies.Repository
{
    public class AccountRepository : ChildRepository
    {
        public IEnumerable<Account> GetAccounts()
        {
            return DataContext.Account;
        }

        public IEnumerable<Account> GetAccounts(string domain)
        {
            return DataContext.Account.Where(a => a.Domain == domain);
        }

        public void InsertAccount(Account account)
        {
            DataContext.Account.InsertOnSubmit(account);
            DataContext.SubmitChanges();
        }

        public void RemoveAccount(Account account)
        {
            DataContext.Account.DeleteOnSubmit(account);
            DataContext.SubmitChanges();
        }

        public void RemoveAccount(string domain, string userName)
        {
            DataContext.Account.DeleteAllOnSubmit(
                DataContext.Account.Where(a => a.Domain == domain && a.UserName == userName));
            DataContext.SubmitChanges();
        }

        internal AccountRepository(PrimaryRepository parent)
            :base(parent)
        { }
    }
}
