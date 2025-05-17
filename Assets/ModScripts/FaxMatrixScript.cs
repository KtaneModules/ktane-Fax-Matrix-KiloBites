using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using static UnityEngine.Random;
using static UnityEngine.Debug;

public class FaxMatrixScript : MonoBehaviour {

	public KMBombInfo Bomb;
	public KMAudio Audio;
	public KMBombModule Module;

	public KMSelectable[] matrixButtons, modeButtons;

	public TextMesh[] rowTexts, colTexts;
	public TextMesh mainDisplayText;
	public GameObject tpStuff, stage2;
	public AudioClip[] phoneClips;

	public Material[] toggleables;
	public MeshRenderer mainBackground, secondButtonRender;

	static int moduleIdCounter = 1;
	static int faxMatrixIdCounter = 1;
	int moduleId;
	int faxMatrixId;
	private bool moduleSolved;

	private bool isActivated;
	private bool moduleSelected;

	private int phase = 0;
	private List<int> inputtedNumbers = new List<int>();

	private bool[] inputtedGrid = new bool[100], flagged = new bool[100], solution;
	private bool flagging = false;
	private Coroutine holdRoutine, transition;

	private NonogramPuzzle puzzle;

	private List<string> horizClues, vertClues;
	private List<List<string>> encodingLogs;

	private static readonly KeyCode[] keys =
	{
		KeyCode.Keypad0,
		KeyCode.Keypad1,
		KeyCode.Keypad2,
		KeyCode.Keypad3,
		KeyCode.Keypad4,
		KeyCode.Keypad5,
		KeyCode.Keypad6,
		KeyCode.Keypad7,
		KeyCode.Keypad8,
		KeyCode.Keypad9,
		KeyCode.Alpha0,
		KeyCode.Alpha1,
		KeyCode.Alpha2,
		KeyCode.Alpha3,
		KeyCode.Alpha4,
		KeyCode.Alpha5,
		KeyCode.Alpha6,
		KeyCode.Alpha7,
		KeyCode.Alpha8,
		KeyCode.Alpha9
	};

	void Awake()
    {

		moduleId = moduleIdCounter++;
		faxMatrixId = faxMatrixIdCounter++;

		foreach (KMSelectable button in matrixButtons)
			button.OnInteract += delegate () { MatrixButtonPress(button); return false; };

		foreach (KMSelectable modeButton in modeButtons)
		{
			modeButton.OnInteract += delegate () { ModeButtonPress(modeButton); return false; };
			modeButton.OnInteractEnded += delegate () { ModeButtonRelease(modeButton); };
		}

		Module.OnActivate += delegate () { tpStuff.SetActive(TwitchPlaysActive); StartCoroutine(Startup()); };
		Module.GetComponent<KMSelectable>().OnFocus += delegate { moduleSelected = true; };
		Module.GetComponent<KMSelectable>().OnDefocus += delegate { moduleSelected = false; };

    }

	
	void Start()
    {
		puzzle = new NonogramPuzzle(Bomb.GetSerialNumber());

		puzzle.Generate(out horizClues, out vertClues, out solution, out encodingLogs);

		Log($"[Fax Matrix #{moduleId}] The number generated is: {puzzle.GeneratedNumbers.Join("")}");

		for (int i = 0; i < encodingLogs.Count; i++)
			foreach (var log in encodingLogs[i])
				Log($"[Fax Matrix #{moduleId}] {log}");

		Log($"[Fax Matrix #{moduleId}] Horizontal Clues: {horizClues.Select(x => $"[{x}]").Join()}");
		Log($"[Fax Matrix #{moduleId}] Vertical Clues: {vertClues.Select(x => $"[{x}]").Join()}");
    }

	void OnDestroy() => faxMatrixIdCounter = 1;

	IEnumerator Startup()
	{
		if (faxMatrixId == 1)
			Audio.PlaySoundAtTransform("Start", transform);

		Coroutine crazyFunny;

		yield return new WaitForSeconds(1.5f);

		crazyFunny = StartCoroutine(GoCrazyAsShit());

		yield return new WaitForSeconds(4.5f);

		StartCoroutine(ShowClues());

		yield return new WaitForSeconds(1);

		StopCoroutine(crazyFunny);

		for (int i = 1; i >= 0; i--)
		{
			for (int j = 0; j < 100; j++)
				matrixButtons[j].GetComponent<MeshRenderer>().material = toggleables[i];

			yield return new WaitForSeconds(0.1f);
		}

		yield return new WaitForSeconds(1);

		isActivated = true;
	}

