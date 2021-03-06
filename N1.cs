﻿using BakaTest;
using Ex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static ni_compiler.C0Lang;

namespace ni_compiler {

	public static class N1Lang {

		#region Node Types
		public enum N1 : int {
			Int, Read, Negate, Add,
			Var, Let
		}
		public static Node<N1> Int(int val) {
			Node<N1> n = new Node<N1>(N1.Int);
			n.List(val.ToString());
			return n;
		}
		public static Node<N1> Negate(Node<N1> inner) {
			Node<N1> n = new Node<N1>(N1.Negate);
			n.List(inner);
			return n;
		}
		public static Node<N1> Add(Node<N1> a, Node<N1> b) {
			Node<N1> n = new Node<N1>(N1.Add);
			n.List(a);
			n.List(b);
			return n;
		}
		public static Node<N1> Read() { return new Node<N1>(N1.Read); }
		public static Node<N1> Var(string sym) {
			Node<N1> n = new Node<N1>(N1.Var);
			n.List(sym);
			return n;
		}
		public static Node<N1> Let(string sym, Node<N1> expr, Node<N1> body) {
			Node<N1> n = new Node<N1>(N1.Let);
			n.List(sym);
			n.List(expr);
			n.List(body);
			return n;
		}
		#endregion

		#region Interpreter
		public static int Run(Node<N1> n) { return Interp(n); }

