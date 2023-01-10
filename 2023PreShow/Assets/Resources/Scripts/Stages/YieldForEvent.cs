using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Proto.BasicExtensionUtils;
using Proto.Interfaces;
using UnityEngine;
using Random = UnityEngine.Random;

public class YieldForEvent : IState
{
	public string StateName { get; set; }
	public IState YieldState { get; set; }
	public bool IsYield { get; set; } = false;
	public bool IsStarted { get; set; } = false;

	public DialogueScript CurrentDialogue;
	private Dictionary<string, Dictionary<string, object>> _textData;

	private int _currentLine = 0;
	private float _innerTimer;

	public int ChoiceCache = -1;

	public YieldForEvent(DialogueScript dialogue)
	{
		RegisterDialogue(dialogue);
	}

	public void RegisterDialogue(DialogueScript dialogue)
	{
		CurrentDialogue = dialogue;
	}

	public void OnStartState()
	{
		if (IsStarted) return;

		IsStarted = true;

		_currentLine = 0;
		_innerTimer = 0f;
		
		_textData = CSVReader.Read("DialogueKr");
        
		if (CurrentDialogue.fadeOnStart)
			FadeContainer.Instance.FadeTo(1f, 0f, Color.black);
		SpeechContainer.Instance.Show();
		ImageContainer.Instance.HideImage();
		
		// StringBuilder sb = new StringBuilder();
        
		// foreach (var content in data)
		// {
		// 	sb.Append(content.Key);
		// 	foreach (var kv in content.Value)
		// 	{
		// 		sb.Append("\n  ");
		// 		sb.Append(kv.Key);
		// 		sb.Append(" : ");
		// 		sb.Append(kv.Value);
		// 	}
		// 	sb.Append("\n-----------------------\n");
		// }
		//
		// Debug.Log(sb.ToString());
	}

	public void OnState()
	{
		var dt = Time.deltaTime;
		_innerTimer += dt;
		
		//TODO: Implement for events
		if (CurrentDialogue is null)
			return;

		if (_currentLine >= CurrentDialogue.Count)
		{
			Game.Instance.SetState(null);
			return;
		}
		
		var data = CurrentDialogue.DialogueEvents[_currentLine];
		
		Debug.Log($"CurrentStep : {_currentLine}");

		switch (data.Type)
		{
			case DialogueEventType.None:
				if (data.Wait && _innerTimer < data.Duration)
				{
					return;
				}
				else
				{
					_currentLine++;
					_innerTimer = 0f;
				}
				break;
			case DialogueEventType.Speech:
				string rawText = 
					Encoding.Default.GetString(
						Encoding.Default.GetBytes(
					_textData.ContainsKey(data.Text) 
						? _textData[data.Text]["Content"].ToString() : data.Text));
				string rawSpeaker = 
					Encoding.Default.GetString(
					Encoding.Default.GetBytes(
						(data.Speaker.Length == 0) ?
				                    (_textData.ContainsKey(data.Text) 
					                    ? _textData[data.Text]["Speaker"].ToString() 
					                    : data.Speaker) : data.Speaker));
				if (data.Wait && _innerTimer <= data.Text.Length * 0.08f)
				{
					string outText = Encoding.Default.GetString(
						Encoding.Default.GetBytes(rawText.Substring(0, 
						Mathf.Min(rawText.Length, (int)(_innerTimer / 0.08f)))));
					SpeechContainer.Instance.SetText(rawSpeaker, outText);
					
					if (GlobalInputController.Instance.ConfirmPressed)
					{
						_innerTimer = data.Text.Length * 0.08f;
						SpeechContainer.Instance.SetText(rawSpeaker, rawText);
					}
					return;
				}
				else if (GlobalInputController.Instance.ConfirmPressed)
				{
					_currentLine++;
					_innerTimer = 0f;
				}
				else
				{
					SpeechContainer.Instance.SetText(rawSpeaker, rawText);
				}
				break;
			case DialogueEventType.Image:
				if (data.Wait && _innerTimer < data.Duration)
				{
					return;
				}
				else
				{
					_currentLine++;
					_innerTimer = 0f;
				}
				break;
			case DialogueEventType.Shake:
				if (data.Wait && _innerTimer < data.Duration)
				{
					ImageContainer.Instance.Rect.anchoredPosition
						= new Vector2(
							Random.Range(-data.Power, data.Power), 
							Random.Range(-data.Power, data.Power));
					return;
				}
				else
				{
					ImageContainer.Instance.Rect.anchoredPosition = Vector2.zero;
					_currentLine++;
					_innerTimer = 0f;
				}
				break;
			case DialogueEventType.Fade:
				if (data.Wait && _innerTimer < data.Duration)
				{
					if (_innerTimer < dt + Constants.Epsilon)
						FadeContainer.Instance.FadeTo(data.Destination, data.Duration, data.Color);
					return;
				}
				else
				{
					_currentLine++;
					_innerTimer = 0f;
				}
				break;
			case DialogueEventType.Choice:
				string rawChoices = "";

				for (int idx = 0; idx < data.Choices.Count; idx++)
				{
					var choice = data.Choices[idx];
					var marker = ChoiceCache >= 0 ? "▶" : "";
					var rawChoice = _textData.ContainsKey(choice) ? _textData[choice]["Content"].ToString() : choice;
					rawChoices += marker + "/t" + rawChoice + "\n";
				}

				rawChoices =
					Encoding.Default.GetString(
						Encoding.Default.GetBytes(rawChoices));
				
				if (_innerTimer < dt + Constants.Epsilon)
				{
					ChoiceCache = 0;
					return;
				}
				
				if (GlobalInputController.Instance.ConfirmPressed)
				{
					_currentLine++;
					_innerTimer = 0f;
				}
				
				if (GlobalInputController.Instance.CurrentFrameAxis.y > 0.5f)
				{
					ChoiceCache = Mathf.Max(0, ChoiceCache - 1);
				}
				else if (GlobalInputController.Instance.CurrentFrameAxis.y < -0.5f)
				{
					ChoiceCache = Mathf.Min(data.Choices.Count - 1, ChoiceCache + 1);
				}
				break;
		}
	}

	public void OnEndState()
	{
		ImageContainer.Instance.HideImage();
		SpeechContainer.Instance.Hide();
		FadeContainer.Instance.FadeTo(0f, 0f, Color.black);
	}
}
