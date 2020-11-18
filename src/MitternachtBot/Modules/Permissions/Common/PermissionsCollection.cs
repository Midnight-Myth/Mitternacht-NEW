using System;
using System.Collections.Generic;
using Mitternacht.Common.Collections;
using Mitternacht.Database.Models;

namespace Mitternacht.Modules.Permissions.Common {
	public class PermissionsCollection : IndexedCollection<Permissionv2> {
		private readonly object _localLocker = new object();

		public PermissionsCollection(IEnumerable<Permissionv2> source) : base(source) { }

		public static implicit operator List<Permissionv2>(PermissionsCollection x)
			=> x.Source;

		public override void Clear() {
			lock(_localLocker) {
				var first = Source[0];
				base.Clear();
				Source[0] = first;
			}
		}

		public override bool Remove(Permissionv2 permission) {
			lock(_localLocker) {
				if(Source.IndexOf(permission) != 0) {
					return base.Remove(permission);
				} else{
					throw new ArgumentException("Cannot remove the permission 'allow all'.");
				}
			}
		}

		public override void Insert(int index, Permissionv2 permission) {
			lock(_localLocker) {
				if(index != 0) {
					base.Insert(index, permission);
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

		public override Permissionv2 this[int index] {
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
