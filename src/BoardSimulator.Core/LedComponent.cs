namespace BoardSimulator.Core
{
    public class LedComponent : BoardComponent
    {
        public Pin Signal { get; } = new();
        public LedComponent(string id, string displayName)
            : base(id, displayName) { }

        public LedState GetState(NetState observed)
        {
            return observed switch
            {
                NetState.High => LedState.On,
                NetState.Low => LedState.Off,
                _ => LedState.Undefined
            };
        }
    }
}
