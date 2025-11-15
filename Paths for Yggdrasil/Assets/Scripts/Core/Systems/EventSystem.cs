using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yggdrasil.Core
{
    public class EventSystem : Singleton<EventSystem>
    {
        private Dictionary<string, Action<object>> eventDictionary = new Dictionary<string, Action<object>>();

        public void Subscribe(string eventName, Action<object> listener)
        {
            if (eventDictionary.ContainsKey(eventName))
            {
                eventDictionary[eventName] += listener;
            }
            else
            {
                eventDictionary.Add(eventName, listener);
            }
        }

        public void Unsubscribe(string eventName, Action<object> listener)
        {
            if (eventDictionary.ContainsKey(eventName))
            {
                eventDictionary[eventName] -= listener;
            }
        }

        public void TriggerEvent(string eventName, object data = null)
        {
            if (eventDictionary.ContainsKey(eventName))
            {
                eventDictionary[eventName]?.Invoke(data);
            }
        }
    }

    // Event Names Constants
    public static class GameEvents
    {
        // Yggdrasil Events
        public const string YGGDRASIL_HEALTH_CHANGED = "YggdrasilHealthChanged";
        public const string YGGDRASIL_DIED = "YggdrasilDied";
        public const string YGGDRASIL_HEALED = "YggdrasilHealed";

        // Puzzle Events
        public const string PUZZLE_STARTED = "PuzzleStarted";
        public const string PUZZLE_COMPLETED = "PuzzleCompleted";
        public const string PUZZLE_FAILED = "PuzzleFailed";

        // Branch Events
        public const string BRANCH_PICKED_UP = "BranchPickedUp";
        public const string BRANCH_DROPPED = "BranchDropped";
        public const string BRANCH_PLACED_ON_ALTAR = "BranchPlacedOnAltar";

        // Radio Events
        public const string RADIO_TALK_START = "RadioTalkStart";
        public const string RADIO_TALK_END = "RadioTalkEnd";
        public const string RADIO_TURN_CHANGED = "RadioTurnChanged";

        // Network Events
        public const string PLAYER_CONNECTED = "PlayerConnected";
        public const string PLAYER_DISCONNECTED = "PlayerDisconnected";
        public const string ROOM_READY = "RoomReady";
    }
}