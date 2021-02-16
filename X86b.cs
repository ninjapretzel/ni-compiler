using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ni_compiler {
	public static class X86bLang {

		public enum Register {
			RSP, 
			RBP, RAX, RBX, RCX, RDX,
			RSI, RDI, R8, R9, R10,
			R11, R12, R13, R14, R15
		}
		public enum Instr {
			Addq, Subq, Movq, Negq, 
			Callq, Retq, Pushq, Popq, Jmp
		}
		public enum Loc { Imm, Reg, Mem, Var }

		private static readonly Dictionary<Register, string> REG_NAMES = new Dictionary<Register, string>() {
			{ Register.RSP, "%rsp" },
			{ Register.RBP, "%rbp" },
			{ Register.RAX, "%rax" },
			{ Register.RBX, "%rbx" },
			{ Register.RCX, "%rcx" },
			{ Register.RDX, "%rdx" },
			{ Register.RSI, "%rsi" },
			{ Register.RDI, "%rdi" },
			{ Register.R8, "%r8" },
			{ Register.R9, "%r9" },
			{ Register.R10, "%r10" },
			{ Register.R11, "%r11" },
			{ Register.R12, "%r12" },
			{ Register.R13, "%r13" },
			{ Register.R14, "%r14" },
			{ Register.R15, "%r15" },
		};
		public static string AsmName(this Register reg) { return REG_NAMES[reg]; }

		private static string ToAsm(this Node n) {
			switch ((Instr)n.type) {
				// case Instr.Addq: return $"addq   "
			}
			throw new Exception($"Unknown x86b instruction {(Instr)n.type}");
		}
		public static readonly Dictionary<Instr, string> INSTR_NAMES = new Dictionary<Instr, string>() {
			{ Instr.Addq,  "addq   " },
			{ Instr.Subq,  "subq   " },
			{ Instr.Movq,  "movq   " },
			{ Instr.Negq,  "negq   " },
			{ Instr.Callq, "callq  " },
			{ Instr.Pushq, "pushq  " },
			{ Instr.Popq,  "popq   " },
			{ Instr.Jmp,   "jmp    " },
			{ Instr.Retq,  "retq" },
		};
		
		

		public static Node Imm(int val) {
			Node n = new Node(Loc.Imm.Ord());
			n.List(""+val);
			return n;
		}
		public static Node Reg(Register reg) {
			Node n = new Node(Loc.Reg.Ord());
			n.List(reg.Name());
			return n;
		}
		public static Node Mem(Register reg, int offset) {
			Node n = new Node(Loc.Mem.Ord());
			n.List(reg.Name());
			n.List(""+offset);
			return n;
		}
		public static Node Var(string name) {
			Node n = new Node(Loc.Var.Ord());
			n.List(name);
			return n;
		}

	}
}
