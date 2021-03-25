using BakaTest;
using Ex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ni_compiler.C0Lang;

namespace ni_compiler {
	public static class X86bLang {

		#region PASS: Select Instructions
		public static (LL<Instr>, LL<string>) SIPass(Node<C0> c0, LL<string> names) {
			return (SelectTail(c0), names);
		}

		public static Arg AtmToArg(Node<C0> atm) {
			switch (atm.type) {
				case C0.Atm: { return AtmToArg(atm.nodes[0]); }
				case C0.Int: { return int.Parse(atm.datas[0]); }
				case C0.Var: { return atm.datas[0]; }
			}
			throw new Exception($"C0.{atm.type} is not a valid atomic type");
		}

		public static LL<Instr> SelectStmt(Node<C0> exp, Arg dest) {
			switch (exp.type) {
				case C0.Atm: { return new LL<Instr>(Movq(AtmToArg(exp.nodes[0]), dest)); }
				case C0.Add: {
					if (exp.nodes[0].type == C0.Var && dest.kind == Arg.Kind.Var && dest.var == exp.nodes[0].datas[0]) {
						return new LL<Instr>(Addq(AtmToArg(exp.nodes[1]), dest));
					}
					if (exp.nodes[1].type == C0.Var && dest.kind == Arg.Kind.Var && dest.var == exp.nodes[1].datas[0]) {
						return new LL<Instr>(Addq(AtmToArg(exp.nodes[0]), dest));
					}
							
					return new LL<Instr>(Addq(AtmToArg(exp.nodes[1]), dest))
						.Add(Movq(AtmToArg(exp.nodes[0]), dest));
				}
				case C0.Sub: {
					return new LL<Instr>(Negq(dest))
						.Add(Movq(AtmToArg(exp.nodes[0]), dest));
				}
			}
			throw new Exception($"C0.{exp.type} cannot be converted into a statement.");
		}
		public static LL<Instr> SelectTail(Node<C0> tail) {
			switch (tail.type) {
				case C0.Seq:{ 
					var assign = tail.nodes[0];
					var next = SelectTail(tail.nodes[1]);
					var stmt = SelectStmt(assign.nodes[0], assign.datas[0]);
					return stmt+next;
				} 
				case C0.Return:{
					// Construction of return is like Return(Atm(Int(5)))
					return new LL<Instr>(Jmpq("conclusion")) 
						.Add(Movq(AtmToArg(tail.nodes[0]), RAX));
				}
			}
			throw new Exception($"C0.{tail.type} is not a tail type");
		}
		#endregion

		#region PASS: Assign Homes
		public static (LL<Instr>, Env<Arg>, int) AssignHomes(LL<Instr> instrs, LL<string> vars) {
			var env = AssignStackLocations(vars, RBP, -8, 8);
			int cnt = env.size;
			return (AssignHomes(env, instrs), env, cnt);
		}
		public static Env<Arg> AssignStackLocations(LL<string> vars, Register reg, int offset, int size) {
			if (vars == null) { return new Env<Arg>(); }
			return AssignStackLocations(vars.next, reg, offset-size, size).Extend(vars.data, new Arg(reg, offset));
		}
		public static LL<Instr> AssignHomes(Env<Arg> env, LL<Instr> block) {
			if (block == null) { return null; }
			var instrs = AssignHomes(env, block.next);
			return instrs.Add(AssignHome(env, block.data));
		}
		public static (int, string, char) Foo(int val) {
			return (val * 2, ""+val, (char)val);
		}

