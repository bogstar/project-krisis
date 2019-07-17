using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ChatManager : MonoBehaviour
{
	public GameObject chatMessagePrefab;
	public Transform chatMessagesHolder;

	public InputField chatInput;
	public Button sendButton;
	public ScrollRect scrollRect;

	public int maxChatMessages;
	public Queue<ChatMessage> messages = new Queue<ChatMessage>();


	private void Start()
	{
		sendButton.onClick.AddListener(() => { SendMessage(); });
		EventTrigger eventTrigger = chatInput.gameObject.AddComponent<EventTrigger>();

		AddTrigger(eventTrigger, EventTriggerType.Select, OnEnterChatBox);
		AddTrigger(eventTrigger, EventTriggerType.Deselect, OnLeaveChatBox);
	}

	void AddTrigger(EventTrigger eventTrigger, EventTriggerType triggerType, System.Action cb)
	{
		// Create a nee TriggerEvent and add a listener
		EventTrigger.TriggerEvent trigger = new EventTrigger.TriggerEvent();
		trigger.AddListener((eventData) => { cb(); }); // you can capture and pass the event data to the listener

		// Create and initialise EventTrigger.Entry using the created TriggerEvent
		EventTrigger.Entry entry = new EventTrigger.Entry() { callback = trigger, eventID = triggerType };

		// Add the EventTrigger.Entry to delegates list on the EventTrigger
		eventTrigger.triggers.Add(entry);
	}

	public void OnEnterChatBox()
	{
		InputManager.Instance.EnterChatMode(SendMessage);
	}

	public void OnLeaveChatBox()
	{
		InputManager.Instance.ExitChatMode();
	}

	public void DisplaySystemMessage(string messageString, Color color)
	{
		if (messageString == "")
		{
			return;
		}

		ChatMessage message = Instantiate(chatMessagePrefab, chatMessagesHolder).GetComponent<ChatMessage>();
		messages.Enqueue(message);

		System.DateTime now = System.DateTime.UtcNow;

		string actualMessage = "<b><color=#" + ColorUtility.ToHtmlStringRGB(color);
		actualMessage += ">[";
		actualMessage += now.Hour;
		actualMessage += ":";
		actualMessage += now.Minute.ToString("00");
		actualMessage += "] ";
		actualMessage += messageString;
		actualMessage += "</color></b>";

		message.SetMessage(actualMessage);

		if (messages.Count > maxChatMessages)
		{
			for (int i = 0; i < messages.Count - maxChatMessages; i++)
			{
				ChatMessage dequeuedMessage = messages.Dequeue();
				Destroy(dequeuedMessage.gameObject);
			}
		}

		message.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

		StartCoroutine(UnconstrainIt(message));
	}

	public void DisplayChatMessage(PlayerConnection senderConnection, string messageString)
	{
		if (messageString == "")
		{
			return;
		}

		string sender = "Unknown Player";

		if (senderConnection != null)
		{
			sender = senderConnection.name;
		}

		ChatMessage message = Instantiate(chatMessagePrefab, chatMessagesHolder).GetComponent<ChatMessage>();
		messages.Enqueue(message);

		System.DateTime now = System.DateTime.UtcNow;

		string actualMessage = "<b>[";
		actualMessage += now.Hour;
		actualMessage += ":";
		actualMessage += now.Minute.ToString("00");
		actualMessage += "] " + sender + ":</b> ";
		actualMessage += messageString;

		message.SetMessage(actualMessage);

		if (messages.Count > maxChatMessages)
		{
			for (int i = 0; i < messages.Count - maxChatMessages; i++)
			{
				ChatMessage dequeuedMessage = messages.Dequeue();
				Destroy(dequeuedMessage.gameObject);
			}
		}

		message.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

		StartCoroutine(UnconstrainIt(message));
	}

	void SendMessage()
	{
		DisplayChatMessage(MatchManager.localPlayerConnection, chatInput.text);

		MatchManager.localPlayerConnection.SendChatMessage(chatInput.text);

		chatInput.text = "";

		StartCoroutine(focusItAgain());
	}

	IEnumerator UnconstrainIt(ChatMessage message)
	{
		yield return new WaitForEndOfFrame();

		message.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

		yield return new WaitForEndOfFrame();
		scrollRect.verticalNormalizedPosition = 0;

		yield return new WaitForEndOfFrame();
		scrollRect.verticalNormalizedPosition = 0;
	}

	IEnumerator focusItAgain()
	{
		yield return new WaitForSeconds(1f);

		EventSystem.current.SetSelectedGameObject(chatInput.gameObject);
	}
}