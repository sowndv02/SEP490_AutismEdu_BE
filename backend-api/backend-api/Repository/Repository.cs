using backend_api.Data;
using backend_api.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Linq;

namespace backend_api.Repository
{
   
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context;
        internal DbSet<T> dbset;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            this.dbset = _context.Set<T>();
        }

        public async Task CreateAsync(T entity)
        {
            await dbset.AddAsync(entity);
            await SaveAsync();
        }

        public async Task<T> GetAsync(Expression<Func<T, bool>> filter = null, bool tracked = true, string? includeProperties = null, string? excludeProperties = null)
        {
            IQueryable<T> query = dbset;
            if (!tracked)
                query = query.AsNoTracking();

            if (filter != null)
                query.Where(filter);
            if (includeProperties != null)
            {
                var includeProps = includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                if (excludeProperties != null)
                {
                    var excludeProps = excludeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    includeProps = includeProps.Except(excludeProps).ToList();
                }

                foreach (var includeProp in includeProps)
                {
                    query = query.Include(includeProp);
                }
            }

            return await query.FirstOrDefaultAsync(filter);
        }

        public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, string? includeProperties = null, string? excludeProperties = null)
        {
            IQueryable<T> query = dbset;
            if (filter != null)
                query = query.Where(filter);
            if (includeProperties != null)
            {
                var includeProps = includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                if (excludeProperties != null)
                {
                    var excludeProps = excludeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    includeProps = includeProps.Except(excludeProps).ToList();
                }

                foreach (var includeProp in includeProps)
                {
                    query = query.Include(includeProp);
                }
            }


            return await query.ToListAsync();
        }

        public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, string? includeProperties = null,
            int pageSize = 0, int pageNumber = 1)
        {
            IQueryable<T> query = dbset;
            if (filter != null)
                query = query.Where(filter);
            if (pageSize > 0)
            {
                if (pageSize > 100)
                {
                    pageSize = 100;
                }
                query = query.Skip(pageSize * (pageNumber - 1)).Take(pageSize);
            }
            if (includeProperties != null)
            {
                var includeProps = includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                foreach (var includeProp in includeProps)
                {
                    query = query.Include(includeProp);
                }
            }


            return await query.ToListAsync();
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task RemoveAsync(T entity)
        {
            dbset.Remove(entity);
            await SaveAsync();
        }
    }
}
