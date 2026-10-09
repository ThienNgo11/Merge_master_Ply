namespace Funlab.MergeMaster
{
	public enum ActorFaction
	{
		Unset = 0,
		Foe = 1,
		Guardian = 2
	}

	public static class ActorFactionLabels
	{
		public static string ToLabel(this ActorFaction value)
		{
			switch (value)
			{
				case ActorFaction.Unset:
					return "None";
				case ActorFaction.Foe:
					return "Enemy";
				case ActorFaction.Guardian:
					return "Ranger";
				default:
					return ((int)value).ToString();
			}
		}
	}
}
