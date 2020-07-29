using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FiniteStateMachine
{
    public class RegexFSM
    {
        private int _parseIndex = 0;

        public string Pattern
        {
            get;
            protected set;
        }

        public FSM<int> FSM
        {
            get;
            protected set;
        }

        public RegexFSM(string pattern, string label = "")
        {
            this.Pattern = pattern;
            FSM = (new DFSM<int>(ParseRegex())).Minimize();

            foreach(int acc in FSM.AcceptingStates.Keys)
            {
                FSM.AcceptingStates[acc].Clear();
                FSM.AcceptingStates[acc].Add(label);
            }
        }

        private FSM<int> ParseRegex()
        {
            FSM<int> result = ConcatRegex();

            while (_parseIndex < this.Pattern.Length)
            {
                switch (this.Pattern[_parseIndex])
                {
                    case '|':
                        _parseIndex++;
                        result = result.Or(ConcatRegex());
                        break;
                    case ')':
                        _parseIndex++;
                        return result;
                    default:
                        throw new Exception("Invalid regex pattern");
                }
            }

            return result;
        }

        private FSM<int> ConcatRegex()
        {
            FSM<int> result = new NFSM<int>(-1);
            List<FSM<int>> temp = new List<FSM<int>>();

            while (_parseIndex < this.Pattern.Length)
            {
                switch (this.Pattern[_parseIndex])
                {
                    case '|':
                    case ')':
                        return temp.Aggregate(new NFSM<int>(-1), (a, b) => (NFSM<int>)a.And(b));
                    case '*':
                        _parseIndex++;
                        temp[temp.Count - 1] = temp[temp.Count - 1].Kleene();
                        break;
                    case '+':
                        _parseIndex++;
                        temp[temp.Count - 1] = temp[temp.Count - 1].And(temp[temp.Count - 1].Kleene());
                        break;
                    case '?':
                        _parseIndex++;
                        FSM<int> next = new NFSM<int>(-1, -1);
                        temp[temp.Count - 1] = temp[temp.Count - 1].Or(next);
                        break;
                    default:
                        temp.Add(BasicRegex());
                        break;
                }
            }

            return temp.Aggregate(new NFSM<int>(-1), (a, b) => (NFSM<int>)a.And(b));
        }

        private FSM<int> BasicRegex()
        {
            while (_parseIndex < this.Pattern.Length)
            {
                switch (this.Pattern[_parseIndex])
                {
                    case '(':
                        _parseIndex++;
                        return ParseRegex();
                    case '[':
                        _parseIndex++;
                        return ParseSet();
                    case '\\':
                        _parseIndex++;
                        return ParseChar();
                    case '.':
                        _parseIndex++;
                        return AnyChar();
                    default:
                        return ParseChar();
                }
            }

            return null;
        }

        private FSM<int> AnyChar()
        {
            FSM<int> result = new NFSM<int>(-1);
            for (int i = 0; i < 256; i++)
            {
                result = result.Or(i);
            }

            return result;
        }

        private FSM<int> ParseSet()
        {
            List<int> charset = new List<int>();
            bool negate = false;

            if (this.Pattern[_parseIndex] == '^')
            {
                negate = true;
                _parseIndex++;
            }

            while (this.Pattern[_parseIndex] != ']')
            {
                switch (this.Pattern[_parseIndex])
                {
                    case '\\':
                        _parseIndex++;
                        charset.Add(this.Pattern[_parseIndex++]);
                        break;
                    case '-':
                        _parseIndex++;
                        int start = charset.Last();
                        charset.RemoveAt(charset.Count - 1);
                        for (int c = start; c <= this.Pattern[_parseIndex]; c++)
                        {
                            charset.Add(c);
                        }
                        _parseIndex++;
                        break;
                    default:
                        charset.Add(this.Pattern[_parseIndex++]);
                        break;
                }
            }

            //consume closing set
            _parseIndex++;

            FSM<int> result = new NFSM<int>(-1);
            for (int i = 0; i < 256; i++)
            {
                if (i != (int)'\\')
                {
                    if ((!negate && charset.Contains(i)) || (negate && !charset.Contains(i)))
                    {
                        result = result.Or(i);
                    }
                }
            }

            return result;
        }

        private FSM<int> ParseChar()
        {
            return new NFSM<int>(-1, this.Pattern[_parseIndex++]);
        }

        public string GetMatch(string input)
        {
            int i = 0;

            while (i < input.Length)
            {
                int start = 0;
                int end = 0;
                FSM.Reset();

                while (!FSM.Feed(input[i]))
                {
                    i++;
                    if (i == input.Length)
                        break;
                }

                FSM.Reset();
                start = i;
                if (i < input.Length)
                {
                    while (FSM.Feed(input[i]))
                    {
                        i++;
                        if (i == input.Length)
                            break;

                        if (FSM.IsAccepting)
                            end = i;
                    }
                }

                if (end != 0)
                    return input.Substring(start, end - start);

            }

            return "";
        }

    }

}
