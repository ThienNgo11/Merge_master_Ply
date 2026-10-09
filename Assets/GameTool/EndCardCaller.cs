using GameToolSample.GamePlay.Manager;
using UnityEngine;

namespace LunaTool
{
    public class EndCardCaller : MonoBehaviour
    {
        public void GoStore()
        {
            GameManager.Instance.InstallFullGame();
        }
    }
}