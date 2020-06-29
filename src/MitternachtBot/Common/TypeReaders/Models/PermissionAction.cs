namespace Mitternacht.Common.TypeReaders.Models {
	public class PermissionAction {
		public static PermissionAction Enable  => new PermissionAction(true);
		public static PermissionAction Disable => new PermissionAction(false);

		public bool Value { get; }

		public PermissionAction(bool value) {
			Value = value;
		}

		public override bool Equals(object obj) {
			return obj != null && GetType() == obj.GetType() && Value == ((PermissionAction)obj).Value;
		}

		public override int GetHashCode() => Value.GetHashCode();
	}
}
