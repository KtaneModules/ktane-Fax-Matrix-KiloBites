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
	public GameObject tpStuff, stage2;

	public Material[] toggleables;
	public MeshRenderer mainBackground;

	static int moduleIdCounter = 1;
	int moduleId;
	private bool moduleSolved;

	private bool isActivated;
	private bool moduleSelected;

	private int stage = 0;
	private List<int> inputtedNumbers = new List<int>();

	private bool[] inputtedGrid = new bool[100], marked = new bool[100], solution;
	private bool marking = false;
	private Coroutine holdRoutine, transition;
	private KMAudio.KMAudioRef dialing;

	private Sprite dataMatrix; // Can only be used for Souvenir, but who knows

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

		foreach (KMSelectable button in matrixButtons)
			button.OnInteract += delegate () { MatrixButtonPress(button); return false; };

		foreach (KMSelectable modeButton in modeButtons)
		{
			modeButton.OnInteract += delegate () { ModeButtonPress(modeButton); return false; };
			modeButton.OnInteractEnded += delegate () { ModeButtonRelease(modeButton); };
		}

		Module.OnActivate += Activate;
		Module.GetComponent<KMSelectable>().OnFocus += delegate { moduleSelected = true; };
		Module.GetComponent<KMSelectable>().OnDefocus += delegate { moduleSelected = false; };

    }

	
	void Start()
    {
		puzzle = new NonogramPuzzle(Bomb.GetSerialNumber());

		puzzle.Generate(out horizClues, out vertClues, out encodingLogs, out solution);

		dataMatrix = puzzle.DataMatrix;

		foreach (var encoding in encodingLogs)
			foreach (var log in encoding)
				Log($"[Fax Matrix #{moduleId}] {log}");

		Log($"[Fax Matrix #{moduleId}] Horizontal Clues: {horizClues.Select(x => $"[{x}]").Join()}");
		Log($"[Fax Matrix #{moduleId}] Vertical Clues: {vertClues.Select(x => $"[{x}]").Join()}");
    }

	void OnDestroy() => dialing?.StopSound();

	void Activate()
	{
		isActivated = true;
		tpStuff.SetActive(TwitchPlaysActive);
		SetHints();
	}

	void SetHints()
	{
		for (int i = 0; i < 10; i++)
		{
			rowTexts[i].text = horizClues[i];
			colTexts[i].text = vertClues[i].Replace(' ', '\n');
		}
	}

	void ModeButtonPress(KMSelectable button)
	{
		button.AddInteractionPunch(0.4f);
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);

		if (moduleSolved || !isActivated)
			return;

		if (Array.IndexOf(modeButtons, button) == 1)
			holdRoutine = StartCoroutine(HoldX());
	}

	void ModeButtonRelease(KMSelectable button)
	{
		if (moduleSolved || !isActivated)
			return;

		switch (Array.IndexOf(modeButtons, button))
		{
			case 0:
				if (stage == 0)
				{
					if (inputtedGrid.SequenceEqual(solution))
					{
						Log($"[Fax Matrix #{moduleId}] The submitted grid matches the puzzle desired. Progressing to phase 2...");
						transition = StartCoroutine(Stage2Transition());
						button.GetComponent<TextMesh>().text = "Reset";
						marking = false;

					}
					else
					{
						Log($"[Fax Matrix #{moduleId}] The submitted grid doesn't match the puzzle desired ({Enumerable.Range(0, 100).Where(x => inputtedGrid[x] ^ solution[x]).Select(x => $"{"ABCDEFGHIJ"[x % 10]}{(x / 10) + 1}").Join(", ")} is/are wrong). Strike!");
						Module.HandleStrike();
					}
				}
				else
				{
					inputtedGrid = solution;
					UpdateMatrixGrid();
				}
				break;
			case 1:
				if (holdRoutine == null)
				{
					inputtedGrid = Enumerable.Repeat(false, 100).ToArray();
					marked = Enumerable.Repeat(false, 100).ToArray();
					UpdateMatrixGrid();
					break;
				}

				StopCoroutine(holdRoutine);

				if (stage != 0)
					return;

				marking = !marking;

				button.GetComponent<MeshRenderer>().material = toggleables[marking ? 0 : 1];
				break;
		}
	}

	IEnumerator Stage2Transition()
	{
		var oldColor = mainBackground.material.color;

		modeButtons[1].GetComponent<MeshRenderer>().material = toggleables[1];

		var elapsed = 0f;
		var duration = 0.25f;

		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			mainBackground.material.color = Color.Lerp(oldColor, Color.white, elapsed);
			yield return null;
		}

		stage2.SetActive(true);
		transition = null;
	}

	void MatrixButtonPress(KMSelectable button)
	{
		button.AddInteractionPunch(0.4f);
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);

		if (moduleSolved || !isActivated)
			return;

		var ix = Array.IndexOf(matrixButtons, button);

		if (marking)
		{
			marked[ix] = !marked[ix];
			inputtedGrid[ix] = false;
		}
		else
		{
			if (marked[ix])
				return;

			inputtedGrid[ix] = !inputtedGrid[ix];
		}

		UpdateMatrixGrid();
	}

	void UpdateMatrixGrid()
	{
		for (int i = 0; i < 100; i++)
			matrixButtons[i].GetComponent<MeshRenderer>().material = toggleables[!inputtedGrid[i] && marked[i] ? 2 : inputtedGrid[i] && !marked[i] ? 1 : 0];
	}

	IEnumerator HoldX()
	{
		yield return new WaitForSeconds(1);
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
		holdRoutine = null;
	}

	void Update()
	{
		if (moduleSolved || !isActivated || stage != 1 || inputtedNumbers.Count == 7)
			return;

		if (Input.GetKeyDown(KeyCode.X))
		{
			inputtedNumbers.Clear();
			return;
		}

		for (int i = 0; i < keys.Length; i++)
			if (Input.GetKeyDown(keys[i]))
			{
				Audio.PlaySoundAtTransform($"S_DTMF_0{i % 10}", transform);
				inputtedNumbers.Add(i % 10);
			}
	}


    // Twitch Plays

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} something";
	private readonly bool TwitchPlaysActive;
#pragma warning restore 414

	IEnumerator ProcessTwitchCommand(string command)
    {
		string[] split = command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
		yield return null;
    }

	IEnumerator TwitchHandleForcedSolve()
    {
		yield return null;
    }


}





