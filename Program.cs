﻿using System;
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
			//Node program = Let("x", Int(5), Add(Negate(Var("x")), Read()));
			//int result = InterpN1(program, new Env<int>());
			//Console.WriteLine($"Program Result: {result}");
			Node program = Let("x", 
				Negate(
					Add(Int(10), Int(30))
				), 
				Add(Var("x"), 
					Add(Int(2), Int(3))
				));
			Console.WriteLine($"Original program: {program.ToString<N1>()}");

			var result1 = InterpN1(program, new Env<int>());
			Console.WriteLine($"\n\nResult: {result1}");

			Node reduced = ReduceComplexOperators(program);
			Console.WriteLine($"Reduced program: {reduced.ToString<N1>()}");

			var result2 = InterpN1(reduced, new Env<int>());
			Console.WriteLine($"\n\nResult: {result2}");

			Node pevaled = PartialEvaluate(program); 
			Console.WriteLine($"Partially Evaluated: {pevaled.ToString<N1>()}");

			var result3 = InterpN1(pevaled, new Env<int>());
			Console.WriteLine($"\n\nResult: {result3}");

			Node pevaledAndReduced = ReduceComplexOperators(pevaled);
			Console.WriteLine($"Partially Evaluated and Reduced: {pevaledAndReduced.ToString<N1>()}");

			var result4 = InterpN1(pevaledAndReduced, new Env<int>());
			Console.WriteLine($"\n\nResult: {result4}");

			Node reducedAndUniqued = UniquifyNames(reduced);
			Console.WriteLine($"Reduced and Uniquified: {reducedAndUniqued}");
			var result5 = InterpN1(reducedAndUniqued, new Env<int>());
			Console.WriteLine($"\n\nResult: {result5}");


		}

		public static Node PartialEvaluate(Node n) {
			switch ((N1)n.type) {
				case N1.Int: 
				case N1.Var: 
				case N1.Read: 
					return n;
				case N1.Add:{
					Node a = PartialEvaluate(n.nodes[0]);
					Node b = PartialEvaluate(n.nodes[1]);
					if (a.type == N1.Int.Ord() && b.type == N1.Int.Ord()) {
						int av = int.Parse(a.datas[0]);
						int bv = int.Parse(b.datas[0]);
						return Int(av + bv);
					}
					return Add(a, b);
				}
				case N1.Negate: {
					Node expr = PartialEvaluate(n.nodes[0]);
					if (expr.type == N1.Int.Ord()) {
						return Int(-int.Parse(expr.datas[0]));
					}
					return Negate(expr);
				}
				case N1.Let: {
					Node expr = PartialEvaluate(n.nodes[0]);
					Node body = PartialEvaluate(n.nodes[1]);
					return Let(n.datas[0], expr, body);
				}
			}
			throw new Exception($"Unknown N1 node type {n.type}");
		}

		public static Node ReduceComplexOperators(Node tree) {
			(int k, Node res) = ReduceExp(0, tree);
			return res;
		}
		public static Node Reduce(Node body, (string sym, Node expr) t) {
			return Let(t.sym, t.expr, body);
		}

		public static (int, Node) ReduceExp(int cnt, Node n) {
			switch ((N1)n.type) {
				case N1.Int: 
				case N1.Var: 
				case N1.Read: 
					return (cnt, n);
				case N1.Negate:{
					(int cnt2, Node body, LL<(string, Node)> bindings) = ReduceAtm(cnt, n.nodes[0], null);
					return ( cnt2, Negate(bindings.FoldL(Reduce, body)) );
				}
				case N1.Add: {
					(int cnta, Node bodya, LL<(string, Node)> binda) = ReduceAtm(cnt, n.nodes[0], null);
					(int cntb, Node bodyb, LL<(string, Node)> bindb) = ReduceAtm(cnta, n.nodes[1], null);
					return ( cntb, Add( binda.FoldL(Reduce, bodya), bindb.FoldL(Reduce, bodyb)) );
				}
				case N1.Let: {
					(int cnta, Node expr) = ReduceExp(cnt, n.nodes[0]);
					(int cntb, Node body) = ReduceExp(cnt, n.nodes[1]);
					return ( cntb, Let(n.datas[0], expr, body) );
				}
			}
					
			throw new Exception($"Unknown N1 node type {n.type}");
		}


		public static (int, Node, LL<(string, Node)>) ReduceAtm(int cnt, Node n, LL<(string, Node)> bindings = null) {
			// if (bindings == null) { bindings = new LL<(string, Node)>(); }

			switch ((N1)n.type){
				case N1.Int:
				case N1.Var:
				case N1.Read:
					return (cnt, n, bindings);
				case N1.Negate:{
					Node inner = n.nodes[0];
					(int cnt_, Node expr, _) = ReduceAtm(cnt, inner, bindings);
					string newName = $"s{cnt_}";
					return (cnt_+1, Var(newName), bindings.Add((newName, Negate(expr))) ); 
				}
				case N1.Add: {
					Node innerA = n.nodes[0];
					Node innerB = n.nodes[1];
					(int cnta, Node exprA, var bas) = ReduceAtm(cnt, innerA, bindings);
					(int cntb, Node exprB, var bbs) = ReduceAtm(cnta, innerB, bas);
					string newName = $"s{cntb}";
					return (cntb+1, Var(newName), bbs.Add((newName, Add(exprA, exprB))));
				}
				case N1.Let: {
					Node innerExp = n.nodes[0];
					Node innerBody = n.nodes[1];
					(int cnta, Node reducedExp) = ReduceExp(cnt, innerExp);
					(int cntb, Node reducedBody) = ReduceExp(cnta, innerBody);
					string sym = n.datas[0];
					return (cntb, reducedBody, bindings.Add((sym, reducedExp)));
				}
			}

			throw new Exception($"Unknown N1 node type {n.type}");
		}

		public static Node UniquifyNames(Node tree) {
			(_, _, Node res) = Uniqueify(0, new Env<string>(), tree);
			return res;
		}

		public static (int, Env<string>, Node) Uniqueify(int cnt, Env<string> env, Node n) {
			switch ((N1)n.type) {
				case N1.Int: 
				case N1.Read: 
					return (cnt, env, n);
				case N1.Var: {
					string sym = n.datas[0];
					string name = env.Lookup(sym);
					return (cnt, env, Var(name));
				}
				case N1.Negate: {
					(int cnt2, var env2, Node expr) = Uniqueify(cnt, env, n.nodes[0]);
					return (cnt2, env2, Negate(expr));
				}
				case N1.Add: {
					(int cnta, var env2, Node a) = Uniqueify(cnt, env, n.nodes[0]);
					(int cntb, var env3, Node b) = Uniqueify(cnta, env2, n.nodes[1]);
					return (cntb, env3, Add(a, b));
				}
				case N1.Let: {
					(int cnta, var env2, Node expr) = Uniqueify(cnt, env, n.nodes[0]);
					string newName = "s"+cnt;
					string sym = n.datas[0];
					Env<string> newEnv = env.Extend(sym, newName);
					(int cntb, var env3, Node body) = Uniqueify(cnt+1, newEnv, n.nodes[1]);
					return (cntb, env3, Let(newName, expr, body));
				}
			}
			throw new Exception($"Unknown N1 node type {n.type}");
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
				return InterpN1(n.nodes[0], env) + InterpN1(n.nodes[1], env);
			} else if (type == N1.Negate) {
				return -InterpN1(n.nodes[0], env);
			} else if (type == N1.Int) {
				return int.Parse(n.datas[0]);
			} else if (type == N1.Read) {
				Console.Write($"Enter Value: ");
				int val = int.Parse(Console.ReadLine());
				Console.WriteLine();
				return val;
			} else if (type == N1.Var) {
				return env.Lookup(n.datas[0]);
			} else if (type == N1.Let) {
				return InterpN1(n.nodes[1], env.Extend(n.datas[0], InterpN1(n.nodes[0], env)));
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
