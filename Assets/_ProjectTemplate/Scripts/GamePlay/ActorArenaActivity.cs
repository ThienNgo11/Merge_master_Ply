namespace Funlab.MergeMaster
{
	public enum ActorArenaActivity
	{
		Resting = 0,
		Slot = 1,
		Choose = 2,
		Fuse = 3
	}

	public static class ActorArenaActivityStates
	{
		public static string ToAnimatorState(this ActorArenaActivity value)
		{
			switch (value)
			{
				case ActorArenaActivity.Resting:
					return "Idle";
				case ActorArenaActivity.Slot:
					return "Place";
				case ActorArenaActivity.Choose:
					return "Select";
				case ActorArenaActivity.Fuse:
					return "Merge";
				default:
					return ((int)value).ToString();
			}
		}
	}
}
