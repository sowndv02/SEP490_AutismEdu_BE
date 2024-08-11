using backend_api.Data;
using backend_api.Services.IServices;

namespace backend_api.Services
{
    public class NumberOfDaysForAccount : INumberOfDaysForAccount
    {
        private readonly ApplicationDbContext _db;
        public NumberOfDaysForAccount(ApplicationDbContext db)
        {
            _db = db;
        }
        public int Get(string userId)
        {
            var user = _db.ApplicationUsers.FirstOrDefault(u => u.Id == userId);
            if (user != null && user.CreatedDate != DateTime.MinValue)
            {
                return (DateTime.Today - user.CreatedDate).Days;
            }
            return 0;
        }
    }
}
