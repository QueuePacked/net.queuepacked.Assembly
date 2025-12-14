using System.Collections.Generic;

namespace net.queuepacked.Assembly.CPU
{
    public class Cpu
    {
        private readonly List<State> _history;
        public int TickChanges => _history.Count - 1;

        public State State => _history[TickChanges];

        public bool Stopped => State.Stopped;

        public Cpu(State initialState)
        {
            _history = [initialState];
        }

        public Cpu() : this(State.Empty)
        {
        }

        public Cpu(IEnumerable<byte> program) : this(new State(program))
        {
        }

        public bool Back()
        {
            if (TickChanges < 1)
                return false;

            _history.RemoveAt(TickChanges);
            return true;
        }

        public bool Forward()
        {
            if (Stopped)
                return false;

            State newState = State.Tick();
            _history.Add(newState);
            return !newState.Stopped;
        }

        public void UpdateInputOutput(byte value) => _history[TickChanges] = State.ChangeInputOutput(value);

        public void Reset()
        {
            if (_history.Count > 1)
                _history.RemoveRange(1, _history.Count - 1);
        }
    }
}