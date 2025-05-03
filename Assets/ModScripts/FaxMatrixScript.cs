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

	public TextMesh[] rowTexts, colTexts;

	public SpriteRenderer render;

	static int moduleIdCounter = 1;
	int moduleId;
	private bool moduleSolved;

	private Sprite dataMatrix;

	private NonogramPuzzle puzzle;

	private List<string> horizClues, vertClues;
	private List<List<string>> encodingLogs;

	void Awake()
    {

		moduleId = moduleIdCounter++;

		/*
		foreach (KMSelectable button in Buttons)
			button.OnInteract() += delegate () { ButtonPress(button); return false; };
		*/

		//Button.OnInteract += delegate () { ButtonPress(); return false; };

    }

	
	void Start()
    {
		puzzle = new NonogramPuzzle(Bomb.GetSerialNumber());

		puzzle.Generate(out horizClues, out vertClues, out encodingLogs);

		render.sprite = puzzle.DataMatrix;

		foreach (var encoding in encodingLogs)
			foreach (var log in encoding)
				Log($"[Fax Matrix #{moduleId}] {log}");

		Log($"[Fax Matrix #{moduleId}] Horizontal Clues: {horizClues.Join(", ")}");
		Log($"[Fax Matrix] #{moduleId} Vertical Clues: {vertClues.Join(", ")}");
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
	
	
	void Update()
    {

    }

	// Twitch Plays


#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} something";
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