		public static Instr AssignHome(Env<Arg> env, Instr instr) {
			switch (instr.kind) {
				case Instr.Kind.Jmp:
				case Instr.Kind.Retq:
				case Instr.Kind.Callq: return instr;
				case Instr.Kind.Negq: return Negq(AssignHomeArg(env, instr.arg1));
				case Instr.Kind.Popq: return Popq(AssignHomeArg(env, instr.arg1));
				case Instr.Kind.Pushq: return Pushq(AssignHomeArg(env, instr.arg1));
				case Instr.Kind.Addq: return Addq(AssignHomeArg(env, instr.arg1), AssignHomeArg(env, instr.arg2));
				case Instr.Kind.Subq: return Subq(AssignHomeArg(env, instr.arg1), AssignHomeArg(env, instr.arg2));
				case Instr.Kind.Movq: return Movq(AssignHomeArg(env, instr.arg1), AssignHomeArg(env, instr.arg2));
			}
			throw new Exception($"Unknown instruction {instr.kind}");
		}
		public static Arg AssignHomeArg(Env<Arg> env, Arg arg) {
			switch (arg.kind) {
				case Arg.Kind.Var: return env[arg.var];
				default: return arg;
			}
		}
		#endregion
		#region PASS: Patch Instructions
		public static LL<Instr> PatchInstructions(LL<Instr> instrs) {
			return PatchBlock(instrs);
		}
		public static LL<Instr> PatchBlock(LL<Instr> block) {
			if (block == null) { return null; }
			return PatchInstr(block.data) + PatchBlock(block.next);

		}
		public static LL<Instr> PatchInstr(Instr instr) {
			if (instr.arg1 != null && instr.arg1.kind == Arg.Kind.Mem && instr.arg2 != null && instr.arg2.kind == Arg.Kind.Mem) {
				switch (instr.kind) {
					case Instr.Kind.Movq: {
						return new LL<Instr>(Movq(instr.arg1, RAX),
								new LL<Instr>(Movq(RAX, instr.arg2)));
					}
					case Instr.Kind.Addq: {
						return new LL<Instr>(Movq(instr.arg1, RAX),
								new LL<Instr>(Addq(RAX, instr.arg2)));
					}
					case Instr.Kind.Subq: {
						return new LL<Instr>(Movq(instr.arg1, RAX), 
								new LL<Instr>(Subq(RAX, instr.arg2)));
					}

					default: throw new Exception($"{instr.kind} should not have two memory arguments!");
				}
			}
			return new LL<Instr>(instr);
		}
		#endregion

		#region Liveness Detection & Interference
		public class LivenessData {
			public Set<Arg> liveAfter { get; private set; }
			public Set<Arg> reads { get; private set; }
			public Set<Arg> writes { get; private set; }
			public Set<Arg> liveBefore { get; private set; }
			public Instr instruction { get; private set; }
			public LivenessData() {
				var empty = new Set<Arg>();
				liveAfter = reads = writes = liveBefore = empty;
				instruction = null;
			}
			public LivenessData(Set<Arg> LA, Set<Arg> R, Set<Arg> W, Set<Arg> LB, Instr ins = null) {
				liveAfter = LA;
				reads = R;
				writes = W;
				liveBefore = LB;
				instruction = ins;
			}
			public override string ToString() {
				return $"{instruction,-20}{liveAfter,-20}{reads,-15}{writes,-10}";
			}
		}
		public static (Set<Arg>, LL<LivenessData>) LiveCheck(LL<Instr> instrs) {
			if (instrs == null) { 
				var initial = new LivenessData();
				return (initial.liveBefore, new LL<LivenessData>(initial)); 
			}
			(var liveAfter, var rest) = LiveCheck(instrs.next);
			var read = ReadSet(instrs.data);
			var write = WriteSet(instrs.data);
			var liveBefore = (liveAfter - write) + read;
			var liveData = new LivenessData(liveAfter, read, write, liveBefore, instrs.data);

			return (liveBefore, rest.Add(liveData));
		}
		public static Graph Interference(LL<Instr> instrs) {
			(var liveBefore, var liveness) = LiveCheck(instrs);
			return Interference(liveness);
		}
		public static Graph Interference(LL<LivenessData> liveness) {
			if (liveness == null) { return new Graph(); }
			
			Graph justHere = new Graph();

			var cur = liveness.data;
			foreach (var write in cur.writes) {
				Set<Arg> interfere = new Set<Arg>();
				foreach (var arg in cur.liveAfter) {
					if (!cur.writes.Contains(arg) 
						&& (cur.instruction.kind == Instr.Kind.Movq ? !cur.reads.Contains(arg) : true)) {
						
						justHere = justHere.OneWay(write, arg);
					}
				}		
			}

			Graph g = Interference(liveness.next);
			foreach (var pair in justHere) {
				foreach (var other in pair.Value) {
					g = g.Interfere(pair.Key, other);
				}
			}

			return g;
		}
			
		public class Graph : Dictionary<Arg, Set<Arg>> {
			public Graph() : base() { }
			public Graph(Graph other)  : base(other) { }

