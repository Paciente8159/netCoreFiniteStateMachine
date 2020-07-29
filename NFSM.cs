using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FiniteStateMachine
{
    public class NFSM<T> : FSM<T>, ICloneable
    where T : IEquatable<T>
    {
        public T Epsilon
        {
            get;
            set;
        }

        public override List<T> Symbols
        {
            get
            {
                List<T> result = base.Symbols;
                result.Remove(this.Epsilon);
                return result;
            }
        }

        public NFSM(T epsilon) : base()
        {
            this.Epsilon = epsilon;
            this._transitions[0] = new Dictionary<T, List<int>>();
        }

        public NFSM(T epsilon, T symbol, string label = "") : this(epsilon)
        {
            this._transitions[0][symbol] = new List<int>();
            this._transitions[0][symbol].Add(1);
            this._transitions[1] = new Dictionary<T, List<int>>();
            this.AcceptingStates = new Dictionary<int, List<string>>();
            this.AcceptingStates[1] = new List<string>(new string[] { label });
        }

        public NFSM(NFSM<T> nfsm) : this(nfsm.Epsilon)
        {
            foreach (int state in nfsm._transitions.Keys)
            {
                this._transitions[state] = new Dictionary<T, List<int>>();
                foreach (T symbol in nfsm._transitions[state].Keys)
                {
                    this._transitions[state][symbol] = new List<int>(nfsm._transitions[state][symbol]);
                }
            }

            this.AcceptingStates = new Dictionary<int, List<string>>();
            foreach (KeyValuePair<int, List<string>> acc in nfsm.AcceptingStates)
            {
                this.AcceptingStates[acc.Key] = new List<string>(acc.Value);
            }
        }

        public NFSM(FSM<T> fsm, T epsilon) : this(epsilon)
        {
            foreach (int state in fsm._transitions.Keys)
            {
                this._transitions[state] = new Dictionary<T, List<int>>();
                foreach (T symbol in fsm._transitions[state].Keys)
                {
                    this._transitions[state][symbol] = new List<int>(fsm._transitions[state][symbol]);
                }
            }

            this.AcceptingStates = new Dictionary<int, List<string>>();
            foreach (KeyValuePair<int, List<string>> acc in fsm.AcceptingStates)
            {
                this.AcceptingStates[acc.Key] = new List<string>(acc.Value);
            }
        }

        public object Clone()
        {
            NFSM<T> result = new NFSM<T>(this.Epsilon);

            foreach(KeyValuePair<int,Dictionary<T,List<int>>> i in this._transitions)
            {
                result._transitions[i.Key] = new Dictionary<T, List<int>>();
                foreach(KeyValuePair<T,List<int>> j in i.Value)
                {
                    result._transitions[i.Key][j.Key] = new List<int>(this._transitions[i.Key][j.Key]);
                }
            }

            foreach(KeyValuePair<int,List<string>> i in this.AcceptingStates)
            {
                result.AcceptingStates[i.Key] = new List<string>(this.AcceptingStates[i.Key]);
            }

            return result;
        }

        public override List<T> SymbolsFrom(int state)
        {
            List<int> marked = new List<int>();
            List<T> result = new List<T>();

            if (!this._transitions.ContainsKey(state))
                return result;

            //add all epsilons
            marked.Add(state);
            for (int i = 0; i < marked.Count; i++)
            {
                if (this._transitions[state].ContainsKey(this.Epsilon))
                    marked.AddRange(this._transitions[state][this.Epsilon].Except(marked));
            }

            //add reaching possible symbols
            for (int i = 0; i < marked.Count; i++)
            {
                result.AddRange(this._transitions[marked[i]].Keys.Except(new T[] { this.Epsilon }));
            }

            return result.Distinct().ToList();
        }

        public override List<int> StatesFrom(int state, T symbol)
        {
            List<int> marked = new List<int>();
            List<int> result = new List<int>();

            if (!this._transitions.ContainsKey(state))
                return result;

            //add all epsilons
            marked.Add(state);
            for (int i = 0; i < marked.Count; i++)
            {
                if (this._transitions[state].ContainsKey(this.Epsilon))
                    marked.AddRange(this._transitions[state][this.Epsilon].Except(marked));
            }

            //add reaching states
            for (int i = 0; i < marked.Count; i++)
            {
                if (this._transitions[marked[i]].ContainsKey(symbol))
                    result.AddRange(this._transitions[marked[i]][symbol]);
            }

            //add following epsilons
            for (int i = 0; i < result.Count; i++)
            {
                if(this._transitions.ContainsKey(result[i]))
                    if (this._transitions[result[i]].ContainsKey(this.Epsilon))
                        result.AddRange(this._transitions[result[i]][this.Epsilon]);
            }

            return result.Distinct().ToList();
        }

        public override FSM<T> And(FSM<T> fsm)
        {
            NFSM<T> result = (NFSM<T>)this.Clone();
            result.AcceptingStates.Clear();

            if (this._transitions[0].Count == 0)
                return fsm;

            for (int i = 0; i < this.AcceptingStates.Count; i++)
            {
                int offset = result.States.Last();

                foreach (int state in fsm._transitions.Keys)
                {
                    foreach (T symbol in fsm._transitions[state].Keys)
                    {
                        int startstate = state + offset;
                        if (state == 0)
                            startstate = this.AcceptingStates.Keys.ToArray()[i];
                        if (!result._transitions.ContainsKey(startstate))
                            result._transitions[startstate] = new Dictionary<T, List<int>>();
                        result._transitions[startstate][symbol] = new List<int>();
                        foreach (int target in fsm._transitions[state][symbol])
                        {
                            int endstate = target + offset;
                            if (target == 0)
                                endstate = this.AcceptingStates.Keys.ToArray()[i];
                            result._transitions[startstate][symbol].Add(endstate);
                            if (!result._transitions.ContainsKey(endstate))
                                result._transitions[endstate] = new Dictionary<T, List<int>>();

                            if (fsm.AcceptingStates.ContainsKey(target))
                                result.AcceptingStates[endstate] = new List<string>(fsm.AcceptingStates[target]);
                        }
                    }
                }
            }

            return result;
        }

        public override FSM<T> And(T symbol)
        {
            FSM<T> andvar = new NFSM<T>(this.Epsilon, symbol);
            foreach(string label in this.AcceptingLabels)
            {
                andvar.AcceptingStates.Last().Value.Add(label);
            }

            return this.And(andvar);
        }

        public override FSM<T> Or(FSM<T> fsm)
        {
            NFSM<T> result = (NFSM<T>)this.Clone();

            if (this._transitions[0].Count == 0)
                return fsm;

            int offset = result._transitions.Keys.OrderBy(x => x).Last();

            foreach (int state in fsm._transitions.Keys)
            {
                foreach (T symbol in fsm._transitions[state].Keys)
                {
                    int startstate = (state != 0) ? state + offset : 0;
                    if (!result._transitions.ContainsKey(startstate))
                        result._transitions[startstate] = new Dictionary<T, List<int>>();
                    if (!result._transitions[startstate].ContainsKey(symbol))
                        result._transitions[startstate][symbol] = new List<int>();
                    foreach (int target in fsm._transitions[state][symbol])
                    {
                        int endstate = target + offset;

                        if (fsm.AcceptingStates.ContainsKey(target))
                            endstate = result.AcceptingStates.Keys.Last();

                        result._transitions[startstate][symbol].Add(endstate);
                        if (!result._transitions.ContainsKey(endstate))
                            result._transitions[endstate] = new Dictionary<T, List<int>>();

                        if (fsm.AcceptingStates.ContainsKey(target))
                        {
                            if (!result.AcceptingStates.ContainsKey(endstate))
                                result.AcceptingStates[endstate] = new List<string>();
                            result.AcceptingStates[endstate].AddRange(fsm.AcceptingStates[target].Except(result.AcceptingStates[endstate]));
                        }
                    }
                }
            }

            return result;
        }

        public override FSM<T> Or(T symbol)
        {
            return this.Or(new NFSM<T>(this.Epsilon, symbol));
        }

        public override FSM<T> Kleene()
        {
            NFSM<T> result = (NFSM<T>)this.Clone();
            NFSM<T> eps = new NFSM<T>(this.Epsilon, this.Epsilon);
            eps.AcceptingStates.Clear();
            eps.AcceptingStates[1] = new List<string>(this.AcceptingLabels);
            result = (NFSM<T>)eps.And(result).And(eps).Or(eps);

            foreach (int acc in result.AcceptingStates.Keys)
            {
                if (!result._transitions[acc].ContainsKey(this.Epsilon))
                    result._transitions[acc][this.Epsilon] = new List<int>();

                result._transitions[acc][this.Epsilon].Add(1);
            }

            return result;
        }

    }
}