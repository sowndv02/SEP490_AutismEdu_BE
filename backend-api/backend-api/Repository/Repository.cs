using backend_api.Data;
using backend_api.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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

        public async Task<T> CreateAsync(T entity)
        {
            await dbset.AddAsync(entity);
            await SaveAsync();
            return entity;
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

        public async Task<(int TotalCount, List<T> list)> GetAllNotPagingAsync(Expression<Func<T, bool>>? filter = null, string? includeProperties = null, string? excludeProperties = null)
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

            return (query.ToListAsync().GetAwaiter().GetResult().Count, await query.ToListAsync());
        }

        public async Task<(int TotalCount, List<T> list)> GetAllAsync(Expression<Func<T, bool>>? filter = null, string? includeProperties = null,
            int pageSize = 0, int pageNumber = 1, Expression<Func<T, object>>? orderBy = null, bool isDesc = true)
        {
            IQueryable<T> query = dbset;
            if (filter != null)
            {
                query = query.Where(filter);
            }
            int totalCount = await query.CountAsync();
            if (pageSize > 0)
            {
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
            if (orderBy != null)
            {
                if (isDesc)
                    query = query.OrderByDescending(orderBy);
                else
                    query = query.OrderBy(orderBy);
            }
            return (totalCount, await query.ToListAsync());
        }

        public async Task<(int TotalCount, List<T> list)> GetAllWithIncludeAsync(Expression<Func<T, bool>>? filter = null, string? includeProperties = null,
            int pageSize = 0, int pageNumber = 1, Expression<Func<T, object>>? orderBy = null, bool isDesc = true)
        {
            IQueryable<T> query = dbset;
            if (includeProperties != null)
            {
                var includeProps = includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                foreach (var includeProp in includeProps)
                {
                    query = query.Include(includeProp);
                }
            }
            if (filter != null)
            {
                query = query.Where(filter);
            }
            int totalCount = await query.CountAsync();
            if (pageSize > 0)
            {
                query = query.Skip(pageSize * (pageNumber - 1)).Take(pageSize);
            }

            if (orderBy != null)
            {
                if (isDesc)
                    query = query.OrderByDescending(orderBy);
                else
                    query = query.OrderBy(orderBy);
            }
            return (totalCount, await query.ToListAsync());
        }

        public async Task SaveAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task RemoveAsync(T entity)
        {
            dbset.Remove(entity);
            await SaveAsync();
        }
    }
}
