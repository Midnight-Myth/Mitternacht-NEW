using Mitternacht.Database.Models;
using System.Linq;

namespace Mitternacht.Database.Repositories {
	public interface IRepository<T> where T : DbEntity {
		T Get(int id);
		IQueryable<T> GetAll();

		void Add(T obj);
		void AddRange(params T[] objs);

		void Remove(int id);
		void Remove(T obj);
		void RemoveRange(params T[] objs);

		void Update(T obj);
		void UpdateRange(params T[] objs);
	}
}
