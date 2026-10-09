using UnityEngine;

namespace Funlab.MergeMaster
{
	[CreateAssetMenu(fileName = "GameAssets")]
	public class MatchAssets : ScriptableObject
	{
		private static MatchAssets _shared;

		public Sprite[] personPictures;

		public Sprite[] saurianPictures;

		public static MatchAssets Shared
		{
			get
			{
				if (_shared == null)
				{
					_shared = Resources.Load<MatchAssets>("GameAssets");
				}
				return _shared;
			}
		}

		// tier = unit VALUE (1..1024); the Rank-* sprite index is its level - 1.
		public Sprite GetActorPicture(ActorKind actorKind, int grade)
		{
			Sprite[] pictures;
			switch (actorKind)
			{
				case ActorKind.Person:
					pictures = personPictures;
					break;
				case ActorKind.Saurian:
				case ActorKind.Overlord:
					pictures = saurianPictures;
					break;
				default:
					return null;
			}
			if (pictures == null || pictures.Length == 0)
			{
				return null;
			}
			int ordinal = Mathf.Clamp(PlayMechanics.GetActorGrade(grade) - 1, 0, Mathf.Min(10, pictures.Length - 1));
			return pictures[ordinal];
		}
	}
}
