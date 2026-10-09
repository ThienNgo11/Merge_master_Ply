namespace Funlab.MergeMaster
{
	public enum ActorKind
	{
		Unset = 0,
		Person = 1,
		Saurian = 2,
		Overlord = 3
	}

	public static class ActorKindLabels
	{
		public static string ToLabel(this ActorKind value)
		{
			switch (value)
			{
				case ActorKind.Unset:
					return "None";
				case ActorKind.Person:
					return "Human";
				case ActorKind.Saurian:
					return "Dino";
				case ActorKind.Overlord:
					return "Boss";
				default:
					return ((int)value).ToString();
			}
		}
	}
}
