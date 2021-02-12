using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ni_compiler {

	public class N0Lang {

		public static void Run() {
			Node program = Add(Negate(Int(5)), Read());

			int result = InterpN0(program);
			Console.WriteLine($"Program Result: {result}");
		}

		public enum N0 : int {
			Int, Read, Negate, Add
		}
		public static Node Read() { return new Node(N0.Read.Ord()); }
		public static Node Int(int val) {
			Node n = new Node(N0.Int.Ord());
			n.List(val.ToString());
			return n;
		}
		public static Node Negate(Node inner) {
			Node n = new Node(N0.Negate.Ord());
			n.List(inner);
			return n;
		}
		public static Node Add(Node a, Node b) {
			Node n = new Node(N0.Add.Ord());
			n.List(a);
			n.List(b);
			return n;
		}

		public static int InterpN0(Node tree) {
			if (tree == null) { throw new Exception($"No node to execute"); }
			N0 type = (N0)tree.type;
			if (type == N0.Add) {
				return InterpN0(tree.nodes[0]) + InterpN0(tree.nodes[1]);
			} else if (type == N0.Negate) {
				return -InterpN0(tree.nodes[0]);
			} else if (type == N0.Int) {
				return int.Parse(tree.datas[0]);
			} else if (type == N0.Read) {
				Console.Write($"Enter Value: ");
				int val = int.Parse(Console.ReadLine());
				Console.WriteLine();
				return val;
			} else { throw new Exception($"Unknown Type {type}"); }
		}

	}

}