			public Graph Interfere(Arg a, Arg b) {
				Graph g = new Graph(this);
				// Console.WriteLine($"Interfering {a} with {b}");
				if (!g.ContainsKey(a)) { g[a] = new Set<Arg>(); }
				if (!g.ContainsKey(b)) { g[b] = new Set<Arg>(); }
				g[a] += b;
				g[b] += a;
				return g;
			}
			public Graph OneWay(Arg a, Arg b) {
				Graph g = new Graph(this);
				if (!g.ContainsKey(a)) { g[a] = new Set<Arg>(); }
				g[a] += b;
				return g;
			}
			public override string ToString() {
				return ToString(false);
			}
			public string ToString(bool insertNewlines) {
				StringBuilder str = new StringBuilder("{");
				if (insertNewlines) { str.Append("\n\t"); }

				foreach (var pair in this) {
					str.Append(pair.Key);
					str.Append(" ~~ {");
					foreach (var other in pair.Value) {
						str.Append(other);
						str.Append(",");
					}
					str.Append("}");
					if (insertNewlines) {
						str.Append("\n\t");
					}
				}

				if (insertNewlines) { str.Append("\n"); }
				str.Append("}");
				return str.ToString();
			}
		}

		public static Set<Arg> ReadSet(Instr instr) {
			Set<Arg> s = new Set<Arg>();
			void maybeAdd(Arg arg) {
				if (arg.kind == Arg.Kind.Var) { s = s.Add(arg); }
				if (arg.kind == Arg.Kind.Mem) { s = s.Add(arg); }
				if (arg.kind == Arg.Kind.Reg) { s = s.Add(arg); }
			}
			switch (instr.kind) {
				case Instr.Kind.Negq:
				case Instr.Kind.Pushq:
				case Instr.Kind.Movq: { maybeAdd(instr.arg1);  break; }
				case Instr.Kind.Subq: 
				case Instr.Kind.Addq: { maybeAdd(instr.arg1); maybeAdd(instr.arg2); break; }
				case Instr.Kind.Jmp: { s = s.Add(RSP); break; }
			}
			return s;
		}
		public static Set<Arg> WriteSet(Instr instr) {
			Set<Arg> s = new Set<Arg>();
			void maybeAdd(Arg arg) {
				if (arg.kind == Arg.Kind.Var) { s = s.Add(arg); }
				if (arg.kind == Arg.Kind.Mem) { s = s.Add(arg); }
				if (arg.kind == Arg.Kind.Reg) { s = s.Add(arg); }
			}
			switch (instr.kind) {
				case Instr.Kind.Negq:
				case Instr.Kind.Pushq: { maybeAdd(instr.arg1); break; }
				case Instr.Kind.Addq:
				case Instr.Kind.Subq:
				case Instr.Kind.Movq: {maybeAdd(instr.arg2); break; }
				case Instr.Kind.Callq: { s = s.AddAll(CALLEE_SAVED); break; }
			}
			return s;
		}
		#endregion


		#region Definitions
		public struct Register : IComparable<Register> {
			public readonly string name;
			public readonly int ord;
			public Register(string s, int i) { name = s; ord = i; }
			public int CompareTo(Register other) { return ord.CompareTo(other.ord); }
			public override bool Equals(object obj) {
				if (obj is Register other) { return other.name == name && other.ord == ord; }
				return false;
			}
			public override string ToString() { return name; }
			public override int GetHashCode() { return name.GetHashCode() ^ ord.GetHashCode(); }
			public static implicit operator Register((string s, int i) _) { return new Register(_.s, _.i); }
		}
		#region Registers
		public static readonly Register RSP = ("%rsp", 0);
		public static readonly Register RBP = ("%rbp", 1);
		public static readonly Register RAX = ("%rax", 2);
		public static readonly Register RBX = ("%rbx", 3);
		public static readonly Register RCX = ("%rcx", 4);
		public static readonly Register RDX = ("%rdx", 5);
		public static readonly Register RSI = ("%rsi", 6);
		public static readonly Register RDI = ("%rdi", 7);
		public static readonly Register R8 = ("%r8", 8);
		public static readonly Register R9 = ("%r9", 9);
		public static readonly Register R10 = ("%r10", 10);
		public static readonly Register R11 = ("%r11", 11);
		public static readonly Register R12 = ("%r12", 12);
		public static readonly Register R13 = ("%r13", 13);
		public static readonly Register R14 = ("%r14", 14);
		public static readonly Register R15 = ("%r15", 15);
		#endregion
		
