using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace ni_compiler {

	public class LL<T> {
		public T val;
		public LL<T> next;
		public LL(T val, LL<T> next = null) {
			this.val = val;
			this.next = null;
		}
	}
	public class Env<T> {
		public LL<(string, T)> values;
		public Env() {}
		public Env(Env<T> old, string sym, T val) {
			values = new LL<(string, T)>((sym, val), old?.values);
		}
		public Env<T> Extend(string sym, T val) { return new Env<T>(this, sym, val); }
		public T Lookup(string sym) {
			var trace = values;
			while (trace != null) {
				if (trace.val.Item1 == sym) { return trace.val.Item2; }
				trace = trace.next;
			}
			throw new Exception($"No variable '{sym}' found.");
		}

	}
	/// <summary> Class used to build program trees from </summary>
	public class Node {

		/// <summary> Unordered Map of data within the node </summary>
		public Dictionary<string, string> dataMap;
		/// <summary> Unordered Map of children of the node </summary>
		public Dictionary<string, Node> nodeMap;

		/// <summary> Ordered List of children </summary>
		public List<Node> nodeList;
		/// <summary> Ordered list of data </summary>
		public List<string> dataList;

		/// <summary> Tokens that compose this node for sourcemapping information. </summary>
		public List<Token> tokens;

		/// <summary> Number of entries in the ordered data 'list' </summary>
		public int DataListed { get { return dataList?.Count ?? 0; } }

		/// <summary> Number of entries in the ordered children 'list' </summary>
		public int NodesListed { get { return nodeList?.Count ?? 0; } }

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
			nodeList = null;
			dataList = null;

			type = UNTYPED;
		}

		/// <summary> Constructor which takes a type parameter. </summary>
		/// <param name="type"> Type value for the node. </param>
		public Node(int type) {
			nodeMap = null;
			dataMap = null;
			nodeList = null;
			dataList = null;

			this.type = type;
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
			if (nodeList == null) { nodeList = new List<Node>(); }
			if (node != null) { nodeList.Add(node); }
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
			if (dataList == null) { dataList = new List<string>(); }
			if (val != null) { dataList.Add(val); }
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
			if (nodeList == null) { return null; }
			if (index < nodeList.Count) { return nodeList[index]; }
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
			if (dataList == null) { return null; }
			if (index < dataList.Count) { return dataList[index]; }
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

			str.Append($"\n{ident}Node {type} From [Line {line}, Col {col}] - [Line {lastLine}, Col {lastCol}]");

			if (dataMap != null) {
				str.Append($"\n{ident2}DataMap:");
				foreach (var pair in dataMap) {
					str.Append($"\n{ident3}{pair.Key}: {pair.Value}");
				}
			}

			if (dataList != null) {
				str.Append($"\n{ident2}DataList: [");

				for (int i = 0; i < dataList.Count; i++) {
					str.Append(i > 0 ? ", " : "");
					str.Append(dataList[i]);
				}

				str.Append("]");
			}

			if (nodeMap != null) {
				str.Append($"\n{ident2}NodeMap: ");

				foreach (var pair in nodeMap) {
					str.Append($"\n{ident3}{pair.Key}: {pair.Value.ToString(indent + 1, indentString)}");
				}

			}

			if (nodeList != null) {
				str.Append($"\n{ident2} NodeList:");

				for (int i = 0; i < nodeList.Count; i++) {
					str.Append($"\n{ident3}{i}: {nodeList[i].ToString(indent + 1, indentString)}");
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
