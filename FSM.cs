using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FiniteStateMachine
{
    public abstract class FSM<T>
        where T : IEquatable<T>
    {
        internal Dictionary<int, Dictionary<T, List<int>>> _transitions = new Dictionary<int, Dictionary<T, List<int>>>();
        internal List<int> _next = new List<int>();

        public bool IsAccepting
        {
            get
            {
                return _next.Intersect(this.AcceptingStates.Keys).Any();
            }
        }

        public Dictionary<int, List<string>> AcceptingStates
        {
            get;
            protected set;
        }

        public List<string> AcceptingLabels
        {
            get
            {
                List<string> result = new List<string>();
                foreach (List<string> labels in this.AcceptingStates.Values)
                {
                    result.AddRange(labels);
                }

                return result.Distinct().ToList();
            }
        }

        public virtual List<T> Symbols
        {
            get
            {
                List<T> result = new List<T>();
                foreach (int state in this._transitions.Keys)
                {
                    result.AddRange(this._transitions[state].Keys);
                }

                return result.Distinct().ToList();
            }
        }

        public virtual List<int> States
        {
            get
            {
                List<int> result = new List<int>(this._transitions.Keys);

                foreach (int i in this._transitions.Keys)
                {
                    foreach (T s in this._transitions[i].Keys)
                    {
                        if (this._transitions[i][s] != null)
                            result.AddRange(this._transitions[i][s]);
                    }
                }

                return result.Distinct().OrderBy(x => x).ToList();
            }
        }

        public FSM()
        {
            this.AcceptingStates = new Dictionary<int, List<string>>();
            _next.Add(0);
        }

        public virtual List<T> SymbolsFrom(int state)
        {
            if (!this._transitions.ContainsKey(state))
                return new List<T>();

            return new List<T>(_transitions[state].Keys);
        }

        public void AddTransition(int startstate, T symbol, int endstate, bool isaccpting = false, string label = "")
        {
            if (!this._transitions.ContainsKey(startstate))
                this._transitions[startstate] = new Dictionary<T, List<int>>();
            if (!this._transitions.ContainsKey(endstate))
                this._transitions[endstate] = new Dictionary<T, List<int>>();
            if (!this._transitions[startstate].ContainsKey(symbol))
                this._transitions[startstate][symbol] = new List<int>();
            if (!this._transitions[startstate][symbol].Contains(endstate))
                this._transitions[startstate][symbol].Add(endstate);
            if (isaccpting)
            {
                if (!this.AcceptingStates.ContainsKey(endstate))
                    this.AcceptingStates[endstate] = new List<string>();

                if (!this.AcceptingStates[endstate].Contains(label))
                    this.AcceptingStates[endstate].Add(label);
            }
        }

        public void RemoveTransition(int startstate, T symbol, int endstate)
        {
            if (!this._transitions.ContainsKey(startstate))
                return;
            if (!this._transitions[startstate].ContainsKey(symbol))
                return;

            this._transitions[startstate][symbol].Remove(endstate);
        }

        public void RemoveState(int state)
        {
            this._transitions.Remove(state);

            foreach (int i in this._transitions.Keys)
            {
                foreach (T s in this._transitions[i].Keys)
                {
                    this._transitions[i][s].Remove(state);
                }
            }

            this.AcceptingStates.Remove(state);
        }

        public void RenumberState(int oldstate, int newstate)
        {
            if (this._transitions.ContainsKey(oldstate))
            {
                if (!this._transitions.ContainsKey(newstate))
                    this._transitions[newstate] = new Dictionary<T, List<int>>();

                foreach (T s in this._transitions[oldstate].Keys)
                {
                    if (!this._transitions[newstate].ContainsKey(s))
                        this._transitions[newstate][s] = new List<int>();
                    this._transitions[newstate][s].AddRange(this._transitions[oldstate][s]);
                }
            }

            //this._transitions.Remove(oldstate);

            foreach (int i in this._transitions.Keys)
            {
                foreach (T s in this._transitions[i].Keys)
                {
                    if (this._transitions[i][s].Contains(oldstate))
                    {
                        this._transitions[i][s].Remove(oldstate);
                        if (!this._transitions[i][s].Contains(newstate))
                            this._transitions[i][s].Add(newstate);
                    }
                }
            }

            if (this.AcceptingStates.ContainsKey(oldstate))
            {
                if (!this.AcceptingStates.ContainsKey(newstate))
                    this.AcceptingStates[newstate] = new List<string>();

                this.AcceptingStates[newstate].AddRange(this.AcceptingStates[oldstate].Except(this.AcceptingStates[newstate]));
                this.AcceptingStates.Remove(oldstate);
            }
        }

        public void Trim()
        {
            //remove unreachable states
            List<int> marked = new List<int>();
            marked.Add(0);
            for (int i = 0; i < marked.Count; i++)
            {
                foreach (T s in this.SymbolsFrom(marked[i]))
                {
                    marked.AddRange(this.StatesFrom(marked[i], s));
                }
                marked = marked.Distinct().ToList();
            }

            foreach (int state in this.States.Except(marked))
            {
                foreach (int j in this._transitions.Keys)
                {
                    foreach (T s in this._transitions[j].Keys)
                    {
                        this._transitions[j][s].Remove(state);
                    }
                }

                this._transitions.Remove(state);
            }

            //remove states that will never reach an accepting state (trap-states)
            foreach (int state in this.States.Except(this.AcceptingStates.Keys))
            {
                bool deletable = true;
                int i = 0;
                marked.Clear();
                marked.Add(state);
                while (i < marked.Count && deletable)
                {
                    foreach (T s in this.Symbols)
                    {
                        marked.AddRange(this.StatesFrom(marked[i], s));
                        if (marked.Intersect(this.AcceptingStates.Keys).Any())
                        {
                            deletable = false;
                            break;
                        }
                    }

                    marked = marked.Distinct().ToList();
                    i++;
                }

                if (deletable)
                {
                    foreach (int j in this._transitions.Keys)
                    {
                        foreach (T s in this._transitions[j].Keys)
                        {
                            this._transitions[j][s].Remove(state);
                        }
                    }

                    this._transitions.Remove(state);
                }
            }
        }

        public abstract FSM<T> And(FSM<T> fsm);

        public abstract FSM<T> And(T symbol);

        public abstract FSM<T> Or(FSM<T> fsm);

        public abstract FSM<T> Or(T symbol);

        public abstract FSM<T> Kleene();

        public virtual List<int> StatesFrom(int state, T symbol)
        {
            if (!this._transitions.ContainsKey(state))
                return new List<int>();

            if (!this._transitions[state].ContainsKey(symbol))
                return new List<int>();

            return new List<int>(_transitions[state][symbol]);
        }

        public virtual List<int> StatesFrom(T symbol)
        {
            List<int> result = new List<int>();
            foreach(Dictionary<T, List<int>> states in this._transitions.Where(x=>x.Value.ContainsKey(symbol)).Select(x=>x.Value))
            {
                result.AddRange(states[symbol]);
            }

            return result;
        }

        public bool Feed(T symbol)
        {
            List<int> result = new List<int>();

            foreach (int state in _next)
            {
                result.AddRange(this.StatesFrom(state, symbol));
            }

            if (result.Count != 0)
            {
                _next = result;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            _next.Clear();
            _next.Add(0);
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();

            foreach (int startstate in this._transitions.Keys)
            {
                foreach (T symbol in this._transitions[startstate].Keys)
                {
                    foreach (int endstate in this._transitions[startstate][symbol])
                    {
                        string ss = startstate.ToString();
                        string es = endstate.ToString();
                        if (this.AcceptingStates.ContainsKey(endstate))
                            es = es + "*";
                        if (this.AcceptingStates.ContainsKey(startstate))
                            ss = ss + "*";

                        s.AppendLine(ss + "->" + symbol + "->" + es);
                    }
                }
            }

            foreach (int acc in this.AcceptingStates.Keys)
            {
                s.Append(acc + "*");
                foreach (string label in this.AcceptingStates[acc])
                {
                    s.AppendFormat("-<{0}>", label);
                }
                s.AppendLine();
            }

            return s.ToString();
        }

    }
}
