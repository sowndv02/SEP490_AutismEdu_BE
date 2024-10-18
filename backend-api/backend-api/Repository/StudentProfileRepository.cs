using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;
using MailKit.Search;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Expressions;

namespace backend_api.Repository
{
    public class StudentProfileRepository : Repository<StudentProfile>, IStudentProfileRepository
    {
        private readonly ApplicationDbContext _context;

        public StudentProfileRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