		public static Arg[] CALLER_SAVED = new Arg[] { RAX, RCX, RDX, RSI, RDI, R8, R9, R10, R11 };
		public static Arg[] CALLEE_SAVED = new Arg[] { RSP, RBP, RBX, R12, R13, R14, R15 };
		public static Arg[] PARAMETERS = new Arg[] { RDI, RSI, RDX, RCX, R8, R9 };
		public static Arg[] RETURNS = new Arg[] { RAX };
		public static Arg[] REGISTER_PRIORITY = new Arg[] { RBX, RCX, RDX, RSI, RDI, R8, R9, R10, R11, R12, R13, R14, R15 };

		public class Arg : IComparable<Arg> {
			public enum Kind { Reg, Mem, Imm, Var }
			public readonly Kind kind;
			public readonly long imm;
			public readonly Register reg;
			public readonly string var;
			public Arg(long imm) { kind = Kind.Imm; this.imm = imm; reg = default; var = null; }
			public Arg(Register reg) { kind = Kind.Reg; this.reg = reg; imm = -1; var = null; }
			public Arg(Register reg, long immoffset) { kind = Kind.Mem; this.reg = reg; imm = immoffset; var = null; }
			public Arg(string var) { kind = Kind.Var; this.var = var; reg = default; imm = -1; }
			public static implicit operator Arg(int imm) { return new Arg(imm); }
			public static implicit operator Arg(long imm) { return new Arg(imm); }
			public static implicit operator Arg(Register reg) { return new Arg(reg); }
			public static implicit operator Arg((Register reg, int imm) _) { return new Arg(_.reg, _.imm); }
			public static implicit operator Arg(string var) { return new Arg(var); }
			public override string ToString() {
				switch (kind) {
					case Kind.Imm: return $"${imm}";
					case Kind.Reg: return $"{reg}";
					case Kind.Mem: return $"{imm}({reg})";
					case Kind.Var: return var;
					default: return "UnknownArg";
				}
			}
			public override bool Equals(object obj) {
				if (obj is Arg other && kind == other.kind) {
					switch (kind) {
						case Kind.Imm: return imm == other.imm;
						case Kind.Reg: return reg.Equals(other.reg);
						case Kind.Mem: return reg.Equals(other.reg) && imm == other.imm;
						case Kind.Var: return var == other.var;
					}
				}
				return false;
			}
			public override int GetHashCode() {
				switch (kind) {
					case Kind.Imm: return imm.GetHashCode();
					case Kind.Reg: return reg.GetHashCode();
					case Kind.Mem: return reg.GetHashCode() ^ imm.GetHashCode();
					case Kind.Var: return var.GetHashCode();
					default: throw new Exception($"Cannot hash Arg [{this}]");
				}
			}
			public int CompareTo(Arg other) {
				if (other.kind != kind) { return kind < other.kind ? -1 : 1; }
				switch (kind) {
					case Kind.Imm: return imm.CompareTo(other.imm);
					case Kind.Reg: return reg.CompareTo(other.reg);
					case Kind.Mem: {
						int cmp = reg.CompareTo(other.reg);
						return cmp == 0 ? imm.CompareTo(other.imm) : cmp;
					}
					case Kind.Var: return var.CompareTo(other.var);
				}
				throw new Exception($"Invalid Arg Comparison [{this}] -> [{other}]");
			}
		}

		public struct Label {
			public readonly string content;
			public Label(string s) { content = s; }
			public static implicit operator Label(string s) { return new Label(s); }
			public override string ToString() { return content; }
		}

