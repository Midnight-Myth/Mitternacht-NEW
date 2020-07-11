using System.Collections.Generic;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories {
	public interface IRepository<T> where T : DbEntity {
		T Get(int id);
		IEnumerable<T> GetAll();

		void Add(T obj);
		void AddRange(params T[] objs);

		void Remove(int id);
		void Remove(T obj);
		void RemoveRange(params T[] objs);

		void Update(T obj);
		void UpdateRange(params T[] objs);
	}
}
