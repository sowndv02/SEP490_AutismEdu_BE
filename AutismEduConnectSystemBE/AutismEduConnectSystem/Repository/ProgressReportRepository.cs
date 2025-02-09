﻿using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;

namespace AutismEduConnectSystem.Repository
{
    public class ProgressReportRepository : Repository<ProgressReport>, IProgressReportRepository
    {
        private readonly ApplicationDbContext _context;

        public ProgressReportRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<ProgressReport> UpdateAsync(ProgressReport model)
        {
            try
            {
                _context.ProgressReports.Update(model);
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
