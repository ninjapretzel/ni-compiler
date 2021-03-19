using BakaTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ni_compiler {

	public class C0Lang {
		public class _Tests {
			public static void TestBasicProgram() {
				Node<C0> program =
					Seq(Assign("x", Atm(Int(5))),
					Seq(Assign("nx", Sub(Var("x"))),
					Return(Add(Var("nx"), Int(120)))
				));

				(int result, Env<int> final) = Interp(program, new Env<int>());

				result.ShouldBe(115);
			}

		}
		public enum C0 : int {
			Int, Var, // Atomics
			Atm, // expr, Atomic wrapper
			Read, // expr, Expression
			Sub, // expr, -(atomic)
			Add, // expr, (atomic + atomic)
			Assign, // stmt, name = expr
			Return, Seq // Tail
		}
		public static Node<C0> Int(int val) {
			Node<C0> n = new Node<C0>(C0.Int);
			n.List(val.ToString());
			return n;
		}
		public static Node<C0> Var(string name) {
			Node<C0> n = new Node<C0>(C0.Var);
			n.List(name);
			return n;
		}
		public static Node<C0> Atm(Node<C0> atom) {
			Node<C0> n = new Node<C0>(C0.Atm);
			n.List(atom);
			return n;
		}
		public static Node<C0> Sub(Node<C0> inner) {
			Node<C0> n = new Node<C0>(C0.Sub);
			n.List(inner);
			return n;
		}
		public static Node<C0> Read() { return new Node<C0>(C0.Read); }
		public static Node<C0> Add(Node<C0> a, Node<C0> b) {
			Node<C0> n = new Node<C0>(C0.Add);
			n.List(a);
			n.List(b);
			return n;
		}
		public static Node<C0> Assign(string name, Node<C0> exp) {
			Node<C0> n = new Node<C0>(C0.Assign);
			n.List(name);
			n.List(exp);
			return n;
		}
		public static Node<C0> Return(Node<C0> exp) {
			Node<C0> n = new Node<C0>(C0.Return);
			n.List(exp);
			return n;
		}
		public static Node<C0> Seq(Node<C0> stmt, Node<C0> tail) {
			Node<C0> n = new Node<C0>(C0.Seq);
			n.List(stmt);
			n.List(tail);
			return n;
		}
		public static (int, Env<int>) Interp(Node<C0> n, Env<int> env) {
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
