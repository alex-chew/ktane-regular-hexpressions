using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class PuzzleData
{

	public class RegexData
	{
		private const RegexOptions REGEX_OPTIONS = RegexOptions.ExplicitCapture;

		public string realStr { get; private set; }

		public string decoyStr { get; private set; }

		public Regex realRegex { get; private set; }

		public Regex decoyRegex { get; private set; }

		public RegexData(string real, string decoy)
		{
			realStr = real;
			decoyStr = decoy;

			realRegex = new Regex("^" + real + "$", REGEX_OPTIONS);
			decoyRegex = new Regex("^" + decoy + "$", REGEX_OPTIONS);
		}
	}

	public class Puzzle
	{
		public List<RegexData> regexData { get; private set; }

		public List<string> matchingWords { get; private set; }

		public List<string> decoyWords { get; private set; }

		public Puzzle(List<RegexData> regexData, List<string> matchingWords, List<string> decoyWords)
		{
			this.regexData = regexData;
			this.matchingWords = matchingWords;
			this.decoyWords = decoyWords;
		}
	}

	public static Puzzle SamplePuzzle(int rowNorth, int rowSouth, int colWest, int colEast)
	{
		// NW, NE, SE, SW
		RegexData[] regexData = new RegexData[] {
			RegexDataAtTablePos(rowNorth, colWest),
			RegexDataAtTablePos(rowNorth, colEast),
			RegexDataAtTablePos(rowSouth, colEast),
			RegexDataAtTablePos(rowSouth, colWest),
		};
		WordRegexMatches wordRegexMatches = ComputeWordRegexMatches(regexData);

		List<string> matchingWords = SampleMatchingWords(wordRegexMatches);
		List<string> decoyWords = SampleDecoyWords(regexData);
		return new Puzzle(regexData.ToList(), matchingWords, decoyWords);
	}

	private static RegexData RegexDataAtTablePos(int row, int col)
	{
		if (REGEX_TABLE_INDICES == null)
			InitializeRegexTableIndices();

		RawRegexData raw = ALL_RAW_REGEX_DATA[REGEX_TABLE_INDICES[row * REGEX_TABLE_COLS + col]];
		return new RegexData(raw.real, raw.decoy);
	}

	private class WordRegexMatches
	{
		public byte[] wordMatches;
		public int[] countsByRegex;
	}

	private static WordRegexMatches ComputeWordRegexMatches(RegexData[] regexData)
	{
		byte[] wordMatches = new byte[ALL_WORDS.Length];
		int[] countsByRegex = new int[regexData.Length];
		for (int ri = 0; ri < regexData.Length; ri++) {
			Regex regex = regexData[ri].realRegex;
			for (int wi = 0; wi < ALL_WORDS.Length; wi++) {
				string word = ALL_WORDS[wi];
				if (regex.IsMatch(word)) {
					wordMatches[wi] |= (byte)(1 << ri);
					countsByRegex[ri]++;
				}
			}
		}

		return new WordRegexMatches {
			wordMatches = wordMatches,
			countsByRegex = countsByRegex,
		};
	}

	private static List<string> SampleMatchingWords(WordRegexMatches wordRegexMatches)
	{
		byte[] wordMatches = wordRegexMatches.wordMatches;
		int[] countsByRegex = wordRegexMatches.countsByRegex;
		string[] selectedWords = new string[countsByRegex.Length];

		// Examine regexes in order of increasing number of matching words
		List<int> remainingRegexIndices = Enumerable.Range(0, countsByRegex.Length)
			.OrderBy(regexIndex => countsByRegex[regexIndex]).ToList();
		byte satisfiedRegexFlags = 0;
		while (remainingRegexIndices.Count > 0) {
			int? matchedRegexIndex = null;
			List<int> wordIndexChoices = new List<int>();

			foreach (int ri in remainingRegexIndices) {
				for (int wi = 0; wi < ALL_WORDS.Length; wi++) {
					byte wm = wordMatches[wi];
					byte regexFlag = (byte)(1 << ri);
					// If wm matches this regex, and any other matches in wm overlap only
					// with previously-matched regexes, then we found a new match
					bool matchesThisRegex = (wm & regexFlag) != 0;
					bool matchesOtherUnsatisfiedRegexes = (wm & ~regexFlag & ~satisfiedRegexFlags) != 0;
					if (matchesThisRegex && !matchesOtherUnsatisfiedRegexes) {
						matchedRegexIndex = ri;
						wordIndexChoices.Add(wi);
					}
				}
				// Only accept choices from a single regex
				if (matchedRegexIndex != null) {
					break;
				}
			}

			if (matchedRegexIndex == null) {
				// This should never happen!
				throw new System.Exception("No solution found for regexes!");
			} else {
				satisfiedRegexFlags |= (byte)(1 << matchedRegexIndex.Value);
				remainingRegexIndices.Remove(matchedRegexIndex.Value);
				selectedWords[matchedRegexIndex.Value] = ALL_WORDS[wordIndexChoices.PickRandom()];
			}
		}

		return selectedWords.ToList();
	}

	private static List<string> SampleDecoyWords(RegexData[] regexData)
	{
		List<string> candidateDecoys = new List<string>();
		foreach (string word in ALL_WORDS) {
			if (regexData.Where(rd => rd.realRegex.IsMatch(word)).Any())
				continue;
			if (regexData.Where(rd => rd.decoyRegex.IsMatch(word)).Any())
				candidateDecoys.Add(word);
		}
		Debug.Assert(candidateDecoys.Count >= regexData.Length);

		List<string> chosenDecoys = new List<string>();
		while (chosenDecoys.Count < regexData.Length) {
			string choice = candidateDecoys.PickRandom();
			candidateDecoys.Remove(choice);
			chosenDecoys.Add(choice);
		}
		return chosenDecoys;
	}

	private class RawRegexData
	{
		public string real;
		public string decoy;
	}

	private static readonly RawRegexData[] ALL_RAW_REGEX_DATA = new RawRegexData[] {
		new RawRegexData { real = "A.*", decoy = "[^A].*A.*" },
		new RawRegexData { real = "D.*", decoy = "[^D].*D.*" },
		new RawRegexData { real = "H.*", decoy = "[^H].*H.*" },
		new RawRegexData { real = "W.*", decoy = "[^W].*W.*" },
		new RawRegexData { real = "U.*", decoy = "[^U].*U.*" },
		new RawRegexData { real = ".*E", decoy = ".*E.*[^E]" },
		new RawRegexData { real = ".*H", decoy = ".*H.*[^H]" },
		new RawRegexData { real = ".*P", decoy = ".*P.*[^P]" },
		new RawRegexData { real = ".*Y", decoy = ".*Y.*[^Y]" },
		new RawRegexData { real = ".*O", decoy = ".*O.*[^O]" },
		new RawRegexData { real = ".*[AEIOU][AEIOU].*", decoy = ".*[AEIOU].*[AEIOU].*" },
		new RawRegexData { real = ".*[AEIOU].*[AEIOU].*", decoy = ".*[AEIOU].*" },
		new RawRegexData { real = ".*[AEIOU]", decoy = ".*[AEIOU].*[^AEIOU]" },
		new RawRegexData { real = ".*[AEIOU].[AEIOU].*", decoy = ".*[AEIOU][AEIOU].*" },
		new RawRegexData { real = ".*[^AEIOU][^AEIOU].*", decoy = ".*[^AEIOU].*[^AEIOU].*" },
		new RawRegexData { real = ".*[^AEIOU][^AEIOU].", decoy = ".*[^AEIOU][^AEIOU]" },
		new RawRegexData { real = "[^AEIOU][^AEIOU].*", decoy = "[^AEIOU][AEIOU].*" },
		new RawRegexData { real = ".*O.*E.*", decoy = ".*E.*O.*" },
		new RawRegexData { real = ".*I.*T.*", decoy = ".*T.*I.*" },
		new RawRegexData { real = ".*B.*T.*", decoy = ".*T.*B.*" },
		new RawRegexData { real = ".*F.*R.*", decoy = ".*R.*F.*" },
		new RawRegexData { real = ".*O.*H.*", decoy = ".*H.*O.*" },
		new RawRegexData { real = ".*F.*T.*", decoy = ".*T.*F.*" },
		new RawRegexData { real = ".*OW.*", decoy = ".*O.+W.*" },
		new RawRegexData { real = ".*ON.*", decoy = ".*O.+N.*" },
		new RawRegexData { real = ".*OR.*", decoy = ".*O.+R.*" },
		new RawRegexData { real = ".*OT.*", decoy = ".*O.+T.*" },
		new RawRegexData { real = ".*HT.*", decoy = ".*H.+T.*" },
		new RawRegexData { real = ".*TH.*", decoy = ".*T.+H.*" },
		new RawRegexData { real = ".*DI.*", decoy = ".*D.+I.*" },
		new RawRegexData { real = ".*(OW|WO).*", decoy = ".*(O.+W|W.+O).*" },
		new RawRegexData { real = ".*(NO|ON).*", decoy = ".*(N.+O|O.+N).*" },
		new RawRegexData { real = ".*(OT|TO).*", decoy = ".*(O.+T|T.+O).*" },
		new RawRegexData { real = ".*(HT|TH).*", decoy = ".*(H.+T|T.+H).*" },
		new RawRegexData { real = ".*(ER|RE).*", decoy = ".*(E.+R|R.+E).*" },
		new RawRegexData { real = ".*(FI|IF).*", decoy = ".*(F.+I|I.+F).*" },
		new RawRegexData { real = "..", decoy = ".(..)?" },
		new RawRegexData { real = "...", decoy = "..(..)?" },
		new RawRegexData { real = "....", decoy = "...(..)?" },
		new RawRegexData { real = ".....", decoy = "....(..)?" },
		new RawRegexData { real = "......", decoy = ".....(..)?" },
		new RawRegexData { real = "..?", decoy = "..." },
		new RawRegexData { real = "...?", decoy = "...." },
		new RawRegexData { real = "....?", decoy = "....." },
		new RawRegexData { real = ".....?", decoy = "......" },
		new RawRegexData { real = "(..)?.", decoy = "(..)?.." },
		new RawRegexData { real = ".*O[^T].*", decoy = ".*O.*T.*" },
		new RawRegexData { real = ".*M[^O].*", decoy = ".*M.*O.*" },
		new RawRegexData { real = ".*U[^G].*", decoy = ".*U.*G.*" },
		new RawRegexData { real = ".*O[^W].*", decoy = ".*O.*W.*" },
		new RawRegexData { real = ".*E[^A].*", decoy = ".*E.*A.*" },
		new RawRegexData { real = ".*R[^E].*", decoy = ".*R.*E.*" },
		new RawRegexData { real = ".*F[^O].*", decoy = ".*F.*O.*" },
		new RawRegexData { real = ".*B[^O].*", decoy = ".*B.*O.*" },
		new RawRegexData { real = ".*I[^N].*", decoy = ".*I.*N.*" },
		new RawRegexData { real = ".*[^O]N.*", decoy = ".*O.*N.*" },
		new RawRegexData { real = ".*[^O]T.*", decoy = ".*O.*T.*" },
		new RawRegexData { real = ".*[^H]T.*", decoy = ".*H.*T.*" },
		new RawRegexData { real = ".*[^E]A.*", decoy = ".*E.*A.*" },
		new RawRegexData { real = ".*[^S]T.*", decoy = ".*S.*T.*" },
		new RawRegexData { real = ".*[OU][MN].*", decoy = ".*[OUMN][OUMN].*" },
		new RawRegexData { real = ".*[BT][OE].*", decoy = ".*[BTOE][BTOE].*" },
		new RawRegexData { real = ".*L[AEIOU].*", decoy = ".*L[^AEIOU]?.*" },
		new RawRegexData { real = ".[AEIOU].", decoy = ".(.[AEIOU]|[AEIOU].)." },
		new RawRegexData { real = ".*W(HA|HI|A).*", decoy = ".*W.*" },
		new RawRegexData { real = ".*OL?[DT].*", decoy = ".*[OLDT].*[OLDT].*" },
		new RawRegexData { real = "[^STRAIGHT]+", decoy = "[^STRAIGHT]*[STRAIGHT][^STRAIGHT]*" },
		new RawRegexData { real = "[^QUESTION]+", decoy = "[^QUESTION]*[QUESTION][^QUESTION]*" },
		new RawRegexData { real = "[^ASTERISK]+", decoy = "[^ASTERISK]*[ASTERISK][^ASTERISK]*" },
		new RawRegexData { real = "[^BRACKET]+", decoy = "[^BRACKET]*[BRACKET][^BRACKET]*" },
		new RawRegexData { real = "[^REFUTE]+", decoy = "[^REFUTE]*[REFUTE][^REFUTE]*" },
		new RawRegexData { real = "[^ELIMINATE]+", decoy = "[^ELIMINATE]*[ELIMINATE][^ELIMINATE]*" },
	};

	public static readonly int REGEX_TABLE_ROWS = 12;
	public static readonly int REGEX_TABLE_COLS = ALL_RAW_REGEX_DATA.Length / REGEX_TABLE_ROWS;

	// REGEX_TABLE_INDICES[r * TABLE_COLS + c] is the index into ALL_RAW_REGEX_DATA
	// of the regex at row r and column c of the regex table
	private static int[] REGEX_TABLE_INDICES = null;

	private static void InitializeRegexTableIndices()
	{
		REGEX_TABLE_INDICES = new int[ALL_RAW_REGEX_DATA.Length];
		for (int i = 0; i < ALL_RAW_REGEX_DATA.Length; i++)
			REGEX_TABLE_INDICES[(i * 17 + 3) % ALL_RAW_REGEX_DATA.Length] = i;
	}

	private static readonly string[] ALL_WORDS = new string[] {
		"BOOM",
		"BOMB",
		"OHNO",
		"BOB",
		"KNOB",
		"BUTTON",
		"HOLD",
		"TWO",
		"HOT",
		"HALT",
		"NOT",
		"NO",
		"FOR",
		"LOW",
		"HIGH",
		"BOOL",
		"LOOM",
		"ONE",
		"ON",
		"OFF",
		"LEFT",
		"RIGHT",
		"MIDDLE",
		"FOURTH",
		"FOUR",
		"TOP",
		"BOT",
		"BOTTOM",
		"BOLT",
		"LONE",
		"ABORT",
		"ALONE",
		"AFT",
		"FIFTH",
		"SIXTH",
		"SEVENTH",
		"EIGHTH",
		"THIRD",
		"BLAST",
		"NINTH",
		"TENTH",
		"BALLAST",
		"FIRST",
		"LAST",
		"TEN",
		"DIGIT",
		"LIGHT",
		"LED",
		"INDICATE",
		"INDICATOR",
		"DOT",
		"DOOM",
		"WORD",
		"ROW",
		"NUMBER",
		"TO",
		"TON",
		"WORK",
		"KNOW",
		"WOW",
		"WOAH",
		"WAIT",
		"WHAT",
		"AGAIN",
		"STOP",
		"REDO",
		"RED",
		"BLUE",
		"BLOW",
		"HUE",
		"UP",
		"FORTH",
		"FAR",
		"SO",
		"BAR",
		"STAR",
		"KEEP",
		"BEEP",
		"THREE",
		"FREE",
		"UNDO",
		"DO",
		"DID",
		"WON",
		"LOST",
		"DONE",
		"TONE",
		"NONE",
		"ARE",
		"WERE",
		"THERE",
		"YUP",
		"YEP",
		"YEAH",
		"YES",
		"MAYBE",
		"MIGHT",
		"MIGHTY",
		"MODULE",
		"MOST",
		"MORE",
		"TRUE",
		"FALSE",
		"DOWN",
		"GOT",
		"LOT",
		"THOUGHT",
		"NOUGHT",
		"BROKE",
		"BROKEN",
		"BELOW",
		"FUSE",
		"DISPLAY",
		"DISARM",
		"DEFUSE",
		"DIFFUSE",
		"ALARM",
		"WAY",
		"LOSE",
		"AWAY",
		"SAY",
		"USE",
		"USED",
		"WHERE",
		"WHY",
		"WHICH",
		"EAST",
		"WEST",
		"NORTH",
		"SOUTH",
		"SIDE",
		"UPSIDE",
		"DOWNSIDE",
		"UNDER",
		"ALREADY",
		"READY",
		"TWENTY",
		"THIRTY",
		"FORTY",
		"FIFTY",
		"FIFTIETH",
		"HOLY",
		"AUDIO",
		"ANOTHER",
		"ABOUT",
		"AFFIRM",
		"CONFIRM",
		"REAFFIRM",
		"AFTER",
		"WHISKEY",
		"WHILE",
		"WHICHEVER",
		"WHATEVER",
		"WHATNOT",
		"WAITING",
		"WARN",
		"OFT",
		"OFTEN",
		"FAULT",
		"FORT",
		"FIFTEEN",
		"FIFTEENTH",
		"ALIGHT",
		"ALRIGHT",
		"OUGHT",
		"OUTRIGHT",
		"OVERSIGHT",
		"RIGHTY",
		"RIGHTLY",
		"ABSENT",
		"ABRUPT",
		"ABUT",
		"AMBIGUITY",
		"ARBITRARY",
		"BACKTRACK",
		"BACK",
		"SOLUTION",
		"MISDID",
		"MISDIRECT",
		"AUDIBLE",
		"AUDITORY",
		"CONDITION",
		"CONTRADICT",
		"DECIDING",
		"DIAGRAM",
		"SADISTIC",
		"SADISM",
		"UNKNOWN",
		"UNSEEN",
		"ALL",
		"HARD",
		"HALFWAY",
		"HACK",
		"HARDLY",
		"HAZARD",
		"SHOW",
		"SHOWED",
		"HOW",
		"HOWEVER",
		"UH",
		"UHHH",
		"UH-HUH",
		"UNABLE",
		"UNWORTHY",
		"UNDERGO",
		"UNTO",
		"AM",
		"ME",
		"IF",
		"IFFY",
		"YA",
		"BE",
		"BY",
		"OF",
		"FRO",
		"CRYPTO",
		"HERETO",
		"GOTO",
		"MAP",
		"MISHAP",
		"MISSTEP",
		"MIXUP",
		"WARMUP",
		"ALLOT",
		"BOTH",
		"FIGURE",
		"A",
		"I",
		"TUBE",
		"PETABYTE",
		"STABLE",
		"TABLE",
		"TERRIBLE",
		"BRIEF",
		"CAREFUL",
		"CLARIFY",
		"INTERFERE",
		"PERFECT",
		"REFER",
		"REFLEX",
		"CROSSWAY",
		"CROSSWISE",
		"FORWARD",
		"NORTHWEST",
		"OTHERWISE",
		"SOMEWHAT",
		"SOMEWHERE",
		"ADMIT",
		"ADVICE",
		"ADVISE",
		"ATTACH",
		"BOTCH",
		"CLUTCH",
		"BEAUTIFUL",
		"TWELFTH",
		"THEREFORE",
		"AFRAID",
		"CONFUSION",
		"FAIL",
		"FAILED",
		"INFER",
		"INFO",
		"RELIEF",
		"DECIDE",
		"DECIPHER",
		"TEMPO",
		"TENSOR",
		"TRIGON",
		"PITFALL",
		"TRUTHFUL",
		"THEREOF",
		"TRANSFORM",
		"FOCUS",
	};

}
