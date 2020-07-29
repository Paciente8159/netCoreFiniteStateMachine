using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace FiniteStateMachine
{
    public class DFSM<T> : FSM<T>, ICloneable
    where T : IEquatable<T>
    {

        public DFSM() : base()
        {
            this._transitions[0] = new Dictionary<T, List<int>>();
        }

        public DFSM(FSM<T> fsm) : this()
        {
            List<BuildSet> marked = new List<BuildSet>();
            marked.Add(new BuildSet(new int[] { 0 }));

            for (int j = 0; j < marked.Count; j++)
            {
                foreach (T s in fsm.Symbols)
                {
                    List<int> acumulator = new List<int>();
                    foreach (int i in marked[j].Items)
                    {
                        acumulator.AddRange(fsm.StatesFrom(i, s));
                    }

                    if (acumulator.Count > 0)
                    {
                        BuildSet set = new BuildSet(acumulator);
                        if (!marked.Contains(set))
                        {
                            marked.Add(set);
                        }

                        int k = marked.IndexOf(set);
                        if (!this._transitions.ContainsKey(j))
                            this._transitions[j] = new Dictionary<T, List<int>>();

                        if (!this._transitions[j].ContainsKey(s))
                            this._transitions[j][s] = new List<int>();

                        if (!this._transitions[j][s].Contains(k))
                            this._transitions[j][s].Add(k);

                        foreach (int fs in set.Items)
                        {
                            if (fsm.AcceptingStates.ContainsKey(fs))
                            {
                                if (!this.AcceptingStates.ContainsKey(k))
                                {
                                    this.AcceptingStates[k] = new List<string>();
                                }

                                this.AcceptingStates[k].AddRange(fsm.AcceptingStates[fs].Except(this.AcceptingStates[k]));
                            }
                        }
                    }
                }

            }
        }

        public DFSM(T symbol, string label = "") : this()
        {
            this._transitions[0][symbol] = new List<int>();
            this._transitions[0][symbol].Add(1);
            this._transitions[1] = new Dictionary<T, List<int>>();
            this.AcceptingStates = new Dictionary<int, List<string>>();
            this.AcceptingStates[1] = new List<string>(new string[] { label });
        }

        public object Clone()
        {
            DFSM<T> result = new DFSM<T>();

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

        public override FSM<T> And(FSM<T> fsm)
        {
            DFSM<T> result = (DFSM<T>)this.Clone();
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
            return this.And(new DFSM<T>(symbol));
        }

        public override FSM<T> Or(FSM<T> fsm)
        {
            DFSM<T> result = (DFSM<T>)this.Clone();
            //DFSM<T> result = new DFSM<T>(this);
            Dictionary<int,int> conv = new Dictionary<int, int>();


            if (this._transitions[0].Count == 0)
                return fsm;

            int offset = result.States.Last();
            List<int> states = fsm.States;
            conv[0] = 0;

            while(states.Count!=0)
            {
                int start = states[0];
                if(fsm._transitions.ContainsKey(start))
                {
                    foreach (T symbol in fsm._transitions[start].Keys)
                    {
                        foreach (int end in fsm._transitions[start][symbol])
                        {
                            if(!conv.ContainsKey(end))
                            {
                                offset++;
                                conv[end] = offset;
                            }

                            if(!result._transitions.ContainsKey(conv[start]))
                                result._transitions[conv[start]] = new Dictionary<T, List<int>>();

                            if(!result._transitions.ContainsKey(conv[end]))
                                result._transitions[conv[end]] = new Dictionary<T, List<int>>();
                            
                            if(!result._transitions[conv[start]].ContainsKey(symbol))
                                result._transitions[conv[start]][symbol] = new List<int>();

                            result._transitions[conv[start]][symbol].Add(conv[end]);

                            if(fsm.AcceptingStates.ContainsKey(end))
                            {
                                if(!result.AcceptingStates.ContainsKey(conv[end]))
                                    result.AcceptingStates[conv[end]] = new List<string>(fsm.AcceptingStates[end]);
                            }
                        }
                    }
                }

                states.RemoveAt(0);
            }

            return result;
        }

        // public override FSM<T> Or(FSM<T> fsm)
        // {
        //     DFSM<T> result = new DFSM<T>(this);

        //     if (this._transitions[0].Count == 0)
        //         return fsm;

        //     int offset = result.States.Last();

        //     foreach (int state in fsm._transitions.Keys)
        //     {
        //         foreach (T symbol in fsm._transitions[state].Keys)
        //         {
        //             int startstate = (state != 0) ? state + offset : 0;
        //             if (!result._transitions.ContainsKey(startstate))
        //                 result._transitions[startstate] = new Dictionary<T, List<int>>();
        //             if (!result._transitions[startstate].ContainsKey(symbol))
        //                 result._transitions[startstate][symbol] = new List<int>();
        //             foreach (int target in fsm._transitions[state][symbol])
        //             {
        //                 int endstate = target + offset;

        //                 result._transitions[startstate][symbol].Add(endstate);
        //                 if (!result._transitions.ContainsKey(endstate))
        //                     result._transitions[endstate] = new Dictionary<T, List<int>>();

        //                 if (fsm.AcceptingStates.ContainsKey(target))
        //                 {
        //                     if (!result.AcceptingStates.ContainsKey(endstate))
        //                         result.AcceptingStates[endstate] = new List<string>();
        //                     result.AcceptingStates[endstate].AddRange(fsm.AcceptingStates[target].Except(result.AcceptingStates[endstate]));
        //                 }
        //             }
        //         }
        //     }

        //     return result;
        // }

        public override FSM<T> Or(T symbol)
        {
            return this.Or(new DFSM<T>(symbol));
        }

        public override FSM<T> Kleene()
        {
            DFSM<T> result = (DFSM<T>)this.Clone();

            foreach (int i in this._transitions.Keys)
            {
                foreach (T s in this._transitions[i].Keys)
                {
                    foreach (int fs in this._transitions[i][s].Intersect(this.AcceptingStates.Keys))
                    {
                        result._transitions[i][s].Remove(fs);
                        if (!result._transitions[i][s].Contains(0))
                            result._transitions[i][s].Add(0);
                    }
                }
            }

            result.AcceptingStates.Clear();
            result.AcceptingStates[0] = new List<string>();

            foreach (int acc in this.AcceptingStates.Keys)
            {
                result.AcceptingStates[0].AddRange(this.AcceptingStates[acc]);
            }

            result.AcceptingStates[0] = result.AcceptingStates[0].Distinct().ToList();

            return result;
        }

        public DFSM<T> Minimize()
        {
            DFSM<T> result = new DFSM<T>();

            //make clone
            result._transitions = new Dictionary<int, Dictionary<T, List<int>>>();
            foreach (int i in this._transitions.Keys)
            {
                result._transitions[i] = new Dictionary<T, List<int>>();
                foreach (T s in this._transitions[i].Keys)
                {
                    result._transitions[i][s] = new List<int>(this._transitions[i][s]);
                }
            }

            foreach (KeyValuePair<int, List<string>> kv in this.AcceptingStates)
            {
                result.AcceptingStates[kv.Key] = new List<string>(kv.Value);
            }

            //remove unreachable and looped states
            result.Trim();

            List<int> states = result.States;
            List<int> acc = result.AcceptingStates.Keys.ToList();
            List<int> nonacc = result.States.Except(acc).ToList();
            List<T> symbols = result.Symbols;
            List<BuildSet> distinctsets = new List<BuildSet>();
            List<BuildSet> sets = new List<BuildSet>();
            Dictionary<BuildSet, List<BuildSet>> deltasets = new Dictionary<BuildSet, List<BuildSet>>();

            //populate all sets
            for (int i = 0; i < states.Count; i++)
            {
                for (int j = i + 1; j < states.Count; j++)
                {
                    BuildSet newset = new BuildSet(new int[] { states[i], states[j] });
                    sets.Add(newset);
                    if ((acc.Contains(states[i]) && nonacc.Contains(states[j])) || (acc.Contains(states[j]) && nonacc.Contains(states[i])))
                    {
                        distinctsets.Add(newset);
                    }
                    else
                    {
                        if (!deltasets.ContainsKey(newset))
                            deltasets[newset] = new List<BuildSet>();

                        foreach (T s in symbols)
                        {
                            foreach (int x in result.StatesFrom(states[i], s))
                            {
                                foreach (int y in result.StatesFrom(states[j], s))
                                {
                                    if (x != y)
                                    {
                                        deltasets[newset].Add(new BuildSet(new int[] { x, y }));
                                    }
                                }
                            }
                        }

                        deltasets[newset] = deltasets[newset].Distinct().ToList();
                    }
                }
            }

            //mark all distinct states
            int distcounter = 0;
            while (distinctsets.Count != distcounter)
            {
                distcounter = distinctsets.Count;
                foreach (BuildSet bs in deltasets.Keys)
                {
                    if (deltasets[bs].Intersect(distinctsets).Any())
                        distinctsets.Add(bs);

                    if (deltasets[bs].Count == 0)
                        distinctsets.Add(bs);
                }

                distinctsets = distinctsets.Distinct().ToList();
                foreach (BuildSet bs in distinctsets)
                {
                    deltasets.Remove(bs);
                }
            }

            //join mergeble sets
            List<BuildSet> identicsets = sets.Except(distinctsets).ToList();
            for (int i = 0; i < identicsets.Count; i++)
            {
                for (int j = i + 1; j < identicsets.Count; j++)
                {
                    if (identicsets[i].Items.Intersect(identicsets[j].Items).Any())
                    {
                        List<int> newset = identicsets[i].Items.Union(identicsets[j].Items).ToList();
                        identicsets.RemoveAt(j);
                        identicsets.RemoveAt(i);
                        identicsets.Insert(i, new BuildSet(newset));
                        j = i;
                    }
                }
            }

            foreach (BuildSet b in identicsets)
            {
                List<int> items = new List<int>(b.Items);
                while (items.Count > 1)
                {
                    result.RenumberState(items[1], items[0]);
                    items.RemoveAt(1);
                }
            }

            return result;
        }


        // public DFSM<T> Minimize()
        // {
        //     DFSM<T> result = new DFSM<T>();

        //     //make clone
        //     result._transitions = new Dictionary<int, Dictionary<T, List<int>>>();
        //     foreach(int i in this._transitions.Keys)
        //     {
        //         result._transitions[i] = new Dictionary<T, List<int>>();
        //         foreach(T s in this._transitions[i].Keys)
        //         {
        //             result._transitions[i][s] = new List<int>(this._transitions[i][s]);
        //         }
        //     }

        //     foreach(KeyValuePair<int,List<string>> kv in this.AcceptingStates)
        //     {
        //         result.AcceptingStates[kv.Key] = new List<string>(kv.Value);
        //     }

        //     List<int> states = this.States;
        //     List<int> acc = this.AcceptingStates.Keys.ToList();
        //     List<int> nonacc = this.States.Except(acc).ToList();
        //     List<T> symbols = this.Symbols;
        //     List<BuildSet> distinctsets = new List<BuildSet>();
        //     List<BuildSet> sets = new List<BuildSet>();

        //     //populate distinct pairs ({Non Final, Final})
        //     foreach(int i in acc)
        //     {
        //         foreach(int j in nonacc)
        //         {
        //             distinctsets.Add(new BuildSet(new int[] {i, j}));
        //         }
        //     }

        //     distinctsets = distinctsets.Distinct().ToList();

        //     int statecount = 0;
        //     while(statecount!=distinctsets.Count)
        //     {
        //         statecount = distinctsets.Count;

        //         for(int i=0; i<states.Count; i++)
        //         {
        //             for(int j=i+1; j<states.Count; j++)
        //             {
        //                 BuildSet b = new BuildSet(new int[] {states[i], states[j]});
        //                 if(!sets.Contains(b))
        //                     sets.Add(b);

        //                 if(!distinctsets.Contains(b))
        //                 {
        //                     bool found = false;
        //                     List<T> commonsymbols = this.SymbolsFrom(states[i]).Intersect(this.SymbolsFrom(states[j])).ToList();
        //                     foreach(T s in commonsymbols)
        //                     {
        //                         foreach(int k in this.StatesFrom(states[i], s))
        //                         {
        //                             foreach(int l in this.StatesFrom(states[j], s))
        //                             {
        //                                 BuildSet nextb = new BuildSet(new int[] {k, l});
        //                                 if(distinctsets.Contains(nextb))
        //                                 {
        //                                     distinctsets.Add(b);
        //                                     found = true;
        //                                     break;
        //                                 }
        //                             }

        //                             if(found)
        //                                 break;
        //                         }

        //                         if(found)
        //                             break;
        //                     }

        //                     if(!commonsymbols.Any())
        //                         distinctsets.Add(b);
        //                 }
        //             }
        //         }

        //         distinctsets = distinctsets.Distinct().ToList();
        //     }

        //     //join mergeble sets
        //     List<BuildSet> identicsets = sets.Except(distinctsets).ToList();
        //     for(int i = 0; i < identicsets.Count; i++)
        //     {
        //         for(int j = i + 1; j < identicsets.Count; j++)
        //         {
        //             if(identicsets[i].Items.Intersect(identicsets[j].Items).Any())
        //             {
        //                 List<int> newset = identicsets[i].Items.Union(identicsets[j].Items).ToList();
        //                 identicsets.RemoveAt(j);
        //                 identicsets.RemoveAt(i);
        //                 identicsets.Insert(i, new BuildSet(newset));
        //                 j = i;
        //             }
        //         }
        //     }

        //     foreach(BuildSet b in identicsets)
        //     {
        //         List<int> items = new List<int>(b.Items);
        //         while(items.Count > 1)
        //         {
        //             foreach(int i in result._transitions.Keys)
        //             {
        //                 foreach(T s in result._transitions[i].Keys)
        //                 {
        //                     if(result._transitions[i][s].Contains(items[1]))
        //                     {
        //                         result._transitions[i][s].Remove(items[1]);
        //                         if(!result._transitions[i][s].Contains(items[0]))
        //                             result._transitions[i][s].Add(items[0]);
        //                     }

        //                 }
        //             }

        //             foreach(T s in result._transitions[items[1]].Keys)
        //             {
        //                 if(!result._transitions[items[0]].ContainsKey(s))
        //                     result._transitions[items[0]][s] = new List<int>();
        //                 result._transitions[items[0]][s].AddRange(result._transitions[items[1]][s].Except(result._transitions[items[0]][s]));
        //             }

        //             result._transitions.Remove(items[1]);

        //             if(result.AcceptingStates.ContainsKey(items[1]))
        //             {
        //                 if(!result.AcceptingStates.ContainsKey(items[0]))
        //                     result.AcceptingStates[items[0]] = new List<string>();
        //                 result.AcceptingStates[items[0]].AddRange(result.AcceptingStates[items[1]].Except(result.AcceptingStates[items[0]]));
        //                 result.AcceptingStates.Remove(items[1]);
        //             }

        //             items.RemoveAt(1);
        //         }   
        //     }

        //     //remove unreachable paths
        //     List<int> marked = new List<int>();
        //     marked.Add(0);
        //     for(int i = 0; i<marked.Count; i++)
        //     {
        //         foreach(T s in result.SymbolsFrom(marked[i]))
        //         {
        //             marked.AddRange(result.StatesFrom(marked[i], s));
        //         }
        //         marked = marked.Distinct().ToList();
        //     }

        //     foreach(int state in result.States.Except(marked))
        //     {
        //         foreach(int j in result._transitions.Keys)
        //         {
        //             foreach(T s in result._transitions[j].Keys)
        //             {
        //                 result._transitions[j][s].Remove(state);
        //             }
        //         }

        //         result._transitions.Remove(state);
        //     }

        //     foreach(int state in result.States.Except(result.AcceptingStates.Keys))
        //     {
        //         bool deletable = true;
        //         int i = 0;
        //         marked.Clear();
        //         marked.Add(state);
        //         while(i<marked.Count && deletable)
        //         {
        //             foreach(T s in symbols)
        //             {
        //                 marked.AddRange(result.StatesFrom(marked[i], s));
        //                 if(marked.Intersect(acc).Any())
        //                 {
        //                     deletable = false;
        //                     break;
        //                 }
        //             }

        //             marked = marked.Distinct().ToList();
        //             i++;
        //         }

        //         if(deletable)
        //         {
        //             foreach(int j in result._transitions.Keys)
        //             {
        //                 foreach(T s in result._transitions[j].Keys)
        //                 {
        //                     result._transitions[j][s].Remove(state);
        //                 }
        //             }

        //             result._transitions.Remove(state);
        //         }
        //     }

        //     return result;
        // }
    }
}