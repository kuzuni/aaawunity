using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 의존성 없는 소형 JSON 파서/직렬화기. data/*.json 과 세이브 파일을 읽고 쓴다.
    /// 값 표현: object → Dictionary&lt;string,object&gt;, array → List&lt;object&gt;, number → double,
    /// string → string, true/false → bool, null → null.
    /// 순수 C# 이라 EditMode 테스트·dotnet 콘솔에서 그대로 돈다.
    /// </summary>
    public static class MiniJson
    {
        public static object Parse(string text)
        {
            var p = new Parser(text);
            p.SkipWs();
            var v = p.ReadValue();
            p.SkipWs();
            if (!p.End) throw p.Error("trailing characters");
            return v;
        }

        public static string Serialize(object value, bool pretty = false)
        {
            var sb = new StringBuilder();
            Write(sb, value, pretty, 0);
            return sb.ToString();
        }

        static void Write(StringBuilder sb, object v, bool pretty, int depth)
        {
            switch (v)
            {
                case null: sb.Append("null"); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case string s: WriteString(sb, s); break;
                case double d: sb.Append(FormatNumber(d)); break;
                case float f: sb.Append(FormatNumber(f)); break;
                case int i: sb.Append(i.ToString(CultureInfo.InvariantCulture)); break;
                case long l: sb.Append(l.ToString(CultureInfo.InvariantCulture)); break;
                case IDictionary<string, object> o:
                {
                    sb.Append('{');
                    bool first = true;
                    foreach (var kv in o)
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        if (pretty) { sb.Append('\n'); sb.Append(' ', (depth + 1) * 2); }
                        WriteString(sb, kv.Key);
                        sb.Append(pretty ? ": " : ":");
                        Write(sb, kv.Value, pretty, depth + 1);
                    }
                    if (pretty && !first) { sb.Append('\n'); sb.Append(' ', depth * 2); }
                    sb.Append('}');
                    break;
                }
                case IEnumerable<object> a:
                {
                    sb.Append('[');
                    bool first = true;
                    foreach (var e in a)
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        Write(sb, e, pretty, depth + 1);
                    }
                    sb.Append(']');
                    break;
                }
                default:
                    throw new ArgumentException("MiniJson: unsupported type " + v.GetType());
            }
        }

        static string FormatNumber(double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d)) return "null";
            if (Math.Abs(d) < 1e15 && d == Math.Floor(d)) return ((long)d).ToString(CultureInfo.InvariantCulture);
            return d.ToString("R", CultureInfo.InvariantCulture);
        }

        static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        sealed class Parser
        {
            readonly string _s; int _i;
            public Parser(string s) { _s = s; _i = 0; }
            public bool End => _i >= _s.Length;
            public Exception Error(string msg) => new FormatException($"MiniJson: {msg} at {_i}");
            public void SkipWs() { while (_i < _s.Length && (_s[_i] == ' ' || _s[_i] == '\t' || _s[_i] == '\n' || _s[_i] == '\r')) _i++; }

            public object ReadValue()
            {
                if (End) throw Error("unexpected end");
                char c = _s[_i];
                switch (c)
                {
                    case '{': return ReadObject();
                    case '[': return ReadArray();
                    case '"': return ReadString();
                    case 't': Expect("true"); return true;
                    case 'f': Expect("false"); return false;
                    case 'n': Expect("null"); return null;
                    default:
                        if (c == '-' || (c >= '0' && c <= '9')) return ReadNumber();
                        throw Error("unexpected char '" + c + "'");
                }
            }

            void Expect(string lit)
            {
                if (string.CompareOrdinal(_s, _i, lit, 0, lit.Length) != 0) throw Error("expected " + lit);
                _i += lit.Length;
            }

            Dictionary<string, object> ReadObject()
            {
                var o = new Dictionary<string, object>();
                _i++; SkipWs();
                if (_s[_i] == '}') { _i++; return o; }
                while (true)
                {
                    SkipWs();
                    if (_s[_i] != '"') throw Error("expected key");
                    var k = ReadString();
                    SkipWs();
                    if (_s[_i] != ':') throw Error("expected ':'");
                    _i++; SkipWs();
                    o[k] = ReadValue();
                    SkipWs();
                    if (_s[_i] == ',') { _i++; continue; }
                    if (_s[_i] == '}') { _i++; return o; }
                    throw Error("expected ',' or '}'");
                }
            }

            List<object> ReadArray()
            {
                var a = new List<object>();
                _i++; SkipWs();
                if (_s[_i] == ']') { _i++; return a; }
                while (true)
                {
                    SkipWs();
                    a.Add(ReadValue());
                    SkipWs();
                    if (_s[_i] == ',') { _i++; continue; }
                    if (_s[_i] == ']') { _i++; return a; }
                    throw Error("expected ',' or ']'");
                }
            }

            string ReadString()
            {
                _i++; // opening quote
                var sb = new StringBuilder();
                while (true)
                {
                    if (End) throw Error("unterminated string");
                    char c = _s[_i++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    char e = _s[_i++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            sb.Append((char)int.Parse(_s.Substring(_i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            _i += 4; break;
                        default: throw Error("bad escape");
                    }
                }
            }

            double ReadNumber()
            {
                int start = _i;
                if (_s[_i] == '-') _i++;
                while (_i < _s.Length && ((_s[_i] >= '0' && _s[_i] <= '9') || _s[_i] == '.' || _s[_i] == 'e' || _s[_i] == 'E' || _s[_i] == '+' || _s[_i] == '-')) _i++;
                return double.Parse(_s.Substring(start, _i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
            }
        }
    }

    /// <summary>파싱 결과를 타입 있게 읽는 얇은 래퍼. 없는 키는 예외가 아니라 기본값(엄격 모드는 Req*).</summary>
    public readonly struct JNode
    {
        public readonly object Raw;
        public JNode(object raw) { Raw = raw; }
        public bool IsNull => Raw == null;
        public bool IsObject => Raw is Dictionary<string, object>;
        public bool IsArray => Raw is List<object>;
        public Dictionary<string, object> Obj => Raw as Dictionary<string, object>;
        public List<object> Arr => Raw as List<object>;

        public JNode this[string key]
        {
            get { var o = Obj; return new JNode(o != null && o.TryGetValue(key, out var v) ? v : null); }
        }
        public JNode this[int idx]
        {
            get { var a = Arr; return new JNode(a != null && idx >= 0 && idx < a.Count ? a[idx] : null); }
        }
        public bool Has(string key) => Obj != null && Obj.ContainsKey(key);
        public int Count => Arr != null ? Arr.Count : (Obj != null ? Obj.Count : 0);
        public IEnumerable<string> Keys => Obj != null ? (IEnumerable<string>)Obj.Keys : Array.Empty<string>();

        public double Num(double def = 0) => Raw is double d ? d : (Raw is bool b ? (b ? 1 : 0) : def);
        public int Int(int def = 0) => Raw is double d ? (int)d : def;
        public string Str(string def = null) => Raw is string s ? s : def;
        public bool Bool(bool def = false) => Raw is bool b ? b : (Raw is double d ? d != 0 : def);

        public double ReqNum(string path)
        {
            if (!(Raw is double d)) throw new FormatException("data: number expected at " + path);
            return d;
        }
        public string ReqStr(string path)
        {
            if (!(Raw is string s)) throw new FormatException("data: string expected at " + path);
            return s;
        }
        public JNode Req(string key)
        {
            if (!Has(key)) throw new FormatException("data: missing key '" + key + "'");
            return this[key];
        }

        public double[] NumArray()
        {
            var a = Arr; if (a == null) return Array.Empty<double>();
            var r = new double[a.Count];
            for (int i = 0; i < a.Count; i++) r[i] = new JNode(a[i]).Num();
            return r;
        }
        public int[] IntArray()
        {
            var a = Arr; if (a == null) return Array.Empty<int>();
            var r = new int[a.Count];
            for (int i = 0; i < a.Count; i++) r[i] = new JNode(a[i]).Int();
            return r;
        }
        public string[] StrArray()
        {
            var a = Arr; if (a == null) return Array.Empty<string>();
            var r = new string[a.Count];
            for (int i = 0; i < a.Count; i++) r[i] = new JNode(a[i]).Str();
            return r;
        }
        public bool[] BoolArray()
        {
            var a = Arr; if (a == null) return Array.Empty<bool>();
            var r = new bool[a.Count];
            for (int i = 0; i < a.Count; i++) r[i] = new JNode(a[i]).Bool();
            return r;
        }
        public IEnumerable<JNode> Items()
        {
            var a = Arr; if (a == null) yield break;
            foreach (var e in a) yield return new JNode(e);
        }
    }
}
