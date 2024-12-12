using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;
using MailKit.Search;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Expressions;

namespace AutismEduConnectSystem.Repository
{
    public class StudentProfileRepository : Repository<StudentProfile>, IStudentProfileRepository
    {
        private readonly ApplicationDbContext _context;

        public StudentProfileRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<StudentProfile> UpdateAsync(StudentProfile model)
        {
            try
            {
                _context.StudentProfiles.Update(model);
                await _context.SaveChangesAsync();
                return model;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
