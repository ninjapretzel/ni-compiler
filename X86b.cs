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

		public static Arg AtmToArg(Node<C0> atm) {
			switch (atm.type) {
				case C0.Int: { return int.Parse(atm.datas[0]); }
				case C0.Var: { return atm.datas[0]; }
			}
			throw new Exception($"C0.{atm.type} is not a valid atomic type");
		}

		public static LL<Instr> SelectStmt(Node<C0> exp, Arg dest) {
			switch (exp.type) {
				case C0.Atm: { return new LL<Instr>(Movq(AtmToArg(exp.nodes[0]), dest)); }
				case C0.Add: {
					return new LL<Instr>(Addq(AtmToArg(exp.nodes[0]), dest))
						.Add(Movq(AtmToArg(exp.nodes[1]), dest));
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
					var next = SelectTail(assign.nodes[1]);
					var stmt = SelectStmt(assign.nodes[0], assign.datas[0]);
					return stmt+next;
				} 
				case C0.Return:{
					return new LL<Instr>(Jmpq("conclusion"))
						.Add(Movq(AtmToArg(tail.nodes[0]), RAX));
				}
			}
			throw new Exception($"C0.{tail.type} is not a tail type");
		}

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
							if (cmp == 0) {
								return imm.CompareTo(other.imm);
							}
							return cmp;
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
			public static void TestA() {
				var res = SelectStmt(Atm(Int(5)), "x");
				var expected = new LL<Instr>(Movq(5, "x"));
				res.ShouldEqual(expected);
			}
		}


	}
}
