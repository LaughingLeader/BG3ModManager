namespace ModManager.Helpers.Sorting;

/// Source: https://github.com/tompazourek/NaturalSort.Extension
/// Copyright (c) 2022 Tomáš Pažourek - The MIT License (MIT)
/// This was adapted to support a simple interface that contains a string property to sort.
/// <summary>
/// Creates a string comparer with natural sorting functionality
/// which allows it to sort numbers inside the strings as numbers, not as letters.
/// (e.g. "1", "2", "10" instead of "1", "10", "2").
/// It uses either a <seealso cref="StringComparison" /> (preferred) or arbitrary
/// <see cref="IComparer{T}" /> string comparer for the comparisons.
/// </summary>
public class NaturalStringSortComparer : IComparer<string>
{
	/// <summary>
	/// String comparison used for comparing strings.
	/// Used if <see cref="_stringComparer" /> is null.
	/// </summary>
	private readonly StringComparison _stringComparison;

	/// <summary>
	/// String comparer used for comparing strings.
	/// </summary>
	private readonly IComparer<string>? _stringComparer;

	// Token values (not an enum as a performance micro-optimization)
	private const byte TokenNone = 0;
	private const byte TokenOther = 1;
	private const byte TokenDigits = 2;
	private const byte TokenLetters = 3;

	/// <summary>
	/// Constructs comparer with a <seealso cref="StringComparison" /> as the inner mechanism.
	/// Prefer this to <see cref="NaturalSortComparer(IComparer{string})" /> if possible.
	/// </summary>
	/// <param name="stringComparison">String comparison to use</param>
	public NaturalStringSortComparer(StringComparison stringComparison)
		=> _stringComparison = stringComparison;

	/// <summary>
	/// Constructs comparer with a <seealso cref="IComparer{T}" /> string comparer as the inner mechanism.
	/// Prefer <see cref="NaturalSortComparer(StringComparison)" /> if possible.
	/// </summary>
	/// <param name="stringComparer">String comparer to wrap</param>
	public NaturalStringSortComparer(IComparer<string> stringComparer)
		=> _stringComparer = stringComparer;

	/// <inheritdoc />
	public int Compare(string? x, string? y)
	{
		return StringCompare(x, y);
	}

	/// <inheritdoc />
	private int StringCompare(string? str1, string? str2)
	{
		if (str1 == str2) return 0;
		if (str1 == null) return -1;
		if (str2 == null) return 1;

		var strLength1 = str1.Length;
		var strLength2 = str2.Length;

		var startIndex1 = 0;
		var startIndex2 = 0;

		while (true)
		{
			// get next token from string 1
			var endIndex1 = startIndex1;
			var token1 = TokenNone;
			while (endIndex1 < strLength1)
			{
				var charToken = GetTokenFromChar(str1[endIndex1]);
				if (token1 == TokenNone)
				{
					token1 = charToken;
				}
				else if (token1 != charToken)
				{
					break;
				}

				endIndex1++;
			}

			// get next token from string 2
			var endIndex2 = startIndex2;
			var token2 = TokenNone;
			while (endIndex2 < strLength2)
			{
				var charToken = GetTokenFromChar(str2[endIndex2]);
				if (token2 == TokenNone)
				{
					token2 = charToken;
				}
				else if (token2 != charToken)
				{
					break;
				}

				endIndex2++;
			}

			// if the token kinds are different, compare just the token kind
			var tokenCompare = token1.CompareTo(token2);
			if (tokenCompare != 0)
				return tokenCompare;

			// now we know that both tokens are the same kind

			// didn't find any more tokens, return that they're equal
			if (token1 == TokenNone)
				return 0;

			var rangeLength1 = endIndex1 - startIndex1;
			var rangeLength2 = endIndex2 - startIndex2;

			if (token1 == TokenDigits)
			{
				// compare both tokens as numbers
				var maxLength = Math.Max(rangeLength1, rangeLength2);

				// both spans will get padded by zeroes on the left to be the same length
				const char paddingChar = '0';
				var paddingLength1 = maxLength - rangeLength1;
				var paddingLength2 = maxLength - rangeLength2;

				for (var i = 0; i < maxLength; i++)
				{
					var digit1 = i < paddingLength1 ? paddingChar : str1[startIndex1 + i - paddingLength1];
					var digit2 = i < paddingLength2 ? paddingChar : str2[startIndex2 + i - paddingLength2];

					if (digit1 is >= '0' and <= '9' && digit2 is >= '0' and <= '9')
					{
						// both digits are ordinary 0 to 9
						var digitCompare = digit1.CompareTo(digit2);
						if (digitCompare != 0)
							return digitCompare;
					}
					else
					{
						// one or both digits is unicode, compare parsed numeric values, and only if they are same, compare as char
						var digitNumeric1 = char.GetNumericValue(digit1);
						var digitNumeric2 = char.GetNumericValue(digit2);
						var digitNumericCompare = digitNumeric1.CompareTo(digitNumeric2);
						if (digitNumericCompare != 0)
							return digitNumericCompare;

						var digitCompare = digit1.CompareTo(digit2);
						if (digitCompare != 0)
							return digitCompare;
					}
				}

				// if the numbers are equal, we compare how much we padded the strings
				var paddingCompare = paddingLength1.CompareTo(paddingLength2);
				if (paddingCompare != 0)
					return paddingCompare;
			}
			else if (_stringComparer is not null)
			{
				// compare both tokens as strings
				var tokenString1 = str1.Substring(startIndex1, rangeLength1);
				var tokenString2 = str2.Substring(startIndex2, rangeLength2);
				var stringCompare = _stringComparer.Compare(tokenString1, tokenString2);
				if (stringCompare != 0)
					return stringCompare;
			}
			else
			{
				// use string comparison
				var minLength = Math.Min(rangeLength1, rangeLength2);
				var stringCompare = string.Compare(str1, startIndex1, str2, startIndex2, minLength, _stringComparison);
				if (stringCompare == 0)
				{
					stringCompare = rangeLength1 - rangeLength2;
				}

				if (stringCompare != 0)
					return stringCompare;
			}

			startIndex1 = endIndex1;
			startIndex2 = endIndex2;
		}
	}

	private static byte GetTokenFromChar(char c)
	{
		if (c >= 'a')
		{
			if (c <= 'z')
			{
				return TokenLetters;
			}
			else if (c < 128)
			{
				return TokenOther;
			}
			else if (char.IsLetter(c))
			{
				return TokenLetters;
			}
			else if (char.IsDigit(c))
			{
				return TokenDigits;
			}
			else
			{
				return TokenOther;
			}
		}
		else
		{
			if (c >= 'A')
			{
				if (c <= 'Z')
				{
					return TokenLetters;
				}
				else
				{
					return TokenOther;
				}
			}
			else
			{
				if (c is >= '0' and <= '9')
				{
					return TokenDigits;
				}
				else
				{
					return TokenOther;
				}
			}
		}
	}
}