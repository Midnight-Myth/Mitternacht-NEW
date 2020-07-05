namespace Mitternacht.Services.Database.Models {
	public class CustomReaction : DbEntity {
		public ulong? GuildId           { get; set; }
		public string Response          { get; set; }
		public string Trigger           { get; set; }
		public bool   IsRegex           { get; set; }
		public bool   OwnerOnly         { get; set; }
		public bool   AutoDeleteTrigger { get; set; }
		public bool   DmResponse        { get; set; }
		public bool   ContainsAnywhere  { get; set; }

		public bool IsGlobal => !GuildId.HasValue;
	}

	public class ReactionResponse : DbEntity {
		public bool   OwnerOnly { get; set; }
		public string Text      { get; set; }
	}
}