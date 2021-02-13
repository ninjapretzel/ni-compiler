using BakaTest;
using Ex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ni_compiler {

	public class N1Lang {

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
		public static int Run(Node n) { return Interp(n, new Env<int>()); }
		public static int Interp(Node n, Env<int> env) {
			if (n == null) { throw new Exception($"No node to execute"); }
			N1 type = (N1)n.type;
			switch (type) {
				case N1.Int: return int.Parse(n.datas[0]);
				case N1.Add: return Interp(n.nodes[0], env) + Interp(n.nodes[1], env);
				case N1.Negate: return -Interp(n.nodes[0], env);
				case N1.Var: return env.Lookup(n.datas[0]);
				case N1.Let: return Interp(n.nodes[1], env.Extend(n.datas[0], Interp(n.nodes[0], env)));
				case N1.Read: {
						Console.Write($"Enter Value: ");
						int val = int.Parse(Console.ReadLine());
						Console.WriteLine();
						return val;
					}
			}
			throw new Exception($"Unknown N1 Type {type}");
		}

		public static Node PartialEvaluate(Node n) {
			switch ((N1)n.type) {
				case N1.Int:
				case N1.Var:
				case N1.Read:
					return n;
				case N1.Add: {
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

		public static Node Reduce(Node tree) {
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
				case N1.Negate: {
						(int cnt2, Node body, var bind) = ReduceAtm(cnt, n.nodes[0], null);
						return (cnt2, bind.FoldL(Reduce, Negate(body)));
					}
				case N1.Add: {
						(int cnt2, Node bodya, var binda) = ReduceAtm(cnt, n.nodes[0], null);
						(int cnt3, Node bodyb, var bindb) = ReduceAtm(cnt2, n.nodes[1], binda);
						return (cnt3, bindb.FoldL(Reduce, Add(bodya, bodyb)));
					}
				case N1.Let: {
						(int cnt2, Node expr) = ReduceExp(cnt, n.nodes[0]);
						(int cnt3, Node body) = ReduceExp(cnt2, n.nodes[1]);
						return (cnt3, Let(n.datas[0], expr, body));
					}
			}

			throw new Exception($"Unknown N1 node type {n.type}");
		}


		public static (int, Node, LL<(string, Node)>) ReduceAtm(int cnt, Node n, LL<(string, Node)> bindings = null) {
			// if (bindings == null) { bindings = new LL<(string, Node)>(); }

			switch ((N1)n.type) {
				case N1.Int:
				case N1.Var:
				case N1.Read:
					return (cnt, n, bindings);
				case N1.Negate: {
						Node inner = n.nodes[0];
						(int cnt2, Node expr, var binds) = ReduceAtm(cnt, inner, bindings);
						string newName = $"s{cnt2}";
						return (cnt2 + 1, Var(newName), binds.Add((newName, Negate(expr))));
					}
				case N1.Add: {
						Node innerA = n.nodes[0];
						Node innerB = n.nodes[1];
						(int cnt2, Node exprA, var bas) = ReduceAtm(cnt, innerA, bindings);
						(int cnt3, Node exprB, var bbs) = ReduceAtm(cnt2, innerB, bas);
						string newName = $"s{cnt3}";
						return (cnt3 + 1, Var(newName), bbs.Add((newName, Add(exprA, exprB))));
					}
				case N1.Let: {
						Node exp = n.nodes[0];
						Node body = n.nodes[1];
						string sym = n.datas[0];
						(int cnt2, Node exp2) = ReduceExp(cnt, exp);
						(int cnt3, Node body2) = ReduceExp(cnt2, body);
						string newName = $"s{cnt3}";
						var binding = (newName, Let(sym, exp2, body2));
						return (cnt3 + 1, Var(newName), bindings.Add(binding));
					}
			}

			throw new Exception($"Unknown N1 node type {n.type}");
		}

		public static Node Uniquify(Node tree) {
			(_, _, Node res) = Uniquify(0, new Env<string>(), tree);
			return res;
		}

		public static (int, Env<string>, Node) Uniquify(int cnt, Env<string> env, Node n) {
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
						(int cnt2, var env2, Node expr) = Uniquify(cnt, env, n.nodes[0]);
						return (cnt2, env2, Negate(expr));
					}
				case N1.Add: {
						(int cnta, var env2, Node a) = Uniquify(cnt, env, n.nodes[0]);
						(int cntb, var env3, Node b) = Uniquify(cnta, env2, n.nodes[1]);
						return (cntb, env3, Add(a, b));
					}
				case N1.Let: {
						(int cnta, var env2, Node expr) = Uniquify(cnt, env, n.nodes[0]);
						string newName = "s" + cnt;
						string sym = n.datas[0];
						Env<string> newEnv = env.Extend(sym, newName);
						(int cntb, var env3, Node body) = Uniquify(cnt + 1, newEnv, n.nodes[1]);
						return (cntb, env3, Let(newName, expr, body));
					}
			}
			throw new Exception($"Unknown N1 node type {n.type}");
		}

		public class _Tests {
			/// <summary> Test cases from professor </summary>
			public static void TestUniquify() {
				void Verify(string note, Node a, Node b) {
					try {
						Node uniqued = Uniquify(a);
						uniqued.ShouldEqual(b);
					} catch (Exception e) { throw new Exception("Failed to verify: " + note, e); }
				}
				void ShouldThrow(string note, Node a, string submsg) {
					Exception c = null;
					try {
						Node uniqued = Uniquify(a);
					} catch (Exception e) { c = e; }
					c.ShouldNotBe(null);
					c.Message.IndexOf(submsg).ShouldNotBe(-1);
				}
				Verify("Can uniquify a simple let expression",
					Let("x", Int(5), Var("x")),
					Let("s0", Int(5), Var("s0")));
				Verify("Can uniquify a simple nested let expression 1",
					Let("x", Int(5), Let("y", Int(5), Var("x"))),
					Let("s0", Int(5), Let("s1", Int(5), Var("s0"))));
				Verify("Can uniquify a simple nested let expression 2",
					Let("x", Int(5), Let("y", Int(5), Var("y"))),
					Let("s0", Int(5), Let("s1", Int(5), Var("s1"))));
				Verify("Can uniquify a shadowed name",
					Let("x", Int(5), Let("x", Int(5), Var("x"))),
					Let("s0", Int(5), Let("s1", Int(5), Var("s1"))));
				Verify("Can uniquify a nested let under add",
					Let("x", Int(5), Add(Int(6), Let("y", Int(5), Var("y")))),
					Let("s0", Int(5), Add(Int(6), Let("s1", Int(5), Var("s1")))));
				Verify("Can uniquify a nested let after recursion",
					Let("x", Int(5), Add(Var("x"), Let("y", Int(5), Var("y")))),
					Let("s0", Int(5), Add(Var("s0"), Let("s1", Int(5), Var("s1")))));
				Verify("Can uniquify a nested let after recursion",
					Let("x", Int(5), Add(Let("y", Int(5), Var("y")), Var("x"))),
					Let("s0", Int(5), Add(Let("s1", Int(5), Var("s1")), Var("s0"))));

				ShouldThrow("Should error when variables are not in scope, inside body",
					Let("x", Int(5), Add(Let("y", Int(5), Var("y")), Var("missingVarName"))),
					"missingVarName");
				ShouldThrow("Should error when variables are not in scope, inside expression.",
					Let("missingVarName", Var("missingVarName"), Add(Let("y", Int(5), Var("y")), Var("missingVarName"))),
					"missingVarName");


			}
			/// <summary> Test cases from professor </summary>
			public static void TestReduceComplex() {
				void Verify(string note, Node a, Node b) {
					try {
						Node reduced = Reduce(a);
						reduced.ShouldEqual(b);
					} catch (Exception e) { throw new Exception("Failed to verify: " + note, e); }
				}

				Verify("Reduce does nothing to Int s",
					Int(7),
					Int(7));
				Verify("Reduce does nothing to Read s",
					Read(),
					Read());
				Verify("Reduce does nothing to Var s",
					Var("x"),
					Var("x"));
				Verify("Reduce does nothing to Negate with Atomic inner exp",
					Negate(Int(7)),
					Negate(Int(7)));
				Verify("Reduce does nothing to Negate with Atomic inner Int exp",
					Negate(Int(7)),
					Negate(Int(7)));
				Verify("Reduce does nothing to Negate with Atomic inner Var exp",
					Negate(Var("x")),
					Negate(Var("x")));
				Verify("Reduce does nothing to Add with Atomic inner Int exps",
					Add(Int(6), Int(7)),
					Add(Int(6), Int(7)));
				Verify("Reduce does nothing to Add with Atomic inner Var exps",
					Add(Int(6), Var("x")),
					Add(Int(6), Var("x")));
				Verify("Reduce does nothing to simple Let",
					Let("x", Int(5), Var("x")),
					Let("x", Int(5), Var("x")));
				Verify("Reduce does nothing to complex Let expression",
					Let("x", Negate(Int(5)), Var("x")),
					Let("x", Negate(Int(5)), Var("x")));

				Verify("Reduce hoists inner Negate out of add, but preserves outer let",
					Let("x", Add(Negate(Int(5)), Int(3)), Var("x")),
					Let("x", Let("s0", Negate(Int(5)), Add(Var("s0"), Int(3))), Var("x")) );

				Verify("Reduce hoists inner Add out of negate, but preserves outer let",
					Let("x", Int(5), Negate(Add(Var("x"), Var("x")))),
					Let("x", Int(5), Let("s0", Add(Var("x"), Var("x")), Negate(Var("s0")))) );

				Verify("Reduce hoists nested Negate and Add, and preserves outer let",
					Let("x", Int(5), Negate(Add(Var("x"), Negate(Var("x"))))),
					Let("x", Int(5), Let("s0", Negate(Var("x")), Let("s1", Add(Var("x"), Var("s0")), Negate(Var("s1"))))) );

				Verify("Reduce hoists non-Atomic nested Negate with inner Int exp",
					Negate(Negate(Int(7))),
					Let("s0", Negate(Int(7)), Negate(Var("s0"))) );
				Verify("Reduce hoists non-Atomic nested Negate with inner Var exp",
					Negate(Negate(Var("x"))),
					Let("s0", Negate(Var("x")), Negate(Var("s0"))));
				Verify("Reduce hoists non-atomic Add operands",
					Add(Var("x"), Negate(Int(7))),
					Let("s0", Negate(Int(7)), Add(Var("x"), Var("s0"))) );
				Verify("Reduce hoists non-atomic Add operands in correct left-to-right order",
					Add(Negate(Var("x")), Negate(Int(7))),
					Let("s0", Negate(Var("x")), 
						Let("s1", Negate(Int(7)), 
							Add(Var("s0"), Var("s1")))));
				Verify("Reduce hoists deep nesting in the correct order",
					Add(Negate(Int(5)), Add(Negate(Int(7)), Negate(Int(8)))),
					Let("s0", Negate(Int(5)), 
						Let("s1", Negate(Int(7)), 
							Let("s2", Negate(Int(8)),
								Let("s3", Add(Var("s1"), Var("s2")), 
									Add(Var("s0"), Var("s3")))))));



			}

			public static void TestTransformationsPreserveResults() {
				Node program = Let("x",
					Negate(
						Add(Int(10), Int(30)) // x = -40
					),
					Add(Var("x"),
						Add(Int(2), Int(3)) // x + 2 + 3
					)
				);

				int expected1 = -35;
				int result1 = Run(program);
				result1.ShouldBe(expected1);
				
				Node reduced = Reduce(program);

				int result2 = Run(reduced);
				result2.ShouldBe(expected1);
				
				Node pevaled = PartialEvaluate(program);

				int result3 = Run(pevaled);
				result3.ShouldBe(expected1);
				
				Node pevaledAndReduced = Reduce(pevaled);

				int result4 = Run(pevaledAndReduced);
				result4.ShouldBe(expected1);
				
				Node reducedAndUniqued = Uniquify(reduced);
				int result5 = Run(reducedAndUniqued);
				result5.ShouldBe(expected1);
				
			}

			public static void TestReduction() {
				Node pg = Add(  // (-(-(5)) + (let x=6,y=6 in x+y)
					Int(5),
					Let("x", Int(6),
						Let("y", Int(6),//lets are complex inners, and need to be reduced
							Add(Var("x"), Var("y"))
						)
					)
				);

				int expected = 17;
				Node pgreduced = Reduce(pg);
				pgreduced.type.ShouldBe(N1.Let.Ord()); // A Let should be promoted to the top

				int result1 = Run(pg);
				int result2 = Run(pgreduced);
				result1.ShouldBe(expected);
				result2.ShouldBe(expected);

				Node pg2 = Add(  // (-(-5) + (let x=6,y=6 in x+y)
					Negate(Int(-5)),
					Let("x", Int(6),
						Let("y", Int(6),//lets are complex inners, and need to be reduced
							Add(Var("x"), Var("y"))
						)
					)
				);

				Node pg2Reduced = Reduce(pg2);
				int result3 = Run(pg2Reduced);
				result3.ShouldBe(expected);
				//Log.Info(pg2Reduced.ToString<N1>());
				

				Node pg3 = Add(  // (-(-(5)) + (let x=6,y=6 in x+y)
					Negate(Negate(Int(5))),
					Let("x", Int(6),
						Let("y", Int(6),//lets are complex inners, and need to be reduced
							Add(Var("x"), Var("y"))
						)
					)
				);

				Node pg3Reduced = Reduce(pg3);
				//Log.Info(pg3Reduced.ToString<N1>());
				int result4 = Run(pg3Reduced);

			}




		}
	}

}
