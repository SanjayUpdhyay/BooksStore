using BookStore.DataAccess.Data;
using BookStore.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.DataAccess.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context;

        private readonly DbSet<T> _DbSet;
        public Repository(ApplicationDbContext context)
        {
            _context = context;
            this._DbSet = _context.Set<T>();
        }
        public void Add(T entity)
        {
            _DbSet.Add(entity);
        }

        public T Get(Expression<Func<T, bool>> expression)
        {
            IQueryable<T> query = _DbSet;
            query =  query.Where(expression);

            #pragma warning disable CS8603 // Possible null reference return.
            return query.FirstOrDefault();
            #pragma warning restore CS8603 // Possible null reference return.
        }

        public IEnumerable<T> GetAll()
        {
            IQueryable<T> query = _DbSet;
            return query.ToList();
        }

        public void Remove(T entity)
        {
            _DbSet.Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entity)
        {
            _DbSet.RemoveRange(entity);
        }
    }
}