	IEnumerator GoCrazyAsShit()
	{
		while (true)
		{
			var pickIxes = Enumerable.Range(0, 100).ToList().Shuffle().Take(25).ToArray();

			for (int i = 0; i < 100; i++)
				matrixButtons[i].GetComponent<MeshRenderer>().material = toggleables[pickIxes.Contains(i) ? 1 : 0];

			yield return new WaitForSeconds(0.08f);
		}
	}

	IEnumerator ShowClues()
	{
		for (int i = 0; i < 10; i++)
		{
			rowTexts[i].text = horizClues[i];
			colTexts[i].text = vertClues[i].Replace(' ', '\n');
			yield return new WaitForSeconds(0.1f);
		}
	}

	void ModeButtonPress(KMSelectable button)
	{
		button.AddInteractionPunch(0.4f);
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);

		if (moduleSolved || !isActivated || transition != null || inputtedNumbers.Count == 7)
			return;

		if (Array.IndexOf(modeButtons, button) == 1)
			holdRoutine = StartCoroutine(HoldX());
	}

	void ModeButtonRelease(KMSelectable button)
	{
		if (moduleSolved || !isActivated || transition != null || inputtedNumbers.Count == 7)
			return;

		switch (Array.IndexOf(modeButtons, button))
		{
			case 0:
				if (phase == 0)
				{
					if (inputtedGrid.SequenceEqual(solution))
					{
						Log($"[Fax Matrix #{moduleId}] The submitted grid matches the puzzle desired. Progressing to phase 2...");
						transition = StartCoroutine(Stage2Transition());
						button.GetComponentInChildren<TextMesh>().text = "Reset";
						secondButtonRender.material = toggleables[1];
						flagging = false;
						flagged = Enumerable.Repeat(false, 100).ToArray();
						UpdateMatrixGrid();
						modeButtons[1].GetComponent<MeshRenderer>().material = toggleables[1];
						modeButtons[1].GetComponentInChildren<TextMesh>().text = "Clear";
						phase++;
					}
					else
					{
						Log($"[Fax Matrix #{moduleId}] The submitted grid doesn't match the puzzle desired ({Enumerable.Range(0, 100).Where(x => inputtedGrid[x] ^ solution[x]).Select(x => $"{"ABCDEFGHIJ"[x % 10]}{(x / 10) + 1}").Join(", ")} is/are wrong). Strike!");
						Module.HandleStrike();
						StartCoroutine(PhaseOneStrike());
					}
				}
				else
				{
					inputtedGrid = solution;
					UpdateMatrixGrid();
				}
				break;
			case 1:
				if (holdRoutine == null && phase == 0)
				{
					inputtedGrid = Enumerable.Repeat(false, 100).ToArray();
					flagged = Enumerable.Repeat(false, 100).ToArray();
					UpdateMatrixGrid();
					break;
				}

				StopCoroutine(holdRoutine);

				if (phase != 0)
				{
					inputtedGrid = Enumerable.Repeat(false, 100).ToArray();
					UpdateMatrixGrid();
					break;
				}

				flagging = !flagging;

				button.GetComponent<MeshRenderer>().material = toggleables[flagging ? 0 : 1];
				break;
		}
	}

	IEnumerator Stage2Transition()
	{
		var oldColor = mainBackground.material.color;

		modeButtons[1].GetComponent<MeshRenderer>().material = toggleables[1];

		var elapsed = 0f;
		var duration = 0.5f;

		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			mainBackground.material.color = Color.Lerp(oldColor, Color.white, elapsed);

			for (int i = 0; i < 10; i++)
			{
				rowTexts[i].color = Color.Lerp(Color.white, Color.clear, elapsed);
				colTexts[i].color = Color.Lerp(Color.white, Color.clear, elapsed);
			}

			yield return null;
		}

		stage2.SetActive(true);

		var allTexts = rowTexts.Concat(colTexts).ToList();

		allTexts.ForEach(x => x.text = string.Empty);

		transition = null;
	}

	void MatrixButtonPress(KMSelectable button)
	{
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);

		if (moduleSolved || !isActivated || transition != null)
			return;

		var ix = Array.IndexOf(matrixButtons, button);

		if (flagging)
		{
			if (!inputtedGrid[ix])
				flagged[ix] = !flagged[ix];
		}
		else
		{
			if (!flagged[ix])
			{
                flagged[ix] = false;
                inputtedGrid[ix] = !inputtedGrid[ix];
            }
		}

		UpdateMatrixGrid();
	}

	IEnumerator PhaseOneStrike()
	{
		var topText = colTexts.ToList();

		topText.ForEach(x => x.gameObject.SetActive(false));

		mainDisplayText.text = "CHECK AGAIN";
		mainDisplayText.color = Color.red;

		yield return new WaitForSeconds(0.5f);

		mainDisplayText.text = string.Empty;
		mainDisplayText.color = Color.white;

		topText.ForEach(x => x.gameObject.SetActive(true));
	}

	void UpdateMatrixGrid()
	{
		for (int i = 0; i < 100; i++)
			matrixButtons[i].GetComponent<MeshRenderer>().material = toggleables[!inputtedGrid[i] && flagged[i] ? 2 : inputtedGrid[i] && !flagged[i] ? 1 : 0];
	}

	IEnumerator HoldX()
	{
		yield return new WaitForSeconds(1);
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
		holdRoutine = null;
	}

	IEnumerator Submission()
	{
		Audio.PlaySoundAtTransform(phoneClips[0].name, transform);
		yield return new WaitForSeconds(phoneClips[0].length);

		if (inputtedNumbers.SequenceEqual(puzzle.GeneratedNumbers) && inputtedGrid.SequenceEqual(puzzle.GetPuzzleClusters()))
		{
			Log($"[Fax Matrix #{moduleId}] The number inputted matches the number generated and the submitted grid matches the data matrix generated. Solved!");
			moduleSolved = true;
			Module.HandlePass();
			StartCoroutine(SolveClear());
			mainDisplayText.text = "FAX COMPLETE";
			mainDisplayText.color = Color.green;
			Audio.PlaySoundAtTransform(phoneClips[3].name, transform);
			yield return new WaitForSeconds(phoneClips[3].length);
			Audio.PlaySoundAtTransform(phoneClips.Last().name, transform);
		}
		else
		{
			var rnd = Range(0, 100);
			
			Audio.PlaySoundAtTransform(phoneClips[rnd == 0 ? 2 : 1].name, transform);
			yield return new WaitForSeconds(phoneClips[rnd == 0 ? 2 : 1].length);
			Audio.PlaySoundAtTransform(phoneClips.Last().name, transform);
			yield return new WaitForSeconds(phoneClips.Last().length);

			if (!inputtedNumbers.SequenceEqual(puzzle.GeneratedNumbers) && inputtedGrid.SequenceEqual(puzzle.GetPuzzleClusters()))
				Log($"[Fax Matrix #{moduleId}] The submitted grid matches the data matrix generated. The number expected is {puzzle.GeneratedNumbers.Join("")}, but inputted {inputtedNumbers.Join("")}. Strike!");
			else if (inputtedNumbers.SequenceEqual(puzzle.GeneratedNumbers) && !inputtedGrid.SequenceEqual(puzzle.GetPuzzleClusters()))
				Log($"[Fax Matrix #{moduleId}] The number inputted matches the number generated. The submitted grid doesn't match the data matrix generated ({Enumerable.Range(0, 100).Where(x => inputtedGrid[x] ^ puzzle.GetPuzzleClusters()[x]).Select(x => $"{"ABCDEFGHIJ"[x % 10]}{(x / 10) + 1}").Join(", ")} is/are wrong). Strike!");
			else
				Log($"[Fax Matrix #{moduleId}] The number expected is {puzzle.GeneratedNumbers.Join("")}, but inputted {inputtedNumbers.Join("")}. The submitted grid doesn't match the data matrix generated ({Enumerable.Range(0, 100).Where(x => inputtedGrid[x] ^ puzzle.GetPuzzleClusters()[x]).Select(x => $"{"ABCDEFGHIJ"[x % 10]}{(x / 10) + 1}").Join(", ")} is/are wrong). Strike!");

			Module.HandleStrike();
            mainDisplayText.text = "INCOMPLETE";
			mainDisplayText.color = Color.red;
			yield return new WaitForSeconds(0.5f);
			mainDisplayText.text = string.Empty;
			mainDisplayText.color = Color.white;
            inputtedNumbers.Clear();
			inputtedGrid = solution;
			UpdateMatrixGrid();
        }
	}

	IEnumerator SolveClear()
	{
		for (int i = 0; i < 100; i++)
		{
			matrixButtons[i].GetComponent<MeshRenderer>().material = toggleables.First();
			yield return new WaitForSeconds(0.05f);
		}
	}

	void Update()
	{
		if (moduleSolved || !isActivated || phase != 1 || inputtedNumbers.Count == 7 || !moduleSelected)
			return;

		if (Input.GetKeyDown(KeyCode.Backspace))
		{
			if (inputtedNumbers.Count > 0)
				inputtedNumbers.RemoveAt(inputtedNumbers.Count - 1);

			mainDisplayText.text = inputtedNumbers.Join("");

			return;
		}

		for (int i = 0; i < keys.Length; i++)
			if (Input.GetKeyDown(keys[i]))
			{
				Audio.PlaySoundAtTransform($"S_DTMF_0{i % 10}", transform);
				inputtedNumbers.Add(i % 10);
				mainDisplayText.text = inputtedNumbers.Join("");
			}

		if (inputtedNumbers.Count == 7)
			StartCoroutine(Submission());
	}


    // Twitch Plays

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} fill/flag A/B/C/D/E/F/G/H/I/J 1/2/3/4/5/6/7/8/9/10 fills/unfills or flags (if in phase 1) the squares in those coordinates. || !{0} reset resets the grid back to its previous state (depending on the phase). || !{0} clear clears the entire grid if on phase 2. || !{0} check checks the nonogram puzzle inputted if on phase 1. || !{0} dial 0/1/2/3/4/5/6/7/8/9 dials the 7-digit number desired if on phase 2.";
	private readonly bool TwitchPlaysActive;
