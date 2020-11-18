using Mitternacht.Common;
using System;
using System.Collections.Generic;

namespace Mitternacht.Database.Models {
	public class ShopEntry : DbEntity, IIndexed {
		public int                    Index    { get; set; }
		public int                    Price    { get; set; }
		public string                 Name     { get; set; }
		public ulong                  AuthorId { get; set; }
		public ShopEntryType          Type     { get; set; }
		public string                 RoleName { get; set; }
		public ulong                  RoleId   { get; set; }
		public HashSet<ShopEntryItem> Items    { get; set; } = new HashSet<ShopEntryItem>();
	}

	public class ShopEntryItem : DbEntity {
		public string Text { get; set; }

		public override bool Equals(object obj)
			=> obj != null && GetType() == obj.GetType() && ((ShopEntryItem)obj).Text.Equals(Text, StringComparison.OrdinalIgnoreCase);

		public override int GetHashCode()
			=> Text.GetHashCode();
	}
}
