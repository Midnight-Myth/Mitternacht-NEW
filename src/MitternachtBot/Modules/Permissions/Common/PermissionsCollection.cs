using System;
using System.Collections.Generic;
using Mitternacht.Common;
using Mitternacht.Common.Collections;

namespace Mitternacht.Modules.Permissions.Common {
	public class PermissionsCollection<T> : IndexedCollection<T> where T : class, IIndexed {
		private readonly object _localLocker = new object();

		public PermissionsCollection(IEnumerable<T> source) : base(source) { }

		public static implicit operator List<T>(PermissionsCollection<T> x)
			=> x.Source;

		public override void Clear() {
			lock(_localLocker) {
				var first = Source[0];
				base.Clear();
				Source[0] = first;
			}
		}

		public override bool Remove(T item) {
			lock(_localLocker) {
				if(Source.IndexOf(item) != 0) {
					return base.Remove(item);
				} else{
					throw new ArgumentException("Cannot remove the permission 'allow all'.");
				}
			}
		}

		public override void Insert(int index, T item) {
			lock(_localLocker) {
				if(index != 0) {
					base.Insert(index, item);
				} else {
					throw new IndexOutOfRangeException("Cannot insert permission at index 0. The first permission is always 'allow all'.");
				}
			}
		}

		public override void RemoveAt(int index) {
			lock(_localLocker) {
				if(index != 0) {
					base.RemoveAt(index);
				} else {
					throw new IndexOutOfRangeException("Cannot remove permission at index 0. It is always 'allow all'.");
				}
			}
		}

		public override T this[int index] {
			get => Source[index];
			set {
				lock(_localLocker) {
					if(index != 0){
						base[index] = value;
					} else{
						throw new IndexOutOfRangeException("Cannot set permission at index 0. The first permission is always 'allow all'.");
					}
				}
			}
		}
	}

}
