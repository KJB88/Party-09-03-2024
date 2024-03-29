﻿using GDLib.Comms;
using GDLib.State;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PlayerState_InFlight : State, ISubscriber
{
    float thrustPower;
    float thrustStep;
    bool thrusting = false;
    float stoppingThreshold = 0.0f;

    int maxPower;

    int currentLayer;

    SpriteRenderer emotionDisplay;
    EmotionLibrary emotionLib;
    Transform thisTransform;
    VirtualInput input;
    MessageBroker localMsgBroker;
    GameObject thisGameObject;

    Vector3 lockedInput = Vector2.zero;
    Vector3 lockedDest;

    Dictionary<string, object> blackboard;

    public PlayerState_InFlight(FSM fsm) : base(fsm) { }

    public override void OnStateEntry(Dictionary<string, object> blackboard)
    {
        Debug.Log("Entering InFlight state.");
        this.blackboard = blackboard;

        if (ServiceLocator.RequestService("emotionLibrary", out IService service))
            emotionLib = (EmotionLibrary)service;

        object obj;
        if (blackboard.TryGetValue("thrustPower", out obj))
            thrustPower = (float)obj;

        if (blackboard.TryGetValue("thrustStep", out obj))
            thrustStep = (float)obj;

        if (blackboard.TryGetValue("maxPower", out obj))
            maxPower = (int)obj;

        if (blackboard.TryGetValue("localMsgBroker", out obj))
            localMsgBroker = (MessageBroker)obj;

        if (blackboard.TryGetValue("thisTransform", out obj))
            thisTransform = (Transform)obj;

        if (blackboard.TryGetValue("emotionDisplay", out obj))
            emotionDisplay = (SpriteRenderer)obj;

        if (blackboard.TryGetValue("virtualInput", out obj))
            input = (VirtualInput)obj;

        if (blackboard.TryGetValue("thisGameObject", out obj))
            thisGameObject = (GameObject)obj;


        localMsgBroker.RegisterSubscriber(MessageLibrary.CollisionEvent, this);

        emotionDisplay.enabled = true;
        emotionDisplay.sprite = emotionLib.Preparing;

        lockedInput = input.inputAxisVector;
        Vector3 dir = (thisTransform.position - thisTransform.position + (lockedInput.normalized * thrustPower)).normalized;
        lockedDest = thisTransform.position + (dir * thrustPower);

        thisGameObject.layer = LayerMask.NameToLayer("Attacking");
        thrusting = true;
    }

    public override void OnStateExit(Dictionary<string, object> blackboard)
    {
        emotionDisplay.enabled = false;
        localMsgBroker.RemoveSubscriber(MessageLibrary.CollisionEvent, this);
    }

    public override void UpdateState(Dictionary<string, object> blackboard)
    {
        if (!thrusting)
            return;

        Vector3 pos = thisTransform.position;
        thisTransform.position = Vector3.MoveTowards(pos, lockedDest, thrustStep);

        float dist = Vector3.Distance(pos, lockedDest);

        if (dist <= stoppingThreshold)
            fsm.SetState(new PlayerState_Dizzy(fsm), blackboard);
    }

    public bool Receive(Message msg)
    {
        if (msg.MessageType == MessageLibrary.CollisionEvent)
        {
            MSG_Collision2D c2d = (MSG_Collision2D)msg;

            IHittable hittable = c2d.collider.GetComponent<IHittable>();
            hittable.OnHit(maxPower);

            fsm.SetState(new PlayerState_Dizzy(fsm), blackboard);
            return true;
        }

        return false;
    }
}