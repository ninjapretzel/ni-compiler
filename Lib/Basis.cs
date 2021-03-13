using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace ni_compiler {

	/// <summary> Basic singly linked list node type. </summary>
	/// <typeparam name="T"> Generic content type </typeparam>
	public class LL<T> : IEnumerable<T> {
		/// <summary> Current data </summary>
		public T data;
		/// <summary> Link to next node </summary>
		public LL<T> next;

		/// <summary> Construct a new node with the given data/next link </summary>
		/// <param name="data"> data item to store </param>
		/// <param name="next"> next link or null </param>
		public LL(T data, LL<T> next = null) {
			this.data = data;
			this.next = next;
		}
		public IEnumerator<T> GetEnumerator() { return new Enumerator(this); }
		IEnumerator IEnumerable.GetEnumerator() { return new Enumerator(this); }

		public class Enumerator : IEnumerator<T> {
			public Enumerator(LL<T> start) { this.start = start; }
			private LL<T> start;
			private LL<T> cur;
			public T Current { get { return cur.data; } }
			object IEnumerator.Current { get { return cur.data; } }
			public void Dispose() { }
			public void Reset() { cur = start; }
			public bool MoveNext() { 
				if (cur.next != null) {
					cur = cur.next;
					return true;
				}
				return false;
			}
		}
	}
	/// <summary> Extension methods for <see cref="LL{T}"/> so that calling them on null is valid. </summary>
	public static class LLExt {
		/// <summary> Add an item to a list, returning the new list node </summary>
		/// <typeparam name="T"> Generic content type </typeparam>
		/// <param name="list"> List to add to </param>
		/// <param name="val"> item to add </param>
		/// <returns> newly constructed list </returns>
		public static LL<T> Add<T>(this LL<T> list, T val) {
			return new LL<T>(val, list);
		}
		/// <summary> Fold a list from "Right to left" </summary>
		/// <typeparam name="T"> Generic content type </typeparam>
		/// <typeparam name="R"> Generic result type </typeparam>
		/// <param name="list"> List to reduce </param>
		/// <param name="reducer"> Function to reduce list content </param>
		/// <param name="value"> Initial accumulator value </param>
		/// <returns> Final reduction result </returns>
		public static R FoldL<T, R>(this LL<T> list, Func<R, T, R> reducer, R value) {
			if (list == null) { return value; }
			return FoldL(list.next, reducer, reducer(value, list.data));
		}
		/// <summary> Fold a list from "Left to right" </summary>
		/// <typeparam name="T"> Generic content type </typeparam>
		/// <typeparam name="R"> Generic result type </typeparam>
		/// <param name="list"> List to reduce </param>
		/// <param name="reducer"> Function to reduce list content </param>
		/// <param name="value"> Initial accumulator value </param>
		/// <returns> Final reduction result </returns>
		public static R FoldR<T, R>(this LL<T> list, Func<R, T, R> reducer, R value) {
			if (list == null) { return value; }
			R next = FoldR(list.next, reducer, value);
			return reducer(next, list.data);
		}
	}

	/// <summary> Environment type, done for consistancy with functional version. </summary>
	/// <typeparam name="T"> Generic content type </typeparam>
	public class Env<T> {
		/// <summary> List of content </summary>
		public LL<(string, T)> list { get; private set; }
		/// <summary> Link to old environment </summary>
		public Env<T> old { get; private set; }
		/// <summary> Empty constructor </summary>
		public Env() {}
		/// <summary> Extension constructor </summary>
		/// <param name="old"> Old list to extend </param>
		/// <param name="sym"> new symbol to bind </param>
		/// <param name="val"> new value to bind </param>
		public Env(Env<T> old, string sym, T val) {
			this.old = old;
			list = new LL<(string, T)>((sym, val), old?.list);
		}
		/// <summary> Inner field for caching result of <see cref="ToString"/> </summary>
		private string _toString;
		/// <inheritdoc />
		public override string ToString() {
			if (_toString != null) { return $"{{{_toString}\n}}"; }
			if (old == null || list == null) { return (_toString = ""); }
			(string name, T val) = list.data;
			string elem = $"\n\t{name}: {val},";
			old.ToString();
			_toString = elem + old._toString;
			return $"{{{_toString}\n}}";
		}
		public override bool Equals(object obj) {
			if (obj is Env<T> other) {
				var trace1 = list;
				var trace2 = other.list;

				while (trace1 != null && trace2 != null) {
					(string sym1, T val1) = trace1.data;
					(string sym2, T val2) = trace2.data;

					if (sym1 != sym2) { return false; }
					if (!val1.Equals(val2)) { return false; }

					trace1 = trace1.next;
					trace2 = trace2.next;
				}
				if (trace1 == null && trace2 != null) { return false; }
				if (trace1 != null && trace2 == null) { return false; }
				return true;
			}
			return false;
		}
		/// <summary> Extend the given environment with a new symbol/value pair </summary>
		/// <param name="sym"> Symbol to extend with </param>
		/// <param name="val"> Value to bind to symbol </param>
		/// <returns> Newly constructed environment with binding added. </returns>
		public Env<T> Extend(string sym, T val) { return new Env<T>(this, sym, val); }
		/// <summary> Lookup the given symbol in the given environment. </summary>
		/// <param name="sym"> Symbol to look up. </param>
		/// <returns> Found value, or throws an exception if it is not found. </returns>
		public T Lookup(string sym) {
			var trace = list;
			while (trace != null) {
				(string name, T val) = trace.data;
				if (name == sym) { return val; }
				trace = trace.next;
			}
			throw new Exception($"No variable '{sym}' found in env {ToString()}");
		}

	}
	/// <summary> Class used to build program trees from </summary>
	public class Node {

		/// <summary> Unordered Map of data within the node </summary>
		public Dictionary<string, string> dataMap;
		/// <summary> Unordered Map of children of the node </summary>
		public Dictionary<string, Node> nodeMap;

		/// <summary> Ordered List of children </summary>
		public List<Node> nodes;
		/// <summary> Ordered list of data </summary>
		public List<string> datas;

		/// <summary> Tokens that compose this node for sourcemapping information. </summary>
		public List<Token> tokens;

		/// <summary> Number of entries in the ordered data 'list' </summary>
		public int DataListed { get { return datas?.Count ?? 0; } }

		/// <summary> Number of entries in the ordered children 'list' </summary>
		public int NodesListed { get { return nodes?.Count ?? 0; } }

		/// <summary> Number of data values mapped </summary>
		public int DataMapped { get { return dataMap?.Count ?? 0; } }

		/// <summary> Number of child nodes mapped </summary>
		public int NodesMapped { get { return nodeMap?.Count ?? 0; } }

		/// <summary> Gets/sets the type id for this node. </summary>
		public int type { get; set; }
		/// <summary> Constant for untyped nodes. </summary>
		public const int UNTYPED = -1;

		/// <summary> Get first line this node is on, or -1 if no tokens are recorded. </summary>
		public int line { get { return tokens != null ? tokens[0].line : -1; } }
		/// <summary> Get column of first line this node is on, or -1 if no tokens are recorded. </summary>
		public int col { get { return tokens != null ? tokens[0].col : -1; } }

		/// <summary> Does this node have source position information? </summary>
		public bool hasSrcLineCol { get { return line != -1 && col != -1; } }
		/// <summary> Get line/col information </summary>
		public string srcLineCol { get { return $"From [Line {line}, Col {col}] - [Line {lastLine}, Col {lastCol}]"; } }

		/// <summary> Gets the last line this node is on, or -1 if no tokens are recorded. </summary>
		public int lastLine {
			get {
				int max = -1;
				if (tokens != null) {
					foreach (var token in tokens) { if (token.line > max) { max = token.line; } }
				}
				return max;
			}
		}
		/// <summary> Gets the last column on the last line this node is on in the source code. </summary>
		public int lastCol {
			get {
				int maxLine = -1;
				int maxCol = -1;

				if (tokens != null) {
					for (int i = 0; i < tokens.Count; i++) {
						var token = tokens[i];
						if (token.line > maxLine) {
							maxLine = token.line;
							maxCol = token.col;
						} else if (token.line == maxLine) {
							if (token.col > maxCol) { maxCol = token.col; }
						}

					}
				}
				return col;
			}
		}

		/// <summary> Constructor </summary>
		public Node() {
			dataMap = null;
			nodeMap = null;
			nodes = null;
			datas = null;

			type = UNTYPED;
		}

		/// <summary> Constructor which takes a type parameter. </summary>
		/// <param name="type"> Type value for the node. </param>
		public Node(int type) {
			nodeMap = null;
			dataMap = null;
			nodes = null;
			datas = null;

			this.type = type;
		}

		public override bool Equals(object obj) {
			if (obj is Node other) {
				if (other.DataListed != DataListed) { return false; }
				if (other.DataMapped != DataMapped) { return false; }
				if (other.NodesListed != NodesListed) { return false; }
				if (other.NodesMapped != NodesMapped) { return false; }
				if (datas != null) {
					for (int i = 0; i < datas.Count; i++) { 
						if (!datas[i].Equals(other.datas[i])) { return false; } 
					}
				}
				if (nodes != null) {
					for (int i = 0; i < nodes.Count; i++) { 
						if (!nodes[i].Equals(other.nodes[i])) { return false; } 
					}
				}
				if (dataMap != null) {
					foreach (var pair in dataMap) {
						string key = pair.Key; string val = pair.Value;
						if (!other.dataMap.ContainsKey(key)) { return false; }
						if (!val.Equals(other.dataMap[key])) { return false; }
					}
				}
				if (nodeMap != null) {
					foreach (var pair in nodeMap) {
						string key = pair.Key; Node val = pair.Value;
						if (!other.nodeMap.ContainsKey(key)) { return false; }
						if (!val.Equals(other.nodeMap[key])) { return false; }
					}
				}
				return true;
			}
			return false;
		}

		///<summary> Adds the given <paramref name="token"/> to the node's tokens list. </summary>
		public void Add(Token token) {
			if (tokens == null) { tokens = new List<Token>(); }
			tokens.Add(token);
		}

		/// <summary> Maps the given <paramref name="node"/> by <paramref name="name"/> and returns the mapped node </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Node Map(string name, Node node) {
			if (nodeMap == null) { nodeMap = new Dictionary<string, Node>(); }
			if (node != null) { nodeMap[name] = node; }
			return node;
		}

		/// <summary> Inserts the <paramref name="node"/> into the 'list' at index <see cref="NodesListed"/> and returns the listed node </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Node List(Node node) {
			if (nodes == null) { nodes = new List<Node>(); }
			if (node != null) { nodes.Add(node); }
			return node;
		}

		/// <summary> Maps the given <paramref name="val"/> into data by <paramref name="name"/> </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Map(string name, string val) {
			if (dataMap == null) { dataMap = new Dictionary<string, string>(); }
			if (val != null) { dataMap[name] = val; }
		}

		/// <summary> Maps the given <paramref name="val"/>'s content into data by <paramref name="name"/> </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Map(string name, Token val) {
			if (tokens == null) { tokens = new List<Token>(); }
			tokens.Add(val);
			Map(name, val.content);
		}

		/// <summary> Adds the given <paramref name="val"/> into the 'list' of data at index <see cref="DataListed"/> </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void List(string val) {
			if (datas == null) { datas = new List<string>(); }
			if (val != null) { datas.Add(val); }
			//if (val != null) { dataMap[""+(dataListSize++)] = val; }
		}

		/// <summary> Adds the given <paramref name="val"/>'s content into the 'list' of data at index <see cref="DataListed"/> </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void List(Token val) {
			if (tokens == null) { tokens = new List<Token>(); }
			tokens.Add(val);
			List(val.content);
		}

		/// <summary> Returns a child by index. </summary>
		/// <param name="index"> Index of child node to grab </param>
		/// <returns> Child node at <paramref name="index"/>, or null if there is none. </returns>
		public Node Child(int index) {
			if (nodes == null) { return null; }
			if (index < nodes.Count) { return nodes[index]; }
			return null;
		}

		/// <summary> Returns a child by name. </summary>
		/// <param name="name"> Name of child to grab </param>
		/// <returns> Child node mapped to <paramref name="name"/>, or null if there is none. </returns>
		public Node Child(string name) {
			if (nodeMap == null) { return null; }
			if (nodeMap.ContainsKey(name)) { return nodeMap[name]; }
			return null;
		}

		/// <summary> Returns a data value by index. </summary>
		/// <param name="index"> Index of data value to grab </param>
		/// <returns> Data value at <paramref name="index"/>, or null if there is none. </returns>
		public string Data(int index) {
			if (datas == null) { return null; }
			if (index < datas.Count) { return datas[index]; }
			return null;
		}

		/// <summary> Returns a data value by name. </summary>
		/// <param name="name"> Name of data value to grab </param>
		/// <returns> Data value mapped to <paramref name="name"/>, or null if there is none. </returns>
		public string Data(string name) {
			if (dataMap == null) { return null; }
			if (dataMap.ContainsKey(name)) { return dataMap[name]; }
			return null;
		}

		/// <inheritdoc />
		public override string ToString() { return ToString(0); }

		/// <summary> Build s a string representation of this node, with a given <paramref name="indent"/> level. </summary>
		/// <param name="indent"> Number of levels to indent </param>
		/// <param name="indentString"> Characters to indent each level with, default is "  "</param>
		/// <returns> String of the current node and its children, indented at the given <paramref name="indent"/> level. </returns>
		public string ToString(int indent, string indentString = "  ") {
			StringBuilder str = new StringBuilder();
			string ident = "";
			for (int i = 0; i < indent; i++) { ident += indentString; }
			string ident2 = ident + indentString;
			string ident3 = ident2 + indentString;

			str.Append($"\n{ident}Node {type} {(hasSrcLineCol ? srcLineCol : "")}");

			if (dataMap != null) {
				str.Append($"\n{ident2}DataMap:");
				foreach (var pair in dataMap) {
					str.Append($"\n{ident3}{pair.Key}: {pair.Value}");
				}
			}

			if (datas != null) {
				str.Append($"\n{ident2}DataList: [");

				for (int i = 0; i < datas.Count; i++) {
					str.Append(i > 0 ? ", " : "");
					str.Append(datas[i]);
				}

				str.Append("]");
			}

			if (nodeMap != null) {
				str.Append($"\n{ident2}NodeMap: ");

				foreach (var pair in nodeMap) {
					str.Append($"\n{ident3}{pair.Key}: {pair.Value.ToString(indent + 1, indentString)}");
				}

			}

			if (nodes != null) {
				str.Append($"\n{ident2} NodeList:");

				for (int i = 0; i < nodes.Count; i++) {
					str.Append($"\n{ident3}{i}: {nodes[i].ToString(indent + 1, indentString)}");
				}
			}

			return str.ToString();
		}

		public string ToString<T>(int indent = 0, string indentString = "  ") where T : Enum{
			StringBuilder str = new StringBuilder();
			string ident = "";
			for (int i = 0; i < indent; i++) { ident += indentString; }
			string ident2 = ident + indentString;
			string ident3 = ident2 + indentString;
			string kind = Enum<T>.names[type];
			str.Append($"\n{ident}Node {kind} {(hasSrcLineCol ? srcLineCol : "")}");

			if (dataMap != null) {
				str.Append($"\n{ident2}DataMap:");
				foreach (var pair in dataMap) {
					str.Append($"\n{ident3}{pair.Key}: {pair.Value}");
				}
			}

			if (datas != null) {
				str.Append($"\n{ident2}DataList: [");

				for (int i = 0; i < datas.Count; i++) {
					str.Append(i > 0 ? ", " : "");
					str.Append(datas[i]);
				}

				str.Append("]");
			}

			if (nodeMap != null) {
				str.Append($"\n{ident2}NodeMap: ");

				foreach (var pair in nodeMap) {
					str.Append($"\n{ident3}{kind} @ {pair.Key}: {pair.Value.ToString<T>(indent + 1, indentString)}");
				}

			}

			if (nodes != null) {
				str.Append($"\n{ident2} NodeList:");

				for (int i = 0; i < nodes.Count; i++) {
					str.Append($"\n{ident3}{kind} # {i}: {nodes[i].ToString<T>(indent + 1, indentString)}");
				}
			}

			return str.ToString();
		}
	}


	/// <summary> Represents a single token read from a source script </summary>
	public struct Token {

		/// <summary> Fixed, impossible string to represent all invalid tokens </summary>
		public const string INVALID = "!INVALID";
		/// <summary> Generic invalid token for WTF moments. </summary>
		public static Token INVALID_TOKEN = new Token(INVALID);

		/// <summary> Create an invalid token at a certain spot. </summary>
		/// <param name="line"> line number, if applicable </param>
		/// <param name="col"> column in line, if applicable </param>
		/// <returns> Invalid token at location </returns>
		public static Token Invalid(int line = -1, int col = -1) {
			return new Token(INVALID, line, col);
		}

		/// <summary> Generic done token for being FINISHED! </summary>
		public static readonly Token DONE_TOKEN = new Token("DONE!", INVALID);

		/// <summary> Create a done token at a certain spot. </summary>
		/// <param name="line"> line number, if applicable </param>
		/// <param name="col"> column in line, if applicable </param>
		/// <returns> Done token at location </returns>
		public static Token Done(int line = -1, int col = -1) {
			return new Token("DONE!", INVALID, line, col);
		}

		/// <summary> Content of the token </summary>
		public string content { get; private set; }
		/// <summary> Type of the token </summary>
		public string type { get; private set; }
		/// <summary> Line the token was created on, if applicable. </summary>
		public int line { get; private set; }
		/// <summary> Column of line the token was created on, if applicable. </summary>
		public int col { get; private set; }
		
		/// <summary> Assigns both content and type to the same string. </summary>
		/// <param name="content"> Content/type for this token</param>
		public Token(string content, int line = -1, int col = -1) {
			this.content = type = content;
			this.line = line;
			this.col = col;
		}

		/// <summary> Construct a token with a given content/type </summary>
		/// <param name="content"> Content for token </param>
		/// <param name="type"> Type for token </param>
		public Token(string content, string type, int line = -1, int col = -1) {
			this.content = content;
			this.type = type;
			this.line = line;
			this.col = col;
		}

		/// <summary> Returns true if this token is a 'kind' </summary>
		public bool Is(string kind) { return type == kind; }

		/// <summary> Returns true if this token's type is contained in 'types' </summary>
		public bool Is(string[] types) { return types.Contains(type); }

		/// <summary> Returns true if this token represents a valid token from a source file.
		/// False if it represents an error or the DONE condition. </summary>
		public bool IsValid { get { return type != INVALID; } }

		/// <summary> True if this token is a space tab or newline, false otherwise. </summary>
		public bool IsWhitespace { get { return content == " " || content == "\t" || content == "\n"; } }

		/// <summary> Human readable representation </summary>
		public override string ToString() {
			string c = StringContent();
			return $"{{{c}}} @ {line}:{col}";
		}
		private string StringContent() {
			if (content == " ") { return "SPACE"; }
			if (content == "\t") { return "TAB"; }
			if (content == "\n") { return "NEWLINE"; }
			if (!ReferenceEquals(type, content)) { return type + ": [" + content + "]"; }
			return type;

		}


	}

}
