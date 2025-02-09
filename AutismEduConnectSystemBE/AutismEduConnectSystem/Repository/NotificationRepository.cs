﻿using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AutismEduConnectSystem.Repository
{
    public class NotificationRepository : Repository<Notification>, INotificationRepository
    {
        private readonly ApplicationDbContext _context;

        public NotificationRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public Task<int> TotalUnRead(Expression<Func<Notification, bool>>? filter = null)
        {
            
            IQueryable<Notification> query = dbset;
            if (filter != null)
                query = query.Where(filter);
            return query.CountAsync();
        }

        public async Task<Notification> UpdateAsync(Notification model)
        {
            try
            {
                _context.Notifications.Update(model);
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
