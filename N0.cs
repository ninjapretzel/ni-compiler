using BakaTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ni_compiler {

	public class N0Lang {

		public enum N0 : int {
			Int, Read, Negate, Add
		}
		public static Node<N0> Read() { return new Node<N0>(N0.Read); }
		public static Node<N0> Int(int val) {
			Node<N0> n = new Node<N0>(N0.Int);
			n.List(val.ToString());
			return n;
		}
		public static Node<N0> Negate(Node<N0> inner) {
			Node<N0> n = new Node<N0>(N0.Negate);
			n.List(inner);
			return n;
		}
		public static Node<N0> Add(Node<N0> a, Node<N0> b) {
			Node<N0> n = new Node<N0>(N0.Add);
			n.List(a);
			n.List(b);
			return n;
		}

		public static int InterpN0(Node<N0> tree) {
			if (tree == null) { throw new Exception($"No Node<N0> to execute"); }
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

		public class _Tests {

			public static void TestBasicProgram() {
				Node<N0> program = Add(Negate(Int(5)), Int(30));

				int result = InterpN0(program);
				result.ShouldBe(25);
			}
		}
	}

}