#pragma warning restore 414

	IEnumerator ProcessTwitchCommand(string command)
    {
		string[] split = command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

		yield return null;

		if (!isActivated || transition != null || inputtedNumbers.Count == 7)
		{
			yield return "sendtochaterror You cannot interact with the module at this time!";
			yield break;
		}

		if (new[] { "FLAG", "FILL" }.Any(x => x.ContainsIgnoreCase(split[0])))
		{
			if (phase != 0 && "FLAG".ContainsIgnoreCase(split[0]))
			{
				yield return "sendtochaterror You're already past phase 1!";
				yield break;
			}

			if (split.Skip(1).Any(x => x.Length > 3))
			{
				yield return "sendtochaterror One or more coordinates aren't formatted correctly. Make sure you follow the Letter-Number coordinate format!";
				yield break;
			}

			var columns = "ABCDEFGHIJ";
			var rows = Enumerable.Range(0, 10).Select(x => (x + 1).ToString()).ToArray();

			if (split.Skip(1).Any(x => !columns.Contains(x[0]) || Array.IndexOf(rows, x.Skip(1).Join("")) == -1))
			{
				yield return $"sendtochaterror {split.Skip(1).Where(x => !columns.Contains(x[0]) || Array.IndexOf(rows, x.Skip(1).Join("")) == -1).Join(", ")} is/are invalid!";
				yield break;
			}

			var coordinates = split.Skip(1).Select(x => Array.IndexOf(rows, x.Skip(1).Join("")) * 10 + columns.IndexOf(x[0])).ToArray();

			if (("FLAG".ContainsIgnoreCase(split[0]) && coordinates.Any(x => inputtedGrid[x])) || ("FILL".ContainsIgnoreCase(split[0]) && coordinates.Any(x => flagged[x])))
			{
				yield return $"sendtochaterror You cannot override the following squares: {coordinates.Where(x => "FLAG".ContainsIgnoreCase(split[0]) ? inputtedGrid[x] : flagged[x]).Select(x => $"{columns[x % 10]}{(x / 10) + 1}").Join(", ")}. Unflag/unfill this in order to perform that instruction.";
				yield break;
			}

			if (phase == 0)
                if (("FLAG".ContainsIgnoreCase(split[0]) && !flagging) || ("FILL".ContainsIgnoreCase(split[0]) && flagging))
                {
                    modeButtons[1].OnInteract();
                    yield return null;
                    modeButtons[1].OnInteractEnded();
                }

			foreach (var coordinate in coordinates)
			{
				matrixButtons[coordinate].OnInteract();
				yield return new WaitForSeconds(0.1f);
			}

			yield break;
		}

		if ("RESET".ContainsIgnoreCase(split[0]))
		{
			if (split.Length > 1)
			{
				yield return "sendtochaterror Please try again.";
				yield break;
			}

			if (phase == 0)
			{
				modeButtons[1].OnInteract();
				yield return new WaitForSeconds(1);
				modeButtons[1].OnInteractEnded();
				yield return new WaitForSeconds(1);
			}
			else
			{
				modeButtons[0].OnInteract();
				yield return null;
				modeButtons[0].OnInteractEnded();
				yield return new WaitForSeconds(0.1f);
			}
			yield break;
		}

		if ("CLEAR".ContainsIgnoreCase(split[0]))
		{
			if (split.Length > 1)
			{
				yield return "sendtochaterror Please try again.";
				yield break;
			}

			if (phase != 1)
			{
				yield return "sendtochaterror You are not at phase 2 yet!";
				yield break;
			}

			modeButtons[1].OnInteract();
			yield return null;
			modeButtons[1].OnInteractEnded();
			yield return new WaitForSeconds(0.1f);
			yield break;
		}

		if ("CHECK".ContainsIgnoreCase(split[0]))
		{
			if (split.Length > 1)
			{
				yield return "sendtochaterror Please try again.";
				yield break;
			}

			if (phase != 0)
			{
				yield return "sendtochaterror You're already past phase 1!";
				yield break;
			}

			modeButtons[0].OnInteract();
			yield return null;
			modeButtons[0].OnInteractEnded();
			yield return new WaitForSeconds(0.1f);
			yield break;
		}

		if ("DIAL".ContainsIgnoreCase(split[0]))
		{
			if (split.Length > 2)
			{
				yield return "sendtochaterror Please only input dial and then the 7-digit number!";
				yield break;
			}

			if (split[1].Length > 7)
			{
				yield return "sendtochaterror You cannot input more than 7 digits!";
				yield break;
			}

			if (!split[1].Any(char.IsDigit))
			{
				yield return $"sendtochaterror {split[1].Where(x => !char.IsDigit(x)).Join(", ")} is/are invalid!";
				yield break;
			}

			var numbers = split[1].Select(x => int.Parse(x.ToString())).ToArray();

			while (numbers.Join("").StartsWith(inputtedNumbers.Join("")))
			{
				inputtedNumbers.RemoveAt(inputtedNumbers.Count - 1);
				mainDisplayText.text = inputtedNumbers.Join("");
				yield return new WaitForSeconds(0.25f);
			}

			for (int i = inputtedNumbers.Count - 1; i < numbers.Length; i++)
			{
                inputtedNumbers.Add(numbers[i]);
                mainDisplayText.text = inputtedNumbers.Join("");
				Audio.PlaySoundAtTransform($"S_DTMF_0{numbers[i]}", transform);
                yield return new WaitForSeconds(0.25f);
            }
			yield return new WaitForSeconds(0.5f);
			yield return "solve";
			yield return "strike";
		}
    }

	IEnumerator TwitchHandleForcedSolve()
	{
		while (!isActivated || inputtedNumbers.Count == 7)
		{
			if (moduleSolved)
				yield break;

			yield return true;
		}

		if (phase == 0)
		{
			if (flagging)
			{
				modeButtons[1].OnInteract();
				yield return null;
				modeButtons[1].OnInteractEnded();
				yield return new WaitForSeconds(0.1f);
			}

			if (inputtedGrid.Count(x => x) > 0)
			{
				modeButtons[1].OnInteract();
				yield return new WaitForSeconds(1);
				modeButtons[1].OnHighlightEnded();
				yield return new WaitForSeconds(0.1f);
			}

			for (int i = 0; i < 100; i++)
				if (inputtedGrid[i] ^ solution[i])
				{
					matrixButtons[i].OnInteract();
					yield return new WaitForSeconds(0.1f);
				}

			modeButtons[0].OnInteract();
			yield return null;
			modeButtons[0].OnInteractEnded();
			yield return new WaitForSeconds(0.1f);
		}

		while (transition != null)
			yield return true;

		var clusters = puzzle.GetPuzzleClusters();

		for (int i = 0; i < 100; i++)
			if (inputtedGrid[i] ^ clusters[i])
			{
				matrixButtons[i].OnInteract();
				yield return new WaitForSeconds(0.1f);
			}

		while (!puzzle.GeneratedNumbers.Join("").StartsWith(inputtedNumbers.Join("")))
		{
			inputtedNumbers.RemoveAt(inputtedNumbers.Count - 1);
			mainDisplayText.text = inputtedNumbers.Join("");
			yield return new WaitForSeconds(0.25f);
		}

		for (int i = inputtedNumbers.Count; i < puzzle.GeneratedNumbers.Length; i++)
		{
			inputtedNumbers.Add(puzzle.GeneratedNumbers[i]);
			mainDisplayText.text = inputtedNumbers.Join("");

			if (faxMatrixId == 1)
                Audio.PlaySoundAtTransform($"S_DTMF_0{puzzle.GeneratedNumbers[i]}", transform);

            yield return new WaitForSeconds(0.25f);
		}

		StartCoroutine(Submission());

		while (!moduleSolved)
			yield return true;
	}

}





