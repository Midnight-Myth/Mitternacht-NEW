namespace Mitternacht {
	public class Program {
		public static void Main(string[] args) {
			if(args.Length == 3 && int.TryParse(args[0], out var shardId) && int.TryParse(args[1], out var parentProcessId)) {
				if(int.TryParse(args[2], out var outPort)) { }

				new MitternachtBot(shardId, parentProcessId, outPort).RunAndBlockAsync(args).GetAwaiter().GetResult();
			} else new MitternachtBot(0, 0).RunAndBlockAsync(args).GetAwaiter().GetResult();
		}
	}
}
