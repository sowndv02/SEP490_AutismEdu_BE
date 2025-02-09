﻿using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace AutismEduConnectSystem.Repository
{
    public class SyllabusRepository : Repository<Syllabus>, ISyllabusRepository
    {
        private readonly ApplicationDbContext _context;
        public SyllabusRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Syllabus> UpdateAsync(Syllabus model)
        {
            try
            {
                _context.Syllabuses.Update(model);
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
