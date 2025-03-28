using JetBrains.Annotations;
using UnityEngine;

namespace LotsOfBulletsTest
{
	public class TogglePooling : MonoBehaviour
	{
		[UsedImplicitly]
		public void OnTogglePooling(bool state)
		{
			var guns = FindObjectsByType<Gun>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
			foreach (var gun in guns)
			{
				gun.UsePool = state;
			}
		}
	}
}
