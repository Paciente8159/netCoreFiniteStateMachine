using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FiniteStateMachine
{
    internal class BuildSet : IEquatable<BuildSet>
    {

        public List<int> Items
        {
            get;
            protected set;
        }

        public BuildSet(IEnumerable<int> items)
        {
            this.Items = new List<int>();
            this.Items.AddRange(items.Distinct().OrderBy(x => x));
        }

        public bool Equals(BuildSet other)
        {
            // List<int> l1 = this.Items.Except(other.Items).ToList();
            // List<int> l2 = other.Items.Except(this.Items).ToList();

            // return !l1.Any() && !l2.Any();
            int count = this.Items.Count;

            if(count != other.Items.Count)
                return false;

            for(int i = 0; i < count; i++)
            {
                if(this.Items[i] != other.Items[i])
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as BuildSet);
        }

        public override int GetHashCode()
        {
            return this.Items.Aggregate((a, b) => a ^ b);
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();

            s.Append("{");
            for (int i = 0; i < this.Items.Count; i++)
            {
                s.AppendFormat("{0}, ", this.Items[i]);
            }
            s.Remove(s.Length - 2, 2);
            s.Append("}");

            return s.ToString();
        }
    }
}