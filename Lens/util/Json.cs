using System;
using System.Text.Json.Nodes;

namespace Lens.util;

/// <summary>
/// The JSON accessors the content is read with, on System.Text.Json's DOM. lightJson's
/// <c>JsonValue</c> was a struct with a Null sentinel: a missing key read as null-ish, and every
/// accessor took a default instead of throwing. <see cref="JsonNode"/> is a class whose indexer
/// returns null and whose typed getters throw, so these extensions keep the old shape at the
/// call sites — a missing or mistyped value yields the default.
///
/// The strict accessors (<see cref="Number"/>, <see cref="Int"/>, <see cref="Bool"/>,
/// <see cref="String"/>) match lightJson's methods: only the exact type reads, anything else is
/// the default. The coercing ones (<see cref="AsNumber"/>, <see cref="AsString"/>,
/// <see cref="AsBoolean"/>, <see cref="AsInteger"/>) match lightJson's properties.
/// </summary>
public static class Json {
	public static bool IsNull(this JsonNode? node) => node == null;
	public static bool IsJsonObject(this JsonNode? node) => node is JsonObject;
	public static bool IsJsonArray(this JsonNode? node) => node is JsonArray;
	public static bool IsString(this JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out _);
	public static bool IsNumber(this JsonNode? node) => node is JsonValue value && value.TryGetValue<double>(out _);
	public static bool IsBoolean(this JsonNode? node) => node is JsonValue value && value.TryGetValue<bool>(out _);

	public static bool IsInteger(this JsonNode? node) =>
		node is JsonValue value && value.TryGetValue<double>(out var n) && n == Math.Floor(n);

	/// <summary>Null when the node is not an object — lightJson's AsJsonObject shape.</summary>
	public static JsonObject? AsJsonObject(this JsonNode? node) => node as JsonObject;

	/// <summary>Null when the node is not an array — lightJson's AsJsonArray shape.</summary>
	public static JsonArray? AsJsonArray(this JsonNode? node) => node as JsonArray;

	public static float Number(this JsonNode? node, float d = 0) =>
		node is JsonValue value && value.TryGetValue<double>(out var n) ? (float) n : d;

	public static int Int(this JsonNode? node, int d = 0) =>
		node is JsonValue value && value.TryGetValue<double>(out var n) ? (int) n : d;

	public static bool Bool(this JsonNode? node, bool b = false) =>
		node is JsonValue value && value.TryGetValue<bool>(out var v) ? v : b;

	public static string String(this JsonNode? node, string s = "") =>
		node is JsonValue value && value.TryGetValue<string>(out var v) ? v : s;

	/// <summary>lightJson's coercing AsNumber: bool → 1/0, number → value, numeric string → parsed, else 0.</summary>
	public static double AsNumber(this JsonNode? node) {
		if (node is JsonValue value) {
			if (value.TryGetValue<bool>(out var b)) {
				return b ? 1 : 0;
			}

			if (value.TryGetValue<double>(out var n)) {
				return n;
			}

			if (value.TryGetValue<string>(out var s) && double.TryParse(s, out var parsed)) {
				return parsed;
			}
		}

		return 0;
	}

	/// <summary>lightJson's coercing AsInteger: AsNumber, clamped to the int range.</summary>
	public static int AsInteger(this JsonNode? node) {
		var value = node.AsNumber();

		if (value >= int.MaxValue) {
			return int.MaxValue;
		}

		if (value <= int.MinValue) {
			return int.MinValue;
		}

		return (int) value;
	}

	/// <summary>lightJson's coercing AsString: bool → "true"/"false", number → text, string → itself, else null.</summary>
	public static string? AsString(this JsonNode? node) {
		if (node is JsonValue value) {
			if (value.TryGetValue<bool>(out var b)) {
				return b ? "true" : "false";
			}

			if (value.TryGetValue<double>(out var n)) {
				return n.ToString();
			}

			if (value.TryGetValue<string>(out var s)) {
				return s;
			}
		}

		return null;
	}

	/// <summary>lightJson's coercing AsBoolean: bool, number ≠ 0, non-empty string, object/array → true.</summary>
	public static bool AsBoolean(this JsonNode? node) {
		if (node is JsonObject or JsonArray) {
			return true;
		}

		if (node is JsonValue value) {
			if (value.TryGetValue<bool>(out var b)) {
				return b;
			}

			if (value.TryGetValue<double>(out var n)) {
				return n != 0;
			}

			if (value.TryGetValue<string>(out var s)) {
				return s != "";
			}
		}

		return false;
	}
}
