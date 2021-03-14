using BakaTest;
using Ex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ni_compiler {

	public static class N1Lang {

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
		public static int Run(Node n) { return Interp(n); }

		public static int Interp(Node n, Env<int> env = null) {
			if (env == null) { env = new Env<int>(); }
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
			static string badprog = @"let ni omg is wtf in bbq gtfo";
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
				Node parsed = tok.ParseExpression();

				parsed.type.ShouldBe(N1.Let.Ord());
				parsed.datas[0].ShouldBe("y");
				parsed.nodes[0].type.ShouldBe(N1.Let.Ord());
				parsed.nodes[0].nodes[0].type.ShouldBe(N1.Int.Ord());
				parsed.nodes[0].nodes[1].type.ShouldBe(N1.Add.Ord());
				parsed.nodes[0].nodes[1].nodes[0].type.ShouldBe(N1.Var.Ord());
				parsed.nodes[0].nodes[1].nodes[1].type.ShouldBe(N1.Let.Ord());

				parsed.nodes[1].type.ShouldBe(N1.Var.Ord());

				try {
					Tokenizer badTok = new Tokenizer(badprog);
					Node failed = badTok.ParseExpression();
				} catch (Exception e) {
					e.Message.Contains("line 1").ShouldBeTrue();
					e.Message.Contains("Expected: [end]").ShouldBeTrue();
				}

			}
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


		public static Node ParseProgram(this Tokenizer tok) {
			return tok.ParseExpression();
		}

		public static string[] LEAFS = { "!NAME", "!NUM", "read" };

		public static Node ParseExpression(this Tokenizer tok) {
			if (tok.At("let")) { return tok.ParseLet(); }
			if (tok.At("(")) {
				tok.Next();
				Node inner = tok.ParseExpression();
				tok.RequireNext(")");
				return inner;
			}
			if (tok.At("-")) {
				tok.Next();
				Node negate = new Node(N1.Negate.Ord());
				Node expr = tok.ParseExpression();
				negate.List(expr);
				return negate;

			}
			if (tok.At(LEAFS)) {
				Node leaf = tok.ParseLeaf();
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
		public static Node ParseLeaf(this Tokenizer tok) {
			if (tok.At("!NAME")) {
				Node var = new Node(N1.Var.Ord());
				var.List(tok.Next());
				return var;
			}
			if (tok.At("!NUM")) {
				Node num = new Node(N1.Int.Ord());
				num.List(tok.Next());
				return num;
			}
			if (tok.At("read")) {
				return new Node(N1.Read.Ord());
			}
			tok.Error("Unknown leaf value token");
			return null;
		}

		public static Node ParseLet(this Tokenizer tok) {
			tok.RequireNext("let");
			tok.RequireNext("ni");
			tok.Require("!NAME");
			
			Token name = tok.peekToken; tok.Next();
			tok.RequireNext("is");

			Node value = tok.ParseExpression();
			tok.RequireNext("in");
			Node expr = tok.ParseExpression();
			tok.RequireNext("end");
			
			Node let = new Node(N1.Let.Ord());
			let.List(name);
			let.List(value);
			let.List(expr);
			return let;
		}

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
	}
	

}