		public class Instr {
			public enum Kind {
				Addq, Subq, Movq, Negq,
				Callq, Retq, Pushq, Popq, Jmp
			}
			public readonly Kind kind;
			public readonly Arg arg1;
			public readonly Arg arg2;
			public readonly string label;
			public readonly int arity;
			public Instr(Kind k, Arg a = null, Arg b = null, string s = null, int n = 0) {
				kind = k;
				arg1 = a;
				arg2 = b;
				label = s;
				arity = n;
			}
			public override bool Equals(object obj) {
				if (obj is Instr other && kind == other.kind) {
					if (arity != other.arity) { return false; }
					if (label != other.label) { return false; }
					if (arg1 != null && !arg1.Equals(other.arg1)) { return false; }
					if (arg1 == null && other.arg1 != null) { return false; }
					if (arg2 != null && !arg2.Equals(other.arg2)) { return false; }
					if (arg2 == null && other.arg2 != null) { return false; }
					return true;
				}
				return false;
			}
			public override int GetHashCode() {
				return kind.GetHashCode()
					^ ((arg1?.GetHashCode() ?? -1))
					^ ((arg2?.GetHashCode() ?? -1) << 3)
					^ ((label?.GetHashCode() ?? -1) >> 2)
					^ (arity.GetHashCode() << 5);
			}
			public override string ToString() {
				switch (kind) {
					case Kind.Addq: return $"addq   {arg1}, {arg2}";
					case Kind.Subq: return $"subq   {arg1}, {arg2}";
					case Kind.Movq: return $"movq   {arg1}, {arg2}";
					case Kind.Negq: return $"negq   {arg1}";
					case Kind.Callq: return $"callq  {label}";
					case Kind.Retq: return $"retq";
					case Kind.Pushq: return $"pushq  {arg1}";
					case Kind.Popq: return $"popq   {arg1}";
					case Kind.Jmp: return $"jmp    {label}";
					default: return $"Unknown instruction [{kind}, {arg1}, {arg2}, {label}, {arity}]";
				}
			}

		}
		public static Instr Addq(Arg a, Arg b) { return new Instr(Instr.Kind.Addq, a, b); }
		public static Instr Subq(Arg a, Arg b) { return new Instr(Instr.Kind.Subq, a, b); }
		public static Instr Movq(Arg a, Arg b) { return new Instr(Instr.Kind.Movq, a, b); }
		public static Instr Negq(Arg a) { return new Instr(Instr.Kind.Negq, a); }
		public static Instr Callq(string label, int arity) { return new Instr(Instr.Kind.Callq, null, null, label, arity); }
		public static Instr Retq() { return new Instr(Instr.Kind.Retq); }
		public static Instr Pushq(Arg a) { return new Instr(Instr.Kind.Pushq, a); }
		public static Instr Popq(Arg a) { return new Instr(Instr.Kind.Popq, a); }
		public static Instr Jmpq(string label) { return new Instr(Instr.Kind.Jmp, null, null, label); }

		public class Block : LL<Instr> {
			public Block(Instr data, LL<Instr> next) : base(data, next) { }
		}
		
		public static string ToString(this Block b) {
			StringBuilder str = new StringBuilder("        ");
			foreach (var instr in b) { str.Append($"\n        {instr}"); }
			return str.ToString();
		}
		
		public class X86b : LL<(string label, Block block)> {
			public X86b((string label, Block block) data, LL<(string label, Block block)> next) : base(data, next) { }
		}
		public static string ToString(this X86b program) {
			StringBuilder str = new StringBuilder(".global _main\n\n");

			if (program == null) {
				str.Append("        retq");
			} else {
				foreach (var pair in program) {
					str.Append($"{pair.label}:\n{pair.block}\n");
				}
			}

			return str.ToString();
		}

		public static string prelude = @"
.global _main
_main:
	pushq  %rbp
	movq   %rsp, %rbp
	jmp    start";
		public static string conclusion = @"
connclusion:
	popq   %rbp
	retq";
		#endregion

		public static class _Tests {
			public static void TestSelectIntAssign() {
				var res = SelectStmt(Atm(Int(5)), "x");
				var expected = new LL<Instr>(Movq(5, "x"));
				res.ShouldEqual(expected);
			}
			public static void TestSelectAddAssign() {
				var res = SelectStmt(Add(Int(3), Int(2)), "x");
				var expected = LL<Instr>.From(Movq(3, "x"), Addq(2, "x"));
				res.ShouldEqual(expected);
			}
			public static void TestSelectNegateAssign() {
				var res = SelectStmt(Sub(Var("y")), "x");
				var expected = LL<Instr>.From(Movq("y", "x"), Negq("x"));
				res.ShouldEqual(expected);
			}
			public static void TestSelectReturn() {
				var res = SelectTail(Return(Atm(Int(5))));
				var expected = LL<Instr>.From(Movq(5, RAX), Jmpq("conclusion"));
				res.ShouldEqual(expected);
				var res2 = SelectTail(Return(Atm(Var("x"))));
				var expected2 = LL<Instr>.From(Movq(new Arg("x"), RAX), Jmpq("conclusion"));
				res2.ShouldEqual(expected2);
			}

