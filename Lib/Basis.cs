using BakaTest;
using Ex;
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
		public static LL<T> From(params T[] values) {
			LL<T> acc = null;
			for (int i = values.Length - 1; i >= 0; i--) {
				acc = acc.Add(values[i]);
			}
			return acc;
		}
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
		/// <inheritdoc/>
		public IEnumerator<T> GetEnumerator() { return new Enumerator(this); }
		/// <inheritdoc/>
		IEnumerator IEnumerable.GetEnumerator() { return new Enumerator(this); }

		/// <summary> Enumerator for <see cref="LL{T}"/> </summary>
		public class Enumerator : IEnumerator<T> {
			public Enumerator(LL<T> start) { this.start = start; first = true; }
			private LL<T> start;
			private LL<T> cur;
			private bool first = true;
			public T Current { get { return cur.data; } }
			object IEnumerator.Current { get { return cur.data; } }
			public void Dispose() { }
			public void Reset() { cur = start; }
			public bool MoveNext() { 
				if (first) { cur = start; first = false; return true; }
				if (cur == null) { return false; }
				if (cur.next != null) {
					cur = cur.next;
					return true;
				}
				return false;
			}
		}
		/// <inheritdoc/>
		public override bool Equals(object obj) {
			if (obj is LL<T> other) {
				if (next == null && other.next == null) {
					return data.Equals(other.data);
				}
				return next.Equals(other.next);
			}
			return false;
		}
		private int _hash;
		private bool _hashBaked;
		/// <inheritdoc/>
		public override int GetHashCode() {
			if (!_hashBaked) { 
				_hashBaked = true;
				_hash = data.GetHashCode();
				if (next != null) {
					_hash ^= next.GetHashCode();
				}
			}
			return _hash;
		}
		/// <inheritdoc/>
		public override string ToString() {
			StringBuilder str = new StringBuilder("[ ");
			
			foreach (var item in this) {
				str.Append(item.ToString());
				str.Append(", ");
			}
			
			str.Append(" ]");
			return str.ToString();
		}
		/// <summary> Append lists using + operator </summary>
		/// <param name="left"> Left list (left side of data) </param>
		/// <param name="right"> Right list (right side of data) </param>
		/// <returns> Combined list: (left0, left1, ..., leftn, right0, right1, ..., rightn)  </returns>
		public static LL<T> operator +(LL<T> left, LL<T> right) {
			return LLExt.Append(left, right);
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
		/// <summary> Creates a reversed copy of the given list </summary>
		/// <typeparam name="T"> Generic content type </typeparam>
		/// <param name="list"> List to reverse </param>
		/// <returns> Reversed verison of given list </returns>
		public static LL<T> Reverse<T>(this LL<T> list) {
			if (list == null) { return null; }
			LL<T> acc = null;
			foreach (var item in list) { 
				acc = acc.Add(item); 
			}
			return acc;
		}

		/// <summary> Append lists </summary>
		/// <param name="left"> Left list (left side of data) </param>
		/// <param name="right"> Right list (right side of data) </param>
		/// <returns> Combined list: (left0, left1, ..., leftn, right0, right1, ..., rightn)  </returns>
		public static LL<T> Append<T>(this LL<T> left, LL<T> right) {
			LL<T> acc = right;
			Stack<LL<T>> nodes = new Stack<LL<T>>();
			if (left != null) {
				foreach (var item in left) {
					nodes.Push(new LL<T>(item));
				}
			}
			while (nodes.Count > 0) {
				var popped = nodes.Pop();
				popped.next = acc;
				acc = popped;
			}
			return acc;
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
		/// <summary> Number of items in this environment </summary>
		public int size { get; private set; }
		/// <summary> Empty constructor </summary>
		public Env() { size = 0; }
		/// <summary> Extension constructor </summary>
		/// <param name="old"> Old list to extend </param>
		/// <param name="sym"> new symbol to bind </param>
		/// <param name="val"> new value to bind </param>
		public Env(Env<T> old, string sym, T val) {
			this.old = old;
			size = old.size + 1;
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
		/// <inheritdoc/>
		public override int GetHashCode() {
			return list.GetHashCode();
		}
		/// <inheritdoc/>
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
		/// <summary> Indexer, synonym for <see cref="Lookup(string)"/> </summary>
		/// <param name="name"> name of mapping to look up </param>
		/// <returns> Value mapped to the given name </returns>
		public T this[string name] { get { return Lookup(name); } }
		/// <summary> Extend the given environment with a new symbol/value pair </summary>
		/// <param name="sym"> Symbol to extend with </param>
		/// <param name="val"> Value to bind to symbol </param>
		/// <returns> Newly constructed environment with binding added. </returns>
		public Env<T> Extend(string sym, T val) { return new Env<T>(this, sym, val); }
		/// <summary> Lookup the given symbol in the given environment. </summary>
		/// <param name="name"> Name to look up. </param>
		/// <returns> Found value mapped to name, or throws an exception if it is not found. </returns>
		public T Lookup(string name) {
			var trace = list;
			while (trace != null) {
				(string name2, T val) = trace.data;
				if (name2 == name) { return val; }
				trace = trace.next;
			}
			throw new Exception($"No variable '{name}' found in env {ToString()}");
		}

	}

	public class Set_Tests {
		public static void TestEq() {
			Set<int> a = Set<int>.FromList(1,2,3,4);
			Set<int> b = Set<int>.FromList(1,2,3,4);

			a.Equals(b).ShouldBeTrue();
		}
		public static void TestUnion() {
			Set<int> a = new Set<int>(1,2,3);
			Set<int> b = new Set<int>(4,5,6);
			Set<int> expected = new Set<int>(6, 5, 4, 3, 2, 1);
			(a+b).ShouldEqual(expected);

			a = a.Add(4);
			b = b.Add(3);
			(a+b).ShouldEqual(expected);
		}

		public static void TestSubtract() {
			Set<int> a = new Set<int>(1,2,3,4,5,6);
			Set<int> b = new Set<int>(1,3,5);
			Set<int> expected = new Set<int>(2,4,6);

			(a-b).ShouldEqual(expected);
			a = a.Remove(1).Remove(3);
			(a-b).ShouldEqual(expected);
		}
		
	}
	/// <summary> Set Theory Set class with methods like Haskell's </summary>
	/// <typeparam name="T"> Generic type contained within</typeparam>
	public class Set<T> : IEnumerable<T> where T : IComparable<T> {
		
		/// <summary> Internal container of items </summary>
		private ISet<T> items;
		/// <summary> Constructs a new empty set </summary>
		public Set() {
			items = new HashSet<T>();
		}
		/// <summary> Constructs a copy of another Set </summary>
		/// <param name="other"> Set to copy </param>
		public Set(Set<T> other) {
			items = new HashSet<T>(other.items);
		}
		/// <summary> Constructs a new set containing the given items </summary>
		/// <param name="ts"> Items to contain in the set </param>
		public Set(params T[] ts) : this((IEnumerable<T>) ts) { }

		/// <summary> Constructs a new set containing the given items </summary>
		/// <param name="ts"> Items to contain in the set </param>
		public Set(IEnumerable<T> ts) : this() {
			foreach (var t in ts) { items.Add(t); }
		}
		/// <summary> returns the number of items in this set </summary>
		public int Count { get { return items.Count; } }
		/// <summary> Creates a Set that contains all of the given items </summary>
		/// <param name="ts"> Items to contain within the set </param>
		/// <returns> Set containing all given items </returns>
		public static Set<T> FromList(params T[] ts) { return new Set<T>(ts); }

		private int _hash = 0;
		private bool _hashBaked = false;
		/// <inheritdoc/>
		public override int GetHashCode() {
			if (!_hashBaked) { 
				_hash = 0;
				_hashBaked = true;
				foreach (var item in items) { _hash ^= item.GetHashCode(); }
			}
			return _hash;
		}

		/// <inheritdoc/>
		public override bool Equals(object obj) {
			if (obj is Set<T> other) {
				if (other.items.Count != items.Count) { return false; }
				foreach (var item in items) {
					if (!other.items.Contains(item)) { return false; }
				}
				return true;
			}
			return false;
		}
		/// <inheritdoc/>
		public override string ToString() {
			StringBuilder str = new StringBuilder("{");
			foreach (var thing in this) {
				str.Append(thing);
				str.Append(",");
			}
			str.Append("}");
			return str.ToString();
		}

		/// <summary> Creates a new Set with the given item added to it </summary>
		/// <param name="t"> Item to add to the new set </param>
		/// <returns> Copy of this set with the given item added to it </returns>
		public Set<T> Add(T t) { 
			var result = new Set<T>(this);
			result.items.Add(t);
			return result;
		}

		/// <summary> Creates a new Set with the given item removed from it </summary>
		/// <param name="t"> Item to remove from the new set </param>
		/// <returns> Copy of this set with the given item removed from it </returns>
		public Set<T> Remove(T t) {
			var result = new Set<T>(this);
			result.items.Remove(t);
			return result;
		}

		/// <summary> Creates a new Set with all of the given items added to it </summary>
		/// <param name="ts"> Items to add to the new set </param>
		/// <returns> Copy of this set, with the given items added to it </returns>
		public Set<T> AddAll(IEnumerable<T> ts) {
			Set<T> result = new Set<T>(this);
			foreach (var t in ts) { result.items.Add(t); }
			return result;
		}

		/// <summary> Creates a new Set with all of the given items removed from it </summary>
		/// <param name="ts"> Items to remove from the new set </param>
		/// <returns> Copy of this set, with the given items removed from it </returns>
		public Set<T> RemoveAll(IEnumerable<T> ts) {
			Set<T> result = new Set<T>(this);
			foreach (var t in ts) { result.items.Remove(t); }
			return result;
		}
		/// <summary> Reports wether or not the given item is contained in the set </summary>
		/// <param name="t"> Item to check for presense of </param>
		/// <returns> True if the item is a member in the set, false otherwise. </returns>
		public bool Contains(T t) { return items.Contains(t); }

		/// <summary> Creates a new set that is the union of this set and another </summary>
		/// <param name="other"> Other set to union with </param>
		/// <returns> new set containing all items in both this and <paramref name="other"/> </returns>
		public Set<T> Union(Set<T> other) { return AddAll(other); }
		
		/// <summary> Creates a new set that is the difference of this set and another </summary>
		/// <param name="other"> Other set to difference with </param>
		/// <returns> new set containing all items in this set, but not in <paramref name="other"/> </returns>
		public Set<T> Difference(Set<T> other) { return RemoveAll(other); }

		/// <inheritdoc/>
		public IEnumerator<T> GetEnumerator() { return items.GetEnumerator(); }
		/// <inheritdoc/>
		IEnumerator IEnumerable.GetEnumerator() { return items.GetEnumerator(); }

		/// <summary> Synonym for <see cref="Union(Set{T})"/> </summary>
		/// <param name="left"> First set </param>
		/// <param name="right"> Second set or IEnumerable{T} </param>
		/// <returns> union of two sets </returns>
		public static Set<T> operator +(Set<T> left, IEnumerable<T> right) { return left.AddAll(right); }
		/// <summary> Adds a single element to a set as an operator. <see cref="Add(T)"/> </summary>
		/// <param name="left"> Set to add to </param>
		/// <param name="right"> Value to add </param>
		/// <returns> Copy of <paramref name="left"/> with <paramref name="right"/> added </returns>
		public static Set<T> operator +(Set<T> left, T right) { return left.Add(right); }

		/// <summary> Synonmym for <see cref="Difference(Set{T})"/> </summary>
		/// <param name="left"> First set </param>
		/// <param name="right"> Second set </param>
		/// <returns> difference of <paramref name="right"/> subtracted from <paramref name="left"/> </returns>
		public static Set<T> operator -(Set<T> left, IEnumerable<T> right) { return left.RemoveAll(right); }
		/// <summary> Removes a single element to a set as an operator. <see cref="Add(T)"/> </summary>
		/// <param name="left"> Set to remove from </param>
		/// <param name="right"> Value to remove </param>
		/// <returns> Copy of <paramref name="left"/> with <paramref name="right"/> removed </returns>		
		public static Set<T> operator -(Set<T> left, T right) { return left.Remove(right); }
	}

	/// <summary> Class representing a graph of connections between instances of a type. </summary>
	/// <typeparam name="T"> Generic type contained within </typeparam>
	public class Graph<T> : Dictionary<T, Set<T>> where T : IComparable<T> {
		/// <summary> Creates an empty graph </summary>
		public Graph() : base() { }
		/// <summary> Creates a copy of the given <paramref name="other"/> graph </summary>
		public Graph(Graph<T> other) : base(other) { }

		/// <summary> Creates a two-way connection between the given nodes. </summary>
		/// <param name="a"> First node in the pair </param>
		/// <param name="b"> Second node in the pair </param>
		/// <returns> A new graph, as a copy of this graph with the pair added to it. </returns>
		public Graph<T> TwoWay(T a, T b) {
			Graph<T> g = new Graph<T>(this);
			// Console.WriteLine($"Interfering {a} with {b}");
			if (!g.ContainsKey(a)) { g[a] = new Set<T>(); }
			if (!g.ContainsKey(b)) { g[b] = new Set<T>(); }
			g[a] += b;
			g[b] += a;
			return g;
		}
		/// <summary> Creates a one-way connection between the given nodes. </summary>
		/// <param name="a"> Starting node of edge </param>
		/// <param name="b"> Ending node of edge </param>
		/// <returns> A new graph, as a copy of this graph with the edge added to it. </returns>
		public Graph<T> OneWay(T a, T b) {
			Graph<T> g = new Graph<T>(this);
			if (!g.ContainsKey(a)) { g[a] = new Set<T>(); }
			g[a] += b;
			return g;
		}
		/// <inheritdoc/>
		public override string ToString() { return ToString(false); }
		/// <summary> Converts this object to a human readable string </summary>
		/// <param name="insertNewlines"> Whether or not to insert newlines between each set contained within </param>
		/// <returns></returns>
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

	/// <summary> Class that implements a min-heap structure for any type that implements <see cref="IComparable{T}"/></summary>
	/// <typeparam name="T"> Generic type contained within </typeparam>
	public class Heap<T> where T : IComparable<T> {
		/// <summary> Default capacity of a new Heap </summary>
		public const int DEFAULT_CAPACITY = 20;
		/// <summary> Default growth factor of a new Heap </summary>
		public const float  GROWTH = 1.5f;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int Parent(int i) { return (i-1) / 2; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int Left(int i) { return i * 2 + 1; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int Right(int i) { return i * 2 + 2; }

		/// <summary> Delegate for overriding comparison functions. </summary>
		/// <param name="a"> First parameter </param>
		/// <param name="b"> Second parameter </param>
		/// <returns> 0 when a == b, negative number when a &lt; b, positive number when a &gt b </returns>
		public delegate int Compare(T a, T b);

		/// <summary> Heapify an array in-place. </summary>
		/// <param name="ts"> Array to heapify </param>
		/// <param name="cnt"> Number of elements to heapify. if not provided, entire array length is heapified </param>
		/// <param name="compare"> Optional override comparison function. If not provided, default `<see cref="IComparable{T}.CompareTo(T?)"/> is used. </param>
		public static void Heapify(T[] ts, int? cnt = null, Compare compare = null) {
			int n = cnt.HasValue ? cnt.Value : ts.Length;
			for (int i = ts.Length-1; i >= 0; i--) {
				SiftDown(ts, i, n, compare);
			}
		}

		/// <summary> Sift a given index upwards. </summary>
		/// <param name="ts"> Array of values to sift </param>
		/// <param name="index"> Index of item </param>
		/// <param name="cnt"> maximum index to consider. </param>
		/// <param name="compare"> Optional override comparison function. If not provided, default `<see cref="IComparable{T}.CompareTo(T?)"/> is used. </param>
		public static void SiftUp(T[] ts, int index, int cnt, Compare compare = null) {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int cmp(T a, T b) { return (compare == null) ? a.CompareTo(b) : compare(a,b); }

			if (index < 0 || index >= cnt) { return; }
			int i = index;
			int parent = Parent(i);
			T t = ts[i];
			while (cmp(t, (ts[parent])) <= 0) {
				ts[i] = ts[parent];
				ts[parent] = t;
				i = parent;
				parent = Parent(i);
				if (i == 0) { break; }
			}
		}



		/// <summary> Sift a given index downwards. </summary>
		/// <param name="ts"> Array of values to sift </param>
		/// <param name="index"> Index of item </param>
		/// <param name="cnt"> maximum index to consider. </param>
		/// <param name="compare"> Optional override comparison function. If not provided, default `<see cref="IComparable{T}.CompareTo(T?)"/> is used. </param>
		public static void SiftDown(T[] ts, int index, int cnt, Compare compare = null) {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int cmp(T a, T b) { return (compare == null) ? a.CompareTo(b) : compare(a, b); }

			if (index < 0 || index >= cnt) { return; }
			int i = index;
			T t = ts[i];
			while (true) {
				int left = Left(i);
				int right = Right(i);
				if (left >= cnt) { break; }

				if (right < cnt) {
					T tL = ts[left];
					T tR = ts[right];

					if (cmp(tL, tR) <= 0) {
						if (cmp(t, tL) > 0) {
							ts[i] = ts[left];
							ts[left] = t;
							i = left;
							continue;
						} else { break; }
					} else {
						if (cmp(t, tR) > 0) {
							ts[i] = ts[right];
							ts[right] = t;
							i = right;
							continue;
						} else { break; }
					}
				} else {
					if (cmp(t, ts[left]) > 0) {
						ts[i] = ts[left];
						ts[left] = t;
						i = left;
					} else { break; }
				}
			}

		}

		/// <summary> Current number of items in the heap </summary>
		public int Count { get { return cnt; } }
		/// <summary> Is the heap currently empty? </summary>
		public bool IsEmpty { get { return cnt == 0; } }

		/// <summary> Public access to comparator. Re-heapifies internal array on every write. </summary>
		public Compare Comparator { 
			get { return comparator; } 
			set {
				comparator = value;
				Heapify(ts, cnt, comparator);
			}
		}

		/// <summary> Internal comparator. </summary>
		private Compare comparator;

		/// <summary> Current items in heap </summary>
		private T[] ts;
		/// <summary> Current count field </summary>
		private int cnt; 

		/// <summary> Empty constructor </summary>
		public Heap() {
			ts = new T[DEFAULT_CAPACITY];
			cnt = 0;
		}

		public Heap(Compare cmp) : this() {
			comparator = cmp;
		}

		/// <summary> Copy constructor </summary>
		/// <param name="ts"> Array of values to copy </param>
		public Heap(T[] ts, Compare cmp = null) {
			this.ts = new T[ts.Length];
			Array.Copy(ts, this.ts, ts.Length);
			cnt = ts.Length;
			comparator = cmp;
			Heapify(this.ts, cnt, comparator);
		}

		/// <summary> Creates a heap, which internally uses the given array (unlike copy constructor). </summary>
		/// <param name="ts"> Array to wrap a heap around </param>
		/// <param name="cnt"> Number of items to place into the heap </param>
		/// <returns> Heap constructed around the given array </returns>
		public static Heap<T> From(T[] ts, Compare cmp = null) {
			Heap<T> h = new Heap<T>();
			h.ts = ts;
			h.comparator = cmp;
			h.cnt = ts.Length;
			Heapify(ts, h.cnt, h.comparator);
			return h;
		}

		/// <summary> Returns the minimal element in the heap </summary>
		/// <returns> Element at position 0 in heap </returns>
		public T Peek() {
			if (cnt == 0) { throw new InvalidOperationException("Heap is empty, cannot Peek."); }
			return ts[0];
		}

		/// <summary> Adds the given element to the heap structure </summary>
		/// <param name="item"> item to add to heap </param>
		public void Push(T item) {
			if (cnt == ts.Length) { Grow(); }
			ts[cnt] = item;
			cnt++;
			SiftUp(cnt-1);
		}

		/// <summary> Removes the minimal element from the heap </summary>
		/// <returns> Element that was previously at position 0 in heap </returns>
		public T Pop() {
			if (cnt == 0) { throw new InvalidOperationException("Heap is empty, cannot Pop."); }
			T t = ts[0];
			if (cnt == 1) {
				ts[0] = default(T);
				cnt = 0;
			} else {
				ts[0] = ts[cnt-1];
				ts[cnt-1] = default(T);
				cnt--;
				SiftDown(0);
			}
			
			return t;
		}

		/// <inheritdoc/>
		public override string ToString() {
			StringBuilder str = new StringBuilder($"Heap<{typeof(T)}> [ ");
			for (int i = 0; i < cnt; i++) {
				str.Append(ts[i]);
				str.Append(", ");
			}
			str.Append("]");
			return str.ToString();
		}

		/// <summary> Internal function to grow <see cref="ts"/> for more space </summary>
		private void Grow() {
			T[] newTs = new T[(int)(ts.Length * GROWTH)];
			Array.Copy(ts, newTs, cnt);
			ts = newTs;
		}

		/// <summary> Internal function to sift upwards </summary>
		/// <param name="index"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void SiftUp(int index) { SiftUp(ts, index, cnt, comparator); }

		/// <summary> Internal function to sift downwards </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void SiftDown(int index) { SiftDown(ts, index, cnt, comparator); }

	}

	public static class Heap_Tests {
		public static void TestSimple() {
			Heap<string> heap = new Heap<string>();
			heap.Push("20");
			heap.Push("30");
			heap.Push("10");
			heap.Peek().ShouldBe("10");
			heap.ToString().ShouldBe("Heap<System.String> [ 10, 30, 20, ]");

			heap.Push("40");
			heap.Push("50");
			heap.Push("60");
			heap.Push("70");
			heap.Pop().ShouldBe("10");
			heap.Pop().ShouldBe("20");
			heap.Pop().ShouldBe("30");

			heap.Pop().ShouldBe("40");
			heap.Pop().ShouldBe("50");
			heap.Pop().ShouldBe("60");
			heap.Pop().ShouldBe("70");
			heap.IsEmpty.ShouldBeTrue();
		}
		public static void TestHeapify() {
			int[] ints = new int[] { 70, 50, 30, 20, 40, 60, 10, 80 };
			Heap<int> heap = Heap<int>.From(ints);

			heap.Pop().ShouldBe(10);
			heap.Pop().ShouldBe(20);
			heap.Pop().ShouldBe(30);
			heap.Pop().ShouldBe(40);
			heap.Pop().ShouldBe(50);
			heap.Pop().ShouldBe(60);
			heap.Pop().ShouldBe(70);
			heap.Pop().ShouldBe(80);

		}
		public static void TestCustomCompare() {
			int[] ints = new int[] { 70, 50, 30, 20, 40, 60, 10, 80 };
			Heap<int> heap = Heap<int>.From(ints, (a, b)=>{ return b-a; });
			
			heap.Pop().ShouldBe(80);
			heap.Pop().ShouldBe(70);
			heap.Pop().ShouldBe(60);
			heap.Pop().ShouldBe(50);
			heap.Pop().ShouldBe(40);
			heap.Pop().ShouldBe(30);
			heap.Pop().ShouldBe(20);
			heap.Pop().ShouldBe(10);
		
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
		/// <inheritdoc/>
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


	/// <summary> Class used to build program trees from, restricted to a single Type</summary>
	public class Node<T> where T : Enum {

		/// <summary> Unordered Map of data within the node </summary>
		public Dictionary<string, string> dataMap;
		/// <summary> Unordered Map of children of the node </summary>
		public Dictionary<string, Node<T>> nodeMap;

		/// <summary> Ordered List of children </summary>
		public List<Node<T>> nodes;
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
		public T type { get; set; }
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

		/// <summary> Empty Constructor </summary>
		public Node() {
			dataMap = null;
			nodeMap = null;
			nodes = null;
			datas = null;

			type = default(T);
		}

		/// <summary> Constructor which takes a type parameter. </summary>
		/// <param name="type"> Type value for the node. </param>
		public Node(T type) {
			nodeMap = null;
			dataMap = null;
			nodes = null;
			datas = null;

			this.type = type;
		}

		public override bool Equals(object obj) {
			if (obj is Node<T> other) {
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
						string key = pair.Key; Node<T> val = pair.Value;
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
		public Node<T> Map(string name, Node<T> node) {
			if (nodeMap == null) { nodeMap = new Dictionary<string, Node<T>>(); }
			if (node != null) { nodeMap[name] = node; }
			return node;
		}

		/// <summary> Inserts the <paramref name="node"/> into the 'list' at index <see cref="NodesListed"/> and returns the listed node </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Node<T> List(Node<T> node) {
			if (nodes == null) { nodes = new List<Node<T>>(); }
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
		public Node<T> Child(int index) {
			if (nodes == null) { return null; }
			if (index < nodes.Count) { return nodes[index]; }
			return null;
		}

		/// <summary> Returns a child by name. </summary>
		/// <param name="name"> Name of child to grab </param>
		/// <returns> Child node mapped to <paramref name="name"/>, or null if there is none. </returns>
		public Node<T> Child(string name) {
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

		public string ToString(int indent = 0, string indentString = "  ") {
			StringBuilder str = new StringBuilder();
			string ident = "";
			for (int i = 0; i < indent; i++) { ident += indentString; }
			string ident2 = ident + indentString;
			string ident3 = ident2 + indentString;
			string kind = type.ToString();
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
					str.Append($"\n{ident3}{kind} @ {pair.Key}: {pair.Value.ToString(indent + 1, indentString)}");
				}

			}

			if (nodes != null) {
				str.Append($"\n{ident2} NodeList:");

				for (int i = 0; i < nodes.Count; i++) {
					str.Append($"\n{ident3}{kind} # {i}: {nodes[i].ToString(indent + 1, indentString)}");
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
