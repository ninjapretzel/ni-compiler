using BakaTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ni_compiler {

	public class C0Lang {
		public class _Tests {
			public static void TestWhatever() {
				Node program =
					Seq(Assign("x", Int(5)),
					Seq(Assign("nx", Sub(Var("x"))),
					Return(Add(Var("nx"), Int(120)))
				));


				// Console.WriteLine($"Interpreting C0 program {program.ToString<C0>()}");
				(int result, Env<int> final) = Interp(program, new Env<int>());

				result.ShouldBe(5);
				//Console.WriteLine($"Got result {result} / Env<int> {final}");

			}
		}
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
		public static Node Var(string name) {
			Node n = new Node(C0.Var.Ord());
			n.List(name);
			return n;
		}
		public static Node Atm(Node atom) {
			Node n = new Node(C0.Atm.Ord());
			n.List(atom);
			return n;
		}
		public static Node Sub(Node inner) {
			Node n = new Node(C0.Sub.Ord());
			n.List(inner);
			return n;
		}
		public static Node Read() { return new Node(C0.Read.Ord()); }
		public static Node Add(Node a, Node b) {
			Node n = new Node(C0.Sub.Ord());
			n.List(a);
			n.List(b);
			return n;
		}
		public static Node Assign(string name, Node exp) {
			Node n = new Node(C0.Assign.Ord());
			n.List(name);
			n.List(exp);
			return n;
		}
		public static Node Return(Node exp) {
			Node n = new Node(C0.Return.Ord());
			n.List(exp);
			return n;
		}
		public static Node Seq(Node stmt, Node tail) {
			Node n = new Node(C0.Return.Ord());
			n.List(stmt);
			n.List(tail);
			return n;
		}

		public static (int, Env<int>) Interp(Node n, Env<int> env) {
			C0 type = (C0)n.type;
			switch (type) {
				case C0.Int: return (int.Parse(n.datas[0]), env);
				case C0.Var: return (env.Lookup(n.datas[0]), env);
				case C0.Atm: return Interp(n.nodes[0], env);
				case C0.Sub: { (int v, var env2) = Interp(n.nodes[0], env); return (-v, env2); }
				case C0.Add: {
						(int a, var env2) = Interp(n.nodes[0], env);
						(int b, var env3) = Interp(n.nodes[1], env2);
						return (a + b, env3);
					}
				case C0.Assign: {
						(int v, var env2) = Interp(n.nodes[0], env);
						return (v, env2.Extend(n.datas[0], v));
					}
				case C0.Return: { return Interp(n.nodes[0], env); }
				case C0.Seq: {
						(int v, var env2) = Interp(n.nodes[0], env);
						return Interp(n.nodes[1], env2);
					}
				case C0.Read: {
						Console.Write($"Enter Value: ");
						int val = int.Parse(Console.ReadLine());
						Console.WriteLine();
						return (val, env);
					}
			}
			throw new Exception($"Unknown C0 type {type}");
		}


	}

}
