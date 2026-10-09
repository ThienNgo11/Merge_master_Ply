using GameTool.Assistants.DesignPattern;
using Luna.Unity;

namespace LunaTool
{
    public class TrackingMngr : SingletonMonoBehaviour<TrackingMngr>
    {
        public void TrackEvent(string eventName, int parameter)
        {
            Analytics.LogEvent(eventName, parameter);
        }
        
        public void TrackEvent(Analytics.EventType eventName, int parameter)
        {
            Analytics.LogEvent(eventName, parameter);
        }
    }
}