		public static int Interp(Node<N1> n, Env<int> env = null) {
			if (env == null) { env = new Env<int>(); }
			if (n == null) { throw new Exception($"No node to execute"); }
			N1 type = n.type;
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
		#endregion

		#region PASS: Explicate Control
		public static (Node<C0>, LL<string>) Explicate(Node<N1> n) {
			return ExplicateTail(n);
		}

		public static (Node<C0>, LL<string>) ExplicateTail(Node<N1> n) {
			switch (n.type) {
				case N1.Int: 
				case N1.Var: 
					return (Return(Atm(n.ToC0Expr())), null);
				case N1.Read:
				case N1.Negate:
				case N1.Add:
					return (Return(n.ToC0Expr()), null);
				case N1.Let: {
					(var tail, var tailNames) = ExplicateTail(n.nodes[1]);
					(var assign, var exprNames) = ExplicateAssign(n.datas[0], n.nodes[0], tail);
					return (assign, tailNames + exprNames);
				}
					
			}
			return (null, null);
		}
		public static int Num(this string s) { return int.Parse(s);	}
		public static Node<C0> ToC0Expr(this Node<N1> n) {
			switch (n.type) {
				case N1.Int: return C0Lang.Int(n.datas[0].Num());
				case N1.Var: return C0Lang.Var(n.datas[0]);
				case N1.Negate: return C0Lang.Sub(n.nodes[0].ToC0Expr());
				case N1.Add: return C0Lang.Add(n.nodes[0].ToC0Expr(), n.nodes[1].ToC0Expr());
				case N1.Read: return C0Lang.Read();
			}
			return null;
		}
		public static (Node<C0>, LL<string>) ExplicateAssign(string name, Node<N1> expr, Node<C0> tail) {
			if (expr.type == N1.Let) {
				(var seq2, var tailNames) = ExplicateAssign(name, expr.nodes[1], tail);
				(var res, var exprNames) = ExplicateAssign(expr.datas[0], expr.nodes[0], seq2);
				return (res, exprNames+tailNames);
			} 
			var c0 = expr.ToC0Expr();
			if (c0.type == C0.Int || c0.type == C0.Var) {
				c0 = Atm(c0);
			}
			return (Seq(Assign(name, c0), tail), new LL<string>(name));
		}
		#endregion

		#region PASS: Partial Evaluation
		public static Node<N1> PartialEvaluate(Node<N1> n) {
			Node<N1> qadd(Node<N1> inta, Node<N1> intb) {
				int av = int.Parse(inta.datas[0]);
				int bv = int.Parse(intb.datas[0]);
				return Int(av + bv);
			}
			switch (n.type) {
				case N1.Int:
				case N1.Var:
				case N1.Read:
					return n;
				case N1.Add: {
					Node<N1> a = PartialEvaluate(n.nodes[0]);
					Node<N1> b = PartialEvaluate(n.nodes[1]);
					if (a.type == N1.Int && b.type == N1.Int) { return qadd(a,b); }
					if (a.type == N1.Int || b.type == N1.Int) {
						Node<N1> intNode = a.type == N1.Int ? a : b;
						if (a.type == N1.Add) {
							Node<N1> aa = a.nodes[0];
							Node<N1> ab = a.nodes[1];
							if (aa.type == N1.Int) { return Add(qadd(intNode, aa), ab); }
							if (ab.type == N1.Int) { return Add(qadd(intNode, ab), aa); }
						}
						if (b.type == N1.Add) {
							Node<N1> ba = b.nodes[0];
							Node<N1> bb = b.nodes[1];
							if (ba.type == N1.Int) { return Add(qadd(intNode, ba), bb); }
							if (bb.type == N1.Int) { return Add(qadd(intNode, bb), ba); }
						}
					}
					if (a.type == N1.Add && b.type == N1.Add) {
						Node<N1> aa = a.nodes[0];
						Node<N1> ab = a.nodes[1];
						Node<N1> ba = b.nodes[0];
						Node<N1> bb = b.nodes[1];

						if (aa.type == N1.Int && ba.type == N1.Int) { return Add(qadd(aa, ba), Add(ab, bb)); }
						if (aa.type == N1.Int && bb.type == N1.Int) { return Add(qadd(aa, bb), Add(ab, ba)); }
						if (ab.type == N1.Int && ba.type == N1.Int) { return Add(qadd(ab, ba), Add(aa, bb)); }
						if (ab.type == N1.Int && bb.type == N1.Int) { return Add(qadd(ab, bb), Add(aa, ba)); }
					}

					return Add(a, b);
				}
				case N1.Negate: {
					Node<N1> expr = PartialEvaluate(n.nodes[0]);
					if (expr.type == N1.Int) {
						return Int(-int.Parse(expr.datas[0]));
					}
					return Negate(expr);
				}
				case N1.Let: {
					Node<N1> expr = PartialEvaluate(n.nodes[0]);
					Node<N1> body = PartialEvaluate(n.nodes[1]);
					return Let(n.datas[0], expr, body);
				}
			}
			throw new Exception($"Unknown N1 node type {n.type}");
		}
		#endregion

		#region PASS: Reduce Complex Expressions
		public static Node<N1> Reduce(Node<N1> tree) {
			(int k, Node<N1> res) = ReduceExp(0, tree);
			return res;
		}
		public static (int cnt, Node<N1> tree) ReduceFull(Node<N1> tree, int cnt) {
			return ReduceExp(cnt, tree);
		}
		public static Node<N1> Reduce(Node<N1> body, (string sym, Node<N1> expr) t) {
			return Let(t.sym, t.expr, body);
		}

		public static (int, Node<N1>) ReduceExp(int cnt, Node<N1> n) {
			switch (n.type) {
				case N1.Int:
				case N1.Var:
				case N1.Read:
					return (cnt, n);
				case N1.Negate: {
						(int cnt2, Node<N1> body, var bind) = ReduceAtm(cnt, n.nodes[0], null);
						return (cnt2, bind.FoldL(Reduce, Negate(body)));
					}
				case N1.Add: {
						(int cnt2, Node<N1> bodya, var binda) = ReduceAtm(cnt, n.nodes[0], null);
						(int cnt3, Node<N1> bodyb, var bindb) = ReduceAtm(cnt2, n.nodes[1], binda);
						return (cnt3, bindb.FoldL(Reduce, Add(bodya, bodyb)));
					}
				case N1.Let: {
						(int cnt2, Node<N1> expr) = ReduceExp(cnt, n.nodes[0]);
						(int cnt3, Node<N1> body) = ReduceExp(cnt2, n.nodes[1]);
						
						return (cnt3+1, Let(n.datas[0], expr, body));
					}
			}

			throw new Exception($"Unknown N1 node type {n.type}");
		}


		public static (int, Node<N1>, LL<(string, Node<N1>)>) ReduceAtm(int cnt, Node<N1> n, LL<(string, Node<N1>)> bindings = null) {
			// if (bindings == null) { bindings = new LL<(string, Node<N1>)>(); }

			switch (n.type) {
				case N1.Int:
				case N1.Var:
				case N1.Read:
					return (cnt, n, bindings);
				case N1.Negate: {
						Node<N1> inner = n.nodes[0];
						(int cnt2, Node<N1> expr, var binds) = ReduceAtm(cnt, inner, bindings);
						string newName = $"s{cnt2}";
						return (cnt2 + 1, Var(newName), binds.Add((newName, Negate(expr))));
					}
				case N1.Add: {
						Node<N1> innerA = n.nodes[0];
						Node<N1> innerB = n.nodes[1];
						(int cnt2, Node<N1> exprA, var bas) = ReduceAtm(cnt, innerA, bindings);
						(int cnt3, Node<N1> exprB, var bbs) = ReduceAtm(cnt2, innerB, bas);
						string newName = $"s{cnt3}";
						return (cnt3 + 1, Var(newName), bbs.Add((newName, Add(exprA, exprB))));
					}
				case N1.Let: {
						Node<N1> exp = n.nodes[0];
						Node<N1> body = n.nodes[1];
						string sym = n.datas[0];
						(int cnt2, Node<N1> exp2) = ReduceExp(cnt, exp);
						(int cnt3, Node<N1> body2) = ReduceExp(cnt2, body);
						string newName = $"s{cnt3}";
						var binding = (newName, Let(sym, exp2, body2));
						return (cnt3 + 1, Var(newName), bindings.Add(binding));
					}
			}

			throw new Exception($"Unknown N1 node type {n.type}");
		}
		#endregion

		#region PASS: Uniquify Variable Names
		public static Node<N1> Uniquify(Node<N1> tree) {
			(_, _, Node<N1> res) = Uniquify(0, new Env<string>(), tree);
			return res;
		}
		public static (int cnt, Env<string> env, Node<N1> tree) UniquifyFull(Node<N1> tree) {
			return Uniquify(0, new Env<string>(), tree);
		}
		public static (int, Env<string>, Node<N1>) Uniquify(int cnt, Env<string> env, Node<N1> n) {
			switch (n.type) {
				case N1.Int:
				case N1.Read:
					return (cnt, env, n);
				case N1.Var: {
						string sym = n.datas[0];
						string name = env.Lookup(sym);
						return (cnt, env, Var(name));
					}
				case N1.Negate: {
						(int cnt2, var env2, Node<N1> expr) = Uniquify(cnt, env, n.nodes[0]);
						return (cnt2, env2, Negate(expr));
					}
				case N1.Add: {
						(int cnta, var env2, Node<N1> a) = Uniquify(cnt, env, n.nodes[0]);
						(int cntb, var env3, Node<N1> b) = Uniquify(cnta, env2, n.nodes[1]);
						return (cntb, env3, Add(a, b));
					}
				case N1.Let: {
						(int cnta, var env2, Node<N1> expr) = Uniquify(cnt, env, n.nodes[0]);
						string newName = "s" + cnt;
						string sym = n.datas[0];
						Env<string> newEnv = env.Extend(sym, newName);
						(int cntb, var env3, Node<N1> body) = Uniquify(cnt + 1, newEnv, n.nodes[1]);
						return (cntb, env3, Let(newName, expr, body));
					}
			}
			throw new Exception($"Unknown N1 node type {n.type}");
		}
		#endregion

		#region TESTS
		public class _Tests {
			static string prog = 
@"let ni 
	y 
is 
	let ni 
		x1 
	is 
		20
	in
		x1 + (let ni x2 is 22 in x2 end)
	end
in 
	y
end";

			static string prog2 = 
@"let ni y is 
	let ni b is
		let ni z is
			20
		in
			z
		end
	in
		let ni a is
			22
		in
			a + b
		end
	end
in
	y
end";
			static string badprog = @"let ni omg is wtf in bbq gtfo";
			static string verticalSlice = @"
";


			public static void TestExplicate() {
				var prog = 
				Let("y", 
					Let("x1", 
						Int(20), 
						Let("x2", 
							Int(22),
							Add(Var("x1"), Var("x2"))
						)
					),
					Var("y")
				);
				(var res, var names) = Explicate(prog);
				var expected = 
				Seq(Assign("x1", Atm(C0Lang.Int(20))),
				Seq(Assign("x2", Atm(C0Lang.Int(22))),
				Seq(Assign("y", C0Lang.Add(C0Lang.Var("x1"), C0Lang.Var("x2"))),
				Return(Atm(C0Lang.Var("y"))))));

				res.ShouldEqual(expected);

				names.ShouldEqual(LL<string>.From("x1", "x2", "y"));
			}


			public static void TestTokenize() {
				Tokenizer tok = new Tokenizer(prog);
				Token t;
				t = tok.peekToken; tok.Next(); t.type.ShouldBe("let");
				t = tok.peekToken; tok.Next(); t.type.ShouldBe("ni");
				t = tok.peekToken; tok.Next(); t.type.ShouldBe("!NAME"); t.content.ShouldBe("y");
				t = tok.peekToken; tok.Next(); t.type.ShouldBe("is");
				t = tok.peekToken; tok.Next(); t.type.ShouldBe("let");
				t = tok.peekToken; tok.Next(); t.type.ShouldBe("ni");
				t = tok.peekToken; tok.Next(); t.type.ShouldBe("!NAME"); t.content.ShouldBe("x1");
				t = tok.peekToken; tok.Next(); t.type.ShouldBe("is");
				t = tok.peekToken; tok.Next(); t.type.ShouldBe("!NUM"); t.content.ShouldBe("20");
				t = tok.peekToken; tok.Next(); t.type.ShouldBe("in");
				t = tok.peekToken; tok.Next(); t.type.ShouldBe("!NAME"); t.content.ShouldBe("x1");
				t = tok.peekToken; tok.Next(); t.type.ShouldBe("+");
				t = tok.peekToken; tok.Next(); t.type.ShouldBe("(");
				t = tok.peekToken; tok.Next(); t.type.ShouldBe("let");
				t = tok.peekToken; tok.Next(); t.type.ShouldBe("ni");
				t = tok.peekToken; tok.Next(); t.type.ShouldBe("!NAME"); t.content.ShouldBe("x2");
				t = tok.peekToken; tok.Next(); t.type.ShouldBe("is");
				t = tok.peekToken; tok.Next(); t.type.ShouldBe("!NUM"); t.content.ShouldBe("22");
				t = tok.peekToken; tok.Next(); t.type.ShouldBe("in");
				t = tok.peekToken; tok.Next(); t.type.ShouldBe("!NAME"); t.content.ShouldBe("x2");
				t = tok.peekToken; tok.Next(); t.type.ShouldBe("end");
				t = tok.peekToken; tok.Next(); t.type.ShouldBe(")");
				t = tok.peekToken; tok.Next(); t.type.ShouldBe("end");
				t = tok.peekToken; tok.Next(); t.type.ShouldBe("in");
				t = tok.peekToken; tok.Next(); t.type.ShouldBe("!NAME"); t.content.ShouldBe("y");
				t = tok.peekToken; tok.Next(); t.type.ShouldBe("end");
			}
			

			public static void TestParser() {
				Tokenizer tok = new Tokenizer(prog);
				Node<N1> parsed = tok.ParseExpression();

				parsed.type.ShouldBe(N1.Let);
				parsed.datas[0].ShouldBe("y");
				parsed.nodes[0].type.ShouldBe(N1.Let);
				parsed.nodes[0].nodes[0].type.ShouldBe(N1.Int);
				parsed.nodes[0].nodes[1].type.ShouldBe(N1.Add);
				parsed.nodes[0].nodes[1].nodes[0].type.ShouldBe(N1.Var);
				parsed.nodes[0].nodes[1].nodes[1].type.ShouldBe(N1.Let);

				parsed.nodes[1].type.ShouldBe(N1.Var);

				try {
					Tokenizer badTok = new Tokenizer(badprog);
					Node<N1> failed = badTok.ParseExpression();
				} catch (Exception e) {
					e.Message.Contains("line 1").ShouldBeTrue();
					e.Message.Contains("Expected: [end]").ShouldBeTrue();
				}

			}

			/// <summary> Test cases from professor </summary>
			public static void TestUniquify() {
				void Verify(string note, Node<N1> a, Node<N1> b) {
					try {
						Node<N1> uniqued = Uniquify(a);
						uniqued.ShouldEqual(b);
					} catch (Exception e) { throw new Exception("Failed to verify: " + note, e); }
				}
				void ShouldThrow(string note, Node<N1> a, string submsg) {
					Exception c = null;
					try {
						Node<N1> uniqued = Uniquify(a);
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
				void Verify(string note, Node<N1> a, Node<N1> b) {
					try {
						Node<N1> reduced = Reduce(a);
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
				Node<N1> program = Let("x",
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
				
				Node<N1> reduced = Reduce(program);

				int result2 = Run(reduced);
				result2.ShouldBe(expected1);
				
				Node<N1> pevaled = PartialEvaluate(program);

				int result3 = Run(pevaled);
				result3.ShouldBe(expected1);
				
				Node<N1> pevaledAndReduced = Reduce(pevaled);

				int result4 = Run(pevaledAndReduced);
				result4.ShouldBe(expected1);
				
				Node<N1> reducedAndUniqued = Uniquify(reduced);
				int result5 = Run(reducedAndUniqued);
				result5.ShouldBe(expected1);
				
			}
			
			public static void TestReduction() {
				Node<N1> pg = Add(  // (-(-(5)) + (let x=6,y=6 in x+y)
					Int(5),
					Let("x", Int(6),
						Let("y", Int(6),//lets are complex inners, and need to be reduced
							Add(Var("x"), Var("y"))
						)
					)
				);

				int expected = 17;
				Node<N1> pgreduced = Reduce(pg);
				pgreduced.type.ShouldBe(N1.Let); // A Let should be promoted to the top

				int result1 = Run(pg);
				int result2 = Run(pgreduced);
				result1.ShouldBe(expected);
				result2.ShouldBe(expected);

				Node<N1> pg2 = Add(  // (-(-5) + (let x=6,y=6 in x+y)
					Negate(Int(-5)),
					Let("x", Int(6),
						Let("y", Int(6),//lets are complex inners, and need to be reduced
							Add(Var("x"), Var("y"))
						)
					)
				);

				Node<N1> pg2Reduced = Reduce(pg2);
				int result3 = Run(pg2Reduced);
				result3.ShouldBe(expected);
				//Log.Info(pg2Reduced.ToString<N1>());
				

				Node<N1> pg3 = Add(  // (-(-(5)) + (let x=6,y=6 in x+y)
					Negate(Negate(Int(5))),
					Let("x", Int(6),
						Let("y", Int(6),//lets are complex inners, and need to be reduced
							Add(Var("x"), Var("y"))
						)
					)
				);

				Node<N1> pg3Reduced = Reduce(pg3);
				//Log.Info(pg3Reduced.ToString<N1>());
				int result4 = Run(pg3Reduced);

			}
			public static void TestPartialEvaluate() {
				Node<N1> prog = Add(Int(1), Add(Int(2), Int(3)));
				Node<N1> peval = PartialEvaluate(prog);
				peval.ShouldEqual(Int(6));
			}
			public static void TestAdvancedPartialEvaluate() {
				Node<N1> prog = Add(Int(1), Add(Read(), Int(1)));
				var peval = PartialEvaluate(prog);
				peval.ShouldEqual(Add(Int(2), Read()));

				Node<N1> prog2 = Add(Add(Read(), Int(1)), Add(Read(), Int(1)));
				var peval2 = PartialEvaluate(prog2);
				peval2.ShouldEqual(Add(Int(2), Add(Read(), Read())));
				
				Node<N1> prog3 = Add(Add(Read(), Int(1)), Add(Var("x"), Int(1)));
				var peval3 = PartialEvaluate(prog3);
				peval3.ShouldEqual(Add(Int(2), Add(Read(), Var("x"))));
				
				Node<N1> prog4 = Add(Add(Int(1), Var("x")), Add(Read(), Int(1)));
				var peval4 = PartialEvaluate(prog4);
				peval4.ShouldEqual(Add(Int(2), Add(Var("x"), Read())));
			}
			
		}
		#endregion

		#region Parser
		/// <summary> Impossible token representing type for names </summary>
		public const string NAME = "!NAME";
		/// <summary> Impossible token representing type for numbers </summary>
		public const string NUMBER = "!NUM";
		/// <summary> Impossible token representing type for strings</summary>
		public const string STRING = "!STR";
		/// <summary> type for spaces</summary>
		public const string SPACE = " ";
		/// <summary> type for tabs </summary>
		public const string TAB = "\t";
		/// <summary> type for newlines </summary>
		public const string NEWLINE = "\n";
		/// <summary> Keywords of the language </summary>
		public static readonly string[] keywords = {
			"let", "ni", "is", "in", "end", "read",
		};

		/// <summary> Punctuation of the language, with larger constructs checked first. </summary>
		public static readonly string[] punct = {
			// Size 3
			
			// Size 2

			// Size 1
			"+", "-", "(", ")"
		};
		/// <summary> Regex pattern for matching names </summary>
		public const string nameRegex = @"[a-zA-Z_\$][a-zA-Z0-9_\$]*";
		/// <summary> Regex checker for names </summary>
		public static readonly Regex name = new Regex(nameRegex);

		/// <summary> Regex pattern for matching numbers </summary>
		public const string numRegex = @"(0x[0-9A-Fa-f]+[lL]?)|(\d+\.\d*[fF]?)|(\d*\.\d+[fF]?)(0x[0-9A-Fa-f]+[lL]?)|(\d+[lL]?)|(\d+\.\d*[fF]?)|(\d*\.\d+[fF]?)";
		/// <summary> Regex checker for numbers </summary>
		public static readonly Regex num = new Regex(numRegex);


		public static Node<N1> ParseProgram(this Tokenizer tok) {
			return tok.ParseExpression();
		}

		public static string[] LEAFS = { "!NAME", "!NUM", "read" };

		public static Node<N1> ParseExpression(this Tokenizer tok) {
			if (tok.At("let")) { return tok.ParseLet(); }
			if (tok.At("(")) {
				tok.Next();
				Node<N1> inner = tok.ParseExpression();
				tok.RequireNext(")");
				return inner;
			}
			if (tok.At("-")) {
				tok.Next();
				Node<N1> negate = new Node<N1>(N1.Negate);
				Node<N1> expr = tok.ParseExpression();
				negate.List(expr);
				return negate;

			}
			if (tok.At(LEAFS)) {
				Node<N1> leaf = tok.ParseLeaf();
				if (tok.At("+")) {
					tok.Next();
					return Add(leaf, tok.ParseExpression());
				} else {
					return leaf;
				}
				
			}
			tok.Error($"Unknown start of expression {tok.peekToken}");
			return null;
		}
		public static Node<N1> ParseLeaf(this Tokenizer tok) {
			if (tok.At("!NAME")) {
				Node<N1> var = new Node<N1>(N1.Var);
				var.List(tok.peekToken);
				tok.Next();
				return var;
			}
			if (tok.At("!NUM")) {
				Node<N1> num = new Node<N1>(N1.Int);
				num.List(tok.peekToken);
				tok.Next();
				return num;
			}
			if (tok.At("read")) {
				return new Node<N1>(N1.Read);
			}
			tok.Error("Unknown leaf value token");
			return null;
		}

		public static Node<N1> ParseLet(this Tokenizer tok) {
			tok.RequireNext("let");
			tok.RequireNext("ni");
			tok.Require("!NAME");
			
			Token name = tok.peekToken; tok.Next();
			tok.RequireNext("is");

			Node<N1> value = tok.ParseExpression();
			tok.RequireNext("in");
			Node<N1> expr = tok.ParseExpression();
			tok.RequireNext("end");
			
			Node<N1> let = new Node<N1>(N1.Let);
			let.List(name);
			let.List(value);
			let.List(expr);
			return let;
		}
		#endregion

		#region Tokenizer
		/// <summary> Represents a stream of tokens coming from a loaded script </summary>
		public class Tokenizer {

			/// <summary> Original source string </summary>
			public string src { get; private set; }

			/// <summary> Current column </summary>
			public int col { get; private set; } = 0;
			/// <summary> Current line </summary>
			public int line { get; private set; } = 1;
			/// <summary> Current raw char position </summary>
			public int i { get; private set; } = 0;


			/// <summary> Token ahead of cursor, which hasn't been consumed. </summary>
			public Token peekToken { get; private set; }
			/// <summary> Last consumed token </summary>
			public Token lastToken { get; private set; }
			/// <summary> Last consumed non-whitespace token </summary>
			public Token lastRealToken { get; private set; }

			/// <summary> Gets the content from the current peek token </summary>
			public string content { get { return peekToken.content; } }

			/// <summary> Gets the type from the current peek token </summary>
			public string atType { get { return peekToken.type; } }


			/// <summary> Basic Constructor.</summary>
			/// <param name="source"> Source file to read. </param>
			public Tokenizer(string source) {
				src = source.Replace("\r\n", "\n").Replace("\r", "\n");
				Reset();
			}

			/// <summary> Throw an exception with a given message </summary>
			/// <param name="message"></param>
			public void Error(string message) { throw new Exception($"{message}\n{this}"); }

			public override string ToString() {
				return $"Source near line {line}:"
					+ $"\n-------------------===="
					+ $"\n{LinesNear(line)}"
					+ $"\n-------------------===="
					+ $"\nToken: {peekToken} (Line {line} : Col {col})";
			}

			public string LinesNear(int line, int around = 3) {
				StringBuilder str = new StringBuilder();
				int curLine = 0;
				int i = 0;
				for (; i < src.Length; i++) {
					if (src[i] == '\n') { curLine++; }
					if (curLine >= line - around) { i++; break; }
				}
				str.Append($"{curLine:d5}: ");
				for (; i < src.Length; i++) {
					str.Append(src[i]);
					if (src[i] == '\n') { curLine++; str.Append($"{curLine:d5}: "); }
					if (curLine >= line + around) { break; }
				}
				return str.ToString();
			}

			/// <summary> Throws an exception if the peekToken is NOT the given type. </summary>
			public void Require(string type) { if (!peekToken.Is(type)) { Error($"Expected: [{type}]"); } }

			/// <summary> Throws an exception if the peekToken is NOT one of the given types. </summary>
			public void Require(string[] types) {
				if (!peekToken.Is(types)) {
					string str = "";
					foreach (string s in types) { str += s + ", "; }
					Error("Expected " + str);
				}
			}

			/// <summary> Throws an exception if the peekToken is NOT the given type, 
			/// but if it is, consumes it. </summary>
			public void RequireNext(string type) { Require(type); Next(); }

			/// <summary> Throws an exception if the peekToken is NOT one of the given types, 
			/// but if it is, consumes it. </summary>
			public void RequireNext(string[] types) { Require(types); Next(); }


			/// <summary> Returns if the peekToken is a given type </summary>
			public bool At(string type) { return peekToken.Is(type); }

			/// <summary> Returns if the peekToken is one of a given set of types </summary>
			public bool At(string[] types) { return peekToken.Is(types); }

			/// <summary> Returns true if the Tokenizer is out of tokens. </summary>
			public bool Done { get { return !peekToken.IsValid; } }

			/// <summary> Resets the tokenizer to its initial state </summary>
			public void Reset() {
				i = 0;
				line = 1;
				col = 0;
				lastRealToken = lastToken = Token.INVALID_TOKEN;
				peekToken = Peek();
				while (peekToken.IsWhitespace) {
					Move();
				}
			}


			/// <summary> Get the next token </summary>
			/// <returns> Token that has been removed from context, or the current token if invalid. </returns>
			public Token Next() {
				if (peekToken.IsValid) {
					Token save = lastRealToken;

					if (Move()) {
						while (peekToken.IsValid && peekToken.IsWhitespace) { Move(); }
					}

					return save;
				}

				return peekToken;
			}

			/// <summary> Moves this tokenizer forward by one token. </summary>
			/// <returns> True if moved at all, false if nothing happened. </returns>
			public bool Move() {
				if (peekToken.IsValid) {
					if (peekToken.Is(NEWLINE)) { col = 0; line++; } 
					else { col += peekToken.content.Length; }

					i += peekToken.content.Length;
					lastToken = peekToken;
					if (!peekToken.IsWhitespace) { lastRealToken = peekToken; }
					peekToken = Peek();
				}
				return peekToken.IsValid;
			}

			/// <summary> Peeks ahead at the next token that has yet to be consumed. </summary>
			/// <returns> Next token sitting in front of the head, or an invalid token if out of characters or WTF. </returns>
			public Token Peek() {
				if (i >= src.Length) { return Token.Done(line, col); }
				// Whitespace
				char c = src[i];
				if (c == ' ') { return new Token(SPACE, line, col); }
				if (c == '\t') { return new Token(TAB, line, col); }
				if (c == '\n') { return new Token(NEWLINE, line, col); }
				if (c == '\r') { throw new Exception("Oops CLRF! Get fucked!"); }
				if (c == '\"') { return ExtractString('\"'); }
				if (c == '\'') { return ExtractString('\''); }
				foreach (string p in punct) { if (src.MatchAt(i, p)) { return new Token(p, line, col); } }
				foreach (string k in keywords) { if (src.MatchAt(i, k) && !src.AlphaNumAt(i + k.Length)) { return new Token(k, line, col); } }

				Match nameCheck = name.Match(src, i);
				if (nameCheck.Success && nameCheck.Index == i) { return new Token(nameCheck.Value, NAME, line, col); }

				Match numCheck = num.Match(src, i);
				if (numCheck.Success && numCheck.Index == i) { return new Token(numCheck.Value, NUMBER, line, col); }

				return Token.Invalid(line, col);
			}

			/// <summary> Error token message if a newline is sitting inside of a string literal.</summary>
			public static readonly string BAD_STRING_NEWLINE_INSIDE = "Newline in string literal";
			/// <summary> Error token message if no matching character for a string delimeter. </summary>
			public static readonly string BAD_STRING_NO_MATCHING_QUOTE = "No matching quote for string literal";

			/// <summary> Extracts a string from the current position in the source </summary>
			/// <param name="match"> Character to match on the other end of the string region </param>
			/// <returns> Token created from matched string, or an error token if it FAILED. </returns>
			private Token ExtractString(char match) {
				// Find next actual newline
				int nextNL = src.IndexOf('\n', i + 1);
				// Make index always greater than any characters in the string.
				if (nextNL == -1) { nextNL = src.Length + 1; }

				int nextMatch = src.IndexOf(match, i + 1);
				while (true) {
					if (nextMatch == -1) { return new Token(BAD_STRING_NO_MATCHING_QUOTE, Token.INVALID, line, col); }
					if (nextMatch > nextNL) { return new Token(BAD_STRING_NEWLINE_INSIDE, Token.INVALID, line, col); }
					if (src[nextMatch - 1] != '\\') { break; }
					nextMatch = src.IndexOf(match, nextMatch + 1);
				}

				int len = nextMatch - i + 1;
				return new Token(src.Substring(i, len), STRING, line, col);
			}

		}
		#endregion

	}


}
