using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories.Impl {
	public class Repository<T> : IRepository<T> where T : DbEntity {
		protected MitternachtContext _context;
		protected DbSet<T> _set;

		public Repository(MitternachtContext context) {
			_context = context;
			_set = context.Set<T>();
		}

		public void Add(T obj)
			=> _set.Add(obj);

		public void AddRange(params T[] objects)
			=> _set.AddRange(objects);

		public T Get(int id)
			=> _set.FirstOrDefault(e => e.Id == id);

		public IQueryable<T> GetAll()
			=> _set.AsQueryable();

		public void Remove(int id)
			=> _set.Remove(Get(id));

		public void Remove(T obj)
			=> _set.Remove(obj);

		public void RemoveRange(params T[] objects)
			=> _set.RemoveRange(objects);

		public void Update(T obj)
			=> _set.Update(obj);

		public void UpdateRange(params T[] objects)
			=> _set.UpdateRange(objects);
	}
}
