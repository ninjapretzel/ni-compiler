using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ni_compiler {

	public class Program {
		public static void Main(string[] args) {
			N1Lang.Run();
		}
	}

	public class C0Lang {
		public enum C0 : int {
			Int, Var, 
			Atm, Read, Sub, Add,
			Assign, 
			Return, Seq
		}
		public static Node Int(int val) {
			Node n = new Node(C0.Int.Ord());
			n.List(val.ToString());
			return n;
		}
		public static Node Sub(Node inner) {
			Node n = new Node(C0.Sub.Ord());
			n.List(inner);
			return n;
		}
	}

	public class N1Lang {

		public static void Run() {
			Node program = Let("x", Int(5), Add(Negate(Var("x")), Read()));

			int result = InterpN1(program, new Env<int>());
			Console.WriteLine($"Program Result: {result}");
		}

		public enum N1 : int {
			Int, Read, Negate, Add,
			Var, Let
		}
		public static Node Int(int val) {
			Node n = new Node(N1.Int.Ord());
			n.List(val.ToString());
			return n;
		}
		public static Node Negate(Node inner) {
			Node n = new Node(N1.Negate.Ord());
			n.List(inner);
			return n;
		}
		public static Node Add(Node a, Node b) {
			Node n = new Node(N1.Add.Ord());
			n.List(a);
			n.List(b);
			return n;
		}
		public static Node Read() { return new Node(N1.Read.Ord()); }
		public static Node Var(string sym) {
			Node n = new Node(N1.Var.Ord());
			n.List(sym);
			return n;
		}
		public static Node Let(string sym, Node expr, Node body) {
			Node n = new Node(N1.Let.Ord());
			n.List(sym);
			n.List(expr);
			n.List(body);
			return n;
		}

		public static int InterpN1(Node n, Env<int> env) {
			if (n == null) { throw new Exception($"No node to execute"); }
			N1 type = (N1)n.type;
			if (type == N1.Add) {
				return InterpN1(n.nodeList[0], env) + InterpN1(n.nodeList[1], env);
			} else if (type == N1.Negate) {
				return -InterpN1(n.nodeList[0], env);
			} else if (type == N1.Int) {
				return int.Parse(n.dataList[0]);
			} else if (type == N1.Read) {
				Console.Write($"Enter Value: ");
				int val = int.Parse(Console.ReadLine());
				Console.WriteLine();
				return val;
			} else if (type == N1.Var) {
				return env.Lookup(n.dataList[0]);
			} else if (type == N1.Let) {
				return InterpN1(n.nodeList[1], env.Extend(n.dataList[0], InterpN1(n.nodeList[0], env)));
			} else { throw new Exception($"Unknown Type {type}"); }
		}
	}

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
				return InterpN0(tree.nodeList[0]) + InterpN0(tree.nodeList[1]);
			} else if (type == N0.Negate) {
				return -InterpN0(tree.nodeList[0]);
			} else if (type == N0.Int) {
				return int.Parse(tree.dataList[0]);
			} else if (type == N0.Read) {
				Console.Write($"Enter Value: ");
				int val = int.Parse(Console.ReadLine());
				Console.WriteLine();
				return val;
			} else { throw new Exception($"Unknown Type {type}"); }
		}

	}

	
}