			public static Node<C0> c0prog = Seq(
				Assign("x", Add(Int(3), Int(2))), 
				Seq(
					Assign("y", Atm(Int(6))),
					Seq(
						Assign("z", Add(Var("x"), Var("y"))),
						Return(Atm(Var("z")))
					)
				)
			);
			public static Node<C0> c0prog2 = Seq(
				Assign("x", Add(Int(5), Int(2))),
				Seq(
					Assign("x", Add(Var("x"), Int(10))),
					Seq(
						Assign("x", Add(Int(10), Var("x"))),
						Seq(
							Assign("x", Add(Var("x"), Var("x"))),
							Return(Atm(Var("x")))
						)
					)
				)
			);

			public static void TestSelectPass() {
				var instrs = SelectTail(c0prog);
				var expected = LL<Instr>.From(
					Movq(3, "x"),
					Addq(2, "x"),
					Movq(6, "y"),
					Movq("x", "z"),
					Addq("y", "z"),
					Movq("z", RAX),
					Jmpq("conclusion")
				);
				instrs.ShouldEqual(expected);
			}

			public static void TestAdvancedSelect() {
				var instrs = SelectTail(c0prog2);
				var expected = LL<Instr>.From(
					Movq(5, "x"),
					Addq(2, "x"),
					Addq(10, "x"),
					Addq(10, "x"),
					Addq("x", "x"),
					Movq("x", RAX),
					Jmpq("conclusion")
				);
				instrs.ShouldEqual(expected);
			}

			public static string Print(LL<Instr> instrs) {
				StringBuilder str = new StringBuilder();
				foreach (var ins in instrs) {
					str.Append(ins.ToString());
					str.Append("\n");
				}
				return str.ToString();
			}

			public static void TestAssignHomesDontChange() {
				var retq = new LL<Instr>(Retq());
				var addq = new LL<Instr>(Addq(RBX, RAX));

				(var aretq, _, _)= AssignHomes(retq, null);;
				(var aaddq, _, _) = AssignHomes(addq, null);;

				aretq.ShouldEqual(retq);
				aaddq.ShouldEqual(addq);
			}

			public static void TestAssignHomesPass() {
				var instrs = SelectTail(c0prog);
				var prog = 
@"let ni a is 42 in
	let ni b is a in
		b
	end
end";

				var parsed = N1Lang.ParseProgram(new N1Lang.Tokenizer(prog));
				var transformed = N1Lang.Reduce(parsed);
				(var c0d, var names) = N1Lang.Explicate(transformed);
				var asm = SelectTail(c0d);
				(var assigned, var env, var i) = AssignHomes(asm, names);

				var expected = LL<Instr>.From(
					Movq(42, (RBP, -8)),
					Movq((RBP, -8), (RBP, -16)),
					Movq((RBP, -16), RAX),
					Jmpq("conclusion")
				);

				assigned.ShouldEqual(expected);
			}

			public static void TestInterference() {
				LL<Instr> program = LL<Instr>.From(
					Movq(1, "v"),
					Movq(25, "w"),
					Movq("v", "x"),
					Addq(7, "x"),
					Movq("x", "y"),
					Movq("x", "z"),
					Addq("w", "z"),
					Movq("y", "t"),
					Negq("t"),
					Movq("z", RAX),
					Addq("t", RAX),
					Jmpq("conclusion")
				);

				var graph = Interference(program);
				graph[RAX].ShouldEqual(new Set<Arg>(RSP, "t"));
				graph[RSP].ShouldEqual(new Set<Arg>(RAX, "t", "z", "y", "x", "w", "v"));
				graph["t"].ShouldEqual(new Set<Arg>(RAX, RSP, "z"));
				graph["z"].ShouldEqual(new Set<Arg>("t", RSP, "y", "w"));
				graph["y"].ShouldEqual(new Set<Arg>("z", RSP, "w"));
				graph["w"].ShouldEqual(new Set<Arg>("z", "y", "x", RSP, "v"));
				graph["x"].ShouldEqual(new Set<Arg>(RSP, "w"));
				graph["v"].ShouldEqual(new Set<Arg>("w", RSP));

			}
			

		}


	}
}
